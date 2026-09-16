using System.Collections.Generic;
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

    /// <inheritdoc />
    public IEnumerable<IElement> GetChildren() => Child is null ? [] : [Child];

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
        if (Child == null)
        {
            ctx.CurrentY += Margin.Top + Margin.Bottom;
            return RenderStatus.Complete;
        }

        float originalX = ctx.CurrentX;
        float contentWidth = GetContentWidth(width);
        ctx.CurrentX += Margin.Left;
        ctx.CurrentY += Margin.Top;

        RenderStatus status;
        if (Child is IStatefulElement statefulChild)
        {
            try
            {
                status = statefulChild.RenderStateful(ctx, contentWidth);
            }
            finally
            {
                ctx.CurrentX = originalX;
            }
        }
        else
        {
            // For non-stateful children, we must check if they fit on the current page,
            // exactly like VerticalStack does for non-stateful elements.
            float childHeight = Child.MeasureHeight(contentWidth);
            bool wouldOverflow = ctx.CurrentY + childHeight > ctx.MaxY;
            bool notAtTop = ctx.CurrentY > ctx.ContentTop;

            if (wouldOverflow && notAtTop)
            {
                ctx.CurrentX = originalX;
                ctx.CurrentY -= Margin.Top;
                return RenderStatus.WrapRequired;
            }

            // Fits on this page (or we are at the top and must render anyway)
            try
            {
                Child.Render(ctx, contentWidth);
                status = RenderStatus.Complete;
            }
            finally
            {
                ctx.CurrentX = originalX;
            }
        }

        if (status == RenderStatus.Complete)
        {
            ctx.CurrentY += Margin.Bottom;
        }

        return status;
    }

    public void ResetState()
    {
        if (Child is IStatefulElement statefulChild)
        {
            statefulChild.ResetState();
        }
    }
}
