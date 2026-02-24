using System;
using System.Collections.Generic;
using BonniePDF.Charts.Models;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Elements.Text;

namespace BonniePDF.Charts;

/// <summary>
/// Represents a single data series in a grouped bar chart.
/// OBSOLETE: Use ChartSeries instead for consistency with other chart types.
/// This class is maintained for backward compatibility.
/// </summary>
[Obsolete("Use ChartSeries instead for consistency with other chart types")]
public class BarSeries
{
    /// <summary>
    /// The name/label of this series (e.g., "Product A", "Q1 Sales")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The color to use for bars in this series
    /// </summary>
    public Color Color { get; set; } = Color.FromHex("#4f46e5");

    /// <summary>
    /// The data points for this series
    /// </summary>
    public List<decimal> Values { get; set; } = new();
}

/// <summary>
/// Grouped bar chart that renders multiple bars per category.
/// Now extends CartesianChartBase and uses standard ChartData model with ChartSeries.
/// This eliminates code duplication and provides consistent API with other chart types.
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

    #region Backward Compatibility Properties

    /// <summary>
    /// The data series to display (each series will have bars in a different color).
    /// OBSOLETE: Use ChartData.Series instead via standard ChartData model.
    /// </summary>
    [Obsolete("Use ChartData.Series instead via standard ChartData model")]
    public List<BarSeries> Series { get; set; } = new();

    /// <summary>
    /// The labels for each category/group on x-axis.
    /// OBSOLETE: Use ChartSeries[].DataPoints[].Label instead.
    /// </summary>
    [Obsolete("Use ChartSeries[].DataPoints[].Label instead")]
    public List<string> CategoryLabels { get; set; } = new();

    /// <summary>
    /// Maximum value for y-axis.
    /// OBSOLETE: Use ChartData.YAxis.MaxValue instead.
    /// </summary>
    [Obsolete("Use ChartData.YAxis.MaxValue instead")]
    public decimal YAxisMaxValue { get; set; }

    /// <summary>
    /// Prefix for y-axis values (e.g., "£", "$").
    /// OBSOLETE: Use ChartData.YAxis.Prefix instead.
    /// </summary>
    [Obsolete("Use ChartData.YAxis.Prefix instead")]
    public string YAxisPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Number of grid lines/steps on y-axis.
    /// OBSOLETE: Use ChartData.YAxis.StepCount instead.
    /// </summary>
    [Obsolete("Use ChartData.YAxis.StepCount instead")]
    public int YAxisStepCount { get; set; } = 5;

    /// <summary>
    /// Whether to show grid lines.
    /// OBSOLETE: Use ChartData.YAxis.ShowGridLines instead.
    /// </summary>
    [Obsolete("Use ChartData.YAxis.ShowGridLines instead")]
    public bool ShowGridLines { get; set; } = true;

    #endregion

    /// <summary>
    /// Draws chart to specified canvas.
    /// OVERRIDE to handle backward compatibility by converting old API to new format before rendering.
    /// This is called before DrawContent, so we can populate data.Series before base class checks it.
    /// </summary>
    public override void Draw(ICanvas canvas, ChartData data)
    {
        // Handle backward compatibility: if old API is used, convert to new format BEFORE base class renders
        if (Series.Count > 0 && data.Series.Count == 0)
        {
            // Convert old BarSeries format to new ChartSeries format
            foreach (var oldSeries in Series)
            {
                var newSeries = new ChartSeries
                {
                    Name = oldSeries.Name,
                    Color = oldSeries.Color
                };

                // Convert decimal values to ChartDataPoint
                for (int i = 0; i < oldSeries.Values.Count; i++)
                {
                    string label = i < CategoryLabels.Count ? CategoryLabels[i] : $"Cat{i}";
                    newSeries.DataPoints.Add(new ChartDataPoint
                    {
                        Label = label,
                        Value = oldSeries.Values[i]
                    });
                }

                data.Series.Add(newSeries);
            }

            // Set Y-axis options from old properties
            if (YAxisMaxValue > 0)
                data.YAxis.MaxValue = YAxisMaxValue;
            if (!string.IsNullOrEmpty(YAxisPrefix))
                data.YAxis.Prefix = YAxisPrefix;
            if (YAxisStepCount > 0)
                data.YAxis.StepCount = YAxisStepCount;
            data.YAxis.ShowGridLines = ShowGridLines;
        }

        // Call base implementation to draw chart
        base.Draw(canvas, data);
    }

    /// <summary>
    /// Draws the grouped bar chart elements for a single series.
    /// </summary>
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        List<(float x, float y)> coordinates,
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
            (float x, float y) = coordinates[i];
            float barHeight = baselineY - y;

            // Calculate bar position within group
            float groupStartX = LeftPadding + i * totalGroupSpace + groupPadding / 2;
            float barX = groupStartX + seriesIndex * (barGroupWidth / seriesCount) +
                         (barGroupWidth / seriesCount - barWidth) / 2;

            canvas.FillRect(barX, y, barWidth, barHeight, series.Color);
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

    #region Backward Compatibility - Obsolete Method

    /// <summary>
    /// OBSOLETE: This method is no longer needed as we extend CartesianChartBase.
    /// </summary>
    [Obsolete("This method is no longer needed as we extend CartesianChartBase")]
    protected override void DrawChartElements(ICanvas canvas, ChartData data, List<(float x, float y)> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        // Not used anymore - handled by DrawSeriesElements
        throw new NotImplementedException("Use DrawSeriesElements instead");
    }

    #endregion
}
