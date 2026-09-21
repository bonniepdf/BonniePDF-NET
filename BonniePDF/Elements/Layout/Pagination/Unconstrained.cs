using BonniePDF.Rendering;

namespace BonniePDF.Elements.Layout.Pagination;

/// <summary>
/// Renders its child outside the flow: the child draws from the current cursor
/// position but consumes no height, so whatever follows starts where the child
/// started.
/// </summary>
/// <remarks>
/// It is the escape hatch for content that should overlap rather than stack — a
/// badge over a table, a rubber stamp across a paragraph, a rule drawn behind the
/// next few lines. The child is measured as zero-height, which also means it
/// never triggers a page break of its own and never splits: it renders once,
/// here.
/// <para>
/// Because the cursor does not move, two <c>Unconstrained</c> siblings draw on
/// top of each other. That is the point, but it makes the element easy to misuse
/// — reach for it only when overlap is what you want.
/// </para>
/// </remarks>
public class Unconstrained : PaginationWrapperBase
{
    /// <summary>Where the cursor stood when the child started drawing.</summary>
    private float _entryY;

    /// <summary>Creates an empty unconstrained.</summary>
    public Unconstrained() { }

    /// <summary>Creates an unconstrained around <paramref name="child"/>.</summary>
    public Unconstrained(IElement child) => Child = child;

    /// <inheritdoc />
    protected override PaginationDecision Decide(RenderContext ctx, float contentWidth) =>
        PaginationDecision.Render;

    /// <summary>Zero-height in the flow, whatever the child measures.</summary>
    protected override float MeasureContentHeight(float contentWidth) => 0;

    /// <summary>Draws the child exactly once; continuation is never requested.</summary>
    protected override RenderStatus DriveChild(RenderContext ctx, float contentWidth)
    {
        _entryY = ctx.CurrentY;
        base.DriveChild(ctx, contentWidth);
        return RenderStatus.Complete;
    }

    /// <summary>
    /// Rewinds the flow cursor once the child has drawn <i>and</i> its capture
    /// node has closed, so the snapshot still records the real rect while the
    /// flow behaves as though nothing was emitted.
    /// </summary>
    protected override void AfterChild(RenderContext ctx) => ctx.CurrentY = _entryY;
}
