using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BonniePDF.Rendering.Pdf;
using BonniePDF.Typography.TrueType;

namespace BonniePDF.Drawing;

/// <summary>
/// PDF implementation of ICanvas that adapts ICanvas calls to PDF Operators.
/// Handles the critical Y-axis coordinate transformation from Screen Coordinates (Top-Left origin) to PDF Coordinates
/// (Bottom-Left origin).
/// </summary>
internal class PdfCanvas : ICanvas
{
    private readonly Func<string, TtfFont?> _embeddedFontResolver;
    private readonly Func<string, string> _fontResolver;
    private readonly Func<ImageInfo, string> _imageResolver;
    private readonly Action<string, string> _glyphTracker;
    private readonly Func<float, string> _alphaGraphicsStateResolver;
    private readonly Func<string, string, IList<ResolvedTextRun>>? _textRunResolver;
    private readonly Action? _reportUntaggedImage;
    private readonly Action<ImageInfo, float, float>? _imagePlacementTracker;
    private readonly Func<ShadingRequest, ShadingResources?>? _shadingResolver;
    private readonly StringBuilder _stream;
    private readonly float _xOffset;
    private readonly float _yOffset;
    private int _stateDepth;
    private readonly List<(char type, float[] args)> _currentPath = new();
    private (float x, float y)? _currentPoint;
    // Local transform matrix stack for path-building coordinate transforms.
    // Points are stored in screen coords, transformed through _ctm, then Y-flipped at emit time.
    private float[] _ctm = { 1, 0, 0, 1, 0, 0 }; // identity [a b c d tx ty]
    private readonly Stack<float[]> _ctmStack = new();
    private TaggingContext? _tagging;
    private int _openMarkedContent;
    // Number of currently-open BDC sequences (sequences that allocated an MCID).
    private int _openTaggedCount;
    // Stack tracking whether each open marked-content sequence is a BDC (true) or BMC (false).
    private readonly Stack<bool> _markedContentTagged = new();
    // Per-BeginArtifact record of whether an /Artifact wrapper was actually emitted.
    private readonly System.Collections.Generic.Stack<bool> _artifactEmitStack = new();
    // Logical nesting depth of /Artifact regions (whether or not each emitted a BMC).
    private int _artifactDepth;

    /// <summary>
    /// Bands in the strip/disc approximation of a gradient that cannot be painted
    /// as a native shading.
    /// </summary>
    /// <remarks>
    /// Deliberately fixed rather than derived from the painted area: deriving it
    /// would make the byte output depend on layout arithmetic, and this path must
    /// stay comparable across implementations. 100 bands is a 1% colour step, at or
    /// below the visible banding threshold in 8-bit sRGB.
    /// </remarks>
    private const int GradientFallbackBands = 100;

    /// <summary>
    /// Control-point distance for approximating a quarter circle with one cubic
    /// Bezier: 4/3 * (sqrt(2) - 1).
    /// </summary>
    /// <remarks>
    /// Held as a double and written to full precision on purpose. A truncated
    /// literal diverges in the last bits between implementations, and that shows up
    /// as different operands for the same input.
    /// </remarks>
    private const double QuarterArc = 0.5522847498307933;

    /// <summary>Fully transparent black; the implicit backdrop beyond a radial gradient's rim.</summary>
    private static readonly Color Transparent = Color.FromScRgb(0, 0, 0, 0);

    /// <summary>
    /// Creates a new PdfCanvas for writing to the specified PDF content stream.
    /// </summary>
    /// <param name="stream">The StringBuilder to write PDF commands to</param>
    /// <param name="pageHeight">The height of the PDF page for Y-axis transformation</param>
    /// <param name="xOffset">The X offset from the left of the page (for respecting margins)</param>
    /// <param name="yOffset">The Y offset from the top of the page (for positioning within document layout)</param>
    /// <param name="fontResolver">Delegate to resolve font family names to PDF resource names (e.g. /F1)</param>
    /// <param name="imageResolver">Delegate to resolve images to PDF resource names (e.g. /I1)</param>
    /// <param name="embeddedFontResolver">Delegate to resolve embedded font file paths to TtfFont instances</param>
    /// <param name="glyphTracker">Delegate to track glyph usage for font subsetting (fontIdentifier, text)</param>
    public PdfCanvas(
        StringBuilder stream,
        float pageHeight,
        float xOffset = 0,
        float yOffset = 0,
        Func<string, string>? fontResolver = null,
        Func<ImageInfo, string>? imageResolver = null,
        Func<string, TtfFont?>? embeddedFontResolver = null,
        Action<string, string>? glyphTracker = null,
        Func<float, string>? alphaGraphicsStateResolver = null,
        Func<string, string, IList<ResolvedTextRun>>? textRunResolver = null,
        Action? reportUntaggedImage = null,
        Func<ShadingRequest, ShadingResources?>? shadingResolver = null,
        Action<ImageInfo, float, float>? imagePlacementTracker = null)
    {
        _stream = stream;
        Height = pageHeight;
        _xOffset = xOffset;
        _yOffset = yOffset;

        // Provide defaults for backward compatibility or simple usage
        _fontResolver = fontResolver ?? GetDefaultFontResourceName;
        _imageResolver = imageResolver ?? (_ => throw new InvalidOperationException("Image resolver not provided"));
        _embeddedFontResolver = embeddedFontResolver ?? (_ => null);
        _glyphTracker = glyphTracker ?? ((_, _) => { });
        _alphaGraphicsStateResolver = alphaGraphicsStateResolver ?? (alpha => $"/GS_ALPHA_{(int)Math.Round(alpha * 10000)}");
        _textRunResolver = textRunResolver;
        _reportUntaggedImage = reportUntaggedImage;
        _imagePlacementTracker = imagePlacementTracker;
        _shadingResolver = shadingResolver;
    }

    /// <summary>
    /// Gets the width of the canvas (not directly applicable to PDF streams, returns 0)
    /// </summary>
    public float Width => 0;

    /// <summary>
    /// Gets the height of the canvas (returns the page height)
    /// </summary>
    public float Height { get; }

    /// <inheritdoc />
    public int GraphicsStateDepth => _stateDepth;

    /// <summary>
    /// Pushes graphics state (PDF: q)
    /// </summary>
    public void SaveState()
    {
        PdfOperators.SaveState(_stream);
        _ctmStack.Push((float[])_ctm.Clone());
        _stateDepth++;
    }

    /// <summary>
    /// Pops graphics state (PDF: Q)
    /// </summary>
    public void RestoreState()
    {
        if (_stateDepth > 0)
        {
            PdfOperators.RestoreState(_stream);
            if (_ctmStack.Count > 0) _ctm = _ctmStack.Pop();
            _stateDepth--;
        }
    }

    /// <summary>
    /// Draws a filled rectangle.
    /// Converts Top-Left (y) to Bottom-Left (pdfY).
    /// </summary>
    public void FillRect(float x, float y, float width, float height, Color color)
    {
        // Apply X and Y offsets, convert Top-Left (y) to Bottom-Left (pdfY)
        float pdfX = x + _xOffset;
        float pdfY = Height - (y + _yOffset + height);

        DrawWithColorAlpha(color, () => PdfDrawing.FillRect(_stream, pdfX, pdfY, width, height, color));
    }

    /// <summary>
    /// Draws a stroked rectangle (border).
    /// Converts Top-Left (y) to Bottom-Left (pdfY).
    /// </summary>
    public void StrokeRect(float x, float y, float width, float height, Color color, float strokeWidth)
    {
        // Apply X and Y offsets, convert Top-Left (y) to Bottom-Left (pdfY)
        float pdfX = x + _xOffset;
        float pdfY = Height - (y + _yOffset + height);

        DrawWithColorAlpha(color, () => PdfDrawing.StrokeRect(_stream, pdfX, pdfY, width, height, color, strokeWidth));
    }

    /// <summary>
    /// Draws a filled rounded rectangle.
    /// Converts Top-Left (y) to Bottom-Left (pdfY).
    /// </summary>
    public void FillRoundedRect(float x, float y, float width, float height, float radius, Color color)
    {
        float pdfX = x + _xOffset;
        float pdfY = Height - (y + _yOffset + height);
        DrawWithColorAlpha(color, () => PdfDrawing.FillRoundedRect(_stream, pdfX, pdfY, width, height, radius, color));
    }

    /// <summary>
    /// Draws a stroked rounded rectangle (border).
    /// Converts Top-Left (y) to Bottom-Left (pdfY).
    /// </summary>
    public void StrokeRoundedRect(float x, float y, float width, float height, float radius, Color color,
        float strokeWidth)
    {
        float pdfX = x + _xOffset;
        float pdfY = Height - (y + _yOffset + height);
        DrawWithColorAlpha(color, () =>
            PdfDrawing.StrokeRoundedRect(_stream, pdfX, pdfY, width, height, radius, color, strokeWidth));
    }

    /// <summary>
    /// Begin a rounded-rectangle clip. Pushes the graphics state, then narrows the
    /// clip to the rounded rect so all drawing up to <see cref="EndClip"/> is masked.
    /// Converts Top-Left (y) to Bottom-Left (pdfY).
    /// </summary>
    public void BeginRoundedClip(float x, float y, float width, float height, float radius)
    {
        float pdfX = x + _xOffset;
        float pdfY = Height - (y + _yOffset + height);
        SaveState();
        PdfDrawing.ClipRoundedRect(_stream, pdfX, pdfY, width, height, radius);
    }

    /// <summary>Restores the graphics state pushed by <see cref="BeginRoundedClip"/>.</summary>
    public void EndClip() => RestoreState();

    /// <summary>
    /// Draws a line between two points.
    /// Converts both points from Top-Left to Bottom-Left coordinates.
    /// </summary>
    public void DrawLine(float x1, float y1, float x2, float y2, Color color, float lineWidth)
    {
        // Apply X and Y offsets, convert both points from Top-Left to Bottom-Left coordinates
        float pdfX1 = x1 + _xOffset;
        float pdfX2 = x2 + _xOffset;
        float pdfY1 = Height - (y1 + _yOffset);
        float pdfY2 = Height - (y2 + _yOffset);

        DrawWithColorAlpha(color, () =>
        {
            PdfOperators.SetStrokeColor(_stream, color);
            PdfOperators.SetLineWidth(_stream, lineWidth);
            PdfOperators.StrokeLine(_stream, pdfX1, pdfY1, pdfX2, pdfY2);
        });
    }

