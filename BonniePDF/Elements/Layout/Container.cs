using System;
using BonniePDF.Core;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Descriptors;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Tagging;

namespace BonniePDF.Elements.Layout;

/// <summary>
/// A container element that supports padding, background color, and borders.
/// Implements IContentContainer to participate in fluent API chains.
/// </summary>
public class Container : BorderedElementBase, IContentContainer
{
    public IElement Child { get; set; }

    /// <summary>
    /// Explicit interface implementation for IContentContainer.
    /// Enables fluent API chaining through this container.
    /// </summary>
    IElement? IContentContainer.Child
    {
        get => Child;
        set => Child = value!;
    }
    public Thickness Padding { get; set; }

    /// <summary>
    /// Fills this container with an inline z-stack. Layers paint in
    /// declaration order: <c>Background</c> behind, <c>PrimaryLayer</c> in the
    /// middle (it sets the flow height), and <c>Foreground</c> on top.
    /// <example>
    /// <code>
    /// new Container().Layers(layers => layers
    ///     .Background(new Image("card-bg.jpg"))
    ///     .Foreground(new TextBlock("Horizon Ventures").Bold().FontSize(32)));
    /// </code>
    /// </example>
    /// </summary>
    public Container Layers(Action<LayersDescriptor> configure)
    {
        LayersDescriptor descriptor = new();
        configure(descriptor);
        Child = descriptor.Build();
        return this;
    }

    /// <summary>
    /// Optional fixed width for the container (null = use available width)
    /// </summary>
    public float? Width { get; set; }

    /// <summary>
    /// Optional fixed height for the container (null = auto-size based on content)
    /// </summary>
    public float? Height { get; set; }

    /// <summary>
    /// Horizontal alignment of the child within the container's available width
    /// (<c>AlignCenter</c>/<c>AlignRight</c>/<c>AlignLeft</c>).
    /// When the child is narrower than the container it is positioned
    /// accordingly; otherwise it fills the width as before. <c>null</c>/
    /// <c>Start</c> keep the default left placement; <c>Stretch</c> is treated
    /// as "fill" (no offset).
    /// </summary>
    public HorizontalAlignment? HorizontalAlignment { get; set; }

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
            float rectY = ctx.PageSize.Height - ctx.CurrentY - containerHeight;

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

        // Horizontal alignment: when the child is narrower than the available
        // inner width, position it within that width rather than pinning it left.
        var align = HorizontalAlignment;
        if (align == BonniePDF.Core.HorizontalAlignment.Center || align == BonniePDF.Core.HorizontalAlignment.End)
        {
            float preferred = Child.GetPreferredWidth();
            if (preferred > 0 && preferred < childWidth)
            {
                float slack = childWidth - preferred;
                ctx.CurrentX += align == BonniePDF.Core.HorizontalAlignment.Center ? slack / 2f : slack;
                childWidth = preferred;
            }
        }

        // Save Y position before child render. Tag the child so a role-bearing
        // leaf nested directly under a Container isn't emitted as untagged
        // content (PDF/UA 7.1); suppressed automatically inside /Artifact regions.
        float yBeforeChildRender = ctx.CurrentY;
        ChildTagToken token = ChildTagging.BeginChildTag(ctx, Child);
        try
        {
            Child.Render(ctx, childWidth);
        }
        finally
        {
            ChildTagging.EndChildTag(ctx, token);
        }
        float yAfterChildRender = ctx.CurrentY;

        ctx.CurrentX = originalX;

        // If child didn't advance Y, we need to do it based on measured height
        if (yAfterChildRender == yBeforeChildRender)
        {
            float childHeight = Child.MeasureHeight(childWidth);
            ctx.CurrentY = yBeforePadding + Padding.Top + childHeight;
        }

        ctx.CurrentY += Padding.Bottom;

        // Ensure we advance by the full container height if it was explicitly set or larger
        float expectedBottom = yBeforePadding + containerHeight;
        if (ctx.CurrentY < expectedBottom)
        {
            ctx.CurrentY = expectedBottom;
        }
    }
}