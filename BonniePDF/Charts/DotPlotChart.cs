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
    /// Draws the dot plot chart content.
    /// </summary>
    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        int dataPointCount = data.DataPoints.Count;
        if (dataPointCount == 0) return;

        // Calculate dynamic layout based on chart dimensions
        (float chartAreaWidth, float chartAreaHeight, float baselineY, float topY) = CalculateLayout();

        // Calculate coordinates for all data points
        List<(float x, float y)> coordinates = CalculateCoordinates(data, chartAreaWidth, chartAreaHeight, baselineY);

        // Draw grid lines first (behind everything)
        if (data.YAxis.ShowGridLines) DrawGridLines(canvas, data, chartAreaWidth, chartAreaHeight, baselineY, topY);

        // Draw axis lines
        DrawAxisLines(canvas, chartAreaWidth, baselineY, topY);

        // Draw Y-axis labels
        DrawYAxisLabels(canvas, data, chartAreaHeight, baselineY, topY);

        // Draw Y-axis legend
        if (!string.IsNullOrEmpty(data.YAxis.Legend)) DrawYAxisLegend(canvas, data.YAxis.Legend, chartAreaHeight, topY);

        // Draw data point circles
        DrawDataPoints(canvas, coordinates);

        // Draw value labels above data points
        if (ShowValueLabels) DrawValueLabels(canvas, data, coordinates);

        // Draw X-axis labels (data point labels)
        DrawXAxisLabels(canvas, data, coordinates, baselineY);

        // Draw X-axis legend
        if (!string.IsNullOrEmpty(data.XAxis.Legend))
            DrawXAxisLegend(canvas, data.XAxis.Legend, chartAreaWidth, baselineY);
    }

    /// <summary>
    /// Draws the data point circles.
    /// </summary>
    private void DrawDataPoints(ICanvas canvas, List<(float x, float y)> coordinates)
    {
        foreach ((float x, float y) in coordinates) canvas.FillCircle(x, y, DotRadius, DotColor);
    }

    /// <summary>
    /// Draws value labels above each data point.
    /// </summary>
    private void DrawValueLabels(ICanvas canvas, ChartData data, List<(float x, float y)> coordinates)
    {
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
}