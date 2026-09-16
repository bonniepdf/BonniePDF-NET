using System;
using System.Collections.Generic;
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
    public IElement? Child { get; set; }

    /// <inheritdoc />
    public IEnumerable<IElement> GetChildren() => Child is null ? [] : [Child];

    /// <summary>
    /// Explicit interface implementation for IContentContainer.
    /// Enables fluent API chaining through this container.
    /// </summary>
    IElement? IContentContainer.Child
    {
        get => Child;
        set => Child = value;
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
    ///     .Foreground(new TextBlock("Sample Company").Bold().FontSize(32)));
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
    /// When <c>true</c>, the container takes the body content height left on the
    /// current page instead of sizing to its content. This is the primitive
    /// behind the header / body / footer page shape: the middle band is whatever
    /// the page chrome leaves over, a figure an author cannot compute up front
    /// because both templates are functions of the page number.
    /// </summary>
    /// <remarks>
    /// <see cref="Height"/> wins outright - fill is only consulted when no height
    /// is pinned - and fill never <em>shrinks</em> content: a container whose
    /// child is taller than the space left keeps the child's height and
    /// paginates normally. Measurement is unaffected (it reports the natural
    /// content height, because a pure measure has no cursor to measure from);
    /// only arrangement expands. Combines with
    /// <see cref="VerticalAlignment"/>: the slack fill creates is exactly the
    /// space it gained, so <see cref="Layout.VerticalAlignment.End"/> anchors
    /// content to the foot of the band.
    /// </remarks>
    public bool FillHeight { get; set; }

    /// <summary>
    /// When <c>true</c> and <see cref="BorderedElementBase.CornerRadius"/> is
    /// positive, the container's content (background, border, and child) is
    /// clipped to the rounded-rectangle outline. This is what turns a
    /// rounded background into a true rounded-corner card: a full-bleed image or
    /// any overflowing child is masked to the rounded box rather than spilling
    /// past the corners. Defaults to <c>false</c>, so setting
    /// <see cref="BorderedElementBase.CornerRadius"/> alone keeps the historical
    /// behaviour of a rounded fill/stroke with un-clipped content.
    /// </summary>
    public bool ClipContent { get; set; }

    /// <summary>
    /// When <c>true</c>, the container's subtree renders inside a crisp-edge
    /// region: on SVG page export it emits
    /// <c>&lt;g shape-rendering="crispEdges"&gt;</c>, so thin axis-aligned strokes
    /// and rectangles (grid/table lines, dividers) snap to the device pixel grid
    /// instead of being antialiased into a soft blur. It is a no-op on PDF output
    /// (the bytes are unchanged; crispness there is the viewer's job). Only wrap
    /// axis-aligned content — rounded rects, circles, diagonals, and text want
    /// antialiasing and will look jagged inside a crisp-edge region.
    /// </summary>
    public bool CrispEdges { get; set; }

    /// <summary>
    /// Horizontal alignment of the child within the container's available width.
    /// When the child is narrower than the container it is positioned
    /// accordingly; otherwise it fills the width as before. <c>null</c>/
    /// <c>Start</c> keep the default left placement; <c>Stretch</c> is treated
    /// as "fill" (no offset).
    /// </summary>
    public HorizontalAlignment? HorizontalAlignment { get; set; }

    /// <summary>
    /// Vertical alignment of the child within the container's available height.
    /// Only has an effect when the container is taller than its child needs -
    /// normally because <see cref="Height"/> is set - so it is the way to anchor
    /// content to the bottom (or middle) of a fixed box without hand-computing a
    /// top padding against a font's metrics. <c>null</c>/<c>Start</c> keep the
    /// default top placement; <c>Stretch</c> is treated as "fill" (no offset).
    /// </summary>
    public VerticalAlignment? VerticalAlignment { get; set; }

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

    /// <summary>
    /// The arranged height for this render. A pinned <see cref="Height"/> wins
    /// outright; <see cref="FillHeight"/> then expands to the body content height
    /// left below the cursor, never below the natural content height. Resolved
    /// here rather than in <see cref="MeasureContentHeight"/> because fill is a
    /// property of <em>where</em> the container lands, and measurement has no
    /// cursor - see the shared specification, "Layout &amp; Pagination" §3.1.
    /// </summary>
    private float ResolveHeight(RenderContext ctx, float effectiveWidth)
    {
        if (Height.HasValue) return Height.Value;

        float natural = MeasureContentHeight(effectiveWidth);
        if (!FillHeight) return natural;

        return MathF.Max(natural, ctx.MaxY - ctx.CurrentY);
    }

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        // Use fixed width if specified, otherwise use available width
        float effectiveWidth = Width ?? contentWidth;
        float containerHeight = ResolveHeight(ctx, effectiveWidth);

        // PAGINATION CHECK:
        // Containers are tricky. If the *whole* container fits, render it.
        // If not, we trigger a new page here too, to keep borders intact.
        if (ctx.CurrentY + containerHeight > ctx.MaxY && ctx.CurrentY > ctx.ContentTop)
        {
            ctx.NewPage();
            // Fill resolves per page: the band below the cursor on the new page
            // is a different one, and may be measured against different chrome.
            containerHeight = ResolveHeight(ctx, effectiveWidth);
        }

        // Crisp-edge hint: render the subtree inside a crisp-edge region so
        // axis-aligned strokes/rects snap to the pixel grid on SVG export.
        // Outermost wrap; the clip and content render inside it. No-op on PDF.
        if (CrispEdges) ctx.Canvas.BeginCrispEdges();

        try
        {
            // Rounded-corner clipping: mask the background, border, and child
            // to the rounded outline. Opened here and closed in the finally so the
            // early childless return still balances the clip.
            bool clip = ClipContent && CornerRadius > 0;
            if (clip)
                ctx.Canvas.BeginRoundedClip(ctx.CurrentX, ctx.CurrentY, effectiveWidth, containerHeight, CornerRadius);

            try
            {
                RenderClippedContent(ctx, effectiveWidth, containerHeight);
            }
            finally
            {
                if (clip) ctx.Canvas.EndClip();
            }
        }
        finally
        {
            if (CrispEdges) ctx.Canvas.EndCrispEdges();
        }
    }

    private void RenderClippedContent(RenderContext ctx, float effectiveWidth, float containerHeight)
    {
        if (BackgroundColor.HasValue || Border.HasAnyBorder)
        {
            float rectX = ctx.CurrentX;
            float rectWidth = effectiveWidth;

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

        // The child renders inside the border box: MeasureContentHeight and the
        // width below both reserve the border widths, so the origin must be offset
        // by Border.Left/Top as well - otherwise the child hugs the outer top-left
        // edge and the reserved space piles up as a visible gap at the right and
        // bottom edges (the border no longer "lines up" with the content).
        ctx.CurrentX += Border.Left + Padding.Left;
        ctx.CurrentY += Border.Top + Padding.Top;

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

        // Vertical alignment consumes the slack between the child's measured height
        // and the container's own, so a fixed-height box can anchor its content to
        // the bottom instead of being positioned by a hand-tuned top padding.
        var vAlign = VerticalAlignment;
        if (vAlign == BonniePDF.Core.VerticalAlignment.Center || vAlign == BonniePDF.Core.VerticalAlignment.End)
        {
            float innerHeight = containerHeight - Padding.Top - Padding.Bottom - Border.VerticalWidth;
            float slack = innerHeight - Child.MeasureHeight(childWidth);
            if (slack > 0)
                ctx.CurrentY += vAlign == BonniePDF.Core.VerticalAlignment.Center ? slack / 2f : slack;
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
            ctx.CurrentY = yBeforePadding + Border.Top + Padding.Top + childHeight;
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