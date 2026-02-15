using System.Collections.Generic;
using BonniePDF.Drawing;
using BonniePDF.Models;

namespace BonniePDF.Charts;

/// <summary>
/// Dot plot chart that renders circular data points with value labels.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class DotPlotChart : CartesianChartBase
{
    private const string ColorHex = "#4f46e5";
    private const float CircleRadius = 10;

    /// <summary>
    /// Color for the data point circles
    /// </summary>
    public Color DotColor { get; set; } = Color.FromHex(ColorHex);

    /// <summary>
    /// Radius of the data point circles
    /// </summary>
    public float DotRadius { get; set; } = CircleRadius;

    /// <summary>
    /// Draws the dot plot chart elements (circular data points).
    /// </summary>
    protected override void DrawChartElements(ICanvas canvas, ChartData data, List<(float x, float y)> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        // Draw data point circles
        DrawDataPoints(canvas, coordinates);
    }

    /// <summary>
    /// Gets the color to use for value labels.
    /// </summary>
    protected override Color GetLabelColor() => DotColor;

    /// <summary>
    /// Gets the vertical offset for value labels (accounts for dot radius).
    /// </summary>
    protected override float GetLabelVerticalOffset() => DotRadius + 7;

    /// <summary>
    /// Draws the data point circles.
    /// </summary>
    private void DrawDataPoints(ICanvas canvas, List<(float x, float y)> coordinates)
    {
        foreach ((float x, float y) in coordinates) canvas.FillCircle(x, y, DotRadius, DotColor);
    }
}