    /// <summary>
    /// Draws a filled circle.
    /// Converts the center point from Top-Left to Bottom-Left coordinates.
    /// </summary>
    public void FillCircle(float cx, float cy, float radius, Color color)
    {
        // Apply X and Y offsets, convert center from Top-Left to Bottom-Left coordinates
        float pdfCx = cx + _xOffset;
        float pdfCy = Height - (cy + _yOffset);

        DrawWithColorAlpha(color, () => PdfDrawing.FillCircle(_stream, pdfCx, pdfCy, radius, color));
    }

    public void FillEllipse(float cx, float cy, float rx, float ry, Color color)
    {
        float pdfCx = cx + _xOffset;
        float pdfCy = Height - (cy + _yOffset);
        DrawWithColorAlpha(color, () => PdfDrawing.FillEllipse(_stream, pdfCx, pdfCy, rx, ry, color));
    }

    /// <summary>
    /// Fills a closed path defined by a series of points.
    /// Converts all points from Top-Left to Bottom-Left coordinates.
    /// </summary>
    public void FillPath(IList<Point>? points, Color color)
    {
        if (points == null || points.Count < 3) return;

        // Convert all points from Top-Left to Bottom-Left coordinates
        List<Point> pdfPoints = new();
        foreach (Point p in points)
        {
            float pdfX = p.X + _xOffset;
            float pdfY = Height - (p.Y + _yOffset);
            pdfPoints.Add(new Point(pdfX, pdfY));
        }

        DrawWithColorAlpha(color, () => PdfDrawing.FillPath(_stream, pdfPoints, color));
    }

    /// <summary>
    /// Draws text at the specified location.
    /// Converts the Y position from Top-Left to Bottom-Left coordinates.
    /// PDF text y is the baseline, so we approximate: y (top) + size (height) roughly equals baseline.
    /// </summary>
    public void DrawText(float x, float y, string text, Color color, float fontSize, string fontFamily,
        TextAlignment align)
    {
        // PDF text y is the baseline.
        // Apply Y offset and approximation: y (top) + size (height) roughly equals baseline.
        float pdfY = Height - (y + _yOffset + fontSize);

        BeginColorAlpha(color);
        PdfOperators.BeginText(_stream);

        // Resolve runs first so any embedded/fallback fonts are registered before
        // glyph tracking and width measurement.
        IList<ResolvedTextRun> runs = ResolveRuns(text, fontFamily);
        PdfOperators.SetFillColor(_stream, color);
        BeginTextSpacing();

        // Calculate X offset based on alignment
        float xOffset = 0;
        if (align == TextAlignment.Center || align == TextAlignment.Right)
        {
            float width = MeasureRuns(runs, fontSize);
            xOffset = align == TextAlignment.Center ? -width / 2 : -width;
        }

        float pdfX = x + _xOffset + xOffset;
        PdfOperators.SetTextPosition(_stream, pdfX, pdfY);

        EmitShowText(runs, fontSize);

        EndTextSpacing();
        PdfOperators.EndText(_stream);
        EndColorAlpha(color);
    }

    /// <summary>Pending letter spacing in points, applied by the next DrawText.</summary>
    private float _letterSpacing;
    /// <summary>Pending word spacing in points, applied by the next DrawText.</summary>
    private float _wordSpacing;

    /// <inheritdoc />
    public void SetTextSpacing(float letterSpacing, float wordSpacing)
    {
        _letterSpacing = letterSpacing;
        _wordSpacing = wordSpacing;
    }

    /// <summary>
    /// Emits the <c>Tc</c>/<c>Tw</c> text-state operators for the pending
    /// letter/word spacing. <c>Tc</c> is universal; <c>Tw</c> only affects
    /// single-byte fonts, so word spacing for embedded (composite) fonts is
    /// injected into the <c>TJ</c> array by <see cref="EmitShowText"/>. No-op
    /// (byte-identical) when both are zero.
    /// </summary>
    private void BeginTextSpacing()
    {
        if (_letterSpacing != 0) PdfOperators.SetCharacterSpacing(_stream, _letterSpacing);
        if (_wordSpacing != 0) PdfOperators.SetWordSpacing(_stream, _wordSpacing);
    }

    /// <summary>Resets <c>Tc</c>/<c>Tw</c> to zero after a spaced text object.</summary>
    private void EndTextSpacing()
    {
        if (_letterSpacing != 0) PdfOperators.SetCharacterSpacing(_stream, 0);
        if (_wordSpacing != 0) PdfOperators.SetWordSpacing(_stream, 0);
    }

    /// <summary>
    /// Draws rotated text at the specified location.
    /// Uses transformation matrix to rotate text around the specified point.
    /// </summary>
    public void DrawTextRotated(float x, float y, string text, Color color, float fontSize, string fontFamily, float angleDegrees)
    {
        // PDF text y is the baseline.
        float pdfY = Height - (y + _yOffset + fontSize);
        float pdfX = x + _xOffset;

        // Save state and apply rotation
        PdfOperators.SaveState(_stream);
        PdfOperators.Translate(_stream, pdfX, pdfY);
        PdfOperators.Rotate(_stream, angleDegrees);
        ApplyColorAlpha(color);

        // Now draw text at origin (0,0) since we've already translated
        PdfOperators.BeginText(_stream);

        IList<ResolvedTextRun> runs = ResolveRuns(text, fontFamily);
        PdfOperators.SetFillColor(_stream, color);
        BeginTextSpacing();
        PdfOperators.SetTextPosition(_stream, 0, 0);

        EmitShowText(runs, fontSize);

        EndTextSpacing();
        PdfOperators.EndText(_stream);
        PdfOperators.RestoreState(_stream);
    }

    /// <summary>
    /// Draws justified text by distributing extra space across word gaps using the PDF Tw operator.
    /// </summary>
    public void DrawTextJustified(float x, float y, string text, Color color, float fontSize, string fontFamily,
        float availableWidth)
    {
        // Count spaces to distribute extra width across
        int spaceCount = 0;
        foreach (char c in text)
            if (c == ' ') spaceCount++;

        // If no spaces or single word, fall back to left-aligned text
        if (spaceCount == 0)
        {
            DrawText(x, y, text, color, fontSize, fontFamily, TextAlignment.Left);
            return;
        }

        // Resolve runs and measure per-run so spacing matches what is emitted.
        IList<ResolvedTextRun> runs = ResolveRuns(text, fontFamily);
        float extraSpace = availableWidth - MeasureRuns(runs, fontSize);
        float wordSpacing = extraSpace / spaceCount;

        // PDF text y is the baseline
        float pdfY = Height - (y + _yOffset + fontSize);
        float pdfX = x + _xOffset;

        BeginColorAlpha(color);
        PdfOperators.BeginText(_stream);

        PdfOperators.SetFillColor(_stream, color);
        PdfOperators.SetWordSpacing(_stream, wordSpacing);
        PdfOperators.SetTextPosition(_stream, pdfX, pdfY);

        EmitShowText(runs, fontSize);

        // Reset word spacing to 0 so it doesn't leak to subsequent text
        PdfOperators.SetWordSpacing(_stream, 0);
        PdfOperators.EndText(_stream);
        EndColorAlpha(color);
    }

    /// <summary>
    /// Draws an image at the specified location.
    /// </summary>
    public void DrawImage(ImageInfo image, float x, float y, float width, float height)
    {
        // Resolve the image resource name (e.g., /I1)
        string imageName = _imageResolver(image);

        // Tell the resource registry how large the image is actually painted, on the page, so it
        // can decide how many pixels the embedded copy needs.
        _imagePlacementTracker?.Invoke(image, width * MathF.Sqrt(_ctm[0] * _ctm[0] + _ctm[1] * _ctm[1]),
            height * MathF.Sqrt(_ctm[2] * _ctm[2] + _ctm[3] * _ctm[3]));

        // Calculate PDF coordinates (Bottom-Left origin)
        // PDF Y is bottom-left. x,y is top-left.
        float pdfX = x + _xOffset;
        // y + height is the bottom of the image in Screen coords.
        // _pageHeight - (bottom) is the PDF Y.
        float pdfY = Height - (y + _yOffset + height);

        DrawAsArtifactFallback(() =>
        {
            PdfOperators.SaveState(_stream);
            if (IsIdentityCtm())
            {
                PdfOperators.Translate(_stream, pdfX, pdfY);
            }
            else
            {
                // Under a transform, the image is placed in the canvas's own space: the
                // transform, then the box's bottom-left corner with y pointing up.
                PdfOperators.ConcatMatrix(_stream, _ctm[0], -_ctm[1], _ctm[2], -_ctm[3],
                    _ctm[4] + _xOffset, Height - _yOffset - _ctm[5]);
                PdfOperators.ConcatMatrix(_stream, 1, 0, 0, -1, x, y + height);
            }

            // Combine EXIF orientation with the display-size scale into a
            // single cm matrix. For orientation 1 (the common case) this is
            // [width 0 0 height 0 0], byte-identical to a plain Scale, so
            // unrotated images are unchanged; 2-8 rotate/flip the placement
            // without re-encoding the image stream.
            (float a, float b, float c, float d, float e, float f) =
                OrientationMatrix(image?.Orientation ?? 1, width, height);
            PdfOperators.ConcatMatrix(_stream, a, b, c, d, e, f);
            PdfOperators.PaintXObject(_stream, imageName);
            PdfOperators.RestoreState(_stream);
        }, _reportUntaggedImage);
    }

    /// <summary>
    /// Maps an EXIF orientation (1-8) to the PDF image placement matrix that
    /// draws the unit image square upright at <paramref name="width"/> x
    /// <paramref name="height"/>. Orientation 1 is the plain scale matrix.
    /// </summary>
    private static (float, float, float, float, float, float) OrientationMatrix(
        int orientation, float width, float height)
    {
        return orientation switch
        {
            2 => (-width, 0, 0, height, width, 0),
            3 => (-width, 0, 0, -height, width, height),
            4 => (width, 0, 0, -height, 0, height),
            5 => (0, -height, -width, 0, width, height),
            6 => (0, -height, width, 0, 0, height),
            7 => (0, height, width, 0, 0, 0),
            8 => (0, height, -width, 0, width, 0),
            _ => (width, 0, 0, height, 0, 0)
        };
    }

