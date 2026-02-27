using System.Collections.Generic;
using BonniePDF.Charts.Models;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Elements.Text;

namespace BonniePDF.Charts;

/// <summary>
/// Grouped bar chart that renders multiple bars per category.
/// Extends CartesianChartBase and uses standard ChartData model with ChartSeries.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class GroupedBarChart : CartesianChartBase
{
    /// <summary>
    /// Width of bars as percentage of available space (0.0 to 1.0, default: 0.6).
    /// </summary>
    public float BarWidthRatio { get; set; } = 0.6f;

    /// <summary>
    /// Gap between groups as percentage of available space (0.0 to 1.0, default: 0.2).
    /// </summary>
    public float GroupGapRatio { get; set; } = 0.2f;

    /// <summary>
    /// Draws the grouped bar chart elements for a single series.
    /// </summary>
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        // Calculate bar dimensions
        int seriesCount = data.Series.Count;
        int categoryCount = series.DataPoints.Count;

        float totalGroupSpace = chartAreaWidth / categoryCount;
        float groupPadding = totalGroupSpace * GroupGapRatio;
        float barGroupWidth = totalGroupSpace - groupPadding;
        float barWidth = (barGroupWidth / seriesCount) * BarWidthRatio;

        // Find this series' index
        int seriesIndex = data.Series.IndexOf(series);

        // Draw bars
        for (int i = 0; i < coordinates.Count; i++)
        {
            var p = coordinates[i];
            float barHeight = baselineY - p.Y;

            // Calculate bar position within group
            float groupStartX = LeftPadding + i * totalGroupSpace + groupPadding / 2;
            float barX = groupStartX + seriesIndex * (barGroupWidth / seriesCount) +
                         (barGroupWidth / seriesCount - barWidth) / 2;

            canvas.FillRect(barX, p.Y, barWidth, barHeight, series.Color);
        }
    }

    /// <summary>
    /// Gets the vertical offset for value labels (simple spacing above bars).
    /// </summary>
    protected override float GetLabelVerticalOffset(ChartSeries series) => 7;

    /// <summary>
    /// Draws X-axis labels using first series' category labels.
    /// </summary>
    protected override void DrawXAxisLabels(ICanvas canvas, ChartData data, float chartAreaWidth, float baselineY)
    {
        if (data.Series.Count == 0) return;

        // Use the first series' labels for the X-axis
        ChartSeries firstSeries = data.Series[0];
        int dataPointCount = firstSeries.DataPoints.Count;

        float labelY = baselineY + 15;
        float totalGroupSpace = chartAreaWidth / dataPointCount;

        for (int i = 0; i < dataPointCount; i++)
        {
            float x = LeftPadding + i * totalGroupSpace + totalGroupSpace / 2;
            string label = firstSeries.DataPoints[i].Label;

            canvas.DrawText(x, labelY, label, AxisColor, AxisLabelFontSize, FontFamily, TextAlignment.Center);
        }
    }
}
