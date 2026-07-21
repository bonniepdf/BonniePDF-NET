using System;
using BonniePDF.Rendering;

namespace BonniePDF.Core;

/// <summary>
/// A clickable internal cross-reference. Unlike a plain <see cref="Hyperlink"/>
/// (which needs an explicit <see cref="Hyperlink.TargetPageIndex"/>), a
/// SectionLink targets a named <see cref="Elements.Text.Section"/> - or a
/// <see cref="Elements.Text.Heading"/> carrying an <c>AnchorId</c> - by id and
/// resolves the destination page at render time from the shared
/// <see cref="RenderContext.TocEntries"/> registry.
/// </summary>
/// <remarks>
/// Because the destination page is only known once layout has run, a document
/// containing a SectionLink is laid out in two passes (see
/// <c>TocLayout.RequiresTwoPass</c>): a throw-away pass seeds every anchor's page
/// number, then the real pass emits a <c>/GoTo</c> annotation pointing at it.
/// </remarks>
public class SectionLink : Hyperlink
{
    /// <summary>
    /// The anchor id this link jumps to (a <see cref="Elements.Text.Section"/>
    /// or anchored <see cref="Elements.Text.Heading"/>).
    /// </summary>
    public string AnchorTarget { get; }

    /// <summary>
    /// Creates a new internal cross-reference link.
    /// </summary>
    /// <param name="text">The visible link text.</param>
    /// <param name="anchorTarget">The id of the <see cref="Elements.Text.Section"/>/anchor to jump to.</param>
    public SectionLink(string text, string anchorTarget) : base(text)
    {
        if (string.IsNullOrEmpty(anchorTarget))
            throw new ArgumentException("SectionLink anchorTarget cannot be null or empty", nameof(anchorTarget));
        AnchorTarget = anchorTarget;
    }

    /// <summary>
    /// Resolves <see cref="AnchorTarget"/> to a 0-based page index from the
    /// seeded TOC registry, then delegates to the base <see cref="Hyperlink"/>
    /// render/annotation path. Falls back to the first page when the target was
    /// never registered (unknown id / single-pass layout), matching the
    /// VisualToc behaviour.
    /// </summary>
    public override void Render(RenderContext ctx, float width)
    {
        TargetPageIndex = ctx.TocEntries.TryGetValue(AnchorTarget, out int pageNumber)
            ? pageNumber - 1
            : 0;
        base.Render(ctx, width);
    }
}
