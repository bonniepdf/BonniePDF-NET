using System;
using System.Diagnostics.CodeAnalysis;
using BonniePDF.Rendering;

namespace BonniePDF.Elements.Text;

/// <summary>
/// Renders the page number on which a named <see cref="Section"/> (or anchored
/// <see cref="Heading"/>) sits - the "see page 42" cross-reference. The number is
/// resolved from the shared <see cref="RenderContext.TocEntries"/> registry,
/// which is seeded by the two-pass layout (a document containing a
/// <see cref="PageNumberOf"/> is laid out twice; see
/// <c>TocLayout.RequiresTwoPass</c>).
/// </summary>
/// <remarks>
/// It is a <see cref="TextBlock"/> at heart, so it inherits all text styling
/// (font, size, colour, alignment, decorations). Until the anchor is resolved
/// (the throw-away first pass, or an unknown id) it renders the
/// <see cref="Placeholder"/>.
/// </remarks>
public class PageNumberOf : TextBlock
{
    /// <summary>The anchor id whose page number is printed.</summary>
    public string AnchorTarget { get; }

    /// <summary>
    /// Text shown before the page is resolved (dry-run pass) or when the anchor id
    /// is unknown. Kept short and stable so it does not disturb pagination.
    /// </summary>
    public string Placeholder { get; set; } = "?";

    /// <summary>
    /// Creates a new page-number cross-reference.
    /// </summary>
    /// <param name="anchorTarget">The id of the <see cref="Section"/>/anchor to report.</param>
    [SetsRequiredMembers]
    public PageNumberOf(string anchorTarget) : base(string.Empty)
    {
        if (string.IsNullOrEmpty(anchorTarget))
            throw new ArgumentException("PageNumberOf anchorTarget cannot be null or empty", nameof(anchorTarget));
        AnchorTarget = anchorTarget;
    }

    /// <summary>Stable node name for the layout snapshot tree.</summary>
    public override string CaptureType => "PageNumberOf";

    /// <summary>
    /// Refreshes <see cref="TextBlock.Text"/> from the resolved page number before
    /// the base class computes lines. Clears the line cache so a value that
    /// changed between passes is re-measured.
    /// </summary>
    private void ResolveText(RenderContext ctx)
    {
        string next = ctx.TocEntries.TryGetValue(AnchorTarget, out int page)
            ? page.ToString()
            : Placeholder;
        if (next != Text)
        {
            Text = next;
            ResetState();
        }
    }

    /// <inheritdoc />
    public override RenderStatus RenderStateful(RenderContext ctx, float width)
    {
        ResolveText(ctx);
        return base.RenderStateful(ctx, width);
    }

    /// <inheritdoc />
    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        ResolveText(ctx);
        base.RenderContent(ctx, contentWidth);
    }
}