    /// <summary>
    /// Draws a Form XObject (external object reference) at the specified location.
    /// Uses translation only (no scaling).
    /// </summary>
    public void DrawFormXObject(string resourceName, float x, float y)
    {
        // Calculate PDF coordinates (Bottom-Left origin)
        // PDF Y is bottom-left. x,y is top-left.
        float pdfX = x + _xOffset;
        // Map Top-Left (y) to Bottom-Left (pdfY).
        // Note: For Form XObjects, we assume the content is drawn relative to its own origin (0,0).
        // By translating to (pdfX, pdfY), (0,0) of the form aligns with this point.
        float pdfY = Height - (y + _yOffset);

        DrawAsArtifactFallback(() =>
        {
            PdfOperators.SaveState(_stream);
            PdfOperators.Translate(_stream, pdfX, pdfY);
            PdfOperators.PaintXObject(_stream, resourceName);
            PdfOperators.RestoreState(_stream);
        }, _reportUntaggedImage);
    }

    /// <summary>
    /// Sets the tagging context that governs marked-content emission.
    /// When non-null, <see cref="BeginMarkedContent"/> /
    /// <see cref="EndMarkedContent"/> emit PDF marked-content operators
    /// and register MCIDs with the structure tree; when null, both are
    /// no-ops and the canvas emits untagged content.
    /// </summary>
    public void SetTaggingContext(TaggingContext tagging) => _tagging = tagging;

    /// <summary>Depth of MCID-bearing marked-content scopes currently open on this canvas.</summary>
    internal int OpenTaggedCount => _openTaggedCount;

    /// <summary>Depth of /Artifact regions currently open on this canvas.</summary>
    internal int ArtifactDepth => _artifactDepth;

    /// <summary>
    /// Adopts the open-scope depths of another canvas writing into the same
    /// content stream.
    /// </summary>
    /// <remarks>
    /// <see cref="RenderContext.GetCanvas"/> hands out a fresh canvas positioned
    /// at a child's origin, but it appends to the *same* stream as the canvas
    /// that is already rendering. Without this, the new canvas believes it is at
    /// top level: a chart or drawing rendered inside a `/Figure` BDC opened by
    /// its parent would wrap its own marks in `/Artifact BMC`, producing an
    /// artifact nested inside tagged content and failing PDF/UA-1 7.1 (tests 1
    /// and 2).
    ///
    /// Only the depth *counters* are copied, never the emit stacks: this canvas
    /// did not open those scopes and must never close them. The counters are
    /// exactly what the "do I need to wrap this?" decisions in
    /// <see cref="BeginArtifact"/> and <see cref="DrawAsArtifactFallback"/> read.
    /// </remarks>
    internal void InheritOpenScopes(int openTaggedCount, int artifactDepth)
    {
        _openTaggedCount = openTaggedCount;
        _artifactDepth = artifactDepth;
    }

    /// <inheritdoc />
    public void BeginMarkedContent(string role)
    {
        if (_tagging == null) return;
        bool tagged = false;
        if (_openTaggedCount == 0)
        {
            int mcid = _tagging.AllocateMcid();
            _tagging.Tree.AddMcid(mcid, _tagging.CurrentPageIndex);
            PdfOperators.BeginMarkedContentTagged(_stream, role, mcid);
            _openTaggedCount++;
            tagged = true;
        }
        else
        {
            PdfOperators.BeginMarkedContentBmc(_stream, role);
        }
        _markedContentTagged.Push(tagged);
        _tagging.PushOpenRole(role);
        _openMarkedContent++;
    }

    /// <inheritdoc />
    public void BeginContainerContent(string role)
    {
        if (_tagging == null) return;
        PdfOperators.BeginMarkedContentBmc(_stream, role);
        _markedContentTagged.Push(false);
        _tagging.PushOpenRole(role);
        _openMarkedContent++;
    }

    /// <inheritdoc />
    public void EndMarkedContent()
    {
        if (_tagging == null) return;
        if (_openMarkedContent <= 0) return; // tolerate unbalanced End() rather than emit a stray EMC
        PdfOperators.EndMarkedContent(_stream);
        if (_markedContentTagged.Count > 0 && _markedContentTagged.Pop())
            _openTaggedCount--;
        _tagging.PopOpenRole();
        _openMarkedContent--;
    }

    /// <inheritdoc />
    public void BeginArtifact()
    {
        // Emit an /Artifact wrapper only for tagged documents and only when not
        // already inside an MCID-bearing marked-content sequence. A container BMC
        // (for example /Div BMC) does not tag its paint operations as real content,
        // so decorative marks inside it still need an /Artifact wrapper. Content
        // inside a BDC is already real content and must not contain an artifact.
        // The emit decision is recorded so EndArtifact stays balanced.
        // Only emit a BMC at the outermost artifact level: a nested /Artifact is
        // redundant, and emitting one inside an open structure scope is invalid.
        bool emit = _tagging != null && _openTaggedCount == 0 && _artifactDepth == 0;
        if (emit) PdfOperators.BeginArtifact(_stream);
        _artifactEmitStack.Push(emit);
        _artifactDepth++;
    }

    /// <inheritdoc />
    public void EndArtifact()
    {
        if (_artifactDepth > 0) _artifactDepth--;
        bool emit = _artifactEmitStack.Count > 0 && _artifactEmitStack.Pop();
        if (emit) PdfOperators.EndMarkedContent(_stream);
    }

    /// <summary>
    /// True when drawing currently sits inside an /Artifact region. Structure
    /// tagging must be suppressed here: PDF/UA 7.1-2 forbids tagged (real)
    /// content inside an artifact, so role-bearing children of a decorative
    /// subtree (header/footer/slot/chart) stay artifact content rather than
    /// getting their own BDC.
    /// </summary>
    public bool IsInsideArtifact() => _artifactDepth > 0;

    /// <summary>
    /// Measures text size to allow for precise alignment calculations in charts.
    /// Uses FontMetrics to get accurate text width.
    /// </summary>
    public Size MeasureText(string text, float fontSize, string fontFamily)
    {
        // Measure per glyph-fallback run so the width matches what is actually
        // emitted (including any fallback-font glyphs and excluding dropped
        // default-ignorable code points). When the primary font covers the whole
        // string this collapses to a single run and is identical to before.
        float width = MeasureRuns(ResolveRuns(text, fontFamily), fontSize);
        return new Size(width, fontSize * 1.2f);
    }

    /// <summary>
    /// Resolves <paramref name="text"/> into per-font runs via the injected run
    /// resolver (glyph fallback). Without a resolver, returns a single legacy run
    /// using the primary font resolvers, preserving standalone-canvas behaviour.
    /// </summary>
    private IList<ResolvedTextRun> ResolveRuns(string text, string fontFamily)
    {
        if (_textRunResolver != null) return _textRunResolver(fontFamily, text);
        return new List<ResolvedTextRun>
        {
            new()
            {
                ResourceName = _fontResolver(fontFamily),
                EmbeddedFont = _embeddedFontResolver(fontFamily),
                FontIdentifier = fontFamily,
                Text = text
            }
        };
    }

    /// <summary>Sums the rendered width of <paramref name="runs"/> at <paramref name="fontSize"/>, each run in its own font.</summary>
    private float MeasureRuns(IList<ResolvedTextRun> runs, float fontSize)
    {
        float total = 0;
        foreach (ResolvedTextRun run in runs)
        {
            TtfFont? font = run.EmbeddedFont;
            if (font != null)
            {
                foreach (System.Text.Rune rune in run.Text.EnumerateRunes())
                {
                    if (rune.Value < 32) continue; // matches ToGlyphIdHexString
                    total += font.GetCharWidthInPoints(rune.Value, fontSize);
                }
            }
            else
            {
                total += FontMetrics.GetStringWidth(run.Text, run.FontIdentifier, fontSize);
            }
            total += SpacingWidth(run.Text);
        }
        return total;
    }

    /// <summary>
    /// Extra advance contributed by the pending letter/word spacing for a
    /// run's text: letter spacing after every rendered glyph plus word spacing
    /// after every space. Matches the PDF <c>Tc</c>/<c>Tw</c>/<c>TJ</c> output
    /// and the reserved layout width. Zero when no spacing is set.
    /// </summary>
    private float SpacingWidth(string text)
    {
        if (_letterSpacing == 0 && _wordSpacing == 0) return 0;
        int glyphs = 0;
        int spaces = 0;
        foreach (System.Text.Rune rune in text.EnumerateRunes())
        {
            if (rune.Value < 32) continue;
            glyphs++;
            if (rune.Value == ' ') spaces++;
        }
        return glyphs * _letterSpacing + spaces * _wordSpacing;
    }

    /// <summary>
    /// Emits the font-selection (<c>Tf</c>) and show-text operators for the
    /// resolved runs - kerned <c>TJ</c> for embedded fonts with kern pairs,
    /// plain <c>Tj</c> otherwise. The caller must have positioned the text matrix
    /// and set the fill colour first; each show-text operator advances the text
    /// point by its glyph widths (plus <c>TJ</c> adjustments), so consecutive
    /// runs flow on naturally.
    /// </summary>
    private void EmitShowText(IList<ResolvedTextRun> runs, float fontSize)
    {
        foreach (ResolvedTextRun run in runs)
        {
            if (run.Text.Length == 0) continue;
            _glyphTracker(run.FontIdentifier, run.Text);
            PdfOperators.SetFont(_stream, run.ResourceName, fontSize);
            if (run.EmbeddedFont != null)
            {
                // Word spacing (Tw) does not apply to composite fonts, so when
                // it is set, inject it into the TJ array (combined with any kerning).
                // Letter spacing is handled universally by Tc, above.
                if (_wordSpacing != 0 && run.Text.Contains(' '))
                {
                    PdfOperators.ShowTextPositioned(_stream,
                        PdfSyntax.ToSpacedGlyphArray(run.Text, run.EmbeddedFont, fontSize, _wordSpacing));
                    continue;
                }
                // Embedded fonts with kern pairs paint with `TJ` so the PDF
                // matches the kerned widths the layout engine reserved (and the
                // kerned SVG export). Unkerned strings keep the `Tj` path.
                string? kerned = run.EmbeddedFont.HasKerning
                    ? PdfSyntax.ToKernedGlyphArray(run.Text, run.EmbeddedFont)
                    : null;
                if (kerned != null)
                    PdfOperators.ShowTextPositioned(_stream, kerned);
                else
                    PdfOperators.ShowText(_stream, PdfSyntax.ToGlyphIdHexString(run.Text, run.EmbeddedFont));
            }
            else
            {
                PdfOperators.ShowText(_stream, $"({PdfSyntax.Escape(run.Text)})");
            }
        }
    }

