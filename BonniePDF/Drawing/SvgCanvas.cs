using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Export;
using BonniePDF.Rendering.Images;
using BonniePDF.Typography;
using BonniePDF.Typography.TrueType;

namespace BonniePDF.Drawing;

/// <summary>
/// SVG implementation of ICanvas that adapts ICanvas calls to SVG tags.
/// Unlike PdfCanvas, this passes coordinates through unchanged since SVG also uses
/// Screen Coordinates (Top-Left origin).
/// In multipage mode, each page is wrapped in a group with id="pageN" and
/// translated vertically to stack pages.
/// </summary>
public class SvgCanvas : ICanvas
{
    private readonly StringBuilder _sb;
    private readonly float _xOffset;
    private readonly float _yOffset;
    private int _stateDepth;
    private readonly Func<string, TtfFont?>? _fontResolver;

    /// <summary>
    /// Process-wide counter for generating unique <c>&lt;clipPath&gt;</c> ids. Static so
    /// ids stay unique even when several canvases compose into one shared buffer
    /// (page export writes positioned child canvases into the same StringBuilder).
    /// </summary>
    private static int _clipIdCounter;

    /// <summary>Process-wide counter for unique gradient definition ids.</summary>
    private static int _gradientIdCounter;

    /// <summary>Accumulated SVG path data segments for the current path.</summary>
    private readonly List<string> _currentPathData = new();

    /// <summary>Number of extra &lt;g&gt; groups opened by transform/clip/alpha since the last SaveState.</summary>
    private int _extraGroupDepth;

    /// <summary>Stack of extra group depths at each SaveState call.</summary>
    private readonly Stack<int> _extraGroupStack = new();

    /// <summary>
    /// Creates a new SvgCanvas for generating SVG markup.
    /// </summary>
    /// <param name="width">Width of the SVG canvas</param>
    /// <param name="height">Height of the SVG canvas</param>
    /// <param name="xOffset">X offset to apply to all drawing operations</param>
    /// <param name="yOffset">Y offset to apply to all drawing operations</param>
    /// <param name="existingSb">Optional existing StringBuilder to write to</param>
    /// <param name="includeHeader">Whether to write the SVG header (default true if creating new SB, false if using existing)</param>
    public SvgCanvas(float width, float height, float xOffset = 0, float yOffset = 0, StringBuilder? existingSb = null,
        bool? includeHeader = null, Func<string, TtfFont?>? fontResolver = null)
        : this(width, height, xOffset, yOffset, existingSb, includeHeader, 0, false, fontResolver)
    {
    }

    /// <summary>
    /// Creates a new SvgCanvas for generating SVG markup with multipage support.
    /// </summary>
    /// <param name="width">Width of the SVG canvas</param>
    /// <param name="height">Height of the SVG canvas</param>
    /// <param name="xOffset">X offset to apply to all drawing operations</param>
    /// <param name="yOffset">Y offset to apply to all drawing operations</param>
    /// <param name="existingSb">Optional existing StringBuilder to write to</param>
    /// <param name="includeHeader">Whether to write the SVG header (default true if creating new SB, false if using existing)</param>
    /// <param name="pageNumber">Page number (0-based) for multipage mode</param>
    /// <param name="isMultiPageMode">Whether this canvas is part of a multipage document</param>
    public SvgCanvas(float width, float height, float xOffset, float yOffset, StringBuilder? existingSb,
        bool? includeHeader, int pageNumber, bool isMultiPageMode, Func<string, TtfFont?>? fontResolver = null)
    {
        Width = width;
        Height = height;
        _xOffset = xOffset;
        _yOffset = yOffset;
        PageNumber = pageNumber;
        IsMultiPageMode = isMultiPageMode;
        _fontResolver = fontResolver;

        _sb = existingSb ?? new StringBuilder();

        // If includeHeader is explicitly set, use it.
        // Otherwise, default to true if we created the SB, false if we appended to existing.
        bool shouldWriteHeader = includeHeader ?? existingSb == null;

        if (shouldWriteHeader)
            _sb.AppendLf(
                $"<svg width=\"{Inv(width)}\" height=\"{Inv(height)}\" xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\">");

        // In multipage mode, open a group for this page with vertical translation
        if (IsMultiPageMode)
        {
            float translateY = PageNumber * height;
            _sb.AppendLf($"<g id=\"page{PageNumber + 1}\" transform=\"translate(0, {Inv(translateY)})\">");
        }
    }

    /// <summary>
    /// Low-level rendering options (coordinate precision, PDF baseline parity,
    /// image embedding mode). Defaults to <see cref="SvgRenderOptions.Legacy"/>,
    /// which reproduces the original byte-for-byte output used by chart export.
    /// </summary>
    public SvgRenderOptions Options { get; set; } = SvgRenderOptions.Legacy;

    /// <summary>
    /// Optional sink invoked for every (font name, embedded face, code point) the
    /// glyph-positioned text path draws, so the page exporter can embed a minimal
    /// subset (only used glyphs) of each font as an <c>@font-face</c>. Set by
    /// <see cref="Rendering.RenderContext"/>.
    /// </summary>
    public System.Action<string, TtfFont, int>? FontUsageSink { get; set; }

