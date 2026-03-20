using BonniePDF.Rendering;

namespace BonniePDF.Elements.Layout;

/// <summary>
/// An invisible element that reserves vertical space.
/// Use this to add gaps between content without relying on margins.
/// </summary>
public class Spacer : ElementBase
{
    /// <summary>
    /// The vertical space to reserve, in points.
    /// </summary>
    public float Height { get; set; }

    public Spacer(float height)
    {
        Height = height;
    }

    public Spacer() { }

    protected override float MeasureContentHeight(float contentWidth) => Height;

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        ctx.CurrentY += Height;
    }
}
