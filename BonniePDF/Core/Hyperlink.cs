using System;
using System.Collections.Generic;
using BonniePDF.Elements;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Pdf;
using BonniePDF.Rendering.Tagging;
using BonniePDF.Typography;
using BonniePDF.Typography.TrueType;

namespace BonniePDF.Core;

/// <summary>
/// Represents a hyperlink annotation in a PDF document.
/// Hyperlinks are clickable areas that navigate to a URI.
/// This element implements IElement so it can be added like other elements.
/// </summary>
public class Hyperlink : IElement, IRoleProvider
{
    /// <inheritdoc />
    public SemanticRole SemanticRole => Rendering.Tagging.SemanticRole.Link;

    /// <summary>
    /// Creates a new hyperlink with the specified URI and display text
    /// </summary>
    /// <param name="uri">The URI (URL) that this hyperlink points to</param>
    /// <param name="text">The link text to display</param>
    public Hyperlink(string uri, string text) : this(text)
    {
        if (string.IsNullOrEmpty(uri))
            throw new ArgumentException("URI cannot be null or empty", nameof(uri));

        Uri = uri;
    }

    /// <summary>
    /// Creates a new internal hyperlink (bookmark) with the specified display text
    /// </summary>
    /// <param name="text">The link text to display</param>
    public Hyperlink(string text)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("TextBlock cannot be null or empty", nameof(text));
        Text = text;
    }

    /// <summary>
    /// Gets or sets the URI (URL) that this hyperlink points to
    /// </summary>
    public string Uri { get; set; }

    /// <summary>
    /// Gets or sets the target 0-based page index (for internal links)
    /// </summary>
    public int? TargetPageIndex { get; set; }

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

    /// <summary>Line height derived from <see cref="FontSize"/> with a standard 1.2× leading factor.</summary>
    private float LineHeight => FontSize * 1.2f;

    /// <summary>
    /// The structure element created for this link by the tagging pipeline.
    /// Set during render; used by the serializer to wire /StructParent and /OBJR.
    /// </summary>
    internal Rendering.Tagging.StructureElement LinkedStructElement { get; set; }

    // Internal properties set during rendering
    /// <summary>Gets the X position of the rendered annotation rectangle in PDF coordinates (bottom-left origin).</summary>
    internal float RenderX { get; private set; }
    /// <summary>Gets the Y position of the rendered annotation rectangle in PDF coordinates (bottom-left origin).</summary>
    internal float RenderY { get; private set; }
    /// <summary>Gets the width of the rendered annotation rectangle in points.</summary>
    internal float RenderWidth { get; private set; }
    /// <summary>Gets the height of the rendered annotation rectangle in points.</summary>
    internal float RenderHeight { get; private set; }

    /// <summary>
    /// Sets the rendered bounding rectangle in PDF coordinates (bottom-left origin).
    /// Called during rendering so that the annotation can be written after layout is complete.
    /// </summary>
    internal void SetRenderBounds(float x, float y, float width, float height)
    {
        RenderX = x;
        RenderY = y;
        RenderWidth = width;
        RenderHeight = height;
    }

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

        float contentWidth = width - Margin.Left - Margin.Right;
        List<string> lines = ComputeLines(contentWidth);
        float localY = ctx.CurrentY;

        // Draw through the canvas (not by appending raw operators to the page
        // stream) so the link text lands inside the /Link marked-content scope
        // the pipeline opens around this element. Writing to the page stream
        // directly emitted the text outside that scope, leaving it untagged
        // (PDF/UA 7.1). Drawing via the canvas also picks up glyph fallback.
        float maxWidth = 0;
        foreach (string line in lines)
        {
            float lineWidth = FontMetrics.GetStringWidth(line, FontName, FontSize);
            if (lineWidth > maxWidth)
                maxWidth = lineWidth;

            ctx.Canvas.DrawText(ctx.CurrentX + Margin.Left, localY, line, TextColor, FontSize, FontName,
                Elements.Text.TextAlignment.Left);

            localY += LineHeight;
        }

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
        return $"[{PdfSyntax.FormatDecimal(RenderX)} {PdfSyntax.FormatDecimal(RenderY)} {PdfSyntax.FormatDecimal(RenderX + RenderWidth)} {PdfSyntax.FormatDecimal(RenderY + RenderHeight)}]";
    }

    /// <summary>
    /// Returns the number of wrapped lines required to render <see cref="Text"/> within the given width.
    /// </summary>
    private int ComputeLineCount(float maxWidth)
    {
        if (string.IsNullOrEmpty(Text)) return 0;
        return ComputeLines(maxWidth).Count;
    }

    /// <summary>
    /// Wraps <see cref="Text"/> into a list of lines that each fit within <paramref name="maxWidth"/>.
    /// Uses a greedy word-wrap algorithm measured with <see cref="FontMetrics"/>.
    /// </summary>
    private List<string> ComputeLines(float maxWidth)
    {
        List<string> lines = [];
        if (string.IsNullOrEmpty(Text)) return lines;

        string[] words = Text.Split(' ');
        string currentLine = "";

        foreach (string word in words)
        {
            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + ' ' + word;

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
}
