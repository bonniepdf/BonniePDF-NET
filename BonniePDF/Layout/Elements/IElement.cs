namespace BonniePDF.Layout.Elements;

/// <summary>
/// Interface for all renderable PDF elements
/// </summary>
public interface IElement
{
    /// <summary>
    /// Measures the height of this element given the available width
    /// </summary>
    float MeasureHeight(float availableWidth);

    /// <summary>
    /// Gets the preferred width of this element, if any.
    /// This is used for auto-sizing in horizontal layouts.
    /// </summary>
    /// <returns>The preferred width, or 0 if no preference.</returns>
    float GetPreferredWidth();

    /// <summary>
    /// Renders this element to the PDF context
    /// </summary>
    void Render(RenderContext ctx, float width);
}

/// <summary>
/// Interface for elements that support stateful rendering across page boundaries.
/// Elements implementing this interface can track their render state and resume
/// on subsequent pages when content overflows.
/// </summary>
public interface IStatefulElement : IElement
{
    /// <summary>
    /// Renders this element with state tracking for pagination.
    /// Unlike Render(), this method returns a status indicating whether
    /// more content needs to be rendered on subsequent pages.
    /// </summary>
    /// <param name="ctx">The render context</param>
    /// <param name="width">The available width for this element</param>
    /// <returns>Complete if all content was rendered, Overflow if more pages are needed</returns>
    RenderStatus RenderStateful(RenderContext ctx, float width);

    /// <summary>
    /// Resets the internal state of this element, preparing it for a fresh render.
    /// Should be called before starting a new document or after completing a multipage render.
    /// </summary>
    void ResetState();
}