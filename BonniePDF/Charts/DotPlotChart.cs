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
    /// Font size for value labels above data points
    /// </summary>
    public float LabelFontSize { get; set; } = 9;

    /// <summary>
    /// Whether to show value labels above each data point
    /// </summary>
    public bool ShowValueLabels { get; set; } = true;

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
    /// Draws value labels above each data point.
    /// </summary>
    protected override void DrawValueLabels(ICanvas canvas, ChartData data, List<(float x, float y)> coordinates)
    {
        if (!ShowValueLabels) return;

        Color labelColor = DotColor;

        for (int i = 0; i < data.DataPoints.Count; i++)
        {
            (float x, float y) = coordinates[i];
            string text = data.DataPoints[i].FormattedValue;

            // Draw text above the data point
            canvas.DrawText(x, y - DotRadius - 7 - LabelFontSize, text, labelColor, LabelFontSize, FontFamily,
                TextAlignment.Center);
        }
    }

    /// <summary>
    /// Draws the data point circles.
    /// </summary>
    private void DrawDataPoints(ICanvas canvas, List<(float x, float y)> coordinates)
    {
        foreach ((float x, float y) in coordinates) canvas.FillCircle(x, y, DotRadius, DotColor);
    }
}