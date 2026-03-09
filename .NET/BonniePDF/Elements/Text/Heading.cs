using BonniePDF.Rendering;

namespace BonniePDF.Elements.Text;

/// <summary>
/// A text block that registers itself as a heading in the Table of Contents when rendered.
/// On first render, the element records its page number against <see cref="TocIdentifier"/>
/// in the <see cref="RenderContext.TocEntries"/> dictionary so that a <see cref="VisualToc"/>
/// can display the correct page reference.
/// </summary>
public class Heading : TextBlock, IStatefulElement
{
    /// <summary>
    /// Gets or sets the unique identifier used to look up this heading in the TOC entries dictionary.
    /// Must match the identifier passed to <see cref="VisualToc.HeadingIdentifiers"/>.
    /// </summary>
    public required string TocIdentifier { get; set; }

    private bool _tocRegistered;

    /// <summary>
    /// Initializes a new instance of <see cref="Heading"/> with default settings.
    /// </summary>
    public Heading() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Heading"/> with the specified text.
    /// </summary>
    /// <param name="text">The heading text to display.</param>
    public Heading(string text) : base(text)
    {
    }

    /// <summary>
    /// Renders this heading in a stateful manner, registering the TOC entry on first render
    /// and delegating text rendering to the base <see cref="TextBlock"/> implementation.
    /// </summary>
    /// <param name="ctx">The current render context.</param>
    /// <param name="width">The available width for rendering.</param>
    /// <returns>The render status indicating whether the element is complete or overflowed.</returns>
    public new RenderStatus RenderStateful(RenderContext ctx, float width)
    {
        if (!_tocRegistered)
        {
            ctx.TocEntries[TocIdentifier] = ctx.CurrentPageIndex + 1;
            _tocRegistered = true;
        }
        return base.RenderStateful(ctx, width);
    }

    /// <summary>
    /// Resets the render state so the heading can be re-rendered on subsequent layout passes
    /// (e.g. during two-pass TOC resolution).
    /// </summary>
    public new void ResetState()
    {
        _tocRegistered = false;
        base.ResetState();
    }

    /// <summary>
    /// Renders the heading content and records the current page number against
    /// <see cref="TocIdentifier"/> on the first render pass.
    /// </summary>
    /// <param name="ctx">The current render context.</param>
    /// <param name="contentWidth">The available content width after margins.</param>
    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        if (!_tocRegistered)
        {
            ctx.TocEntries[TocIdentifier] = ctx.CurrentPageIndex + 1;
            _tocRegistered = true;
        }
        base.RenderContent(ctx, contentWidth);
    }
}