    /// <summary>
    /// Optional callback that registers a font (by name/path) so it can then be
    /// resolved to an embedded face. The SVG export sets this to the render
    /// context's font registrar; without it, embedded fonts referenced only in
    /// SVG output would never be loaded. Set by <see cref="Rendering.RenderContext"/>.
    /// </summary>
    public System.Func<string, string>? RegisterFontCallback { get; set; }

    /// <summary>Registers (if needed) and resolves an embedded TTF for a font name.</summary>
    private TtfFont? ResolveFont(string fontFamily)
    {
        RegisterFontCallback?.Invoke(fontFamily);
        return _fontResolver?.Invoke(fontFamily);
    }

    /// <summary>Records every code point of <paramref name="text"/> as used by the
    /// embedded <paramref name="font"/>, for building a minimal subset.</summary>
    private void RecordEmbeddedUsage(string fontFamily, TtfFont font, string text)
    {
        if (FontUsageSink == null) return;
        foreach (Rune rune in text.EnumerateRunes())
        {
            if (Rune.IsControl(rune)) continue;
            FontUsageSink(fontFamily, font, rune.Value);
        }
    }

    /// <summary>
    /// Formats a coordinate. When <see cref="SvgRenderOptions.Precision"/> is set
    /// the value is rounded to that many decimals (invariant, trailing zeros
    /// trimmed) for compact, deterministic output; otherwise it is emitted
    /// verbatim to preserve the legacy chart golden output.
    /// </summary>
    private string N(float v) => Options.Precision is int p
        ? Math.Round((double)v, p).ToString(CultureInfo.InvariantCulture)
        // SVG/XML numeric values must use '.' as the decimal separator regardless
        // of the host locale. Always format invariantly - emitting CurrentCulture
        // here produced malformed output (e.g. "595,276") on comma-decimal locales.
        : v.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Computes the text baseline Y. PDF-parity mode places it a full em below
    /// the top of the line box (matching PdfCanvas); the legacy chart path keeps
    /// the historical 0.8 approximation so its golden output stays stable.
    /// </summary>
    private float BaselineY(float topY, float fontSize)
        => Options.PdfBaseline ? topY + fontSize : topY + fontSize * 0.8f;

