using BonniePDF.Elements;
using BonniePDF.Rendering;

namespace BonniePDF.Elements.Layout;

/// <summary>
/// Forces a page break at the current position in the layout flow.
/// When encountered inside a Column or stateful render pass, the renderer moves
/// to a new page before rendering the next element.
/// Has no effect if already at the top of a page.
/// </summary>
public class PageBreak : ElementBase, IStatefulElement
{
    private bool _triggered;

    public RenderStatus RenderStateful(RenderContext ctx, float width)
    {
        if (!_triggered)
        {
            _triggered = true;
            // Only issue a page break if we are not already at the top of the page
            if (ctx.CurrentY > ctx.ContentTop)
                return RenderStatus.WrapRequired;
        }

        return RenderStatus.Complete;
    }

    public void ResetState() => _triggered = false;

    protected override float MeasureContentHeight(float contentWidth) => 0;

    protected override void RenderContent(RenderContext ctx, float contentWidth) { }
}
