
namespace BonniePDF;

/// <summary>
/// Renders its child on every page except the document's first.
/// </summary>
/// <remarks>
/// The mirror of <see cref="ShowOnce"/>, and the usual way to keep a running
/// header off a cover page while letting it appear everywhere else.
/// </remarks>
public class SkipOnce : PaginationWrapperBase
{
    /// <summary>Creates an empty skip-once.</summary>
    public SkipOnce() { }

    /// <summary>Creates a skip-once around <paramref name="child"/>.</summary>
    public SkipOnce(IElement child) => Child = child;

    /// <inheritdoc />
    protected override PaginationDecision Decide(RenderContext ctx, float contentWidth) =>
        ctx.CurrentPageIndex == 0 ? PaginationDecision.Skip : PaginationDecision.Render;
}
