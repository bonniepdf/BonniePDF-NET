namespace BonniePDF.Elements;

/// <summary>
/// Indicates the result of rendering an element to a page.
/// Used by stateful elements to communicate whether they have completed
/// rendering or need to continue on subsequent pages.
/// </summary>
public enum RenderStatus
{
    /// <summary>
    /// The element has finished rendering all of its content.
    /// No further pages are needed for this element.
    /// </summary>
    Complete,

    /// <summary>
    /// The element has more content that did not fit on the current page.
    /// The pipeline should create a new page and continue rendering.
    /// </summary>
    Overflow,

    /// <summary>
    /// The element could not render anything at all in the available space.
    /// The pipeline should move to a new page before retrying.
    /// Unlike Overflow, this indicates zero progress was made.
    /// </summary>
    WrapRequired
}