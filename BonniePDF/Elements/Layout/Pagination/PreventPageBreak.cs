
namespace BonniePDF;

/// <summary>
/// Keeps its child together: if the child will not fit in the space left on the
/// current page but would fit on an empty one, the whole child moves to the next
/// page rather than splitting across the boundary.
/// </summary>
/// <remarks>
/// This is the element form of the <see cref="ElementBase.BreakAllowed"/>
/// property (spec "Layout &amp; Pagination" §7), and behaves identically. Prefer
/// the property when you own the element; prefer this wrapper when you do not —
/// around a subtree you were handed, or in a fluent chain.
/// <para>
/// A child taller than a whole content box still splits: the specification
/// requires it, because no page break can rescue content that fits on no page at
/// all.
/// </para>
/// </remarks>
public class PreventPageBreak : PaginationWrapperBase
{
    /// <summary>Creates an empty prevent-page-break.</summary>
    public PreventPageBreak() { }

    /// <summary>Creates a prevent-page-break around <paramref name="child"/>.</summary>
    public PreventPageBreak(IElement child) => Child = child;

    /// <inheritdoc />
    protected override PaginationDecision Decide(RenderContext ctx, float contentWidth)
    {
        if (Child is null) return PaginationDecision.Render;

        float height = Child.MeasureHeight(contentWidth);
        if (RemainingHeight(ctx) >= height || !CanGainSpaceByBreaking(ctx)) return PaginationDecision.Render;

        // A child that fits on no page at all has to split wherever it is.
        return height <= ctx.ContentHeight ? PaginationDecision.Wrap : PaginationDecision.Render;
    }
}
