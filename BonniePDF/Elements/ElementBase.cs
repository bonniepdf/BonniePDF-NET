using BonniePDF.Core;
using BonniePDF.Rendering;

namespace BonniePDF.Elements;

/// <summary>
/// Base class for PDF elements that provides common functionality for margin handling,
/// Y-position tracking, and content width calculation. This reduces code duplication
/// across elements and ensures consistent behavior.
/// </summary>
public abstract class ElementBase : IElement, IMargin, ICaptureMetadata
{
    /// <summary>
    /// The starting Y position when rendering begins (used for Y-position tracking)
    /// </summary>
    protected float StartingY { get; private set; }

    /// <summary>
    /// Gets the preferred width of this element, if any.
    /// This implementation includes margins.
    /// </summary>
    public float GetPreferredWidth()
    {
        return GetPreferredContentWidth() + Margin.Left + Margin.Right;
    }

    /// <summary>
    /// Measures the height of this element given the available width.
    /// This implementation accounts for margins in addition to content height.
    /// </summary>
    /// <param name="availableWidth">The available width for this element</param>
    /// <returns>The total height including margins</returns>
    public float MeasureHeight(float availableWidth)
    {
        float contentWidth = GetContentWidth(availableWidth);
        float contentHeight = MeasureContentHeight(contentWidth);
        return contentHeight + Margin.Top + Margin.Bottom;
    }

    /// <summary>
    /// Renders this element to the PDF context.
    /// This implementation handles margin application and Y-position tracking.
    /// </summary>
    /// <param name="ctx">The render context</param>
    /// <param name="width">The available width for this element</param>
    public void Render(RenderContext ctx, float width)
    {
        // Save starting Y position for Y-position tracking
        StartingY = ctx.CurrentY;
        float originalX = ctx.CurrentX;

        // Layout capture (opt-in, no-op without a recorder): record this element's
        // full margin box. The atomic render path renders in one shot, so the node
        // opens and closes (complete) around RenderContent.
        ctx.LayoutRecorder?.OpenNode(this, CaptureType, CaptureName, ctx.CurrentPageIndex, originalX, StartingY, width);

        // Apply top margin
        ctx.CurrentY += Margin.Top;
        ctx.CurrentX += Margin.Left;

        // Get content width after margins
        float contentWidth = GetContentWidth(width);

        try
        {
            // Render the content
            RenderContent(ctx, contentWidth);
        }
        finally
        {
            ctx.CurrentX = originalX;
        }

        // Apply bottom margin
        ctx.CurrentY += Margin.Bottom;

        ctx.LayoutRecorder?.CloseNode(this, ctx.CurrentPageIndex, ctx.CurrentY, complete: true);
    }

    /// <summary>
    /// Canonical element type name used for the layout snapshot tree. Defaults to
    /// the runtime type name; subclasses may override for a stable spec name.
    /// </summary>
    public virtual string CaptureType => GetType().Name;

    /// <summary>
    /// Optional author-supplied name/key surfaced in the layout snapshot. Null by
    /// default; a future capture API can populate it.
    /// </summary>
    public string? CaptureName { get; set; }

    /// <summary>
    /// Margin around this element
    /// </summary>
    public Thickness Margin { get; set; } = new(0);

    /// <summary>
    /// Whether this element can be split across page boundaries.
    /// When false, the element is treated as atomic and will be moved to a new page
    /// rather than split. If it doesn't fit on a fresh page either, it renders anyway.
    /// Default is true.
    /// </summary>
    public bool BreakAllowed { get; set; } = true;

    /// <summary>
    /// Gets the content width after accounting for margins
    /// </summary>
    /// <param name="availableWidth">The total available width</param>
    /// <returns>The width available for content (total width minus left and right margins)</returns>
    protected float GetContentWidth(float availableWidth)
    {
        return availableWidth - Margin.Left - Margin.Right;
    }

    /// <summary>
    /// Gets the preferred content width.
    /// Derived classes can override this to provide intrinsic sizing.
    /// </summary>
    protected virtual float GetPreferredContentWidth()
    {
        return 0;
    }

    /// <summary>
    /// Measures the height of the content only (excluding margins).
    /// Derived classes must implement this to provide their specific content measurement logic.
    /// </summary>
    /// <param name="contentWidth">The width available for content (margins already subtracted)</param>
    /// <returns>The height of the content only</returns>
    protected abstract float MeasureContentHeight(float contentWidth);

    /// <summary>
    /// Renders the content of this element.
    /// Derived classes must implement this to provide their specific rendering logic.
    /// The context will have the top margin already applied, and bottom margin will be applied after.
    /// </summary>
    /// <param name="ctx">The render context</param>
    /// <param name="contentWidth">The width available for content (margins already subtracted)</param>
    protected abstract void RenderContent(RenderContext ctx, float contentWidth);
}
