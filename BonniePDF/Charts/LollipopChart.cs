using System.Collections.Generic;
using BonniePDF.Drawing;
using BonniePDF.Models;

namespace BonniePDF.Charts;

/// <summary>
/// Lollipop chart that renders vertical lines (stems) with circular markers (heads).
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class LollipopChart : CartesianChartBase
{
    private const string ColorHex = "#4f46e5";
    private const float CircleRadius = 8;

    /// <summary>
    /// Color for the lollipop stems and heads
    /// </summary>
    public Color LollipopColor { get; set; } = Color.FromHex(ColorHex);

    /// <summary>
    /// Width of the stem line
    /// </summary>
    public float StemWidth { get; set; } = 2f;

    /// <summary>
    /// Radius of the lollipop head (circle)
    /// </summary>
    public float HeadRadius { get; set; } = CircleRadius;

    /// <summary>
    /// Draws the lollipop chart elements (stems and heads).
    /// </summary>
    protected override void DrawChartElements(ICanvas canvas, ChartData data, List<(float x, float y)> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        // Draw lollipop stems (vertical lines)
        DrawLollipopStems(canvas, coordinates, baselineY);

        // Draw lollipop heads (circles)
        DrawLollipopHeads(canvas, coordinates);
    }

    /// <summary>
    /// Gets the color to use for value labels.
    /// </summary>
    protected override Color GetLabelColor() => LollipopColor;

    /// <summary>
    /// Gets the vertical offset for value labels (accounts for lollipop head radius).
    /// </summary>
    protected override float GetLabelVerticalOffset() => HeadRadius + 7;

    /// <summary>
    /// Draws the lollipop stems (vertical lines from baseline to data point).
    /// </summary>
    private void DrawLollipopStems(ICanvas canvas, List<(float x, float y)> coordinates, float baselineY)
    {
        foreach ((float x, float y) in coordinates) canvas.DrawLine(x, baselineY, x, y, LollipopColor, StemWidth);
    }

    /// <summary>
    /// Draws the lollipop heads (circles at data points).
    /// </summary>
    private void DrawLollipopHeads(ICanvas canvas, List<(float x, float y)> coordinates)
    {
        foreach ((float x, float y) in coordinates) canvas.FillCircle(x, y, HeadRadius, LollipopColor);
    }
}
