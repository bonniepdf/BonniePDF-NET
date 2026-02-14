using BonniePDF.Models;

namespace BonniePDF.Layout.Elements;

/// <summary>
/// A container element that supports padding, background color, and borders
/// </summary>
public class Container : BorderedElementBase
{
    public IElement Child { get; set; }
    public Thickness Padding { get; set; }

    /// <summary>
    /// Optional fixed width for the container (null = use available width)
    /// </summary>
    public float? Width { get; set; }

    /// <summary>
    /// Optional fixed height for the container (null = auto-size based on content)
    /// </summary>
    public float? Height { get; set; }

    protected override float GetPreferredContentWidth()
    {
        if (Width.HasValue)
            return Width.Value;

        float childWidth = Child?.GetPreferredWidth() ?? 0;
        return childWidth + Padding.Left + Padding.Right + Border.HorizontalWidth;
    }

    protected override float MeasureContentHeight(float contentWidth)
    {
        // Use fixed width if specified, otherwise use available width
        float effectiveWidth = Width ?? contentWidth;
        float childContentWidth = effectiveWidth - Padding.Left - Padding.Right - Border.HorizontalWidth;

        // Use fixed height if specified, otherwise measure child height
        if (Height.HasValue) return Height.Value;

        float childHeight = Child == null ? 0 : Child.MeasureHeight(childContentWidth);
        return childHeight + Padding.Top + Padding.Bottom + Border.VerticalWidth;
    }

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        // Use fixed width if specified, otherwise use available width
        float effectiveWidth = Width ?? contentWidth;
        float containerHeight = Height ?? MeasureContentHeight(effectiveWidth);

        // PAGINATION CHECK:
        // Containers are tricky. If the *whole* container fits, render it.
        // If not, we trigger a new page here too, to keep borders intact.
        if (ctx.CurrentY + containerHeight > ctx.MaxY && ctx.CurrentY > ctx.Margin) ctx.NewPage();

        if (BackgroundColor.HasValue || Border.HasAnyBorder)
        {
            float rectX = ctx.CurrentX;
            float rectWidth = effectiveWidth;
            float rectY = ctx.PageHeight - ctx.CurrentY - containerHeight;

            if (BackgroundColor.HasValue)
                DrawBackground(ctx, rectX, ctx.CurrentY, rectWidth, containerHeight, BackgroundColor.Value);

            if (Border.HasAnyBorder)
                DrawBorder(ctx, rectX, ctx.CurrentY, rectWidth, containerHeight, Border, BorderColors);
        }

        if (Child == null)
        {
            ctx.CurrentY += containerHeight;
            return;
        }

        float originalX = ctx.CurrentX;

        // Save Y position before adding padding for accurate comparison
        float yBeforePadding = ctx.CurrentY;

        ctx.CurrentX += Padding.Left;
        ctx.CurrentY += Padding.Top;

        float childWidth = effectiveWidth - Padding.Left - Padding.Right - Border.HorizontalWidth;

        // Save Y position before child render
        float yBeforeChildRender = ctx.CurrentY;
        Child.Render(ctx, childWidth);
        float yAfterChildRender = ctx.CurrentY;

        ctx.CurrentX = originalX;

        // If child didn't advance Y, we need to do it based on measured height
        if (yAfterChildRender == yBeforeChildRender)
        {
            float childHeight = Child.MeasureHeight(childWidth);
            ctx.CurrentY = yBeforePadding + Padding.Top + childHeight;
        }

        ctx.CurrentY += Padding.Bottom;
    }
}