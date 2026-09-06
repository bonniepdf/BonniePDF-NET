using BonniePDF.Core;
using BonniePDF.Rendering;

namespace BonniePDF.Elements.Shapes;

/// <summary>
/// Draws a filled and optionally stroked ellipse at the current flow position.
/// </summary>
public class Ellipse : ElementBase
{
    public float Width { get; set; } = 16;
    public float Height { get; set; } = 8;
    public Color FillColor { get; set; } = Color.Black;

    protected override float MeasureContentHeight(float contentWidth)
    {
        return Height;
    }

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        float rx = Width / 2;
        float ry = Height / 2;
        float centerX = ctx.CurrentX + rx;
        float centerY = ctx.CurrentY + ry;

        ctx.Canvas.FillEllipse(centerX, centerY, rx, ry, FillColor);
        ctx.CurrentY += Height;
    }
}