    /// <summary>
    /// Maps a font family name to a PDF standard font resource name (Legacy fallback).
    /// </summary>
    private static string GetDefaultFontResourceName(string fontFamily)
    {
        // Map common font names to PDF standard fonts
        return fontFamily.ToLowerInvariant() switch
        {
            var s when s.Contains("arial") || s.Contains("helvetica") => "/Helvetica",
            var s when s.Contains("times") => "/Times-Roman",
            var s when s.Contains("courier") || s.Contains("mono") => "/Courier",
            _ => "/Helvetica"
        };
    }

    // --- 6. Path building ---

    /// <inheritdoc />
    public void BeginPath()
    {
        _currentPath.Clear();
        _currentPoint = null;
    }

    /// <inheritdoc />
    public void MoveTo(float x, float y)
    {
        _currentPath.Add(('M', new[] { x, y }));
        _currentPoint = (x, y);
    }

    /// <inheritdoc />
    public void LineTo(float x, float y)
    {
        _currentPath.Add(('L', new[] { x, y }));
        _currentPoint = (x, y);
    }

    /// <inheritdoc />
    public void CubicBezierTo(float cp1x, float cp1y, float cp2x, float cp2y, float x, float y)
    {
        _currentPath.Add(('C', new[] { cp1x, cp1y, cp2x, cp2y, x, y }));
        _currentPoint = (x, y);
    }

    /// <inheritdoc />
    public void QuadraticBezierTo(float cpx, float cpy, float x, float y)
    {
        // Convert quadratic to cubic: CP1 = P0 + 2/3*(CP-P0), CP2 = P1 + 2/3*(CP-P1)
        float x0 = _currentPoint?.x ?? 0;
        float y0 = _currentPoint?.y ?? 0;
        float cp1x = x0 + 2f / 3f * (cpx - x0);
        float cp1y = y0 + 2f / 3f * (cpy - y0);
        float cp2x = x + 2f / 3f * (cpx - x);
        float cp2y = y + 2f / 3f * (cpy - y);
        _currentPath.Add(('C', new[] { cp1x, cp1y, cp2x, cp2y, x, y }));
        _currentPoint = (x, y);
    }

    /// <inheritdoc />
    public void AddRect(float x, float y, float width, float height)
    {
        _currentPath.Add(('R', new[] { x, y, width, height }));
    }

    /// <inheritdoc />
    public void ClosePath()
    {
        _currentPath.Add(('H', Array.Empty<float>()));
    }

    /// <inheritdoc />
    public void AddEllipse(float cx, float cy, float rx, float ry)
    {
        (float startX, float startY, var segments) = EllipseGeometry.ToBeziers(cx, cy, rx, ry);
        MoveTo(startX, startY);
        foreach (var s in segments) CubicBezierTo(s.C1X, s.C1Y, s.C2X, s.C2Y, s.X, s.Y);
        ClosePath();
    }

    /// <inheritdoc />
    public void AddCircle(float cx, float cy, float radius) => AddEllipse(cx, cy, radius, radius);

    /// <inheritdoc />
    public void FillCurrentPath(Paint paint, FillRule fillRule = FillRule.NonZero)
    {
        if (_currentPath.Count == 0) return;

        DrawAsArtifactFallback(() =>
        {
            if (paint is SolidPaint solid)
            {
                BeginColorAlpha(solid.Color);
                PdfOperators.SaveState(_stream);
                PdfOperators.SetFillColor(_stream, solid.Color);
                EmitCurrentPath();
                if (fillRule == FillRule.EvenOdd)
                    PdfOperators.FillEvenOdd(_stream);
                else
                    PdfOperators.Fill(_stream);
                PdfOperators.RestoreState(_stream);
                EndColorAlpha(solid.Color);
            }
            else if (paint is LinearGradientPaint linear)
            {
                FillPathWithLinearGradient(linear, fillRule);
            }
            else if (paint is RadialGradientPaint radial)
            {
                FillPathWithRadialGradient(radial, fillRule);
            }
        });

        _currentPath.Clear();
        _currentPoint = null;
    }

    /// <inheritdoc />
    public void StrokeCurrentPath(StrokeStyle style)
    {
        if (_currentPath.Count == 0) return;

        DrawAsArtifactFallback(() =>
        {
            if (style.Paint is SolidPaint solid)
            {
                BeginColorAlpha(solid.Color);
                PdfOperators.SaveState(_stream);
                ApplyStrokeSettings(style);
                PdfOperators.SetStrokeColor(_stream, solid.Color);
                EmitCurrentPath();
                PdfOperators.Stroke(_stream);
                PdfOperators.RestoreState(_stream);
                EndColorAlpha(solid.Color);
            }
            else if (style.Paint is LinearGradientPaint or RadialGradientPaint)
            {
                StrokePathWithGradient(style.Paint, style);
            }
        });

        _currentPath.Clear();
        _currentPoint = null;
    }

    // --- 7. Transforms ---

    /// <inheritdoc />
    public void Translate(float dx, float dy)
    {
        // Concatenate translation into local CTM
        // [1 0 0 1 dx dy] * [a b c d tx ty]
        _ctm[4] += _ctm[0] * dx + _ctm[2] * dy;
        _ctm[5] += _ctm[1] * dx + _ctm[3] * dy;
    }

    /// <inheritdoc />
    public void Scale(float sx, float sy)
    {
        // Concatenate scaling into local CTM
        _ctm[0] *= sx;
        _ctm[1] *= sx;
        _ctm[2] *= sy;
        _ctm[3] *= sy;
    }

    /// <inheritdoc />
    public void Rotate(float angleDegrees)
    {
        float radians = angleDegrees * (float)(Math.PI / 180.0);
        float cos = (float)Math.Cos(radians);
        float sin = (float)Math.Sin(radians);
        float a = _ctm[0], b = _ctm[1], c = _ctm[2], d = _ctm[3];
        _ctm[0] = a * cos + c * sin;
        _ctm[1] = b * cos + d * sin;
        _ctm[2] = -a * sin + c * cos;
        _ctm[3] = -b * sin + d * cos;
    }

    internal void TransformElement(PaintTransform t) => PdfOperators.ConcatMatrix(_stream,
        t.A, -t.B, -t.C, t.D, t.C * Height + t.E, Height * (1 - t.D) - t.F);

    /// <inheritdoc />
    public void Transform(float a, float b, float c, float d, float tx, float ty)
    {
        float a0 = _ctm[0], b0 = _ctm[1], c0 = _ctm[2], d0 = _ctm[3], tx0 = _ctm[4], ty0 = _ctm[5];
        _ctm[0] = a0 * a + c0 * b;
        _ctm[1] = b0 * a + d0 * b;
        _ctm[2] = a0 * c + c0 * d;
        _ctm[3] = b0 * c + d0 * d;
        _ctm[4] = a0 * tx + c0 * ty + tx0;
        _ctm[5] = b0 * tx + d0 * ty + ty0;
    }

    // --- 8. Clipping & Alpha ---

    /// <inheritdoc />
    public void ClipToCurrentPath(FillRule fillRule = FillRule.NonZero)
    {
        if (_currentPath.Count == 0) return;

        EmitCurrentPath();
        if (fillRule == FillRule.EvenOdd)
            PdfOperators.ClipEvenOdd(_stream);
        else
            PdfOperators.Clip(_stream);

        _currentPath.Clear();
        _currentPoint = null;
    }

    /// <inheritdoc />
    public void SetGlobalAlpha(float alpha)
    {
        alpha = Math.Clamp(alpha, 0f, 1f);
        if (alpha < 1f)
            PdfOperators.SetGraphicsState(_stream, _alphaGraphicsStateResolver(alpha));
    }

    // --- Path emit and gradient helpers ---

    /// <summary>
    /// Transforms a screen-coordinate point through the local CTM, then applies
    /// the PDF Y-flip and offsets to produce final PDF coordinates.
    /// </summary>
    private (float x, float y) TransformPoint(float x, float y)
    {
        float tx = _ctm[0] * x + _ctm[2] * y + _ctm[4];
        float ty = _ctm[1] * x + _ctm[3] * y + _ctm[5];
        return (tx + _xOffset, Height - (ty + _yOffset));
    }

    /// <summary>
    /// <see cref="TransformPoint(float,float)"/> without narrowing, for geometry a
    /// caller generates in double.
    /// </summary>
    /// <remarks>
    /// Widening the matrix's floats to double is exact, so this loses nothing and
    /// keeps the generated point at full precision until the formatter rounds it.
    /// The gradient approximations use it because they synthesise hundreds of points
    /// from a Bezier constant and a band index, and a float round-trip on each one
    /// prints a different last digit than the TypeScript implementation for the same
    /// input.
    /// </remarks>
    private (double X, double Y) TransformPointPrecise(double x, double y)
    {
        double tx = _ctm[0] * x + _ctm[2] * y + _ctm[4];
        double ty = _ctm[1] * x + _ctm[3] * y + _ctm[5];
        return (tx + _xOffset, Height - (ty + _yOffset));
    }

