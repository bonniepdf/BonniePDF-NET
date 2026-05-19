using BonniePDF.Elements;
using BonniePDF.Rendering;

namespace BonniePDF.Fluent;

/// <summary>
/// A lightweight element that acts as both an IElement (can be added to layout containers)
/// and an IContentContainer (can be the target of fluent chaining).
/// Used by descriptors as item slots in Column, Row, and Table layouts.
/// </summary>
internal class ContentSlot : ElementBase, IContentContainer, IStatefulElement
{
    public IElement? Child { get; set; }

    protected override float MeasureContentHeight(float contentWidth)
    {
        return Child?.MeasureHeight(contentWidth) ?? 0;
    }

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        Child?.Render(ctx, contentWidth);
    }

    protected override float GetPreferredContentWidth()
    {
        return Child?.GetPreferredWidth() ?? 0;
    }

    public RenderStatus RenderStateful(RenderContext ctx, float width)
    {
        if (Child == null) return RenderStatus.Complete;

        if (Child is IStatefulElement statefulChild)
        {
            return statefulChild.RenderStateful(ctx, width);
        }

        // For non-stateful children, we must check if they fit on the current page,
        // exactly like VerticalStack does for non-stateful elements.
        float childHeight = Child.MeasureHeight(width);
        bool wouldOverflow = ctx.CurrentY + childHeight > ctx.MaxY;
        bool notAtTop = ctx.CurrentY > ctx.Margin;

        if (wouldOverflow && notAtTop)
        {
            return RenderStatus.WrapRequired;
        }

        // Fits on this page (or we are at the top and must render anyway)
        Child.Render(ctx, width);
        return RenderStatus.Complete;
    }

    public void ResetState()
    {
        if (Child is IStatefulElement statefulChild)
        {
            statefulChild.ResetState();
        }
    }
}
