using System;

namespace BonniePDF;

/// <summary>
/// An invisible, zero-height named anchor. When rendered, a Section records the
/// 1-based page number on which it sits against its <see cref="AnchorId"/> in the
/// shared <see cref="RenderContext.TocEntries"/> registry, so that a
/// <see cref="SectionLink"/> can jump to it and a <see cref="PageNumberOf"/>
/// can print "see page 42".
/// </summary>
/// <remarks>
/// A Section paints nothing and occupies no vertical space; it simply marks a
/// position in the flow. Use it where the anchor is not itself a visible heading
/// (a <see cref="Heading"/> can double as an anchor via its <c>AnchorId</c>).
/// </remarks>
public class Section : ElementBase
{
    /// <summary>
    /// The unique anchor identifier. A <see cref="SectionLink"/> or
    /// <see cref="PageNumberOf"/> referencing this id resolves to the page this
    /// Section rendered on.
    /// </summary>
    public string AnchorId { get; }

    /// <summary>
    /// Creates a new named anchor.
    /// </summary>
    /// <param name="anchorId">The unique identifier links and page references target.</param>
    public Section(string anchorId)
    {
        if (string.IsNullOrEmpty(anchorId))
            throw new ArgumentException("Section anchorId cannot be null or empty", nameof(anchorId));
        AnchorId = anchorId;
    }

    /// <summary>Stable node name for the layout snapshot tree.</summary>
    public override string CaptureType => "Section";

    /// <summary>An anchor takes up no space.</summary>
    protected override float MeasureContentHeight(float contentWidth) => 0;

    /// <summary>
    /// Records this anchor's resolved page number. Runs on every layout pass
    /// (including the two-pass dry run) so the value is always current; a later
    /// page always overwrites an earlier throw-away one for the same id.
    /// </summary>
    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        ctx.TocEntries[AnchorId] = ctx.CurrentPageIndex + 1;
    }
}