    /// <summary>
    /// Emits the accumulated path commands to the PDF content stream,
    /// applying the local CTM transform and Y-flip to each point.
    /// </summary>
    private void EmitCurrentPath()
    {
        foreach (var (type, args) in _currentPath)
        {
            switch (type)
            {
                case 'M':
                {
                    var (px, py) = TransformPoint(args[0], args[1]);
                    PdfOperators.MoveTo(_stream, px, py);
                    break;
                }
                case 'L':
                {
                    var (px, py) = TransformPoint(args[0], args[1]);
                    PdfOperators.LineTo(_stream, px, py);
                    break;
                }
                case 'C':
                {
                    var (cp1x, cp1y) = TransformPoint(args[0], args[1]);
                    var (cp2x, cp2y) = TransformPoint(args[2], args[3]);
                    var (px, py) = TransformPoint(args[4], args[5]);
                    PdfOperators.CurveTo(_stream, cp1x, cp1y, cp2x, cp2y, px, py);
                    break;
                }
                case 'R':
                {
                    // Transform all four corners to handle rotation/skew
                    var (x1, y1) = TransformPoint(args[0], args[1]);
                    var (x2, y2) = TransformPoint(args[0] + args[2], args[1]);
                    var (x3, y3) = TransformPoint(args[0] + args[2], args[1] + args[3]);
                    var (x4, y4) = TransformPoint(args[0], args[1] + args[3]);

                    // If the CTM is axis-aligned (no rotation/skew), use re operator
                    if (IsAxisAligned())
                    {
                        float rx = Math.Min(x1, x3);
                        float ry = Math.Min(y1, y3);
                        float rw = Math.Abs(x3 - x1);
                        float rh = Math.Abs(y3 - y1);
                        PdfOperators.Rectangle(_stream, rx, ry, rw, rh);
                    }
                    else
                    {
                        // Emit as a general quadrilateral path
                        PdfOperators.MoveTo(_stream, x1, y1);
                        PdfOperators.LineTo(_stream, x2, y2);
                        PdfOperators.LineTo(_stream, x3, y3);
                        PdfOperators.LineTo(_stream, x4, y4);
                        PdfOperators.ClosePath(_stream);
                    }
                    break;
                }
                case 'H':
                    PdfOperators.ClosePath(_stream);
                    break;
            }
        }
    }

    /// <summary>True while no transform is in effect.</summary>
    private bool IsIdentityCtm() =>
        _ctm[0] == 1f && _ctm[1] == 0f && _ctm[2] == 0f && _ctm[3] == 1f && _ctm[4] == 0f && _ctm[5] == 0f;

    /// <summary>Returns true when the local CTM has no rotation or skew (b and c are zero).</summary>
    private bool IsAxisAligned()
        => Math.Abs(_ctm[1]) < 1e-6f && Math.Abs(_ctm[2]) < 1e-6f;

    /// <summary>Applies stroke settings (line width, cap, join, dash, miter) to the PDF state.</summary>
    /// <remarks>
    /// A stroke's width and dash lengths are in user space, like its points; the points
    /// are transformed at emit time, so these are scaled here. The scale is the square
    /// root of the transform's absolute determinant: exact for uniform scaling and
    /// rotation, and the geometric mean of the axis scales for a non-uniform transform.
    /// Under a transform that does not scale, they are written unchanged.
    /// </remarks>
    private void ApplyStrokeSettings(StrokeStyle style)
    {
        float k = MathF.Sqrt(MathF.Abs(_ctm[0] * _ctm[3] - _ctm[1] * _ctm[2]));
        PdfOperators.SetLineWidth(_stream, k == 1f ? style.Width : style.Width * k);
        PdfOperators.SetLineCap(_stream, MapLineCap(style.LineCap));
        PdfOperators.SetLineJoin(_stream, MapLineJoin(style.LineJoin));
        if (style.MiterLimit != 10f)
            PdfOperators.SetMiterLimit(_stream, style.MiterLimit);
        if (style.DashPattern is { Length: > 0 })
        {
            if (k == 1f)
            {
                PdfOperators.SetDashPattern(_stream, style.DashPattern, style.DashOffset);
            }
            else
            {
                float[] dashes = new float[style.DashPattern.Length];
                for (int i = 0; i < dashes.Length; i++) dashes[i] = style.DashPattern[i] * k;
                PdfOperators.SetDashPattern(_stream, dashes, style.DashOffset * k);
            }
        }
    }

    private static int MapLineCap(LineCap cap) => cap switch
    {
        LineCap.Butt => 0,
        LineCap.Round => 1,
        LineCap.Square => 2,
        _ => 0
    };

    private static int MapLineJoin(LineJoin join) => join switch
    {
        LineJoin.Miter => 0,
        LineJoin.Round => 1,
        LineJoin.Bevel => 2,
        _ => 0
    };

    /// <summary>
    /// Fills the current path with a linear gradient. Prefers a native PDF Type-2
    /// axial shading (see <see cref="EmitShading"/>); falls back to the
    /// perpendicular-strip approximation when shadings can't be used (a translucent
    /// gradient under PDF/A-1, which forbids transparency).
    /// </summary>
    private void FillPathWithLinearGradient(LinearGradientPaint gradient, FillRule fillRule)
    {
        if (gradient.End.X == gradient.Start.X && gradient.End.Y == gradient.Start.Y)
            return; // degenerate axis — nothing to paint

        // A transform that collapses paint space leaves no space to interpret the
        // shading's coordinates in, so the paint draws nothing at all.
        if (PaintSpaceBBox(gradient.Transform) is not { } bbox) return;

        List<ShadingStop> stops = NormalizeShadingStops(gradient.Stops);
        var request = new ShadingRequest
        {
            ShadingType = 2,
            Coords = new[] { gradient.Start.X, gradient.Start.Y, gradient.End.X, gradient.End.Y },
            Stops = stops,
            Extend = (true, true),
            Bbox = Array.ConvertAll(bbox, v => (float)v),
            HasAlpha = HasAlpha(gradient.Stops)
        };
        ShadingResources? resolved = _shadingResolver?.Invoke(request);
        if (resolved is { } r) EmitShading(r, fillRule, gradient.Transform);
        // The approximation samples the same normalized stops the shading would
        // have, so switching between the two paths changes smoothness and nothing
        // else, and unsorted input cannot make the two implementations disagree.
        else FillPathWithLinearGradientStrips(gradient, fillRule, stops, bbox);
    }

    /// <summary>
    /// Paints a resolved shading across the current path's clipped region. A
    /// <c>cm</c> maps the shading's canvas-space coordinates into PDF page space —
    /// PdfCanvas bakes its own transform into every point via <see cref="TransformPoint"/>
    /// and runs an identity stream CTM, so the shading needs that matrix re-applied
    /// here (it also aligns the soft-mask group, established under the same CTM).
    /// </summary>
    private void EmitShading(ShadingResources resolved, FillRule fillRule, PaintTransform? paintTransform = null)
    {
        PdfOperators.SaveState(_stream);

        EmitCurrentPath();
        if (fillRule == FillRule.EvenOdd)
            PdfOperators.ClipEvenOdd(_stream);
        else
            PdfOperators.Clip(_stream);

        PdfOperators.ConcatMatrix(_stream, _ctm[0], -_ctm[1], _ctm[2], -_ctm[3],
            _ctm[4] + _xOffset, Height - _yOffset - _ctm[5]);

        // The paint transform is concatenated after the canvas matrix, so the
        // shading's /Coords are read in the paint's own space. A second `cm`
        // rather than a pre-multiplied one keeps the canvas mapping
        // byte-identical for the overwhelmingly common paint with no transform.
        if (paintTransform is { } t && !t.IsIdentity)
            PdfOperators.ConcatMatrix(_stream, t.A, t.B, t.C, t.D, t.E, t.F);

        // The soft mask is set under the same CTM as the colour shading, so the
        // mask's group aligns with it.
        if (resolved.SoftMaskGs != null)
            PdfOperators.SetGraphicsState(_stream, resolved.SoftMaskGs);
        PdfOperators.PaintShading(_stream, resolved.ShadingName);

        PdfOperators.RestoreState(_stream);
    }

    /// <summary>
    /// A point in a paint's own space, mapped to PDF page space.
    /// </summary>
    /// <remarks>
    /// Used by the strip and disc fallbacks, which generate their geometry in
    /// paint space so that a transform shears or scales the generated shapes
    /// rather than only moving them. Computed without narrowing; see
    /// <see cref="TransformPointPrecise"/>.
    /// </remarks>
    private (double X, double Y) PaintToPdfPointPrecise(PaintTransform? paintTransform, double x, double y)
    {
        if (paintTransform is not { } t || t.IsIdentity) return TransformPointPrecise(x, y);
        double px = t.A * x + t.C * y + t.E;
        double py = t.B * x + t.D * y + t.F;
        return TransformPointPrecise(px, py);
    }

    /// <summary>
    /// The painted region's bounding box, in the paint's own space.
    /// </summary>
    /// <remarks>
    /// A translucent gradient's soft mask is a form XObject whose <c>/BBox</c> is
    /// in the form's space - which is paint space once a transform exists - so the
    /// canvas box has to be mapped back through the inverse. Leaving it in canvas
    /// space clips the mask wherever the transform moves content outside it, which
    /// reads as a gradient fading to nothing at one edge.
    ///
    /// Returns null for a transform that cannot be inverted: it has collapsed
    /// paint space, so there is nothing to paint.
    ///
    /// Computed in double, like the geometry the fallbacks derive from it, and
    /// narrowed only where it enters a <see cref="ShadingRequest"/>. Inverting in
    /// float would move the box by enough to change printed digits the
    /// TypeScript implementation computes in double.
    /// </remarks>
    private double[]? PaintSpaceBBox(PaintTransform? paintTransform)
    {
        float[] canvasBox = CurrentPathBBox();
        if (paintTransform.IsIdentityOrAbsent())
            return [canvasBox[0], canvasBox[1], canvasBox[2], canvasBox[3]];
        if (InvertPrecise(paintTransform!.Value) is not { } inverse) return null;

        double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;
        foreach ((double x, double y) in new (double, double)[]
                 {
                     (canvasBox[0], canvasBox[1]), (canvasBox[2], canvasBox[1]),
                     (canvasBox[2], canvasBox[3]), (canvasBox[0], canvasBox[3]),
                 })
        {
            double mx = inverse.A * x + inverse.C * y + inverse.E;
            double my = inverse.B * x + inverse.D * y + inverse.F;
            minX = Math.Min(minX, mx);
            minY = Math.Min(minY, my);
            maxX = Math.Max(maxX, mx);
            maxY = Math.Max(maxY, my);
        }
        return [minX, minY, maxX, maxY];
    }

