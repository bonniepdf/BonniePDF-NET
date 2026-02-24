using System;
using System.Collections.Generic;
using BonniePDF.Elements;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Pdf;
using BonniePDF.Typography;
using BonniePDF.Typography.TrueType;

namespace BonniePDF.Core;

/// <summary>
/// Represents a hyperlink annotation in a PDF document.
/// Hyperlinks are clickable areas that navigate to a URI.
/// This element implements IElement so it can be added like other elements.
/// </summary>
public class Hyperlink : IElement
{
    /// <summary>
    /// Creates a new hyperlink with the specified URI and display text
    /// </summary>
    /// <param name="uri">The URI (URL) that this hyperlink points to</param>
    /// <param name="text">The link text to display</param>
    public Hyperlink(string uri, string text)
    {
        if (string.IsNullOrEmpty(uri))
            throw new ArgumentException("URI cannot be null or empty", nameof(uri));

        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("TextBlock cannot be null or empty", nameof(text));

        Uri = uri;
        Text = text;
    }

    /// <summary>
    /// Gets the URI (URL) that this hyperlink points to
    /// </summary>
    public string Uri { get; }

    /// <summary>
    /// Gets the link text to display
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the font name for the link text
    /// </summary>
    public string FontName { get; set; } = StandardFonts.Helvetica;

    /// <summary>
    /// Gets or sets the font size for the link text
    /// </summary>
    public int FontSize { get; set; } = 12;

    /// <summary>
    /// Gets or sets the text color for the link
    /// </summary>
    public Color TextColor { get; set; } = Color.Blue;

    /// <summary>
    /// Gets or sets the margin around the hyperlink
    /// </summary>
    public Thickness Margin { get; set; } = new(0);

    private float LineHeight => FontSize * 1.2f;

    // Internal properties set during rendering
    internal float RenderX { get; private set; }
    internal float RenderY { get; private set; }
    internal float RenderWidth { get; private set; }
    internal float RenderHeight { get; private set; }

    /// <summary>
    /// Gets the preferred width of this element, if any.
    /// </summary>
    public float GetPreferredWidth()
    {
        return FontMetrics.GetStringWidth(Text, FontName, FontSize) + Margin.Left + Margin.Right;
    }

    /// <summary>
    /// Measures the height of this hyperlink given the available width
    /// </summary>
    public float MeasureHeight(float availableWidth)
    {
        float contentWidth = availableWidth - Margin.Left - Margin.Right;
        float contentHeight = ComputeLineCount(contentWidth) * LineHeight;
        return contentHeight + Margin.Top + Margin.Bottom;
    }

    /// <summary>
    /// Renders this hyperlink to the PDF context and registers the annotation
    /// </summary>
    public void Render(RenderContext ctx, float width)
    {
        // Save starting Y position for correct advancement
        float startY = ctx.CurrentY;

        // Apply top margin
        ctx.CurrentY += Margin.Top;

        // Register font
        string fontResourceName = ctx.RegisterFont(FontName);
        TtfFont embeddedFont = ctx.ResolveEmbeddedFont(FontName);
        bool isEmbedded = embeddedFont != null;

        float contentWidth = width - Margin.Left - Margin.Right;
        List<string> lines = ComputeLines(contentWidth);
        float localY = ctx.CurrentY;

        PdfOperators.BeginText(ctx.CurrentStream);
        PdfOperators.SetFont(ctx.CurrentStream, fontResourceName, FontSize);
        PdfOperators.SetFillColor(ctx.CurrentStream, TextColor);

        float maxWidth = 0;
        foreach (string line in lines)
        {
            float lineWidth = FontMetrics.GetStringWidth(line, FontName, FontSize);
            if (lineWidth > maxWidth)
                maxWidth = lineWidth;

            float pdfY = ctx.PageSize.Height - (localY + FontSize);

            PdfOperators.SetTextPosition(ctx.CurrentStream, ctx.CurrentX + Margin.Left, pdfY);

            if (isEmbedded)
                PdfOperators.ShowText(ctx.CurrentStream, EncodeTextAsGlyphIds(ctx, line));
            else
                PdfOperators.ShowText(ctx.CurrentStream, $"({PdfSyntax.Escape(line)})");

            localY += LineHeight;
        }

        PdfOperators.EndText(ctx.CurrentStream);

        // Calculate total content height
        float contentHeight = lines.Count * LineHeight;

        // Store render position for annotation (PDF coordinates: bottom-left origin)
        // The rectangle should be in PDF coordinates where (0,0) is bottom-left
        RenderX = ctx.CurrentX + Margin.Left;
        // Convert from top-down to bottom-up coordinates
        RenderY = ctx.PageSize.Height - (startY + Margin.Top + contentHeight);
        RenderWidth = maxWidth;
        RenderHeight = contentHeight;

        // Register this hyperlink in the render context
        ctx.RegisterHyperlink(this);

        // Advance Y by the total height (margins + content)
        ctx.CurrentY = startY + Margin.Top + contentHeight + Margin.Bottom;
    }

    /// <summary>
    /// Gets the PDF rectangle string for this hyperlink in the format [x1 y1 x2 y2]
    /// where (x1, y1) is the bottom-left corner and (x2, y2) is the top-right corner
    /// </summary>
    internal string GetRectangleString()
    {
        // PDF coordinates: (0,0) is bottom-left
        // RenderX, RenderY are already in PDF coordinates (bottom-left based)
        return $"[{RenderX} {RenderY} {RenderX + RenderWidth} {RenderY + RenderHeight}]";
    }

    private int ComputeLineCount(float maxWidth)
    {
        if (string.IsNullOrEmpty(Text)) return 0;
        return ComputeLines(maxWidth).Count;
    }

    private List<string> ComputeLines(float maxWidth)
    {
        List<string> lines = [];
        if (string.IsNullOrEmpty(Text)) return lines;

        string[] words = Text.Split(' ');
        string currentLine = "";

        foreach (string word in words)
        {
            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;

            float testWidth = FontMetrics.GetStringWidth(testLine, FontName, FontSize);

            if (testWidth > maxWidth)
            {
                lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine)) lines.Add(currentLine);
        return lines;
    }

    private string EncodeTextAsGlyphIds(RenderContext ctx, string text)
    {
        TtfFont font = ctx.ResolveEmbeddedFont(FontName);
        if (font == null)
            return $"({PdfSyntax.Escape(text)})";

        // Track glyph usage for font subsetting (alias-aware)
        ctx.TrackGlyphUsage(FontName, text);

        return PdfSyntax.ToGlyphIdHexString(text, font);
    }
}