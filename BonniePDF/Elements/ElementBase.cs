using BonniePDF.Core;
using BonniePDF.Rendering;

namespace BonniePDF.Elements;

/// <summary>
/// Base class for PDF elements that provides common functionality for margin handling,
/// Y-position tracking, and content width calculation. This reduces code duplication
/// across elements and ensures consistent behavior.
/// </summary>
public abstract class ElementBase : IElement, IMargin
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

        // Apply top margin
        ctx.CurrentY += Margin.Top;

        // Get content width after margins
        float contentWidth = GetContentWidth(width);

        // Render the content
        RenderContent(ctx, contentWidth);

        // Apply bottom margin
        ctx.CurrentY += Margin.Bottom;
    }

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