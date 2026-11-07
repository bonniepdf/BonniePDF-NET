
using System;
using BonniePDF.Drawing;

namespace BonniePDF;

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

    /// <summary>Alternating dash and gap lengths in points.</summary>
    public float[]? DashPattern { get; set; }

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
        bool horizontal = Orientation == LineOrientation.Horizontal;
        float length = Length ?? (horizontal ? contentWidth : 100);
        float x = ctx.CurrentX;
        float y = ctx.CurrentY;
        if (DashPattern is not { Length: > 0 } pattern)
        {
            ctx.Canvas.FillRect(x, y, horizontal ? length : Thickness, horizontal ? Thickness : length, Color);
        }
        else
        {
            foreach (float dash in pattern)
            {
                if (!float.IsFinite(dash) || dash <= 0)
                    throw new ArgumentOutOfRangeException(nameof(DashPattern), "Dash lengths must be finite and positive.");
            }

            float half = Thickness / 2;
            ctx.Canvas.BeginPath();
            ctx.Canvas.MoveTo(x + (horizontal ? 0 : half), y + (horizontal ? half : 0));
            ctx.Canvas.LineTo(x + (horizontal ? length : half), y + (horizontal ? half : length));
            ctx.Canvas.StrokeCurrentPath(new StrokeStyle { Paint = Color, Width = Thickness, DashPattern = pattern });
        }

        ctx.CurrentY += horizontal ? Thickness : length;
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
