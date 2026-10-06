
namespace BonniePDF;

/// <summary>
/// Starts a new page unless at least <see cref="MinHeight"/> of body height
/// remains on the current one.
/// </summary>
/// <remarks>
/// This is the orphaned-heading control: wrapping a heading in
/// <c>new EnsureSpace { MinHeight = 150, Child = heading }</c> guarantees the
/// heading is followed by 150pt of its section rather than stranded at the foot
/// of a page. The child itself is untouched — once the space test passes it
/// renders and paginates exactly as it would unwrapped.
/// <para>
/// The test is skipped when the cursor is already at the top of the body band:
/// breaking there would produce a blank page without gaining any space.
/// </para>
/// </remarks>
public class EnsureSpace : PaginationWrapperBase
{
    /// <summary>Creates an ensure-space with no requirement and no child.</summary>
    public EnsureSpace() { }

    /// <summary>Creates an ensure-space requiring <paramref name="minHeight"/> points.</summary>
    public EnsureSpace(float minHeight) => MinHeight = minHeight;

    /// <summary>Creates an ensure-space around <paramref name="child"/>.</summary>
    public EnsureSpace(float minHeight, IElement child)
    {
        MinHeight = minHeight;
        Child = child;
    }

    /// <summary>
    /// The body height, in points, that must remain on the current page for the
    /// child to start here. Use <see cref="Units"/> for other units:
    /// <c>Units.Mm(50)</c>.
    /// </summary>
    public float MinHeight { get; set; }

    /// <inheritdoc />
    protected override PaginationDecision Decide(RenderContext ctx, float contentWidth)
    {
        if (RemainingHeight(ctx) >= MinHeight) return PaginationDecision.Render;
        return CanGainSpaceByBreaking(ctx) ? PaginationDecision.Wrap : PaginationDecision.Render;
    }
}
