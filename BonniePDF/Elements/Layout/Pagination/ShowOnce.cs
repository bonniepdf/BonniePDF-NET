using BonniePDF.Rendering;

namespace BonniePDF.Elements.Layout.Pagination;

/// <summary>
/// Renders its child on the document's first page only.
/// </summary>
/// <remarks>
/// The canonical use is a page template that should not repeat: a cover-page
/// watermark, a one-off notice above a running header. Inside body flow, where an
/// element is reached once anyway, it renders normally on page 1 and disappears
/// if the flow has already carried it past that page.
/// </remarks>
public class ShowOnce : PaginationWrapperBase
{
    /// <summary>Creates an empty show-once.</summary>
    public ShowOnce() { }

    /// <summary>Creates a show-once around <paramref name="child"/>.</summary>
    public ShowOnce(IElement child) => Child = child;

    /// <inheritdoc />
    protected override PaginationDecision Decide(RenderContext ctx, float contentWidth) =>
        ctx.CurrentPageIndex == 0 ? PaginationDecision.Render : PaginationDecision.Skip;
}
