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
    /// Font size for value labels above lollipop heads
    /// </summary>
    public float LabelFontSize { get; set; } = 9;

    /// <summary>
    /// Whether to show value labels above each lollipop
    /// </summary>
    public bool ShowValueLabels { get; set; } = true;

    /// <summary>
    /// Draws the lollipop chart content.
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

        // Draw lollipop stems (vertical lines)
        DrawLollipopStems(canvas, coordinates, baselineY);

        // Draw lollipop heads (circles)
        DrawLollipopHeads(canvas, coordinates);

        // Draw value labels above lollipop heads
        if (ShowValueLabels) DrawValueLabels(canvas, data, coordinates);

        // Draw X-axis labels (data point labels)
        DrawXAxisLabels(canvas, data, coordinates, baselineY);

        // Draw X-axis legend
        if (!string.IsNullOrEmpty(data.XAxis.Legend))
            DrawXAxisLegend(canvas, data.XAxis.Legend, chartAreaWidth, baselineY);
    }

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

    /// <summary>
    /// Draws value labels above each lollipop head.
    /// </summary>
    private void DrawValueLabels(ICanvas canvas, ChartData data, List<(float x, float y)> coordinates)
    {
        Color labelColor = LollipopColor;

        for (int i = 0; i < data.DataPoints.Count; i++)
        {
            (float x, float y) = coordinates[i];
            string text = data.DataPoints[i].FormattedValue;

            // Draw text above the lollipop head
            canvas.DrawText(x, y - HeadRadius - 7 - LabelFontSize, text, labelColor, LabelFontSize, FontFamily,
                TextAlignment.Center);
        }
    }
}