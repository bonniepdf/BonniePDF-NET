using BonniePDF.Models;

namespace BonniePDF.Layout.Elements;

/// <summary>
/// A filled circle element, useful for status indicators, bullets, etc.
/// </summary>
public class Circle : ElementBase
{
    /// <summary>
    /// Diameter of the circle in points
    /// </summary>
    public float Diameter { get; set; } = 8;

    /// <summary>
    /// Fill color of the circle
    /// </summary>
    public Color FillColor { get; set; } = Color.Black;

    protected override float MeasureContentHeight(float contentWidth)
    {
        return Diameter;
    }

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        float radius = Diameter / 2;

        // Calculate center position (Screen Coordinates)
        float centerX = ctx.CurrentX + radius;
        float centerY = ctx.CurrentY + radius;

        ctx.Canvas.FillCircle(centerX, centerY, radius, FillColor);

        // Advance Y position by the diameter
        ctx.CurrentY += Diameter;
    }
}