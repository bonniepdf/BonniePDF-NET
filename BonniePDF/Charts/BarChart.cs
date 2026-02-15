using System;
using System.Collections.Generic;
using BonniePDF.Drawing;
using BonniePDF.Models;

namespace BonniePDF.Charts;

/// <summary>
/// Bar chart that renders vertical bars with value labels above each bar.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class BarChart : CartesianChartBase
{
    private const string BarColorHex = "#4f46e5";

    /// <summary>
    /// Color for the bars
    /// </summary>
    public Color BarColor { get; set; } = Color.FromHex(BarColorHex);

    /// <summary>
    /// Font size for value labels above bars
    /// </summary>
    public float LabelFontSize { get; set; } = 9;

    /// <summary>
    /// Whether to show value labels above each bar
    /// </summary>
    public bool ShowValueLabels { get; set; } = true;

    /// <summary>
    /// Draws the bar chart elements (vertical bars).
    /// </summary>
    protected override void DrawChartElements(ICanvas canvas, ChartData data, List<(float x, float y)> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        // Calculate bar width dynamically
        int dataPointCount = data.DataPoints.Count;
        float totalBarSpace = chartAreaWidth / dataPointCount;
        float barWidth = Math.Min(totalBarSpace * 0.6f, 50); // 60% of space, max 50px

        // Draw bars
        DrawBars(canvas, coordinates, barWidth, baselineY);
    }

    /// <summary>
    /// Draws value labels above each bar.
    /// </summary>
    protected override void DrawValueLabels(ICanvas canvas, ChartData data, List<(float x, float y)> coordinates)
    {
        if (!ShowValueLabels) return;

        Color labelColor = BarColor;

        for (int i = 0; i < data.DataPoints.Count; i++)
        {
            (float x, float y) = coordinates[i];
            string text = data.DataPoints[i].FormattedValue;

            // Draw text above the bar (y - 7 for spacing)
            canvas.DrawText(x, y - 7 - LabelFontSize, text, labelColor, LabelFontSize, FontFamily,
                TextAlignment.Center);
        }
    }

    /// <summary>
    /// Draws the bars for each data point.
    /// </summary>
    private void DrawBars(ICanvas canvas, List<(float x, float y)> coordinates, float barWidth, float baselineY)
    {
        for (int i = 0; i < coordinates.Count; i++)
        {
            (float x, float y) = coordinates[i];
            float barHeight = baselineY - y;
            float barX = x - barWidth / 2; // Center the bar on the data point

            canvas.FillRect(barX, y, barWidth, barHeight, BarColor);
        }
    }
}