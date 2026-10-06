
namespace BonniePDF;

/// <summary>
/// Base class for stateful PDF elements that support rendering across page boundaries.
/// Extends ElementBase with IStatefulElement support, providing a foundation for
/// elements like TextBlock and VerticalStack that need to track render state.
/// </summary>
public abstract class StatefulElementBase : ElementBase, IStatefulElement
{
    /// <summary>
    /// Indicates whether this element has been partially rendered and has more content.
    /// </summary>
    protected bool HasPendingContent { get; set; }

    /// <summary>
    /// Gets the starting Y position (used for Y-position tracking).
    /// Made accessible to derived classes.
    /// </summary>
    protected new float StartingY { get; private set; }

    /// <summary>
    /// Renders this element with state tracking for pagination.
    /// The default implementation calls RenderContentStateful and handles margins.
    /// </summary>
    /// <param name="ctx">The render context</param>
    /// <param name="width">The available width for this element</param>
    /// <returns>Complete if all content was rendered, Overflow if more pages are needed</returns>
    public RenderStatus RenderStateful(RenderContext ctx, float width)
    {
        // Save starting Y position for Y-position tracking
        StartingY = ctx.CurrentY;
        float originalX = ctx.CurrentX;

        // Apply top margin only on first render (not on continuation pages)
        if (!HasPendingContent) ctx.CurrentY += Margin.Top;
        ctx.CurrentX += Margin.Left;

        // Get content width after margins
        float contentWidth = GetContentWidth(width);

        RenderStatus status;
        try
        {
            // Render the content with state tracking
            status = RenderContentStateful(ctx, contentWidth);
        }
        finally
        {
            ctx.CurrentX = originalX;
        }

        // Apply bottom margin only when complete
        if (status == RenderStatus.Complete)
        {
            ctx.CurrentY += Margin.Bottom;
            HasPendingContent = false;
        }
        else
        {
            HasPendingContent = true;
        }

        return status;
    }

    /// <summary>
    /// Resets the internal state of this element, preparing it for a fresh render.
    /// Derived classes should override this to reset their specific state.
    /// </summary>
    public virtual void ResetState()
    {
        HasPendingContent = false;
    }

    /// <summary>
    /// Renders the content with state tracking for pagination.
    /// Derived classes must implement this to provide their specific stateful rendering logic.
    /// </summary>
    /// <param name="ctx">The render context</param>
    /// <param name="contentWidth">The width available for content (margins already subtracted)</param>
    /// <returns>Complete if all content was rendered, Overflow if more pages are needed</returns>
    protected abstract RenderStatus RenderContentStateful(RenderContext ctx, float contentWidth);
}