    /// <summary>
    /// Locale-safe number formatting for SVG output (always '.' decimal separator).
    /// </summary>
    private static string Inv(float v) => v.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a per-glyph <c>&lt;tspan&gt;</c> coordinate. Honours configured
    /// precision; otherwise falls back to the historical two-decimal format.
    /// </summary>
    private string Tspan(float v) => Options.Precision is int p
        ? Math.Round((double)v, p).ToString(CultureInfo.InvariantCulture)
        : v.ToString("F2", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the page number (0-based) for multipage mode.
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets whether this canvas is in multipage mode.
    /// </summary>
    public bool IsMultiPageMode { get; }

    /// <summary>
    /// Gets the width of the canvas
    /// </summary>
    public float Width { get; }

    /// <summary>
    /// Gets the height of the canvas
    /// </summary>
    public float Height { get; }

    /// <summary>
    /// Pushes graphics state (SVG: &lt;g&gt;)
    /// </summary>
    public void SaveState()
    {
        _extraGroupStack.Push(_extraGroupDepth);
        _extraGroupDepth = 0;
        _sb.AppendLf("<g>");
        _stateDepth++;
    }

    /// <summary>
    /// Pops graphics state (SVG: &lt;/g&gt;)
    /// </summary>
    public void RestoreState()
    {
        if (_stateDepth > 0)
        {
            // Close any extra groups opened by transforms/clips/alpha since SaveState
            while (_extraGroupDepth > 0)
            {
                _sb.AppendLf("</g>");
                _extraGroupDepth--;
            }
            _sb.AppendLf("</g>");
            _stateDepth--;
            if (_extraGroupStack.Count > 0)
                _extraGroupDepth = _extraGroupStack.Pop();
        }
    }

    /// <summary>
    /// Draws a filled rectangle.
    /// No coordinate transformation needed for SVG.
    /// </summary>
    public void FillRect(float x, float y, float width, float height, Color color)
    {
        float sx = x + _xOffset;
        float sy = y + _yOffset;
        _sb.AppendLf($"<rect x=\"{N(sx)}\" y=\"{N(sy)}\" width=\"{N(width)}\" height=\"{N(height)}\" {GetFillAttributes(color)}/>");
    }

    /// <summary>
    /// Draws a stroked rectangle (border).
    /// No coordinate transformation needed for SVG.
    /// </summary>
    public void StrokeRect(float x, float y, float width, float height, Color color, float strokeWidth)
    {
        float sx = x + _xOffset;
        float sy = y + _yOffset;
        _sb.AppendLf(
            $"<rect x=\"{N(sx)}\" y=\"{N(sy)}\" width=\"{N(width)}\" height=\"{N(height)}\" fill=\"none\" {GetStrokeAttributes(color)} stroke-width=\"{N(strokeWidth)}\"/>");
    }

    /// <summary>
    /// Draws a filled rounded rectangle.
    /// </summary>
    public void FillRoundedRect(float x, float y, float width, float height, float radius, Color color)
    {
        float sx = x + _xOffset;
        float sy = y + _yOffset;
        _sb.AppendLf($"<rect x=\"{N(sx)}\" y=\"{N(sy)}\" width=\"{N(width)}\" height=\"{N(height)}\" rx=\"{N(radius)}\" ry=\"{N(radius)}\" {GetFillAttributes(color)}/>");
    }

    /// <summary>
    /// Draws a stroked rounded rectangle (border).
    /// </summary>
    public void StrokeRoundedRect(float x, float y, float width, float height, float radius, Color color,
        float strokeWidth)
    {
        float sx = x + _xOffset;
        float sy = y + _yOffset;
        _sb.AppendLf(
            $"<rect x=\"{N(sx)}\" y=\"{N(sy)}\" width=\"{N(width)}\" height=\"{N(height)}\" rx=\"{N(radius)}\" ry=\"{N(radius)}\" fill=\"none\" {GetStrokeAttributes(color)} stroke-width=\"{N(strokeWidth)}\"/>");
    }

    /// <summary>
    /// Draws a line between two points.
    /// No coordinate transformation needed for SVG.
    /// </summary>
    public void DrawLine(float x1, float y1, float x2, float y2, Color color, float lineWidth)
    {
        float sx1 = x1 + _xOffset;
        float sy1 = y1 + _yOffset;
        float sx2 = x2 + _xOffset;
        float sy2 = y2 + _yOffset;
        _sb.AppendLf(
            $"<line x1=\"{N(sx1)}\" y1=\"{N(sy1)}\" x2=\"{N(sx2)}\" y2=\"{N(sy2)}\" {GetStrokeAttributes(color)} stroke-width=\"{N(lineWidth)}\"/>");
    }

    /// <summary>
    /// Draws a filled circle.
    /// No coordinate transformation needed for SVG.
    /// </summary>
    public void FillCircle(float cx, float cy, float radius, Color color)
    {
        float scx = cx + _xOffset;
        float scy = cy + _yOffset;
        _sb.AppendLf($"<circle cx=\"{N(scx)}\" cy=\"{N(scy)}\" r=\"{N(radius)}\" {GetFillAttributes(color)}/>");
    }

    /// <summary>
    /// Draws a filled ellipse.
    /// No coordinate transformation needed for SVG.
    /// </summary>
    public void FillEllipse(float cx, float cy, float rx, float ry, Color color)
    {
        float scx = cx + _xOffset;
        float scy = cy + _yOffset;
        _sb.AppendLf($"<ellipse cx=\"{N(scx)}\" cy=\"{N(scy)}\" rx=\"{N(rx)}\" ry=\"{N(ry)}\" {GetFillAttributes(color)}/>");
    }

    /// <summary>
    /// Fills a closed path defined by a series of points.
    /// No coordinate transformation needed for SVG.
    /// </summary>
    public void FillPath(IList<Point> points, Color color)
    {
        if (points == null || points.Count < 3) return;

        StringBuilder pathData = new();

        // Move to the first point
        var first = points[0];
        pathData.Append($"M {N(first.X + _xOffset)} {N(first.Y + _yOffset)}");

        // Draw lines to subsequent points
        for (int i = 1; i < points.Count; i++)
        {
            Point p = points[i];
            pathData.Append($" L {N(p.X + _xOffset)} {N(p.Y + _yOffset)}");
        }

        // Close the path
        pathData.Append(" Z");

        _sb.AppendLf($"<path d=\"{pathData}\" {GetFillAttributes(color)}/>");
    }

    /// <summary>
    /// Draws text at the specified location.
    /// When an embedded TTF font is available via the font resolver, emits one &lt;tspan&gt;
    /// per glyph with explicit x coordinates derived from actual advance widths and kern pairs,
    /// producing layout that matches the PDF output precisely.
    /// Falls back to a single &lt;text&gt; element (browser-rendered) for standard fonts.
    /// </summary>
    public void DrawText(float x, float y, string text, Color color, float fontSize, string fontFamily,
        TextAlignment align)
    {
        if (string.IsNullOrEmpty(text)) return;

        float sx = x + _xOffset;
        float sy = y + _yOffset;
        string fill = color.ToHexString();
        float baselineY = BaselineY(sy, fontSize);

        TtfFont? ttfFont = ResolveFont(fontFamily);

        if (ttfFont != null)
        {
            float totalWidth = FontMetrics.GetKernedStringWidth(text, ttfFont, fontSize);
            float cursorX = sx;
            if (align == TextAlignment.Center)
                cursorX -= totalWidth / 2f;
            else if (align == TextAlignment.Right)
                cursorX -= totalWidth;

            float scale = fontSize / ttfFont.UnitsPerEm;

            // Reference the private embedded-face name (bound by the exporter's
            // @font-face) so the viewer renders the exact font used for layout.
            _sb.Append($"<text font-family=\"{EmbeddedFontFamily(fontFamily)}\" font-size=\"{N(fontSize)}\" fill=\"{fill}\" xml:space=\"preserve\">");

            ushort prevGid = 0;
            bool hasPrev = false;

            foreach (Rune rune in text.EnumerateRunes())
            {
                if (Rune.IsControl(rune)) continue;

                ushort gid = ttfFont.GetGlyphId(rune.Value);
                FontUsageSink?.Invoke(fontFamily, ttfFont, rune.Value);

                if (hasPrev && ttfFont.HasKerning)
                {
                    short kern = ttfFont.GetKerning(prevGid, gid);
                    cursorX += kern * scale;
                }

                string charStr = XmlEscape.EscapeXml(rune.ToString());
                _sb.Append($"<tspan x=\"{Tspan(cursorX)}\" y=\"{Tspan(baselineY)}\">{charStr}</tspan>");

                cursorX += ttfFont.GetGlyphWidth(gid) * scale;
                prevGid = gid;
                hasPrev = true;
            }

            _sb.AppendLf("</text>");
        }
        else
        {
            // Fallback: no embedded TTF available - rely on browser/viewer rendering
            string anchor = GetTextAnchor(align);
            string escapedText = XmlEscape.EscapeXml(text);
            _sb.AppendLf(
                $"<text x=\"{N(sx)}\" y=\"{N(baselineY)}\" {StandardFontCssAttrs(fontFamily)} font-size=\"{N(fontSize)}\" fill=\"{fill}\" text-anchor=\"{anchor}\">{escapedText}</text>");
        }
    }

    /// <summary>
    /// Emits a placeholder text node for total pages. The page SVG exporter
    /// replaces the node content once the full document page count is known.
    /// </summary>
    public void DrawDeferredTotalPages(float x, float y, Color color, float fontSize, string fontFamily)
    {
        const string placeholder = "00";
        float sx = x + _xOffset;
        float sy = y + _yOffset;
        string fill = color.ToHexString();
        float baselineY = BaselineY(sy, fontSize);

        TtfFont? ttfFont = ResolveFont(fontFamily);
        string fontAttrs;
        if (ttfFont != null)
        {
            for (int cp = '0'; cp <= '9'; cp++)
                FontUsageSink?.Invoke(fontFamily, ttfFont, cp);
            fontAttrs = $"font-family=\"{EmbeddedFontFamily(fontFamily)}\"";
        }
        else
        {
            fontAttrs = StandardFontCssAttrs(fontFamily);
        }

        _sb.AppendLf(
            $"<text x=\"{N(sx)}\" y=\"{N(baselineY)}\" {fontAttrs} font-size=\"{N(fontSize)}\" fill=\"{fill}\" text-anchor=\"start\" data-bonniepdf-total-pages=\"true\">{placeholder}</text>");
    }

    /// <summary>Resolves deferred total-page placeholders in already-rendered SVG markup.</summary>
    public static string ResolveTotalPages(string svg, int totalPages)
    {
        const string marker = "data-bonniepdf-total-pages=\"true\"";
        string value = XmlEscape.EscapeXml(totalPages.ToString(CultureInfo.InvariantCulture));
        StringBuilder? result = null;
        int searchStart = 0;

        while (true)
        {
            int markerIndex = svg.IndexOf(marker, searchStart, StringComparison.Ordinal);
            if (markerIndex < 0) break;

            int contentStart = svg.IndexOf('>', markerIndex);
            if (contentStart < 0) break;

            int contentEnd = svg.IndexOf("</text>", contentStart + 1, StringComparison.Ordinal);
            if (contentEnd < 0) break;

            result ??= new StringBuilder(svg.Length);
            result.Append(svg, searchStart, contentStart + 1 - searchStart);
            result.Append(value);
            searchStart = contentEnd;
        }

        if (result == null) return svg;
        result.Append(svg, searchStart, svg.Length - searchStart);
        return result.ToString();
    }

    /// <summary>
    /// Draws rotated text at the specified location.
    /// Uses SVG transform attribute to rotate text.
    /// </summary>
    public void DrawTextRotated(float x, float y, string text, Color color, float fontSize, string fontFamily, float angleDegrees)
    {
        float sx = x + _xOffset;
        float sy = y + _yOffset;
        string fill = color.ToHexString();
        string escapedText = XmlEscape.EscapeXml(text);
        float baselineY = sy + fontSize;

        // An embedded font renders via the private @font-face name; a standard-14
        // font is viewer-supplied (generic stack + weight/style).
        TtfFont? ttfFont = ResolveFont(fontFamily);
        string fontAttrs;
        if (ttfFont != null)
        {
            RecordEmbeddedUsage(fontFamily, ttfFont, text);
            fontAttrs = $"font-family=\"{EmbeddedFontFamily(fontFamily)}\"";
        }
        else
        {
            fontAttrs = StandardFontCssAttrs(fontFamily);
        }

        _sb.AppendLf(
            $"<text x=\"{N(sx)}\" y=\"{N(baselineY)}\" {fontAttrs} font-size=\"{N(fontSize)}\" fill=\"{fill}\" text-anchor=\"start\" transform=\"rotate({N(angleDegrees)}, {N(sx)}, {N(baselineY)})\">{escapedText}</text>");
    }

    /// <summary>
    /// Draws justified text by distributing extra space across word gaps.
    /// Uses SVG word-spacing attribute.
    /// </summary>
    public void DrawTextJustified(float x, float y, string text, Color color, float fontSize, string fontFamily,
        float availableWidth)
    {
        // Count spaces
        int spaceCount = 0;
        foreach (char c in text)
        {
            if (c == ' ') spaceCount++;
        }

        if (spaceCount == 0)
        {
            DrawText(x, y, text, color, fontSize, fontFamily, TextAlignment.Left);
            return;
        }

        float textWidth = FontMetrics.GetStringWidth(text, fontFamily, fontSize);
        float extraSpace = availableWidth - textWidth;
        float wordSpacing = extraSpace / spaceCount;

        float sx = x + _xOffset;
        float sy = y + _yOffset;
        string fill = color.ToHexString();
        string escapedText = XmlEscape.EscapeXml(text);
        float baselineY = sy + fontSize * 0.8f;

        // An embedded font renders via the private @font-face name; a standard-14
        // font is viewer-supplied (generic stack + weight/style).
        TtfFont? ttfFont = ResolveFont(fontFamily);
        string fontAttrs;
        if (ttfFont != null)
        {
            RecordEmbeddedUsage(fontFamily, ttfFont, text);
            fontAttrs = $"font-family=\"{EmbeddedFontFamily(fontFamily)}\"";
        }
        else
        {
            fontAttrs = StandardFontCssAttrs(fontFamily);
        }

        _sb.AppendLf(
            $"<text x=\"{sx}\" y=\"{baselineY}\" {fontAttrs} font-size=\"{fontSize}\" fill=\"{fill}\" text-anchor=\"start\" word-spacing=\"{wordSpacing:F2}\">{escapedText}</text>");
    }

    /// <summary>
    /// Measures text size.
    /// Uses per-glyph kerned metrics from the embedded TTF when available,
    /// otherwise falls back to FontMetrics approximate widths.
    /// </summary>
    public Size MeasureText(string text, float fontSize, string fontFamily)
    {
        TtfFont? ttfFont = _fontResolver?.Invoke(fontFamily);
        float width = ttfFont != null
            ? FontMetrics.GetKernedStringWidth(text, ttfFont, fontSize)
            : FontMetrics.GetStringWidth(text, fontFamily, fontSize);
        float height = fontSize * 1.2f;
        return new Size(width, height);
    }

    /// <summary>
    /// Draws an image at the specified location.
    /// Embeds image data as base64.
    /// </summary>
    public void DrawImage(ImageInfo image, float x, float y, float width, float height)
    {
        if (Options.EmbedImages == SvgImageEmbedding.Omit) return;
        if (image == null) return;

        float sx = x + _xOffset;
        float sy = y + _yOffset;

        string mimeType = image.Format == ImageFormat.Png ? "image/png" : "image/jpeg";
        byte[] bytes = image.OriginalData is { Length: > 0 } ? image.OriginalData : image.Data;
        if (bytes == null || bytes.Length == 0) return;
        string dataUri = $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";

        int orientation = image.Orientation;
        string? transform = SvgOrientationTransform(orientation, sx, sy, width, height);
        if (transform != null)
        {
            // Orientations 5-8 (90 degree/270 degree rotations) swap the box
            // the image is drawn into before the matrix rotates it back.
            (float bw, float bh) = orientation is >= 5 and <= 8 ? (height, width) : (width, height);
            _sb.AppendLf(
                $"<g transform=\"{transform}\"><image width=\"{N(bw)}\" height=\"{N(bh)}\" href=\"{dataUri}\" preserveAspectRatio=\"none\"/></g>");
            return;
        }

        _sb.AppendLf(
            $"<image x=\"{N(sx)}\" y=\"{N(sy)}\" width=\"{N(width)}\" height=\"{N(height)}\" href=\"{dataUri}\" preserveAspectRatio=\"none\"/>");
    }

    /// <summary>
    /// Maps an EXIF orientation (1-8) to an SVG matrix transform placing the
    /// image upright at (<paramref name="x"/>, <paramref name="y"/>). Returns
    /// null for orientation 1, where the plain positioned &lt;image&gt; is used.
    /// </summary>
    private string? SvgOrientationTransform(int orientation, float x, float y, float width, float height)
    {
        string Matrix(float a, float b, float c, float d, float e, float f)
            => $"matrix({N(a)} {N(b)} {N(c)} {N(d)} {N(e + x)} {N(f + y)})";

        return orientation switch
        {
            2 => Matrix(-1, 0, 0, 1, width, 0),
            3 => Matrix(-1, 0, 0, -1, width, height),
            4 => Matrix(1, 0, 0, -1, 0, height),
            5 => Matrix(0, 1, 1, 0, 0, 0),
            6 => Matrix(0, 1, -1, 0, width, 0),
            7 => Matrix(0, -1, -1, 0, width, height),
            8 => Matrix(0, -1, 1, 0, 0, height),
            _ => null
        };
    }

    /// <summary>
    /// Draws a Form XObject (external object reference) at the specified location.
    /// Not supported in SVG - currently does nothing.
    /// </summary>
    public void DrawFormXObject(string resourceName, float x, float y)
    {
        // SVG does not support deferred Form XObjects like PDF.
        // We could render a placeholder here if needed, but for now we ignore it.
    }

    /// <inheritdoc />
    public void BeginMarkedContent(string role)
    {
        // SVG output doesn't carry PDF structure tags. Tagging is a no-op
        // on this canvas; tagged-PDF emission is PdfCanvas-only.
    }

    /// <inheritdoc />
    public void BeginContainerContent(string role) { } // no-op for SVG output

    /// <inheritdoc />
    public void EndMarkedContent()
    {
        // See BeginMarkedContent - no-op for SVG output.
    }

    /// <inheritdoc />
    public void BeginArtifact() { } // no-op for SVG output

    /// <inheritdoc />
    public void EndArtifact() { } // no-op for SVG output

    /// <inheritdoc />
    public void BeginCrispEdges() => _sb.AppendLf("<g shape-rendering=\"crispEdges\">");

    /// <inheritdoc />
    public void EndCrispEdges() => _sb.AppendLf("</g>");

    /// <inheritdoc />
    public void BeginRoundedClip(float x, float y, float width, float height, float radius)
    {
        float sx = x + _xOffset;
        float sy = y + _yOffset;
        string id = $"clip{_clipIdCounter++}";
        _sb.AppendLf(
            $"<clipPath id=\"{id}\"><rect x=\"{N(sx)}\" y=\"{N(sy)}\" width=\"{N(width)}\" height=\"{N(height)}\" rx=\"{N(radius)}\" ry=\"{N(radius)}\"/></clipPath>");
        _sb.AppendLf($"<g clip-path=\"url(#{id})\">");
        _stateDepth++;
    }

    /// <inheritdoc />
    public void EndClip()
    {
        if (_stateDepth > 0)
        {
            _sb.AppendLf("</g>");
            _stateDepth--;
        }
    }

    // --- 6. Path building ---

    /// <inheritdoc />
    public void BeginPath()
    {
        _currentPathData.Clear();
    }

    /// <inheritdoc />
    public void MoveTo(float x, float y)
    {
        _currentPathData.Add($"M {N(x + _xOffset)} {N(y + _yOffset)}");
    }

    /// <inheritdoc />
    public void LineTo(float x, float y)
    {
        _currentPathData.Add($"L {N(x + _xOffset)} {N(y + _yOffset)}");
    }

    /// <inheritdoc />
    public void CubicBezierTo(float cp1x, float cp1y, float cp2x, float cp2y, float x, float y)
    {
        _currentPathData.Add(
            $"C {N(cp1x + _xOffset)} {N(cp1y + _yOffset)} {N(cp2x + _xOffset)} {N(cp2y + _yOffset)} {N(x + _xOffset)} {N(y + _yOffset)}");
    }

    /// <inheritdoc />
    public void QuadraticBezierTo(float cpx, float cpy, float x, float y)
    {
        _currentPathData.Add(
            $"Q {N(cpx + _xOffset)} {N(cpy + _yOffset)} {N(x + _xOffset)} {N(y + _yOffset)}");
    }

    /// <inheritdoc />
    public void AddRect(float x, float y, float width, float height)
    {
        float sx = x + _xOffset;
        float sy = y + _yOffset;
        _currentPathData.Add($"M {N(sx)} {N(sy)} L {N(sx + width)} {N(sy)} L {N(sx + width)} {N(sy + height)} L {N(sx)} {N(sy + height)} Z");
    }

    /// <inheritdoc />
    public void ClosePath()
    {
        _currentPathData.Add("Z");
    }

    /// <inheritdoc />
    public void FillCurrentPath(Paint paint, FillRule fillRule = FillRule.NonZero)
    {
        if (_currentPathData.Count == 0) return;
        string d = string.Join(" ", _currentPathData);
        string rule = fillRule == FillRule.EvenOdd ? " fill-rule=\"evenodd\"" : "";

        if (paint is SolidPaint solid)
        {
            _sb.AppendLf($"<path d=\"{d}\" {GetFillAttributes(solid.Color)}{rule}/>");
        }
        else if (paint is LinearGradientPaint linear)
        {
            string gradId = EmitLinearGradientDef(linear);
            _sb.AppendLf($"<path d=\"{d}\" fill=\"url(#{gradId})\"{rule}/>");
        }
        else if (paint is RadialGradientPaint radial)
        {
            string gradId = EmitRadialGradientDef(radial);
            _sb.AppendLf($"<path d=\"{d}\" fill=\"url(#{gradId})\"{rule}/>");
        }

        _currentPathData.Clear();
    }

    /// <inheritdoc />
    public void StrokeCurrentPath(StrokeStyle style)
    {
        if (_currentPathData.Count == 0) return;
        string d = string.Join(" ", _currentPathData);
        string strokeAttrs = GetStrokeStyleAttributes(style);

        if (style.Paint is SolidPaint solid)
        {
            _sb.AppendLf($"<path d=\"{d}\" fill=\"none\" {GetStrokeAttributes(solid.Color)} {strokeAttrs}/>");
        }
        else if (style.Paint is LinearGradientPaint linear)
        {
            string gradId = EmitLinearGradientDef(linear);
            _sb.AppendLf($"<path d=\"{d}\" fill=\"none\" stroke=\"url(#{gradId})\" {strokeAttrs}/>");
        }
        else if (style.Paint is RadialGradientPaint radial)
        {
            string gradId = EmitRadialGradientDef(radial);
            _sb.AppendLf($"<path d=\"{d}\" fill=\"none\" stroke=\"url(#{gradId})\" {strokeAttrs}/>");
        }

        _currentPathData.Clear();
    }

    // --- 7. Transforms ---

    /// <inheritdoc />
    public void Translate(float dx, float dy)
    {
        _sb.AppendLf($"<g transform=\"translate({N(dx)}, {N(dy)})\">");
        _extraGroupDepth++;
    }

    /// <inheritdoc />
    public void Scale(float sx, float sy)
    {
        _sb.AppendLf($"<g transform=\"scale({N(sx)}, {N(sy)})\">");
        _extraGroupDepth++;
    }

    /// <inheritdoc />
    public void Rotate(float angleDegrees)
    {
        _sb.AppendLf($"<g transform=\"rotate({N(angleDegrees)})\">");
        _extraGroupDepth++;
    }

    /// <inheritdoc />
    public void Transform(float a, float b, float c, float d, float tx, float ty)
    {
        _sb.AppendLf($"<g transform=\"matrix({N(a)}, {N(b)}, {N(c)}, {N(d)}, {N(tx)}, {N(ty)})\">");
        _extraGroupDepth++;
    }

    // --- 8. Clipping & Alpha ---

    /// <inheritdoc />
    public void ClipToCurrentPath(FillRule fillRule = FillRule.NonZero)
    {
        if (_currentPathData.Count == 0) return;
        string d = string.Join(" ", _currentPathData);
        string id = $"clip{_clipIdCounter++}";
        string rule = fillRule == FillRule.EvenOdd ? " clip-rule=\"evenodd\"" : "";
        _sb.AppendLf($"<clipPath id=\"{id}\"><path d=\"{d}\"{rule}/></clipPath>");
        _sb.AppendLf($"<g clip-path=\"url(#{id})\">");
        _extraGroupDepth++;
        _currentPathData.Clear();
    }

    /// <inheritdoc />
    public void SetGlobalAlpha(float alpha)
    {
        alpha = Math.Clamp(alpha, 0f, 1f);
        if (alpha < 1f)
        {
            _sb.AppendLf($"<g opacity=\"{FormatOpacity(alpha)}\">");
            _extraGroupDepth++;
        }
    }

    // --- SVG gradient/stroke helpers ---

    /// <summary>Emits a &lt;linearGradient&gt; definition and returns its id.</summary>
    private string EmitLinearGradientDef(LinearGradientPaint gradient)
    {
        string id = $"grad{_gradientIdCounter++}";
        _sb.Append($"<defs><linearGradient id=\"{id}\" x1=\"{N(gradient.Start.X + _xOffset)}\" y1=\"{N(gradient.Start.Y + _yOffset)}\" x2=\"{N(gradient.End.X + _xOffset)}\" y2=\"{N(gradient.End.Y + _yOffset)}\" gradientUnits=\"userSpaceOnUse\">");
        foreach (GradientStop stop in gradient.Stops)
        {
            string offset = N(stop.Offset * 100f);
            string color = stop.Color.ToHexString();
            if (stop.Color.ScA < 1f)
                _sb.Append($"<stop offset=\"{offset}%\" stop-color=\"{color}\" stop-opacity=\"{FormatOpacity(stop.Color.ScA)}\"/>");
            else
                _sb.Append($"<stop offset=\"{offset}%\" stop-color=\"{color}\"/>");
        }
        _sb.AppendLf("</linearGradient></defs>");
        return id;
    }

    /// <summary>Emits a &lt;radialGradient&gt; definition and returns its id.</summary>
    private string EmitRadialGradientDef(RadialGradientPaint gradient)
    {
        string id = $"grad{_gradientIdCounter++}";
        float cx = gradient.Center.X + _xOffset;
        float cy = gradient.Center.Y + _yOffset;
        string focalAttrs = "";
        if (gradient.Focal.HasValue)
        {
            focalAttrs = $" fx=\"{N(gradient.Focal.Value.X + _xOffset)}\" fy=\"{N(gradient.Focal.Value.Y + _yOffset)}\"";
        }
        _sb.Append($"<defs><radialGradient id=\"{id}\" cx=\"{N(cx)}\" cy=\"{N(cy)}\" r=\"{N(gradient.Radius)}\"{focalAttrs} gradientUnits=\"userSpaceOnUse\">");
        foreach (GradientStop stop in gradient.Stops)
        {
            string offset = N(stop.Offset * 100f);
            string color = stop.Color.ToHexString();
            if (stop.Color.ScA < 1f)
                _sb.Append($"<stop offset=\"{offset}%\" stop-color=\"{color}\" stop-opacity=\"{FormatOpacity(stop.Color.ScA)}\"/>");
            else
                _sb.Append($"<stop offset=\"{offset}%\" stop-color=\"{color}\"/>");
        }
        _sb.AppendLf("</radialGradient></defs>");
        return id;
    }

    /// <summary>Builds SVG stroke attribute string from a StrokeStyle.</summary>
    private string GetStrokeStyleAttributes(StrokeStyle style)
    {
        var attrs = new StringBuilder();
        attrs.Append($"stroke-width=\"{N(style.Width)}\"");

        if (style.LineCap != LineCap.Butt)
            attrs.Append($" stroke-linecap=\"{style.LineCap.ToString().ToLowerInvariant()}\"");

        if (style.LineJoin != LineJoin.Miter)
            attrs.Append($" stroke-linejoin=\"{style.LineJoin.ToString().ToLowerInvariant()}\"");

        if (style.MiterLimit != 10f)
            attrs.Append($" stroke-miterlimit=\"{N(style.MiterLimit)}\"");

        if (style.DashPattern is { Length: > 0 })
        {
            attrs.Append($" stroke-dasharray=\"{string.Join(" ", System.Array.ConvertAll(style.DashPattern, p => N(p)))}\"");
            if (style.DashOffset != 0)
                attrs.Append($" stroke-dashoffset=\"{N(style.DashOffset)}\"");
        }

        return attrs.ToString();
    }

    /// <summary>
    /// Closes the page group in multipage mode.
    /// Should be called before GetSvg() or when switching pages.
    /// </summary>
    public void ClosePageGroup()
    {
        if (IsMultiPageMode) _sb.AppendLf("</g>");
    }

    /// <summary>
    /// Gets the complete SVG markup as a string.
    /// </summary>
    public string GetSvg()
    {
        // Close any extra groups opened by transforms/clips/alpha
        while (_extraGroupDepth > 0)
        {
            _sb.AppendLf("</g>");
            _extraGroupDepth--;
        }

        // Close any open groups
        for (int i = 0; i < _stateDepth; i++) _sb.AppendLf("</g>");

        // Close the page group in multipage mode
        if (IsMultiPageMode) _sb.AppendLf("</g>");

        // Close SVG document if we created the StringBuilder
        // If we didn't create it, we assume the caller manages the closing tag
        if (_sb.ToString().StartsWith("<svg", StringComparison.OrdinalIgnoreCase))
            // Simple check, robust enough for our usage
            _sb.AppendLf("</svg>");

        return _sb.ToString();
    }

    /// <summary>
    /// Maps TextAlignment to SVG text-anchor attribute.
    /// </summary>
    private static string GetTextAnchor(TextAlignment align)
    {
        return align switch
        {
            TextAlignment.Left => "start",
            TextAlignment.Center => "middle",
            TextAlignment.Right => "end",
            TextAlignment.Middle => "middle",
            TextAlignment.Justify => "start",
            _ => "start"
        };
    }

    /// <summary>
    /// Maps a non-embedded standard-14 PDF font name (e.g. "Helvetica-BoldOblique",
    /// "Times-Italic") to the SVG attributes a viewer needs: a generic family stack
    /// plus explicit <c>font-weight</c> / <c>font-style</c>. The raw PDF name is not
    /// a CSS family, so emitting it verbatim makes browsers fall back to a regular
    /// default - losing bold and italic. The real OS font is preferred and never
    /// overridden. Unrecognised (custom) names pass through unchanged.
    /// </summary>
    private static string StandardFontCssAttrs(string fontFamily)
    {
        string lower = fontFamily.ToLowerInvariant();
        string stack;
        if (lower.StartsWith("times", System.StringComparison.Ordinal))
            stack = "'Times New Roman', Times, serif";
        else if (lower.StartsWith("courier", System.StringComparison.Ordinal))
            stack = "'Courier New', Courier, monospace";
        else if (lower.StartsWith("helvetica", System.StringComparison.Ordinal) || lower.StartsWith("arial", System.StringComparison.Ordinal))
            stack = "Helvetica, Arial, sans-serif";
        else
            return $"font-family=\"{XmlEscape.EscapeAttr(fontFamily)}\"";

        string weight = lower.Contains("bold") ? " font-weight=\"bold\"" : "";
        string style = lower.Contains("oblique") || lower.Contains("italic") ? " font-style=\"italic\"" : "";
        return $"font-family=\"{XmlEscape.EscapeAttr(stack)}\"{weight}{style}";
    }

    /// <summary>
    /// Private CSS family name for an embedded font subset, namespaced ("bpf-…")
    /// so it never collides with or overrides a real installed font. The SVG
    /// references this name; the exporter declares an <c>@font-face</c> for it
    /// carrying the (subsetted) font bytes.
    /// </summary>
    public static string EmbeddedFontFamily(string fontFamily)
    {
        var sb = new StringBuilder("bpf-");
        foreach (char c in fontFamily.ToLowerInvariant())
            sb.Append((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ? c : '-');
        // collapse/trim dashes
        string s = sb.ToString();
        while (s.Contains("--")) s = s.Replace("--", "-");
        return s.Trim('-');
    }

    private static string GetFillAttributes(Color color)
    {
        string fill = color.ToHexString();
        return color.ScA < 1f
            ? $"fill=\"{fill}\" fill-opacity=\"{FormatOpacity(color.ScA)}\""
            : $"fill=\"{fill}\"";
    }

    private static string GetStrokeAttributes(Color color)
    {
        string stroke = color.ToHexString();
        return color.ScA < 1f
            ? $"stroke=\"{stroke}\" stroke-opacity=\"{FormatOpacity(color.ScA)}\""
            : $"stroke=\"{stroke}\"";
    }

    private static string FormatOpacity(float opacity)
        => Math.Round(opacity, 4).ToString("0.####", CultureInfo.InvariantCulture);

}
