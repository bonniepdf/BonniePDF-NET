using System;
using System.Collections.Generic;
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
    private readonly Func<string, TtfFont> _embeddedFontResolver;
    private readonly Func<string, string> _fontResolver;
    private readonly Func<ImageInfo, string> _imageResolver;
    private readonly Action<string, string> _glyphTracker;
    private readonly Func<float, string> _alphaGraphicsStateResolver;
    private readonly Func<string, string, List<ResolvedTextRun>> _textRunResolver;
    private readonly Action _reportUntaggedImage;
    private readonly StringBuilder _stream;
    private readonly float _xOffset;
    private readonly float _yOffset;
    private int _stateDepth;
    private TaggingContext _tagging;
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
        Func<string, string> fontResolver = null,
        Func<ImageInfo, string> imageResolver = null,
        Func<string, TtfFont> embeddedFontResolver = null,
        Action<string, string> glyphTracker = null,
        Func<float, string> alphaGraphicsStateResolver = null,
        Func<string, string, List<ResolvedTextRun>> textRunResolver = null,
        Action reportUntaggedImage = null)
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
        List<ResolvedTextRun> runs = ResolveRuns(text, fontFamily);
        PdfOperators.SetFillColor(_stream, color);

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

        PdfOperators.EndText(_stream);
        EndColorAlpha(color);
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

        List<ResolvedTextRun> runs = ResolveRuns(text, fontFamily);
        PdfOperators.SetFillColor(_stream, color);
        PdfOperators.SetTextPosition(_stream, 0, 0);

        EmitShowText(runs, fontSize);

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
        List<ResolvedTextRun> runs = ResolveRuns(text, fontFamily);
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
            PdfOperators.Scale(_stream, width, height);
            PdfOperators.PaintXObject(_stream, imageName);
            PdfOperators.RestoreState(_stream);
        }, _reportUntaggedImage);
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
        // already inside a tagged marked-content sequence. Content drawn inside
        // a structure element (e.g. a table cell's /TD) is already enclosed real
        // content (satisfies PDF/UA 7.1), and nesting an artifact inside a tag
        // is invalid. The emit decision is recorded so EndArtifact stays balanced.
        // Only emit a BMC at the outermost artifact level: a nested /Artifact is
        // redundant, and emitting one inside an open structure scope is invalid.
        bool emit = _tagging != null && _openMarkedContent == 0 && _artifactDepth == 0;
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
    private List<ResolvedTextRun> ResolveRuns(string text, string fontFamily)
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
    private static float MeasureRuns(List<ResolvedTextRun> runs, float fontSize)
    {
        float total = 0;
        foreach (ResolvedTextRun run in runs)
        {
            TtfFont font = run.EmbeddedFont;
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
        }
        return total;
    }

    /// <summary>
    /// Emits the font-selection (<c>Tf</c>) and show-text (<c>Tj</c>) operators for
    /// the resolved runs. The caller must have positioned the text matrix and set
    /// the fill colour first; each <c>Tj</c> advances the text point by its glyph
    /// widths, so consecutive runs flow on naturally.
    /// </summary>
    private void EmitShowText(List<ResolvedTextRun> runs, float fontSize)
    {
        foreach (ResolvedTextRun run in runs)
        {
            if (run.Text.Length == 0) continue;
            _glyphTracker(run.FontIdentifier, run.Text);
            PdfOperators.SetFont(_stream, run.ResourceName, fontSize);
            if (run.EmbeddedFont != null)
                PdfOperators.ShowText(_stream, PdfSyntax.ToGlyphIdHexString(run.Text, run.EmbeddedFont));
            else
                PdfOperators.ShowText(_stream, $"({PdfSyntax.Escape(run.Text)})");
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
    /// wrapper when already inside a structure scope or an explicit /Artifact
    /// region, so decorative content in headers/footers/slots isn't double-marked.
    /// </summary>
    /// <remarks>
    /// <paramref name="onAutoArtifact"/> runs only when the fallback actually
    /// fires, letting image callers surface a diagnostic rather than silently
    /// demoting meaningful content to an artifact.
    /// </remarks>
    private void DrawAsArtifactFallback(Action draw, Action onAutoArtifact = null)
    {
        bool needsFallbackArtifact = _tagging != null && _openMarkedContent == 0 && _artifactDepth == 0;
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