    private static bool HasAlpha(IReadOnlyList<GradientStop> stops)
    {
        foreach (GradientStop s in stops)
            if (s.Color.ScA < 1f) return true;
        return false;
    }

    /// <summary>
    /// Samples a colour from stops that are already normalized - sorted, clamped to
    /// [0, 1] and padded so one sits at each end of the domain.
    /// </summary>
    /// <remarks>
    /// Both approximations sample through here, so they agree with the native
    /// shading they stand in for and neither has to decide what lies outside the
    /// ramp: the padding guarantees the whole domain is covered.
    /// </remarks>
    private static Color SampleNormalizedStops(IReadOnlyList<ShadingStop> stops, double t)
    {
        if (stops.Count == 0) return Color.Black;
        double tc = t < 0 ? 0 : t > 1 ? 1 : t;

        for (int i = 0; i < stops.Count - 1; i++)
        {
            ShadingStop lo = stops[i];
            ShadingStop hi = stops[i + 1];
            if (tc > hi.Offset) continue;

            double range = (double)hi.Offset - lo.Offset;
            double f = range == 0 ? 0 : (tc - lo.Offset) / range;
            return Color.FromScRgb(
                (float)(lo.R + (hi.R - lo.R) * f),
                (float)(lo.G + (hi.G - lo.G) * f),
                (float)(lo.B + (hi.B - lo.B) * f),
                (float)(lo.A + (hi.A - lo.A) * f));
        }

        ShadingStop last = stops[^1];
        return Color.FromScRgb(last.R, last.G, last.B, last.A);
    }

    /// <summary>
    /// The colour band <paramref name="i"/> is painted with, given that the band
    /// beyond it is already down. Null when it would add nothing visible.
    /// </summary>
    /// <remarks>
    /// Whether the backdrop is corrected for depends on something the canvas can
    /// actually tell: a shading resolver that <em>declined</em> this gradient did
    /// so because it is translucent under PDF/A-1, where the alpha ExtGState is
    /// forced to <c>/ca 1</c>. The correction solves for a (colour, <em>alpha</em>)
    /// pair, so with the alpha discarded the corrected colour is simply the wrong
    /// colour and the ramp comes out visibly distorted. With no resolver at all the
    /// alpha is honoured and the correction is what keeps the overlapping stack
    /// from compounding it.
    /// <para>
    /// <paramref name="i"/> of -1 asks for the /Extend flood before the near
    /// end. (The far-end flood is painted uncorrected, beneath everything, so it
    /// never comes here.)
    /// </para>
    /// </remarks>
    private Color? BandColor(IReadOnlyList<ShadingStop> stops, int i, int bands)
    {
        double At(int n) => n < 0 ? 0 : n >= bands ? 1 : (n + 0.5) / bands;

        Color target = SampleNormalizedStops(stops, At(i));
        if (_shadingResolver is not null) return target;
        return CorrectOverDisc(target, SampleNormalizedStops(stops, At(i + 1)));
    }

    /// <summary>
    /// Solves the (colour, alpha) a disc must be painted with so that, composited
    /// with the source-over operator on top of a backdrop <paramref name="below"/>,
    /// the visible result equals <paramref name="target"/>. Returns null when the
    /// disc would add nothing - the backdrop already meets or exceeds the target
    /// opacity at that radius.
    /// </summary>
    /// <remarks>
    /// Derivation (premultiplied source-over, target = disc over below):
    /// <code>
    ///   Aout = a + Ab(1 - a)              =>  a = (At - Ab) / (1 - Ab)
    ///   Aout*Cout = a*C + Ab*B(1 - a)     =>  C = (At*T - Ab*B(1 - a)) / a
    /// </code>
    /// where (T, At) is the target, (B, Ab) the backdrop and (C, a) the disc.
    /// </remarks>
    private static Color? CorrectOverDisc(Color target, Color below)
    {
        double at = target.ScA;
        double ab = below.ScA;

        // Backdrop is already opaque: can't composite under it, so just lay the
        // opaque target colour on top (covers the realistic opaque-gradient case).
        if (1 - ab < 1e-6) return Color.FromScRgb(target.ScR, target.ScG, target.ScB, 1f);

        double a = (at - ab) / (1 - ab);
        // a <= 0 means the target is *less* opaque than the backdrop - unreachable by
        // stacking another disc on top, so skip it (e.g. a fully transparent rim).
        if (a <= 1e-6) return null;

        double inv = 1 - a;
        // FromScRgb clamps each channel (and alpha) back into [0, 1].
        return Color.FromScRgb(
            (float)((at * target.ScR - ab * below.ScR * inv) / a),
            (float)((at * target.ScG - ab * below.ScG * inv) / a),
            (float)((at * target.ScB - ab * below.ScB * inv) / a),
            (float)Math.Min(1, a));
    }

    /// <summary>Sorts/clamps stops and pads them so the run spans the full [0, 1] domain.</summary>
    private static List<ShadingStop> NormalizeShadingStops(IReadOnlyList<GradientStop> stops)
    {
        List<ShadingStop> mapped = stops
            .Select(s => new ShadingStop(Math.Clamp(s.Offset, 0f, 1f), s.Color.ScR, s.Color.ScG, s.Color.ScB, s.Color.ScA))
            .OrderBy(s => s.Offset)
            .ToList();

        if (mapped.Count == 0)
            return new List<ShadingStop> { new(0, 0, 0, 0, 1), new(1, 0, 0, 0, 1) };
        if (mapped.Count == 1)
            return new List<ShadingStop> { mapped[0] with { Offset = 0 }, mapped[0] with { Offset = 1 } };
        if (mapped[0].Offset > 0) mapped.Insert(0, mapped[0] with { Offset = 0 });
        if (mapped[^1].Offset < 1) mapped.Add(mapped[^1] with { Offset = 1 });
        return mapped;
    }

    /// <summary>Bounding box of the current path in canvas space (control points included).</summary>
    private float[] CurrentPathBBox()
    {
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;

        void Add(float x, float y)
        {
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        foreach ((char type, float[] args) in _currentPath)
        {
            if (type == 'R')
            {
                // Rect stores [x, y, width, height]; the bbox-relevant corners are
                // the two opposite corners.
                Add(args[0], args[1]);
                Add(args[0] + args[2], args[1] + args[3]);
            }
            else
            {
                for (int i = 0; i + 1 < args.Length; i += 2) Add(args[i], args[i + 1]);
            }
        }

        if (float.IsInfinity(minX)) return new[] { 0f, 0f, 0f, 0f };
        return new[] { minX, minY, maxX, maxY };
    }

    /// <summary>
    /// Fills the current path with a linear gradient by clipping to the path and
    /// filling an overlapping stack of quads perpendicular to the gradient axis.
    /// Fallback for when a native shading can't be used.
    /// </summary>
    /// <param name="gradient">The gradient to approximate.</param>
    /// <param name="fillRule">The rule the path is clipped with.</param>
    /// <param name="stops">The same normalized stops the native shading path builds.</param>
    /// <param name="bbox">The current path's bounding box in paint space - the region the quads must cover.</param>
    private void FillPathWithLinearGradientStrips(
        LinearGradientPaint gradient, FillRule fillRule, List<ShadingStop> stops, double[] bbox)
    {
        PdfOperators.SaveState(_stream);

        // Clip to the path
        EmitCurrentPath();
        if (fillRule == FillRule.EvenOdd)
            PdfOperators.ClipEvenOdd(_stream);
        else
            PdfOperators.Clip(_stream);

        // Geometry is generated in the paint's own space and mapped afterwards,
        // which is what makes a sheared ramp come out sheared rather than merely
        // rotated.
        double sx = gradient.Start.X;
        double sy = gradient.Start.Y;

        double dx = gradient.End.X - sx;
        double dy = gradient.End.Y - sy;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len == 0)
        {
            PdfOperators.RestoreState(_stream);
            return;
        }

        // Perpendicular direction for strip width
        double px = -dy / len;
        double py = dx / len;

        // Reach far enough either side of the axis to cover the clipped region,
        // which is exactly the current path. Taking the greatest perpendicular
        // distance to the path's corners is provably sufficient and needs nothing
        // tuned - a constant multiple of the axis length is too short whenever the
        // path is long across the axis, and an absolute constant is meaningless
        // under a paint transform that scales.
        double halfSpan = MaxPerpendicularReach(sx, sy, px, py, bbox);
        // How far the path runs along the axis, which is what the shading's
        // /Extend [true true] would flood. Beyond it there is nothing to paint.
        (double tMin, double tMax) = AxisProjection(sx, sy, dx, dy, bbox);

        // The bands are a stack painted from the far end back: each one reaches
        // all the way to the near end, so every band completely covers the one
        // before it.
        //
        // A hairline overlap is not enough, and measuring is what showed it. A
        // renderer antialiases each quad's edges and composites the results, so
        // two paths that each cover a boundary pixel ~60% leave it ~16% uncovered
        // rather than filling it: coverage multiplies, it does not add. Abutting
        // quads and narrowly overlapping ones both leave the page showing through
        // along every boundary, which on any ramp that is not axis-aligned reads
        // as a moire lattice. Only full coverage by a single path removes it,
        // which is what the stack gives - and it is why the disc approximation
        // never had the problem.

        // Past the far end, /Extend floods with the last stop's colour. It is
        // painted first, beneath the whole stack and across the whole range, at
        // its sampled colour: nothing lies beneath it to correct for. Every
        // band's correction assumes the band beyond it is already down, and for
        // the outermost band that is this flood - so it has to be there wherever
        // that band shows, not only past t=1. A ramp that fades to nothing floods
        // with nothing and drops out, exactly as the radial rim flood does.
        Color far = SampleNormalizedStops(stops, 1);
        if (far.ScA > 0) Quad(tMin, tMax, far);

        for (int i = GradientFallbackBands - 1; i >= 0; i--)
        {
            if (BandColor(stops, i, GradientFallbackBands) is not { } band) continue; // adds nothing
            Quad(tMin, (double)(i + 1) / GradientFallbackBands, band);
        }

        // Before the near end, /Extend floods with the first stop's colour, over
        // the innermost band.
        if (tMin < 0 && BandColor(stops, -1, GradientFallbackBands) is { } near)
            Quad(tMin, 0.5 / GradientFallbackBands, near);

        PdfOperators.RestoreState(_stream);

        // One quad spanning t0..t1 along the axis and the full span across it.
        void Quad(double t0, double t1, Color color)
        {
            double cx0 = sx + dx * t0;
            double cy0 = sy + dy * t0;
            double cx1 = sx + dx * t1;
            double cy1 = sy + dy * t1;

            var a = PaintToPdfPointPrecise(gradient.Transform, cx0 + px * halfSpan, cy0 + py * halfSpan);
            var b = PaintToPdfPointPrecise(gradient.Transform, cx0 - px * halfSpan, cy0 - py * halfSpan);
            var c = PaintToPdfPointPrecise(gradient.Transform, cx1 - px * halfSpan, cy1 - py * halfSpan);
            var d = PaintToPdfPointPrecise(gradient.Transform, cx1 + px * halfSpan, cy1 + py * halfSpan);

            PdfOperators.SaveState(_stream);
            ApplyColorAlpha(color);
            PdfOperators.SetFillColor(_stream, color);
            PdfOperators.MoveTo(_stream, a.X, a.Y);
            PdfOperators.LineTo(_stream, b.X, b.Y);
            PdfOperators.LineTo(_stream, c.X, c.Y);
            PdfOperators.LineTo(_stream, d.X, d.Y);
            PdfOperators.ClosePath(_stream);
            PdfOperators.Fill(_stream);
            PdfOperators.RestoreState(_stream);
        }
    }

