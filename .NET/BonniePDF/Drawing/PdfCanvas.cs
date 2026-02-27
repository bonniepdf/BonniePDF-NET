using System;
using System.Collections.Generic;
using System.Text;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering.Images;
using BonniePDF.Rendering.Pdf;
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
    private readonly StringBuilder _stream;
    private readonly float _xOffset;
    private readonly float _yOffset;
    private int _stateDepth;

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
        Action<string, string> glyphTracker = null)
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

        PdfDrawing.FillRect(_stream, pdfX, pdfY, width, height, color);
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

        PdfDrawing.StrokeRect(_stream, pdfX, pdfY, width, height, color, strokeWidth);
    }

    /// <summary>
    /// Draws a filled rounded rectangle.
    /// Converts Top-Left (y) to Bottom-Left (pdfY).
    /// </summary>
    public void FillRoundedRect(float x, float y, float width, float height, float radius, Color color)
    {
        float pdfX = x + _xOffset;
        float pdfY = Height - (y + _yOffset + height);
        PdfDrawing.FillRoundedRect(_stream, pdfX, pdfY, width, height, radius, color);
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
        PdfDrawing.StrokeRoundedRect(_stream, pdfX, pdfY, width, height, radius, color, strokeWidth);
    }

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

        PdfOperators.SetStrokeColor(_stream, color);
        PdfOperators.SetLineWidth(_stream, lineWidth);
        PdfOperators.StrokeLine(_stream, pdfX1, pdfY1, pdfX2, pdfY2);
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

        PdfDrawing.FillCircle(_stream, pdfCx, pdfCy, radius, color);
    }

    /// <summary>
    /// Fills a closed path defined by a series of points.
    /// Converts all points from Top-Left to Bottom-Left coordinates.
    /// </summary>
    public void FillPath(List<Point> points, Color color)
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

        PdfDrawing.FillPath(_stream, pdfPoints, color);
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

        PdfOperators.BeginText(_stream);

        // Set font using the resolver — this MUST happen before glyph tracking
        // because RegisterFont creates the GlyphUsageTracker for embedded fonts.
        string fontResourceName = _fontResolver(fontFamily);

        // Track glyph usage for font subsetting (after font registration)
        _glyphTracker(fontFamily, text);
        PdfOperators.SetFont(_stream, fontResourceName, fontSize);
        PdfOperators.SetFillColor(_stream, color);

        // Calculate X offset based on alignment
        float xOffset = 0;
        if (align == TextAlignment.Center || align == TextAlignment.Right)
        {
            Size textSize = MeasureText(text, fontSize, fontFamily);
            if (align == TextAlignment.Center)
                xOffset = -textSize.Width / 2;
            else if (align == TextAlignment.Right)
                xOffset = -textSize.Width;
        }

        float pdfX = x + _xOffset + xOffset;
        PdfOperators.SetTextPosition(_stream, pdfX, pdfY);

        // Check if this is an embedded font (TTF/OTF)
        TtfFont embeddedFont = _embeddedFontResolver(fontFamily);
        if (embeddedFont != null)
            PdfOperators.ShowText(_stream, PdfSyntax.ToGlyphIdHexString(text, embeddedFont));
        else
            PdfOperators.ShowText(_stream, $"({PdfSyntax.Escape(text)})");

        PdfOperators.EndText(_stream);
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

        // Measure natural text width
        Size textSize = MeasureText(text, fontSize, fontFamily);
        float extraSpace = availableWidth - textSize.Width;
        float wordSpacing = extraSpace / spaceCount;

        // PDF text y is the baseline
        float pdfY = Height - (y + _yOffset + fontSize);
        float pdfX = x + _xOffset;

        PdfOperators.BeginText(_stream);

        string fontResourceName = _fontResolver(fontFamily);
        _glyphTracker(fontFamily, text);
        PdfOperators.SetFont(_stream, fontResourceName, fontSize);
        PdfOperators.SetFillColor(_stream, color);
        PdfOperators.SetWordSpacing(_stream, wordSpacing);
        PdfOperators.SetTextPosition(_stream, pdfX, pdfY);

        TtfFont embeddedFont = _embeddedFontResolver(fontFamily);
        if (embeddedFont != null)
            PdfOperators.ShowText(_stream, PdfSyntax.ToGlyphIdHexString(text, embeddedFont));
        else
            PdfOperators.ShowText(_stream, $"({PdfSyntax.Escape(text)})");

        // Reset word spacing to 0 so it doesn't leak to subsequent text
        PdfOperators.SetWordSpacing(_stream, 0);
        PdfOperators.EndText(_stream);
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

        PdfOperators.SaveState(_stream);
        PdfOperators.Translate(_stream, pdfX, pdfY);
        PdfOperators.Scale(_stream, width, height);
        PdfOperators.PaintXObject(_stream, imageName);
        PdfOperators.RestoreState(_stream);
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

        PdfOperators.SaveState(_stream);
        PdfOperators.Translate(_stream, pdfX, pdfY);
        PdfOperators.PaintXObject(_stream, resourceName);
        PdfOperators.RestoreState(_stream);
    }

    /// <summary>
    /// Measures text size to allow for precise alignment calculations in charts.
    /// Uses FontMetrics to get accurate text width.
    /// </summary>
    public Size MeasureText(string text, float fontSize, string fontFamily)
    {
        // Get the standard font name
        string standardFontName = GetStandardFontName(fontFamily);

        // Measure the text width using FontMetrics
        float width = FontMetrics.GetStringWidth(text, standardFontName, (int)fontSize);

        // Approximate height as font size * 1.2 (line height)
        float height = fontSize * 1.2f;

        return new Size(width, height);
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

    /// <summary>
    /// Maps a font family name to a standard font name for FontMetrics.
    /// </summary>
    private static string GetStandardFontName(string fontFamily)
    {
        // Map common font names to standard font names
        return fontFamily.ToLowerInvariant() switch
        {
            var s when s.Contains("arial") || s.Contains("helvetica") => StandardFonts.Helvetica,
            var s when s.Contains("times") => StandardFonts.TimesRoman,
            var s when s.Contains("courier") || s.Contains("mono") => StandardFonts.Courier,
            _ => StandardFonts.Helvetica
        };
    }
}