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
    /// Gets the color to use for value labels.
    /// </summary>
    protected override Color GetLabelColor() => BarColor;

    /// <summary>
    /// Gets the vertical offset for value labels (simple spacing above bars).
    /// </summary>
    protected override float GetLabelVerticalOffset() => 7;

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