    /// <summary>
    /// The greatest perpendicular distance from the gradient axis to a corner of
    /// <paramref name="bbox"/>, all in paint space.
    /// </summary>
    private static double MaxPerpendicularReach(double sx, double sy, double px, double py, double[] bbox)
    {
        double x0 = bbox[0];
        double y0 = bbox[1];
        double x1 = bbox[2];
        double y1 = bbox[3];
        double reach = 0;
        Consider(x0, y0);
        Consider(x1, y0);
        Consider(x1, y1);
        Consider(x0, y1);
        return reach;

        void Consider(double cx, double cy)
        {
            double d = Math.Abs((cx - sx) * px + (cy - sy) * py);
            if (d > reach) reach = d;
        }
    }

    /// <summary>
    /// How far the path's bounding box runs along the gradient axis, as a <c>t</c>
    /// range, widened to at least the ramp's own 0..1.
    /// </summary>
    /// <remarks>
    /// This is what a native shading's <c>/Extend [true true]</c> covers. Without
    /// it the approximation paints only 0..1 and leaves everything beyond the axis
    /// unpainted, which shows up as bare wedges on any rotated or off-centre
    /// gradient - the native path fills them.
    /// </remarks>
    private static (double TMin, double TMax) AxisProjection(
        double sx, double sy, double dx, double dy, double[] bbox)
    {
        double lenSq = dx * dx + dy * dy;
        double x0 = bbox[0];
        double y0 = bbox[1];
        double x1 = bbox[2];
        double y1 = bbox[3];
        double tMin = 0;
        double tMax = 1;
        Consider(x0, y0);
        Consider(x1, y0);
        Consider(x1, y1);
        Consider(x0, y1);
        return (tMin, tMax);

        void Consider(double cx, double cy)
        {
            double t = ((cx - sx) * dx + (cy - sy) * dy) / lenSq;
            if (t < tMin) tMin = t;
            if (t > tMax) tMax = t;
        }
    }

    /// <summary>
    /// Fills the current path with a radial gradient. Prefers a native PDF Type-3
    /// radial shading (see <see cref="EmitShading"/>); falls back to the concentric
    /// disc approximation when shadings can't be used (a translucent gradient under
    /// PDF/A-1, which forbids transparency).
    /// </summary>
    private void FillPathWithRadialGradient(RadialGradientPaint gradient, FillRule fillRule)
    {
        Point inner = gradient.Focal ?? gradient.Center;
        if (PaintSpaceBBox(gradient.Transform) is not { } bbox) return;

        List<ShadingStop> stops = NormalizeShadingStops(gradient.Stops);
        var request = new ShadingRequest
        {
            ShadingType = 3,
            Coords = new[] { inner.X, inner.Y, 0f, gradient.Center.X, gradient.Center.Y, gradient.Radius },
            Stops = stops,
            Extend = (true, true),
            Bbox = Array.ConvertAll(bbox, v => (float)v),
            HasAlpha = HasAlpha(gradient.Stops)
        };
        ShadingResources? resolved = _shadingResolver?.Invoke(request);
        if (resolved is { } r) EmitShading(r, fillRule, gradient.Transform);
        else FillPathWithRadialGradientDiscs(gradient, fillRule, stops, bbox);
    }

    /// <summary>
    /// Fills the current path with a radial gradient as a stack of overlapping
    /// discs, largest first. Fallback for when a native shading can't be used.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Overlapping full discs rather than abutting annuli is what keeps the result
    /// smooth: each disc paints over the previous disc's antialiased edge, so no
    /// hairline seam is left between bands.
    /// </para>
    /// <para>
    /// The catch is translucency. Naively painting each disc at its raw sampled
    /// alpha composites the overlapping discs (source-over) and compounds the
    /// opacity far past the intended value. So each disc's alpha <em>and</em> colour
    /// are pre-corrected: given what the larger discs beneath have already laid
    /// down, solve for the (colour, alpha) that, composited over that backdrop,
    /// yields the target sample exactly. For an opaque gradient this collapses to
    /// "paint the target colour"; for a constant-colour fade - the ordinary glow -
    /// it reproduces the alpha ramp precisely, and it is what lets this path render
    /// translucency under PDF/A-1, where the alpha ExtGState is forced to 1.
    /// </para>
    /// <para>
    /// The focal point is not honoured here - it has no analogue in a concentric
    /// stack. That is a recorded limitation of the approximation.
    /// </para>
    /// </remarks>
    private void FillPathWithRadialGradientDiscs(
        RadialGradientPaint gradient, FillRule fillRule, List<ShadingStop> stops, double[] bbox)
    {
        PdfOperators.SaveState(_stream);

        // Clip to the path
        EmitCurrentPath();
        if (fillRule == FillRule.EvenOdd)
            PdfOperators.ClipEvenOdd(_stream);
        else
            PdfOperators.Clip(_stream);

        double cx = gradient.Center.X;
        double cy = gradient.Center.Y;

        // Past the rim, /Extend floods the rest of the path with the last stop's
        // colour - which the native shading does and this approximation used not
        // to, leaving a bare disc where the shading fills the whole clipped
        // region. Painted first, with nothing beneath it, and overlapped by the
        // stack. For a ramp that fades to nothing the flood is transparent and
        // drops out, which is also what the native shading shows.
        Color rim = SampleNormalizedStops(stops, 1);
        if (rim.ScA > 0)
        {
            var a = PaintToPdfPointPrecise(gradient.Transform, bbox[0], bbox[1]);
            var b = PaintToPdfPointPrecise(gradient.Transform, bbox[2], bbox[1]);
            var c = PaintToPdfPointPrecise(gradient.Transform, bbox[2], bbox[3]);
            var d = PaintToPdfPointPrecise(gradient.Transform, bbox[0], bbox[3]);

            PdfOperators.SaveState(_stream);
            ApplyColorAlpha(rim);
            PdfOperators.SetFillColor(_stream, rim);
            PdfOperators.MoveTo(_stream, a.X, a.Y);
            PdfOperators.LineTo(_stream, b.X, b.Y);
            PdfOperators.LineTo(_stream, c.X, c.Y);
            PdfOperators.LineTo(_stream, d.X, d.Y);
            PdfOperators.ClosePath(_stream);
            PdfOperators.Fill(_stream);
            PdfOperators.RestoreState(_stream);
        }

        for (int i = GradientFallbackBands - 1; i >= 0; i--)
        {
            // Beneath this disc is the band one step further out, or - at the
            // outermost band - the /Extend flood, whose colour BandColor also
            // gives. Past the rim with no flood (a ramp that fades to nothing)
            // there is nothing beneath at all.
            Color? candidate = i + 1 < GradientFallbackBands || rim.ScA > 0
                ? BandColor(stops, i, GradientFallbackBands)
                : CorrectOverDisc(SampleNormalizedStops(stops, (i + 0.5) / GradientFallbackBands), Transparent);
            if (candidate is not { } disc) continue; // adds nothing visible

            double r = gradient.Radius * ((double)(i + 1) / GradientFallbackBands);
            PdfOperators.SaveState(_stream);
            ApplyColorAlpha(disc);
            PdfOperators.SetFillColor(_stream, disc);
            EmitCirclePath(cx, cy, r, gradient.Transform);
            PdfOperators.Fill(_stream);
            PdfOperators.RestoreState(_stream);
        }

        PdfOperators.RestoreState(_stream);
    }

    /// <summary>
    /// Emits a closed circle path (4 cubic-Bezier quadrants) in PDF user space.
    /// </summary>
    /// <remarks>
    /// With a paint transform the control points are generated in paint space and
    /// mapped, which yields a true ellipse rather than an approximation of one: an
    /// affine image of a circle-approximating Bezier <em>is</em> the corresponding
    /// ellipse-approximating Bezier, because affine maps take Beziers to Beziers of
    /// the same degree.
    /// </remarks>
    private void EmitCirclePath(double cx, double cy, double r, PaintTransform? paintTransform)
    {
        double kr = r * QuarterArc;
        (double X, double Y) At(double x, double y) => PaintToPdfPointPrecise(paintTransform, x, y);

        var p0 = At(cx + r, cy);
        var c1a = At(cx + r, cy + kr);
        var c1b = At(cx + kr, cy + r);
        var p1 = At(cx, cy + r);
        var c2a = At(cx - kr, cy + r);
        var c2b = At(cx - r, cy + kr);
        var p2 = At(cx - r, cy);
        var c3a = At(cx - r, cy - kr);
        var c3b = At(cx - kr, cy - r);
        var p3 = At(cx, cy - r);
        var c4a = At(cx + kr, cy - r);
        var c4b = At(cx + r, cy - kr);
        PdfOperators.MoveTo(_stream, p0.X, p0.Y);
        PdfOperators.CurveTo(_stream, c1a.X, c1a.Y, c1b.X, c1b.Y, p1.X, p1.Y);
        PdfOperators.CurveTo(_stream, c2a.X, c2a.Y, c2b.X, c2b.Y, p2.X, p2.Y);
        PdfOperators.CurveTo(_stream, c3a.X, c3a.Y, c3b.X, c3b.Y, p3.X, p3.Y);
        PdfOperators.CurveTo(_stream, c4a.X, c4a.Y, c4b.X, c4b.Y, p0.X, p0.Y);
        PdfOperators.ClosePath(_stream);
    }

