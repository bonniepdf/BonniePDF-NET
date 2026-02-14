using System.Collections.Generic;
using BonniePDF.Drawing;
using BonniePDF.Models;

namespace BonniePDF.Charts;

/// <summary>
/// Represents a single data series in a grouped bar chart.
/// </summary>
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
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class GroupedBarChart : ChartBase
{
    // Layout constants (relative to chart height)
    private const float TitleAreaHeight = 60; // Space for title at top
    private const float BottomPadding = 100; // Space for x-axis labels, legend, and series names
    private const float LeftPadding = 80; // Space for y-axis labels and legend
    private const float RightPadding = 40; // Right margin
    private const float AxisLineWidth = 1f;
    private const float GridLineWidth = 0.5f;
    private const float AxisLabelFontSize = 10;
    private const float LegendFontSize = 11;
    private const float SeriesLabelFontSize = 9;

    // Styling constants
    private static readonly Color AxisColor = Color.FromHex("#666666");
    private static readonly Color GridLineColor = Color.FromHex("#e0e0e0");

    /// <summary>
    /// The data series to display (each series will have bars in a different color)
    /// </summary>
    public List<BarSeries> Series { get; set; } = new();

    /// <summary>
    /// The labels for each category/group on the x-axis
    /// </summary>
    public List<string> CategoryLabels { get; set; } = new();

    /// <summary>
    /// Maximum value for the y-axis
    /// </summary>
    public decimal YAxisMaxValue { get; set; }

    /// <summary>
    /// Prefix for y-axis values (e.g., "£", "$")
    /// </summary>
    public string YAxisPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Number of grid lines/steps on the y-axis
    /// </summary>
    public int YAxisStepCount { get; set; } = 5;

    /// <summary>
    /// Font family for labels
    /// </summary>
    public string FontFamily { get; set; } = "Arial, sans-serif";

    /// <summary>
    /// Whether to show value labels above each bar
    /// </summary>
    public bool ShowValueLabels { get; set; } = true;

    /// <summary>
    /// Whether to show grid lines
    /// </summary>
    public bool ShowGridLines { get; set; } = true;

    /// <summary>
    /// Draws the grouped bar chart content.
    /// </summary>
    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        int categoryCount = CategoryLabels.Count;
        int seriesCount = Series.Count;

        if (categoryCount == 0 || seriesCount == 0) return;

        // Calculate dynamic layout based on chart dimensions
        float chartAreaWidth = Width - LeftPadding - RightPadding;
        float chartAreaHeight = Height - TitleAreaHeight - BottomPadding;
        float baselineY = TitleAreaHeight + chartAreaHeight;
        float topY = TitleAreaHeight;

        // Calculate bar dimensions
        float totalGroupSpace = chartAreaWidth / categoryCount;
        float groupPadding = totalGroupSpace * 0.2f; // 20% padding between groups
        float barGroupWidth = totalGroupSpace - groupPadding;
        float barWidth = barGroupWidth / seriesCount;

        // Draw grid lines first (behind everything)
        if (ShowGridLines) DrawGridLines(canvas, chartAreaWidth, chartAreaHeight, baselineY, topY);

        // Draw axis lines
        DrawAxisLines(canvas, chartAreaWidth, baselineY, topY);

        // Draw Y-axis labels
        DrawYAxisLabels(canvas, chartAreaHeight, baselineY, topY);

        // Draw bars for each series
        for (int seriesIndex = 0; seriesIndex < seriesCount; seriesIndex++)
        {
            BarSeries series = Series[seriesIndex];

            for (int categoryIndex = 0; categoryIndex < categoryCount; categoryIndex++)
            {
                if (categoryIndex >= series.Values.Count) continue;

                // Calculate bar position
                float groupStartX = LeftPadding + categoryIndex * totalGroupSpace + groupPadding / 2;
                float barX = groupStartX + seriesIndex * barWidth;

                // Calculate bar height
                decimal value = series.Values[categoryIndex];
                decimal normalizedValue = value / YAxisMaxValue;
                float barHeight = (float)(normalizedValue * (decimal)chartAreaHeight);
                float barY = baselineY - barHeight;

                // Draw the bar
                canvas.FillRect(barX, barY, barWidth - 2, barHeight, series.Color); // -2 for small gap between bars

                // Draw value label if enabled
                if (ShowValueLabels)
                {
                    string label = $"{YAxisPrefix}{value:N0}";
                    canvas.DrawText(barX + barWidth / 2 - 1, barY - 7 - SeriesLabelFontSize,
                        label, series.Color, SeriesLabelFontSize, FontFamily, TextAlignment.Center);
                }
            }
        }

        // Draw category labels
        DrawCategoryLabels(canvas, categoryCount, totalGroupSpace, baselineY);

        // Draw legend
        DrawLegend(canvas, seriesCount);
    }

    /// <summary>
    /// Draws horizontal grid lines across the chart area.
    /// </summary>
    private void DrawGridLines(ICanvas canvas, float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        int stepCount = YAxisStepCount;
        if (stepCount <= 0) stepCount = 5;

        float stepHeight = chartAreaHeight / stepCount;

        for (int i = 1; i <= stepCount; i++)
        {
            float y = baselineY - i * stepHeight;
            float x1 = LeftPadding;
            float x2 = LeftPadding + chartAreaWidth;

            canvas.DrawLine(x1, y, x2, y, GridLineColor, GridLineWidth);
        }
    }

    /// <summary>
    /// Draws the X and Y axis lines.
    /// </summary>
    private void DrawAxisLines(ICanvas canvas, float chartAreaWidth, float baselineY, float topY)
    {
        // Y-axis (vertical line on the left)
        canvas.DrawLine(LeftPadding, topY, LeftPadding, baselineY, AxisColor, AxisLineWidth);

        // X-axis (horizontal line at the bottom)
        canvas.DrawLine(LeftPadding, baselineY, LeftPadding + chartAreaWidth, baselineY, AxisColor, AxisLineWidth);
    }

    /// <summary>
    /// Draws Y-axis value labels.
    /// </summary>
    private void DrawYAxisLabels(ICanvas canvas, float chartAreaHeight, float baselineY, float topY)
    {
        int stepCount = YAxisStepCount;
        if (stepCount <= 0) stepCount = 5;

        decimal stepValue = YAxisMaxValue / stepCount;
        float stepHeight = chartAreaHeight / stepCount;

        for (int i = 0; i <= stepCount; i++)
        {
            float y = baselineY - i * stepHeight;
            decimal value = i * stepValue;

            string label = $"{YAxisPrefix}{value:N0}";

            canvas.DrawText(LeftPadding - 8, y - AxisLabelFontSize / 2, label, AxisColor, AxisLabelFontSize, FontFamily,
                TextAlignment.Right);
        }
    }

    /// <summary>
    /// Draws category labels below each group of bars.
    /// </summary>
    private void DrawCategoryLabels(ICanvas canvas, int categoryCount, float totalGroupSpace, float baselineY)
    {
        float labelY = baselineY + 15;

        for (int i = 0; i < categoryCount; i++)
        {
            float x = LeftPadding + i * totalGroupSpace + totalGroupSpace / 2;
            string label = CategoryLabels[i];

            canvas.DrawText(x, labelY, label, AxisColor, AxisLabelFontSize, FontFamily, TextAlignment.Center);
        }
    }

    /// <summary>
    /// Draws the legend showing series names and colors.
    /// </summary>
    private void DrawLegend(ICanvas canvas, int seriesCount)
    {
        float legendY = Height - 40;
        float legendStartX = LeftPadding;
        float legendItemWidth = 150;
        float colorBoxSize = 12;

        for (int i = 0; i < seriesCount; i++)
        {
            BarSeries series = Series[i];
            float x = legendStartX + i * legendItemWidth;

            // Draw color box
            canvas.FillRect(x, legendY, colorBoxSize, colorBoxSize, series.Color);

            // Draw series name
            canvas.DrawText(x + colorBoxSize + 5, legendY + colorBoxSize / 2 - LegendFontSize / 2,
                series.Name, AxisColor, LegendFontSize, FontFamily, TextAlignment.Left);
        }
    }
}