using System;
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

    /// <summary>Bars and gridlines are axis-aligned, so crisp edges render sharper than antialiased.</summary>
    public GroupedBarChart()
    {
        SvgShapeRendering = "crispEdges";
    }

    /// <summary>Creates a grouped bar chart bound to its document data.</summary>
    public GroupedBarChart(ChartData data) : this()
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <summary>
    /// Draws the grouped bar chart elements for a single series.
    /// In vertical mode draws grouped vertical bars; in horizontal mode draws grouped horizontal bars.
    /// </summary>
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        int seriesCount = data.Series.Count;
        int categoryCount = series.DataPoints.Count;
        int seriesIndex = data.Series.IndexOf(series);

        if (Orientation == ChartOrientation.Horizontal)
        {
            float totalGroupSpace = chartAreaHeight / categoryCount;
            float groupPadding = totalGroupSpace * GroupGapRatio;
            float barGroupHeight = totalGroupSpace - groupPadding;
            float barHeight = (barGroupHeight / seriesCount) * BarWidthRatio;
            float topAreaY = baselineY - chartAreaHeight;

            for (int i = 0; i < coordinates.Count; i++)
            {
                var p = coordinates[i];
                float barWidth = p.X - LeftPadding;

                float groupStartY = topAreaY + i * totalGroupSpace + groupPadding / 2;
                float barY = groupStartY + seriesIndex * (barGroupHeight / seriesCount)
                             + (barGroupHeight / seriesCount - barHeight) / 2;

                canvas.FillRect(LeftPadding, barY, barWidth, barHeight, series.Color);
            }
        }
        else
        {
            float totalGroupSpace = chartAreaWidth / categoryCount;
            float groupPadding = totalGroupSpace * GroupGapRatio;
            float barGroupWidth = totalGroupSpace - groupPadding;
            float barWidth = (barGroupWidth / seriesCount) * BarWidthRatio;

            for (int i = 0; i < coordinates.Count; i++)
            {
                var p = coordinates[i];
                float barHeight = baselineY - p.Y;

                float groupStartX = LeftPadding + i * totalGroupSpace + groupPadding / 2;
                float barX = groupStartX + seriesIndex * (barGroupWidth / seriesCount)
                             + (barGroupWidth / seriesCount - barWidth) / 2;

                canvas.FillRect(barX, p.Y, barWidth, barHeight, series.Color);
            }
        }
    }

    /// <summary>
    /// Gets the offset for value labels (simple spacing above/beside bars).
    /// </summary>
    protected override float GetLabelVerticalOffset(ChartSeries series) => 7;

    /// <summary>
    /// Draws category-axis labels using the first series' labels.
    /// In vertical mode: below each group. In horizontal mode: to the left of each group.
    /// </summary>
    protected override void DrawXAxisLabels(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        if (data.Series.Count == 0) return;

        ChartSeries firstSeries = data.Series[0];
        int dataPointCount = firstSeries.DataPoints.Count;

        if (Orientation == ChartOrientation.Horizontal)
        {
            float topY = baselineY - chartAreaHeight;
            float totalGroupSpace = chartAreaHeight / dataPointCount;

            for (int i = 0; i < dataPointCount; i++)
            {
                float y = topY + i * totalGroupSpace + totalGroupSpace / 2;
                string label = firstSeries.DataPoints[i].Label;
                canvas.DrawText(LeftPadding - 8, y - AxisLabelFontSize / 2, label,
                    AxisColor, AxisLabelFontSize, FontFamily, TextAlignment.Right);
            }
        }
        else
        {
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
}
