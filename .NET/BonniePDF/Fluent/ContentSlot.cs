using BonniePDF.Elements;
using BonniePDF.Rendering;

namespace BonniePDF.Fluent;

/// <summary>
/// A lightweight element that acts as both an IElement (can be added to layout containers)
/// and an IContentContainer (can be the target of fluent chaining).
/// Used by descriptors as item slots in Column, Row, and Table layouts.
/// </summary>
internal class ContentSlot : ElementBase, IContentContainer
{
    public IElement? Child { get; set; }

    protected override float MeasureContentHeight(float contentWidth)
    {
        return Child?.MeasureHeight(contentWidth) ?? 0;
    }

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        Child?.Render(ctx, contentWidth);
    }

    protected override float GetPreferredContentWidth()
    {
        return Child?.GetPreferredWidth() ?? 0;
    }
}
