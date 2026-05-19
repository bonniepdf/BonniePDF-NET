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
    /// Draws the bar chart elements for a single series.
    /// In vertical mode draws upward bars; in horizontal mode draws rightward bars.
    /// </summary>
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        int dataPointCount = series.DataPoints.Count;

        if (Orientation == ChartOrientation.Horizontal)
        {
            float totalBarSpace = chartAreaHeight / dataPointCount;
            float barThickness = Math.Min(totalBarSpace * BarWidthRatio, 50);
            DrawBars(canvas, coordinates, barThickness, baselineY, series.Color);
        }
        else
        {
            float totalBarSpace = chartAreaWidth / dataPointCount;
            float barWidth = Math.Min(totalBarSpace * BarWidthRatio, 50);
            DrawBars(canvas, coordinates, barWidth, baselineY, series.Color);
        }
    }

    /// <summary>
    /// Gets the offset for value labels (spacing above bars in vertical mode, to the right in horizontal).
    /// </summary>
    protected override float GetLabelVerticalOffset(ChartSeries series) => 7;

    /// <summary>
    /// Draws bars for each data point.
    /// In vertical mode bars grow upward; in horizontal mode bars grow rightward from LeftPadding.
    /// </summary>
    private void DrawBars(ICanvas canvas, IList<Point> coordinates, float thickness, float baselineY, Color color)
    {
        foreach (Point p in coordinates)
        {
            if (Orientation == ChartOrientation.Horizontal)
            {
                float barWidth = p.X - LeftPadding;
                canvas.FillRect(LeftPadding, p.Y - thickness / 2, barWidth, thickness, color);
            }
            else
            {
                float barHeight = baselineY - p.Y;
                float barX = p.X - thickness / 2;
                canvas.FillRect(barX, p.Y, thickness, barHeight, color);
            }
        }
    }

    /// <summary>
    /// Gets the default color for the series when converting from old API (DataPoints) to new API (Series).
    /// Returns the chart's BarColor property for backward compatibility.
    /// </summary>
    protected override Color GetDefaultSeriesColor() => BarColor;
}