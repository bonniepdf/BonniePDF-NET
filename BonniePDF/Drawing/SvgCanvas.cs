using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
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
    private readonly Func<string, TtfFont> _fontResolver;

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
        bool? includeHeader = null, Func<string, TtfFont> fontResolver = null)
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
        bool? includeHeader, int pageNumber, bool isMultiPageMode, Func<string, TtfFont> fontResolver = null)
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
            _sb.AppendLine(
                $"<svg width=\"{width}\" height=\"{height}\" xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\">");

        // In multipage mode, open a group for this page with vertical translation
        if (IsMultiPageMode)
        {
            float translateY = PageNumber * height;
            _sb.AppendLine($"<g id=\"page{PageNumber + 1}\" transform=\"translate(0, {translateY})\">");
        }
    }

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
        _sb.AppendLine("<g>");
        _stateDepth++;
    }

    /// <summary>
    /// Pops graphics state (SVG: &lt;/g&gt;)
    /// </summary>
    public void RestoreState()
    {
        if (_stateDepth > 0)
        {
            _sb.AppendLine("</g>");
            _stateDepth--;
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
        _sb.AppendLine($"<rect x=\"{sx}\" y=\"{sy}\" width=\"{width}\" height=\"{height}\" {GetFillAttributes(color)}/>");
    }

    /// <summary>
    /// Draws a stroked rectangle (border).
    /// No coordinate transformation needed for SVG.
    /// </summary>
    public void StrokeRect(float x, float y, float width, float height, Color color, float strokeWidth)
    {
        float sx = x + _xOffset;
        float sy = y + _yOffset;
        _sb.AppendLine(
            $"<rect x=\"{sx}\" y=\"{sy}\" width=\"{width}\" height=\"{height}\" fill=\"none\" {GetStrokeAttributes(color)} stroke-width=\"{strokeWidth}\"/>");
    }

    /// <summary>
    /// Draws a filled rounded rectangle.
    /// </summary>
    public void FillRoundedRect(float x, float y, float width, float height, float radius, Color color)
    {
        float sx = x + _xOffset;
        float sy = y + _yOffset;
        _sb.AppendLine($"<rect x=\"{sx}\" y=\"{sy}\" width=\"{width}\" height=\"{height}\" rx=\"{radius}\" ry=\"{radius}\" {GetFillAttributes(color)}/>");
    }

    /// <summary>
    /// Draws a stroked rounded rectangle (border).
    /// </summary>
    public void StrokeRoundedRect(float x, float y, float width, float height, float radius, Color color,
        float strokeWidth)
    {
        float sx = x + _xOffset;
        float sy = y + _yOffset;
        _sb.AppendLine(
            $"<rect x=\"{sx}\" y=\"{sy}\" width=\"{width}\" height=\"{height}\" rx=\"{radius}\" ry=\"{radius}\" fill=\"none\" {GetStrokeAttributes(color)} stroke-width=\"{strokeWidth}\"/>");
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
        _sb.AppendLine(
            $"<line x1=\"{sx1}\" y1=\"{sy1}\" x2=\"{sx2}\" y2=\"{sy2}\" {GetStrokeAttributes(color)} stroke-width=\"{lineWidth}\"/>");
    }

    /// <summary>
    /// Draws a filled circle.
    /// No coordinate transformation needed for SVG.
    /// </summary>
    public void FillCircle(float cx, float cy, float radius, Color color)
    {
        float scx = cx + _xOffset;
        float scy = cy + _yOffset;
        _sb.AppendLine($"<circle cx=\"{scx}\" cy=\"{scy}\" r=\"{radius}\" {GetFillAttributes(color)}/>");
    }

    /// <summary>
    /// Draws a filled ellipse.
    /// No coordinate transformation needed for SVG.
    /// </summary>
    public void FillEllipse(float cx, float cy, float rx, float ry, Color color)
    {
        float scx = cx + _xOffset;
        float scy = cy + _yOffset;
        _sb.AppendLine($"<ellipse cx=\"{scx}\" cy=\"{scy}\" rx=\"{rx}\" ry=\"{ry}\" {GetFillAttributes(color)}/>");
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
        pathData.Append($"M {first.X + _xOffset} {first.Y + _yOffset}");

        // Draw lines to subsequent points
        for (int i = 1; i < points.Count; i++)
        {
            Point p = points[i];
            pathData.Append($" L {p.X + _xOffset} {p.Y + _yOffset}");
        }

        // Close the path
        pathData.Append(" Z");

        _sb.AppendLine($"<path d=\"{pathData}\" {GetFillAttributes(color)}/>");
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
        float baselineY = sy + fontSize * 0.8f;

        TtfFont ttfFont = _fontResolver?.Invoke(fontFamily);

        if (ttfFont != null)
        {
            float totalWidth = FontMetrics.GetKernedStringWidth(text, ttfFont, fontSize);
            float cursorX = sx;
            if (align == TextAlignment.Center)
                cursorX -= totalWidth / 2f;
            else if (align == TextAlignment.Right)
                cursorX -= totalWidth;

            float scale = fontSize / ttfFont.UnitsPerEm;

            _sb.Append($"<text font-family=\"{fontFamily}\" font-size=\"{fontSize}\" fill=\"{fill}\" xml:space=\"preserve\">");

            ushort prevGid = 0;
            bool hasPrev = false;

            foreach (Rune rune in text.EnumerateRunes())
            {
                if (Rune.IsControl(rune)) continue;

                ushort gid = ttfFont.GetGlyphId(rune.Value);

                if (hasPrev && ttfFont.HasKerning)
                {
                    short kern = ttfFont.GetKerning(prevGid, gid);
                    cursorX += kern * scale;
                }

                string charStr = EscapeXml(rune.ToString());
                _sb.Append($"<tspan x=\"{cursorX:F2}\" y=\"{baselineY:F2}\">{charStr}</tspan>");

                cursorX += ttfFont.GetGlyphWidth(gid) * scale;
                prevGid = gid;
                hasPrev = true;
            }

            _sb.AppendLine("</text>");
        }
        else
        {
            // Fallback: no embedded TTF available - rely on browser/viewer rendering
            string anchor = GetTextAnchor(align);
            string escapedText = EscapeXml(text);
            _sb.AppendLine(
                $"<text x=\"{sx}\" y=\"{baselineY}\" font-family=\"{fontFamily}\" font-size=\"{fontSize}\" fill=\"{fill}\" text-anchor=\"{anchor}\">{escapedText}</text>");
        }
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
        string escapedText = EscapeXml(text);
        float baselineY = sy + fontSize;

        _sb.AppendLine(
            $"<text x=\"{sx}\" y=\"{baselineY}\" font-family=\"{fontFamily}\" font-size=\"{fontSize}\" fill=\"{fill}\" text-anchor=\"start\" transform=\"rotate({angleDegrees}, {sx}, {baselineY})\">{escapedText}</text>");
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
        string escapedText = EscapeXml(text);
        float baselineY = sy + fontSize * 0.8f;

        _sb.AppendLine(
            $"<text x=\"{sx}\" y=\"{baselineY}\" font-family=\"{fontFamily}\" font-size=\"{fontSize}\" fill=\"{fill}\" text-anchor=\"start\" word-spacing=\"{wordSpacing:F2}\">{escapedText}</text>");
    }

    /// <summary>
    /// Measures text size.
    /// Uses per-glyph kerned metrics from the embedded TTF when available,
    /// otherwise falls back to FontMetrics approximate widths.
    /// </summary>
    public Size MeasureText(string text, float fontSize, string fontFamily)
    {
        TtfFont ttfFont = _fontResolver?.Invoke(fontFamily);
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
        float sx = x + _xOffset;
        float sy = y + _yOffset;

        string mimeType = image.Format == ImageFormat.Png ? "image/png" : "image/jpeg";
        string base64 = Convert.ToBase64String(image.OriginalData is { Length: > 0 }
            ? image.OriginalData
            : image.Data);
        string dataUri = $"data:{mimeType};base64,{base64}";

        _sb.AppendLine($"<image x=\"{sx}\" y=\"{sy}\" width=\"{width}\" height=\"{height}\" href=\"{dataUri}\"/>");
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

    /// <summary>
    /// Closes the page group in multipage mode.
    /// Should be called before GetSvg() or when switching pages.
    /// </summary>
    public void ClosePageGroup()
    {
        if (IsMultiPageMode) _sb.AppendLine("</g>");
    }

    /// <summary>
    /// Gets the complete SVG markup as a string.
    /// </summary>
    public string GetSvg()
    {
        // Close any open groups
        for (int i = 0; i < _stateDepth; i++) _sb.AppendLine("</g>");

        // Close the page group in multipage mode
        if (IsMultiPageMode) _sb.AppendLine("</g>");

        // Close SVG document if we created the StringBuilder
        // If we didn't create it, we assume the caller manages the closing tag
        if (_sb.ToString().StartsWith("<svg", StringComparison.OrdinalIgnoreCase))
            // Simple check, robust enough for our usage
            _sb.AppendLine("</svg>");

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

    /// <summary>
    /// Escapes special XML characters in text.
    /// </summary>
    private static string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text
            .Replace("&", "\u0026amp;", StringComparison.OrdinalIgnoreCase)
            .Replace("<", "\u0026lt;", StringComparison.OrdinalIgnoreCase)
            .Replace(">", "\u0026gt;", StringComparison.OrdinalIgnoreCase)
            .Replace("\"", "\u0026quot;", StringComparison.OrdinalIgnoreCase)
            .Replace("'", "\u0026apos;", StringComparison.OrdinalIgnoreCase);
    }
}
