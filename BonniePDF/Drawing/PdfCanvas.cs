using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Images;
using BonniePDF.Rendering.Pdf;
using BonniePDF.Rendering.Tagging;
using BonniePDF.Typography;
using BonniePDF.Typography.TrueType;

namespace BonniePDF.Drawing;

/// <summary>
/// PDF implementation of ICanvas that adapts ICanvas calls to PDF Operators.
/// Handles the critical Y-axis coordinate transformation from Screen Coordinates (Top-Left origin) to PDF Coordinates
/// (Bottom-Left origin).
/// </summary>
public class PdfCanvas : ICanvas
{
    private readonly Func<string, TtfFont?> _embeddedFontResolver;
    private readonly Func<string, string> _fontResolver;
    private readonly Func<ImageInfo, string> _imageResolver;
    private readonly Action<string, string> _glyphTracker;
    private readonly Func<float, string> _alphaGraphicsStateResolver;
    private readonly Func<string, string, IList<ResolvedTextRun>>? _textRunResolver;
    private readonly Action? _reportUntaggedImage;
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
        Func<ShadingRequest, ShadingResources?>? shadingResolver = null)
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
        _shadingResolver = shadingResolver;
    }

    /// <summary>
    /// Gets the width of the canvas (not directly applicable to PDF streams, returns 0)
    /// </summary>
    public float Width { get; }

    /// <summary>
    /// Gets the height of the canvas (returns the page height)
    /// </summary>
    public float Height { get; }

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
    public void FillPath(IList<Point> points, Color color)
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

        // Calculate PDF coordinates (Bottom-Left origin)
        // PDF Y is bottom-left. x,y is top-left.
        float pdfX = x + _xOffset;
        // y + height is the bottom of the image in Screen coords.
        // _pageHeight - (bottom) is the PDF Y.
        float pdfY = Height - (y + _yOffset + height);

        DrawAsArtifactFallback(() =>
        {
            PdfOperators.SaveState(_stream);
            PdfOperators.Translate(_stream, pdfX, pdfY);
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
    /// no-ops and the canvas behaves exactly as it did pre-G-1.
    /// </summary>
    public void SetTaggingContext(TaggingContext tagging) => _tagging = tagging;

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
                // word spacing (Tw) does not apply to composite fonts, so when
                // it is set, inject it into the TJ array (combined with any kerning).
                // Letter spacing is handled universally by Tc, above.
                if (_wordSpacing != 0 && run.Text.Contains(' '))
                {
                    PdfOperators.ShowTextPositioned(_stream,
                        PdfSyntax.ToSpacedGlyphArray(run.Text, run.EmbeddedFont, fontSize, _wordSpacing));
                    continue;
                }
                // embedded fonts with kern pairs paint with `TJ` so the PDF
                // matches the kerned widths the layout engine reserved (and the
                // kerned this feature SVG export). Unkerned strings keep the `Tj` path.
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
            else if (style.Paint is LinearGradientPaint linear)
            {
                StrokePathWithLinearGradient(linear, style);
            }
            else if (style.Paint is RadialGradientPaint radial)
            {
                StrokePathWithRadialGradient(radial, style);
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

    /// <summary>Returns true when the local CTM has no rotation or skew (b and c are zero).</summary>
    private bool IsAxisAligned()
        => Math.Abs(_ctm[1]) < 1e-6f && Math.Abs(_ctm[2]) < 1e-6f;

    /// <summary>Applies stroke settings (line width, cap, join, dash, miter) to the PDF state.</summary>
    private void ApplyStrokeSettings(StrokeStyle style)
    {
        PdfOperators.SetLineWidth(_stream, style.Width);
        PdfOperators.SetLineCap(_stream, MapLineCap(style.LineCap));
        PdfOperators.SetLineJoin(_stream, MapLineJoin(style.LineJoin));
        if (style.MiterLimit != 10f)
            PdfOperators.SetMiterLimit(_stream, style.MiterLimit);
        if (style.DashPattern is { Length: > 0 })
            PdfOperators.SetDashPattern(_stream, style.DashPattern, style.DashOffset);
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

    /// <summary>Interpolates between two colors at the given parameter t (0-1).</summary>
    private static Color LerpColor(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return Color.FromScRgb(
            a.ScR + (b.ScR - a.ScR) * t,
            a.ScG + (b.ScG - a.ScG) * t,
            a.ScB + (b.ScB - a.ScB) * t,
            a.ScA + (b.ScA - a.ScA) * t);
    }

    /// <summary>Samples a color from gradient stops at the given offset (0-1).</summary>
    private static Color SampleGradient(IReadOnlyList<GradientStop> stops, float offset)
    {
        if (stops.Count == 0) return Color.Black;
        if (stops.Count == 1) return stops[0].Color;
        offset = Math.Clamp(offset, 0f, 1f);

        for (int i = 0; i < stops.Count - 1; i++)
        {
            if (offset <= stops[i + 1].Offset)
            {
                float range = stops[i + 1].Offset - stops[i].Offset;
                float t = range > 0 ? (offset - stops[i].Offset) / range : 0;
                return LerpColor(stops[i].Color, stops[i + 1].Color, t);
            }
        }
        return stops[stops.Count - 1].Color;
    }

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

        var request = new ShadingRequest
        {
            ShadingType = 2,
            Coords = new[] { gradient.Start.X, gradient.Start.Y, gradient.End.X, gradient.End.Y },
            Stops = NormalizeShadingStops(gradient.Stops),
            Extend = (true, true),
            Bbox = CurrentPathBBox(),
            HasAlpha = HasAlpha(gradient.Stops)
        };
        ShadingResources? resolved = _shadingResolver?.Invoke(request);
        if (resolved is { } r) EmitShading(r, fillRule);
        else FillPathWithLinearGradientStrips(gradient, fillRule);
    }

    /// <summary>
    /// Paints a resolved shading across the current path's clipped region. A
    /// <c>cm</c> maps the shading's canvas-space coordinates into PDF page space —
    /// PdfCanvas bakes its own transform into every point via <see cref="TransformPoint"/>
    /// and runs an identity stream CTM, so the shading needs that matrix re-applied
    /// here (it also aligns the soft-mask group, established under the same CTM).
    /// </summary>
    private void EmitShading(ShadingResources resolved, FillRule fillRule)
    {
        PdfOperators.SaveState(_stream);

        EmitCurrentPath();
        if (fillRule == FillRule.EvenOdd)
            PdfOperators.ClipEvenOdd(_stream);
        else
            PdfOperators.Clip(_stream);

        PdfOperators.ConcatMatrix(_stream, _ctm[0], -_ctm[1], _ctm[2], -_ctm[3],
            _ctm[4] + _xOffset, Height - _yOffset - _ctm[5]);
        if (resolved.SoftMaskGs != null)
            PdfOperators.SetGraphicsState(_stream, resolved.SoftMaskGs);
        PdfOperators.PaintShading(_stream, resolved.ShadingName);

        PdfOperators.RestoreState(_stream);
    }

    private static bool HasAlpha(IReadOnlyList<GradientStop> stops)
    {
        foreach (GradientStop s in stops)
            if (s.Color.ScA < 1f) return true;
        return false;
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
    /// Fills the current path with a linear gradient using a strip-rendering approach:
    /// clip to the path, then fill thin rectangles with interpolated colors. Fallback
    /// for when a native shading can't be used.
    /// </summary>
    private void FillPathWithLinearGradientStrips(LinearGradientPaint gradient, FillRule fillRule)
    {
        const int strips = 64;
        PdfOperators.SaveState(_stream);

        // Clip to the path
        EmitCurrentPath();
        if (fillRule == FillRule.EvenOdd)
            PdfOperators.ClipEvenOdd(_stream);
        else
            PdfOperators.Clip(_stream);

        // Calculate gradient direction and bounding box
        var (sx, sy) = TransformPoint(gradient.Start.X, gradient.Start.Y);
        var (ex, ey) = TransformPoint(gradient.End.X, gradient.End.Y);

        float dx = ex - sx;
        float dy = ey - sy;
        float len = (float)Math.Sqrt(dx * dx + dy * dy);
        if (len < 0.001f) len = 1f;

        // Perpendicular direction for strip width
        float px = -dy / len;
        float py = dx / len;

        // Extend strips far enough to cover the clipped area
        float extend = len + 500f; // generous extension

        for (int i = 0; i < strips; i++)
        {
            float t0 = (float)i / strips;
            float t1 = (float)(i + 1) / strips;
            float tMid = (t0 + t1) / 2f;

            Color color = SampleGradient(gradient.Stops, tMid);
            PdfOperators.SetFillColor(_stream, color);

            // Strip corners along the gradient direction
            float cx0 = sx + dx * t0;
            float cy0 = sy + dy * t0;
            float cx1 = sx + dx * t1;
            float cy1 = sy + dy * t1;

            PdfOperators.MoveTo(_stream, cx0 + px * extend, cy0 + py * extend);
            PdfOperators.LineTo(_stream, cx1 + px * extend, cy1 + py * extend);
            PdfOperators.LineTo(_stream, cx1 - px * extend, cy1 - py * extend);
            PdfOperators.LineTo(_stream, cx0 - px * extend, cy0 - py * extend);
            PdfOperators.ClosePath(_stream);
            PdfOperators.Fill(_stream);
        }

        PdfOperators.RestoreState(_stream);
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
        var request = new ShadingRequest
        {
            ShadingType = 3,
            Coords = new[] { inner.X, inner.Y, 0f, gradient.Center.X, gradient.Center.Y, gradient.Radius },
            Stops = NormalizeShadingStops(gradient.Stops),
            Extend = (true, true),
            Bbox = CurrentPathBBox(),
            HasAlpha = HasAlpha(gradient.Stops)
        };
        ShadingResources? resolved = _shadingResolver?.Invoke(request);
        if (resolved is { } r) EmitShading(r, fillRule);
        else FillPathWithRadialGradientRings(gradient, fillRule);
    }

    /// <summary>
    /// Fills the current path with a radial gradient using concentric circle strips.
    /// Fallback for when a native shading can't be used.
    /// </summary>
    private void FillPathWithRadialGradientRings(RadialGradientPaint gradient, FillRule fillRule)
    {
        const int rings = 64;
        PdfOperators.SaveState(_stream);

        // Clip to the path
        EmitCurrentPath();
        if (fillRule == FillRule.EvenOdd)
            PdfOperators.ClipEvenOdd(_stream);
        else
            PdfOperators.Clip(_stream);

        var (cx, cy) = TransformPoint(gradient.Center.X, gradient.Center.Y);
        float radius = gradient.Radius * Math.Abs(_ctm[0]); // approximate scale

        // Draw rings from outside in so inner colors overwrite outer
        for (int i = rings - 1; i >= 0; i--)
        {
            float t = (float)(i + 1) / rings;
            float r = radius * t;

            Color color = SampleGradient(gradient.Stops, t);
            PdfOperators.SetFillColor(_stream, color);

            // Approximate circle with 4 bezier curves
            const float k = 0.552284749831f;
            float kr = k * r;
            PdfOperators.MoveTo(_stream, cx + r, cy);
            PdfOperators.CurveTo(_stream, cx + r, cy + kr, cx + kr, cy + r, cx, cy + r);
            PdfOperators.CurveTo(_stream, cx - kr, cy + r, cx - r, cy + kr, cx - r, cy);
            PdfOperators.CurveTo(_stream, cx - r, cy - kr, cx - kr, cy - r, cx, cy - r);
            PdfOperators.CurveTo(_stream, cx + kr, cy - r, cx + r, cy - kr, cx + r, cy);
            PdfOperators.ClosePath(_stream);
            PdfOperators.Fill(_stream);
        }

        PdfOperators.RestoreState(_stream);
    }

    /// <summary>
    /// Strokes the current path with a linear gradient using per-segment color approximation.
    /// </summary>
    private void StrokePathWithLinearGradient(LinearGradientPaint gradient, StrokeStyle style)
    {
        var (sx, sy) = TransformPoint(gradient.Start.X, gradient.Start.Y);
        var (ex, ey) = TransformPoint(gradient.End.X, gradient.End.Y);
        float gdx = ex - sx;
        float gdy = ey - sy;
        float glen = gdx * gdx + gdy * gdy;
        if (glen < 0.001f) glen = 1f;

        StrokePathWithGradientSegments(gradient.Stops, style,
            (mx, my) => ((mx - sx) * gdx + (my - sy) * gdy) / glen);
    }

    /// <summary>
    /// Strokes the current path with a radial gradient using per-segment color approximation.
    /// </summary>
    private void StrokePathWithRadialGradient(RadialGradientPaint gradient, StrokeStyle style)
    {
        var (cx, cy) = TransformPoint(gradient.Center.X, gradient.Center.Y);
        float radius = gradient.Radius * Math.Abs(_ctm[0]);
        if (radius < 0.001f) radius = 1f;

        StrokePathWithGradientSegments(gradient.Stops, style,
            (mx, my) => (float)Math.Sqrt((mx - cx) * (mx - cx) + (my - cy) * (my - cy)) / radius);
    }

    /// <summary>
    /// Walks the current path stroking each segment in the gradient color
    /// sampled at the segment's midpoint. <paramref name="offsetAtMidpoint"/>
    /// maps a (transformed) midpoint to the gradient offset - a linear
    /// projection for linear gradients, a normalised centre distance for
    /// radial ones.
    /// </summary>
    private void StrokePathWithGradientSegments(IReadOnlyList<GradientStop> stops, StrokeStyle style,
        Func<float, float, float> offsetAtMidpoint)
    {
        PdfOperators.SaveState(_stream);
        ApplyStrokeSettings(style);

        (float x, float y)? prev = null;
        foreach (var (type, args) in _currentPath)
        {
            switch (type)
            {
                case 'M':
                {
                    prev = TransformPoint(args[0], args[1]);
                    break;
                }
                case 'L':
                {
                    var cur = TransformPoint(args[0], args[1]);
                    if (prev.HasValue)
                    {
                        float t = offsetAtMidpoint((prev.Value.x + cur.x) / 2f, (prev.Value.y + cur.y) / 2f);
                        Color color = SampleGradient(stops, t);
                        PdfOperators.SetStrokeColor(_stream, color);
                        PdfOperators.MoveTo(_stream, prev.Value.x, prev.Value.y);
                        PdfOperators.LineTo(_stream, cur.x, cur.y);
                        PdfOperators.Stroke(_stream);
                    }
                    prev = cur;
                    break;
                }
                case 'C':
                {
                    var cur = TransformPoint(args[4], args[5]);
                    if (prev.HasValue)
                    {
                        var cp1 = TransformPoint(args[0], args[1]);
                        var cp2 = TransformPoint(args[2], args[3]);
                        float t = offsetAtMidpoint((prev.Value.x + cur.x) / 2f, (prev.Value.y + cur.y) / 2f);
                        Color color = SampleGradient(stops, t);
                        PdfOperators.SetStrokeColor(_stream, color);
                        PdfOperators.MoveTo(_stream, prev.Value.x, prev.Value.y);
                        PdfOperators.CurveTo(_stream, cp1.x, cp1.y, cp2.x, cp2.y, cur.x, cur.y);
                        PdfOperators.Stroke(_stream);
                    }
                    prev = cur;
                    break;
                }
                case 'H':
                    prev = null;
                    break;
            }
        }

        PdfOperators.RestoreState(_stream);
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
