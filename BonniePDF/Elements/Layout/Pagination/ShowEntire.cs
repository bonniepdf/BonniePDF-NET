using BonniePDF.Rendering;

namespace BonniePDF.Elements.Layout.Pagination;

/// <summary>
/// Renders its child only where the child fits whole, and never splits it.
/// </summary>
/// <remarks>
/// Where <see cref="PreventPageBreak"/> moves an oversized child to the next page
/// and still splits it if it fits on no page, <c>ShowEntire</c> refuses to split
/// under any circumstance. A child taller than a whole content box therefore
/// renders from the top of a fresh page and is cut off at the page boundary —
/// the content that did not fit is dropped, not carried over.
/// <para>
/// Use it for content that is meaningless in pieces — a signature block, a
/// certificate panel, a scorecard — where half the thing on each of two pages is
/// worse than a shorter thing on one.
/// </para>
/// </remarks>
public class ShowEntire : PaginationWrapperBase
{
    /// <summary>Creates an empty show-entire.</summary>
    public ShowEntire() { }

    /// <summary>Creates a show-entire around <paramref name="child"/>.</summary>
    public ShowEntire(IElement child) => Child = child;

    /// <inheritdoc />
    protected override PaginationDecision Decide(RenderContext ctx, float contentWidth)
    {
        if (Child is null) return PaginationDecision.Render;

        bool fitsHere = RemainingHeight(ctx) >= Child.MeasureHeight(contentWidth);
        if (fitsHere || !CanGainSpaceByBreaking(ctx)) return PaginationDecision.Render;
        return PaginationDecision.Wrap;
    }

    /// <summary>Never continues onto another page: whatever did not fit is dropped.</summary>
    protected override RenderStatus Settle(RenderStatus status) => RenderStatus.Complete;
}
