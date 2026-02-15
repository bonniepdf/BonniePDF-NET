using BonniePDF.Configuration;
using BonniePDF.Layout;
using BonniePDF.Layout.Elements;
using BonniePDF.Models;

namespace BonniePDF.IO;

/// <summary>
/// Helper class for rendering page templates (headers and footers).
/// Templates are rendered after page content is finalized.
/// </summary>
public static class TemplateRenderer
{
    /// <summary>
    /// Renders headers and footers for all pages in the render context.
    /// Should be called after all content has been rendered.
    /// </summary>
    /// <param name="ctx">The render context containing all rendered pages</param>
    /// <param name="settings">Document settings containing template functions</param>
    public static void RenderTemplates(RenderContext ctx, DocumentSettings settings)
    {
        if (settings.HeaderTemplate == null && settings.FooterTemplate == null) return;

        int totalPages = ctx.Pages.Count;

        for (int i = 0; i < totalPages; i++)
        {
            int pageNumber = i + 1; // 1-based page numbers
            ctx.SwitchToPage(i);

            // Mark if this is the last page
            ctx.IsLastPage = i == totalPages - 1;

            // Render header
            if (settings.HeaderTemplate != null)
            {
                IElement headerElement = settings.HeaderTemplate(pageNumber);
                if (headerElement != null) RenderHeader(ctx, headerElement);
            }

            // Render footer
            if (settings.FooterTemplate != null)
            {
                IElement footerElement = settings.FooterTemplate(pageNumber);
                if (footerElement != null) RenderFooter(ctx, footerElement);
            }
        }
    }

    /// <summary>
    /// Renders a header element at the top of the current page.
    /// </summary>
    private static void RenderHeader(RenderContext ctx, IElement element)
    {
        // Save current position
        float savedX = ctx.CurrentX;
        float savedY = ctx.CurrentY;

        // Position at top of page (above margin)
        ctx.CurrentX = ctx.Margin;
        ctx.CurrentY = ctx.Margin / 2; // Half-way into the top margin

        // Calculate content width
        float contentWidth = ctx.PageSize.Width - 2 * ctx.Margin;

        // Render the header
        element.Render(ctx, contentWidth);

        // Restore position
        ctx.CurrentX = savedX;
        ctx.CurrentY = savedY;
    }

    /// <summary>
    /// Renders a footer element at the bottom of the current page.
    /// </summary>
    private static void RenderFooter(RenderContext ctx, IElement element)
    {
        // Save current position
        float savedX = ctx.CurrentX;
        float savedY = ctx.CurrentY;

        // Position at bottom of page (in the bottom margin)
        ctx.CurrentX = ctx.Margin;

        // Measure footer height to position it properly
        float contentWidth = ctx.PageSize.Width - 2 * ctx.Margin;
        float footerHeight = element.MeasureHeight(contentWidth);

        // Position footer in the bottom margin
        ctx.CurrentY = ctx.PageSize.Height - ctx.Margin + (ctx.Margin - footerHeight) / 2;

        // Render the footer
        element.Render(ctx, contentWidth);

        // Restore position
        ctx.CurrentX = savedX;
        ctx.CurrentY = savedY;
    }

    /// <summary>
    /// Creates a simple text-based page number footer.
    /// </summary>
    /// <param name="pageNumber">The current page number</param>
    /// <param name="format">Format string with {page} placeholder (default: "Page {page}")</param>
    /// <returns>A TextBlock element with the formatted page number</returns>
    public static TextBlock CreatePageNumberFooter(int pageNumber, string format = "Page {page}")
    {
        string text = format.Replace("{page}", pageNumber.ToString());
        return new TextBlock
        {
            Text = text,
            Alignment = TextAlignment.Center,
            FontName = StandardFonts.Helvetica,
            FontSize = 10,
            TextColor = Color.Gray500
        };
    }
}