using System;
using System.Collections.Generic;
using BonniePDF.Charts.Models;
using BonniePDF.Core;
using BonniePDF.Drawing;

namespace BonniePDF.Charts;

/// <summary>
/// Bar chart that renders vertical bars with value labels above each bar.
/// Now supports multiple series.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class BarChart : CartesianChartBase
{
    private const string BarColorHex = "#4f46e5";

    /// <summary>
    /// Color for the bars (backward compatibility).
    /// </summary>
    public Color BarColor { get; set; } = Color.FromHex(BarColorHex);

    /// <summary>
    /// Width of bars as percentage of available space (0.0 to 1.0, default: 0.6).
    /// </summary>
    public float BarWidthRatio { get; set; } = 0.6f;

    /// <summary>
    /// Draws the bar chart elements for a single series (vertical bars).
    /// </summary>
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        // Calculate bar width dynamically
        int dataPointCount = series.DataPoints.Count;
        float totalBarSpace = chartAreaWidth / dataPointCount;
        float barWidth = Math.Min(totalBarSpace * BarWidthRatio, 50); // 60% of space, max 50px

        // Draw bars
        DrawBars(canvas, coordinates, barWidth, baselineY, series.Color);
    }

    /// <summary>
    /// Gets the vertical offset for value labels (simple spacing above bars).
    /// </summary>
    protected override float GetLabelVerticalOffset(ChartSeries series) => 7;

    /// <summary>
    /// Draws the bars for each data point.
    /// </summary>
    private static void DrawBars(ICanvas canvas, IList<Point> coordinates, float barWidth, float baselineY, Color color)
    {
        foreach (Point p in coordinates)
        {
            float barHeight = baselineY - p.Y;
            float barX = p.X - barWidth / 2; // Center the bar on the data point
            canvas.FillRect(barX, p.Y, barWidth, barHeight, color);
        }
    }

    /// <summary>
    /// Gets the default color for the series when converting from old API (DataPoints) to new API (Series).
    /// Returns the chart's BarColor property for backward compatibility.
    /// </summary>
    protected override Color GetDefaultSeriesColor() => BarColor;
}