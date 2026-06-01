using System.Collections.Generic;
using BonniePDF.Core;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Tagging;
using BonniePDF.Typography;

namespace BonniePDF.Elements.Text;

/// <summary>
/// Renders a visual Table of Contents with dotted leaders and right-aligned page numbers,
/// built from a list of heading identifiers that were registered during layout.
/// Supports multi-page overflow via <see cref="IStatefulElement"/> so that very long
/// tables of contents flow naturally across page boundaries.
/// </summary>
public class VisualToc : ElementBase, IStatefulElement, IRoleProvider
{
    /// <inheritdoc />
    public SemanticRole SemanticRole => Rendering.Tagging.SemanticRole.TOC;

    /// <summary>
    /// Gets or sets the ordered list of TOC identifiers to render.
    /// Each identifier must match the <see cref="Heading.TocIdentifier"/> of a rendered heading.
    /// </summary>
    public IList<string> HeadingIdentifiers { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the font name used to render TOC entry text.
    /// </summary>
    public string FontName { get; set; } = StandardFonts.Helvetica;

    /// <summary>
    /// Gets or sets the font size (in points) used to render TOC entry text.
    /// </summary>
    public float FontSize { get; set; } = 12;

    /// <summary>
    /// Gets or sets the text color used for TOC entries.
    /// </summary>
    public Color TextColor { get; set; } = Color.Black;

    private int _lastRenderedIndex = -1;

    /// <summary>
    /// Returns the preferred content width. The element expands to fill all available width.
    /// </summary>
    protected override float GetPreferredContentWidth()
    {
        return 0; // It will expand to available width
    }

    /// <summary>
    /// Measures the total height required to render all TOC entries at the configured font size.
    /// </summary>
    /// <param name="contentWidth">The available content width (unused; height is line-count based).</param>
    /// <returns>Total height in points.</returns>
    protected override float MeasureContentHeight(float contentWidth)
    {
        return HeadingIdentifiers.Count * (FontSize * 1.2f);
    }

    /// <summary>
    /// Renders all TOC entries at the current cursor position without page-boundary checks.
    /// Used by the non-stateful render path when the entire TOC is known to fit.
    /// </summary>
    /// <param name="ctx">The current render context.</param>
    /// <param name="contentWidth">The available content width.</param>
    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        float y = ctx.CurrentY;
        float lineHeight = FontSize * 1.2f;

        for (int i = 0; i < HeadingIdentifiers.Count; i++)
        {
            DrawTaggedTocEntry(ctx, HeadingIdentifiers[i], contentWidth, y);
            y += lineHeight;
        }

        ctx.CurrentY = y;
    }

    /// <summary>
    /// Renders as many TOC entries as fit on the current page, advancing the cursor and
    /// returning <see cref="RenderStatus.Overflow"/> when more entries remain for the next page.
    /// </summary>
    /// <param name="ctx">The current render context.</param>
    /// <param name="width">The total available width including margins.</param>
    /// <returns>
    /// <see cref="RenderStatus.Complete"/> when all entries have been rendered,
    /// <see cref="RenderStatus.Overflow"/> when entries remain for the next page, or
    /// <see cref="RenderStatus.WrapRequired"/> when no entries fit on the very first page.
    /// </returns>
    public RenderStatus RenderStateful(RenderContext ctx, float width)
    {
        if (_lastRenderedIndex < 0)
        {
            ctx.CurrentY += Margin.Top;
        }

        float contentWidth = GetContentWidth(width);
        float originalX = ctx.CurrentX;
        ctx.CurrentX += Margin.Left;
        float lineHeight = FontSize * 1.2f;
        int startIndex = _lastRenderedIndex + 1;

        try
        {
            if (startIndex >= HeadingIdentifiers.Count)
            {
                ctx.CurrentY += Margin.Bottom;
                return RenderStatus.Complete;
            }

            float localY = ctx.CurrentY;
            int itemsThatFit = 0;
            float testY = localY;

            for (int i = startIndex; i < HeadingIdentifiers.Count; i++)
            {
                if (testY + lineHeight > ctx.MaxY) break;
                itemsThatFit++;
                testY += lineHeight;
            }

            if (itemsThatFit == 0)
            {
                return startIndex == 0 ? RenderStatus.WrapRequired : RenderStatus.Overflow;
            }

            int endIndex = startIndex + itemsThatFit;
            for (int i = startIndex; i < endIndex; i++)
            {
                DrawTaggedTocEntry(ctx, HeadingIdentifiers[i], contentWidth, localY);
                localY += lineHeight;
            }

            _lastRenderedIndex = endIndex - 1;
            ctx.CurrentY = localY;

            if (endIndex >= HeadingIdentifiers.Count)
            {
                ctx.CurrentY += Margin.Bottom;
                return RenderStatus.Complete;
            }

            return RenderStatus.Overflow;
        }
        finally
        {
            ctx.CurrentX = originalX;
        }
    }