    /// <summary>
    /// Strokes the current path with a gradient, one straight segment at a time.
    /// </summary>
    /// <remarks>
    /// Each subpath is flattened on its own - curves into short chords, a closed
    /// subpath including its closing segment - and each segment is stroked in the
    /// colour sampled at its midpoint. The midpoint is taken in the same space the
    /// gradient's geometry is in, so a transformed canvas moves the ramp with the
    /// path rather than distorting it, and the colour comes from the normalized
    /// stops the fill paths sample. With a paint transform the midpoint is mapped
    /// back into the paint's own space first, exactly as the fills read their
    /// geometry there; a transform that cannot be inverted paints nothing.
    /// <para>
    /// A translucent segment is wrapped in its own <c>q</c>/<c>Q</c>, so its alpha
    /// cannot leak into the opaque segments after it; an opaque segment needs
    /// neither.
    /// </para>
    /// </remarks>
    private void StrokePathWithGradient(Paint paint, StrokeStyle style)
    {
        IReadOnlyList<GradientStop> gradientStops = paint switch
        {
            LinearGradientPaint linear => linear.Stops,
            RadialGradientPaint radial => radial.Stops,
            _ => throw new ArgumentException("Not a gradient paint.", nameof(paint)),
        };
        PaintTransform? transform = paint is LinearGradientPaint l ? l.Transform : ((RadialGradientPaint)paint).Transform;
        (double A, double B, double C, double D, double E, double F)? toPaintSpace = null;
        if (!transform.IsIdentityOrAbsent())
        {
            toPaintSpace = InvertPrecise(transform!.Value);
            if (toPaintSpace is null) return;
        }
        List<ShadingStop> stops = NormalizeShadingStops(gradientStops);

        PdfOperators.SaveState(_stream);
        ApplyStrokeSettings(style);

        foreach (List<(double X, double Y)> polyline in LinearizeCurrentPath(200))
        {
            for (int i = 0; i < polyline.Count - 1; i++)
            {
                (double X, double Y) p0 = polyline[i];
                (double X, double Y) p1 = polyline[i + 1];
                double mx = (p0.X + p1.X) / 2;
                double my = (p0.Y + p1.Y) / 2;
                if (toPaintSpace is { } inv)
                    (mx, my) = (inv.A * mx + inv.C * my + inv.E, inv.B * mx + inv.D * my + inv.F);
                double t = ProjectOntoGradient(mx, my, paint);
                Color color = SampleNormalizedStops(stops, t);
                (double X, double Y) s = TransformPointPrecise(p0.X, p0.Y);
                (double X, double Y) e = TransformPointPrecise(p1.X, p1.Y);
                bool translucent = color.ScA < 1f;
                if (translucent)
                {
                    PdfOperators.SaveState(_stream);
                    ApplyColorAlpha(color);
                }
                PdfOperators.SetStrokeColor(_stream, color);
                PdfOperators.StrokeLine(_stream, s.X, s.Y, e.X, e.Y);
                if (translucent) PdfOperators.RestoreState(_stream);
            }
        }

        PdfOperators.RestoreState(_stream);
    }

    /// <summary>
    /// The inverse of a paint transform, computed in double, or null when it is
    /// not invertible.
    /// </summary>
    /// <remarks>
    /// <see cref="PaintTransform.Invert"/> works in float, the precision of the
    /// public type. Geometry derived from the inverse is formatted from double,
    /// so the inverse has to be double too, or a float round-trip moves values
    /// across a rounding boundary the TypeScript implementation does not cross.
    /// </remarks>
    private static (double A, double B, double C, double D, double E, double F)? InvertPrecise(PaintTransform t)
    {
        double a = t.A, b = t.B, c = t.C, d = t.D, e = t.E, f = t.F;
        double determinant = a * d - b * c;
        if (determinant == 0 || !double.IsFinite(determinant)) return null;
        return (d / determinant, -b / determinant, -c / determinant, a / determinant,
            (c * f - d * e) / determinant, (b * e - a * f) / determinant);
    }

    /// <summary>
    /// The gradient parameter at a point: its projection onto a linear axis, or
    /// its distance from a radial centre as a fraction of the radius. A
    /// degenerate axis reads as the first stop, a zero radius as the last.
    /// </summary>
    private static double ProjectOntoGradient(double x, double y, Paint paint)
    {
        if (paint is LinearGradientPaint linear)
        {
            double dx = (double)linear.End.X - linear.Start.X;
            double dy = (double)linear.End.Y - linear.Start.Y;
            double lenSq = dx * dx + dy * dy;
            if (lenSq == 0) return 0;
            return ((x - linear.Start.X) * dx + (y - linear.Start.Y) * dy) / lenSq;
        }

        var radial = (RadialGradientPaint)paint;
        if (radial.Radius <= 0) return 1;
        double rx = x - radial.Center.X;
        double ry = y - radial.Center.Y;
        return Math.Sqrt(rx * rx + ry * ry) / radial.Radius;
    }

    /// <summary>
    /// The current path as one polyline per subpath, in canvas space.
    /// </summary>
    /// <remarks>
    /// A moveTo starts a new polyline rather than joining the previous one, and a
    /// closePath adds the segment back to the subpath's start. Each curve becomes
    /// <c>max(4, ceil(totalSegments / pathLength))</c> chords, where
    /// <c>pathLength</c> counts path commands - a rectangle as the five
    /// (move, three lines, close) it is made of, so the count is the same however
    /// a canvas chooses to store one.
    /// </remarks>
    private List<List<(double X, double Y)>> LinearizeCurrentPath(int totalSegments)
    {
        int pathLength = 0;
        foreach ((char type, _) in _currentPath) pathLength += type == 'R' ? 5 : 1;
        int chords = Math.Max(4, (int)Math.Ceiling((double)totalSegments / Math.Max(1, pathLength)));

        var polylines = new List<List<(double X, double Y)>>();
        List<(double X, double Y)>? current = null;
        double cx = 0, cy = 0, startX = 0, startY = 0;

        void MoveTo(double x, double y)
        {
            cx = startX = x;
            cy = startY = y;
            current = [(x, y)];
            polylines.Add(current);
        }

        List<(double X, double Y)> ContinueFromCurrentPoint()
        {
            if (current is null)
            {
                current = [(cx, cy)];
                polylines.Add(current);
            }
            return current;
        }

        void LineTo(double x, double y)
        {
            cx = x;
            cy = y;
            ContinueFromCurrentPoint().Add((x, y));
        }

        void Close()
        {
            current?.Add((startX, startY));
            current = null;
            cx = startX;
            cy = startY;
        }

        foreach ((char type, float[] a) in _currentPath)
        {
            switch (type)
            {
                case 'M':
                    MoveTo(a[0], a[1]);
                    break;
                case 'L':
                    LineTo(a[0], a[1]);
                    break;
                case 'C':
                {
                    List<(double X, double Y)> polyline = ContinueFromCurrentPoint();
                    double c1x = a[0], c1y = a[1], c2x = a[2], c2y = a[3], ex = a[4], ey = a[5];
                    for (int i = 1; i <= chords; i++)
                    {
                        double t = (double)i / chords;
                        double u = 1 - t;
                        polyline.Add((
                            u * u * u * cx + 3 * u * u * t * c1x + 3 * u * t * t * c2x + t * t * t * ex,
                            u * u * u * cy + 3 * u * u * t * c1y + 3 * u * t * t * c2y + t * t * t * ey));
                    }
                    cx = ex;
                    cy = ey;
                    break;
                }
                case 'R':
                {
                    double x = a[0], y = a[1], w = a[2], h = a[3];
                    MoveTo(x, y);
                    LineTo(x + w, y);
                    LineTo(x + w, y + h);
                    LineTo(x, y + h);
                    Close();
                    break;
                }
                case 'H':
                    Close();
                    break;
            }
        }
        return polylines;
    }

    private void DrawWithColorAlpha(Color color, Action draw)
    {
        DrawAsArtifactFallback(() =>
        {
            BeginColorAlpha(color);
            draw();
            EndColorAlpha(color);
        });
    }

    /// <summary>
    /// Wraps unscoped drawing in an /Artifact BMC for tagged documents, so the
    /// marks aren't left as bare (untagged) real content (PDF/UA 7.1). Skips the
    /// wrapper when already inside an MCID-bearing structure scope or an explicit
    /// /Artifact region. Container-only BMC scopes do not count as tagged content.
    /// </summary>
    /// <remarks>
    /// <paramref name="onAutoArtifact"/> runs only when the fallback actually
    /// fires, letting image callers surface a diagnostic rather than silently
    /// demoting meaningful content to an artifact.
    /// </remarks>
    private void DrawAsArtifactFallback(Action draw, Action? onAutoArtifact = null)
    {
        bool needsFallbackArtifact = _tagging != null && _openTaggedCount == 0 && _artifactDepth == 0;
        if (needsFallbackArtifact)
        {
            PdfOperators.BeginArtifact(_stream);
            onAutoArtifact?.Invoke();
        }
        try
        {
            draw();
        }
        finally
        {
            if (needsFallbackArtifact) PdfOperators.EndMarkedContent(_stream);
        }
    }

    private void BeginColorAlpha(Color color)
    {
        if (color.ScA >= 1f) return;
        PdfOperators.SaveState(_stream);
        ApplyColorAlpha(color);
    }

    private void EndColorAlpha(Color color)
    {
        if (color.ScA >= 1f) return;
        PdfOperators.RestoreState(_stream);
    }

    private void ApplyColorAlpha(Color color)
    {
        if (color.ScA >= 1f) return;
        PdfOperators.SetGraphicsState(_stream, _alphaGraphicsStateResolver(color.ScA));
    }
}
