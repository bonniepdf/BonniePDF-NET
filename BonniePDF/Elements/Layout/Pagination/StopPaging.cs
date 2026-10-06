
namespace BonniePDF;

/// <summary>
/// Renders as much of its child as fits on the current page and discards the
/// rest, instead of continuing onto further pages.
/// </summary>
/// <remarks>
/// This is the truncation primitive: a preview that must stay one page long, an
/// appendix capped at whatever the remaining space allows, a summary panel that
/// should never grow the document. Unlike <see cref="ShowEntire"/> it does not
/// move to a fresh page first — it starts exactly where the cursor is and stops
/// at the page boundary.
/// <para>Truncation is silent by design: the child's continuation is never requested.</para>
/// </remarks>
public class StopPaging : PaginationWrapperBase
{
    /// <summary>Creates an empty stop-paging.</summary>
    public StopPaging() { }

    /// <summary>Creates a stop-paging around <paramref name="child"/>.</summary>
    public StopPaging(IElement child) => Child = child;

    /// <inheritdoc />
    protected override PaginationDecision Decide(RenderContext ctx, float contentWidth) =>
        PaginationDecision.Render;

    /// <summary>Whatever the child still had to render is dropped.</summary>
    protected override RenderStatus Settle(RenderStatus status) => RenderStatus.Complete;
}