    /// <summary>
    /// wraps <see cref="DrawTocEntry"/> in a <c>/TOCI</c> struct
    /// element + marked-content scope so each entry is its own
    /// addressable item in the PDF/UA structure tree. The outer
    /// <c>/TOC</c> wrap comes from the pipeline's IRoleProvider hook
    /// using <see cref="SemanticRole"/>. No-op when tagging is off.
    /// </summary>
    private void DrawTaggedTocEntry(RenderContext ctx, string identifier, float contentWidth, float y)
    {
        var tagging = ctx.Tagging;
        bool tagThisEntry = tagging != null;
        if (tagThisEntry)
        {
            tagging.Tree.Begin(Rendering.Tagging.SemanticRole.TOCI);
            ctx.Canvas.BeginMarkedContent("TOCI");
        }

        try
        {
            DrawTocEntry(ctx, identifier, contentWidth, y);
        }
        finally
        {
            if (tagThisEntry)
            {
                ctx.Canvas.EndMarkedContent();
                tagging.Tree.End();
            }
        }
    }

    /// <summary>
    /// Draws a single TOC entry: the heading text on the left, a dotted leader in the middle,
    /// and the resolved page number on the right. Also registers a clickable hyperlink annotation
    /// spanning the full line width so the user can navigate directly to the heading.
    /// </summary>
    /// <param name="ctx">The current render context.</param>
    /// <param name="identifier">The heading identifier to look up in <see cref="RenderContext.TocEntries"/>.</param>
    /// <param name="contentWidth">The available content width for this entry.</param>
    /// <param name="y">The top-down Y coordinate at which to draw the entry.</param>
    private void DrawTocEntry(RenderContext ctx, string identifier, float contentWidth, float y)
    {
        string pageNumStr = "XX";
        if (ctx.TocEntries.TryGetValue(identifier, out int pageNum))
        {
            pageNumStr = pageNum.ToString();
        }

        // Draw heading text
        ctx.Canvas.DrawText(ctx.CurrentX, y, identifier, TextColor, FontSize, FontName, TextAlignment.Left);

        // Draw page number
        float pageNumWidth = FontMetrics.GetStringWidth(pageNumStr, FontName, FontSize);
        float pageNumX = ctx.CurrentX + contentWidth - pageNumWidth;
        ctx.Canvas.DrawText(pageNumX, y, pageNumStr, TextColor, FontSize, FontName, TextAlignment.Left);

        // Draw dotted leader
        float textWidth = FontMetrics.GetStringWidth(identifier, FontName, FontSize);
        float startDotX = ctx.CurrentX + textWidth + 5;
        float endDotX = pageNumX - 5;

        if (endDotX > startDotX)
        {
            string dot = ".";
            float dotWidth = FontMetrics.GetStringWidth(dot, FontName, FontSize);
            float currentDotX = startDotX;
            while (currentDotX + dotWidth <= endDotX)
            {
                ctx.Canvas.DrawText(currentDotX, y, dot, TextColor, FontSize, FontName, TextAlignment.Left);
                currentDotX += dotWidth * 2; // Spacing between dots
            }
        }

        // Make the entire line clickable to the internal named destination
        float lineHeight = FontSize * 1.2f;
        float pdfX = ctx.CurrentX;
        float pdfY = ctx.PageSize.Height - (y + lineHeight); // PDF coordinate is bottom-up

        int targetPage = ctx.TocEntries.TryGetValue(identifier, out int p) ? p - 1 : 0;
        Hyperlink link = new Hyperlink(identifier)
        {
            TargetPageIndex = targetPage
        };
        link.SetRenderBounds(pdfX, pdfY, contentWidth, lineHeight);
        ctx.RegisterHyperlink(link);
    }

    /// <summary>
    /// Resets the render state so that rendering can restart from the first entry,
    /// for example before a second layout pass.
    /// </summary>
    public void ResetState()
    {
        _lastRenderedIndex = -1;
    }
}
