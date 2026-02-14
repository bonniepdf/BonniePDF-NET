using BonniePDF.Models;

namespace BonniePDF.Layout.Elements;

/// <summary>
/// Represents a simple line element that can be horizontal or vertical.
/// Useful for visual dividers and structural lines.
/// </summary>
public class Line : ElementBase
{
    /// <summary>
    /// Gets or sets the thickness of the line in points.
    /// Default is 1.
    /// </summary>
    public float Thickness { get; set; } = 1;

    /// <summary>
    /// Gets or sets the color of the line.
    /// Default is Black.
    /// </summary>
    public Color Color { get; set; } = Color.Black;

    /// <summary>
    /// Gets or sets the orientation of the line.
    /// Default is Horizontal.
    /// </summary>
    public LineOrientation Orientation { get; set; } = LineOrientation.Horizontal;

    /// <summary>
    /// Gets or sets the length of the line.
    /// For Horizontal lines, if null (default), it takes the full available width.
    /// For Vertical lines, this determines the height. If null, it defaults to a standard height (e.g., 100) or 
    /// you might want to specify it explicitly as vertical lines in flow layout usually need a height.
    /// </summary>
    public float? Length { get; set; }

    protected override float MeasureContentHeight(float contentWidth)
    {
        if (Orientation == LineOrientation.Horizontal)
        {
            return Thickness;
        }
        else
        {
            // For vertical lines, we need a height. 
            // If Length is not specified, default to something visible or 0?
            // Usually vertical lines in flow need explicit height.
            return Length ?? 100; 
        }
    }

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        float x = ctx.CurrentX;
        float y = ctx.CurrentY;

        if (Orientation == LineOrientation.Horizontal)
        {
            // Draw horizontal line
            float width = Length ?? contentWidth;
            
            // Adjust Y to center the line in the thickness area? 
            // Or just draw from top edge? Usually top edge is fine, but maybe center it vertically in the allocated space?
            // Since we allocated 'Thickness' height, drawing a line of 'Thickness' width at y + Thickness/2 
            // using a stroke would work, or filling a rectangle.
            
            // Using FillRect is safer for thickness > 1
            ctx.Canvas.FillRect(x, y, width, Thickness, Color);
            
            // Advance Y is handled by MeasureHeight/Render caller usually adding height?
            // No, ElementBase Render adds margins, but inside RenderContent we are just rendering.
            // But wait, ElementBase.Render doesn't advance CurrentY by content height!
            // Let's check ElementBase again.
            
            // ElementBase.Render:
            // 57 |         RenderContent(ctx, contentWidth);
            // ...
            // It does NOT advance Y by content height automatically.
            // The inheriting class MUST advance Y if it consumes vertical space.
            
            ctx.CurrentY += Thickness;
        }
        else
        {
            // Draw vertical line
            float height = Length ?? 100; // Default height if not specified
            
            // Draw vertical line
            ctx.Canvas.FillRect(x, y, Thickness, height, Color);
            
            // Advance Y
            ctx.CurrentY += height;
        }
    }
}

/// <summary>
/// Specifies the orientation of a Line element.
/// </summary>
public enum LineOrientation
{
    Horizontal,
    Vertical
}
