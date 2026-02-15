using System.Collections.Generic;
using BonniePDF.Drawing;
using BonniePDF.Models;

namespace BonniePDF.Charts;

/// <summary>
/// Base class for Cartesian charts (charts with X and Y axes).
/// Provides common functionality for drawing axes, grid lines, labels, and legends.
/// Uses the Template Method pattern to ensure consistent drawing order across all derived charts.
/// </summary>
public abstract class CartesianChartBase : ChartBase
{
    // Layout constants (relative to chart height)
    protected const float TitleAreaHeight = 60; // Space for title at top
    protected const float BottomPadding = 70; // Space for x-axis labels and legend
    protected const float LeftPadding = 80; // Space for y-axis labels and legend
    protected const float RightPadding = 40; // Right margin
    protected const float AxisLineWidth = 1f;
    protected const float GridLineWidth = 0.5f;
    protected const float AxisLabelFontSize = 10;
    protected const float LegendFontSize = 11;

    // Styling constants
    protected static readonly Color AxisColor = Color.FromHex("#666666");
    protected static readonly Color GridLineColor = Color.FromHex("#e0e0e0");

    /// <summary>
    /// Font family for labels
    /// </summary>
    public string FontFamily { get; set; } = "Arial, sans-serif";

    /// <summary>
    /// Draws the chart content using a Template Method pattern.
    /// This ensures consistent drawing order across all Cartesian chart types:
    /// 1. Grid lines (behind everything)
    /// 2. Y-axis labels and legend (behind data)
    /// 3. Chart-specific data elements (bars, lines, etc.)
    /// 4. Value labels (on top of data)
    /// 5. X-axis labels and legend
    /// 6. Axis lines (on top of everything to prevent overlap)
    /// </summary>
    protected sealed override void DrawContent(ICanvas canvas, ChartData data)
    {
        int dataPointCount = data.DataPoints.Count;
        if (dataPointCount == 0) return;

        // Calculate dynamic layout based on chart dimensions
        (float chartAreaWidth, float chartAreaHeight, float baselineY, float topY) = CalculateLayout();

        // Calculate coordinates for all data points
        List<(float x, float y)> coordinates = CalculateCoordinates(data, chartAreaWidth, chartAreaHeight, baselineY);

        // 1. Draw grid lines first (behind everything)
        if (data.YAxis.ShowGridLines)
            DrawGridLines(canvas, data, chartAreaWidth, chartAreaHeight, baselineY, topY);

        // 2. Draw Y-axis labels (behind data)
        DrawYAxisLabels(canvas, data, chartAreaHeight, baselineY, topY);

        // 3. Draw Y-axis legend (behind data)
        if (!string.IsNullOrEmpty(data.YAxis.Legend))
            DrawYAxisLegend(canvas, data.YAxis.Legend, chartAreaHeight, topY);

        // 4. Draw chart-specific data elements (bars, lines, area, etc.)
        DrawChartElements(canvas, data, coordinates, chartAreaWidth, chartAreaHeight, baselineY, topY);

        // 5. Draw value labels on top of data
        DrawValueLabels(canvas, data, coordinates);

        // 6. Draw X-axis labels (data point labels)
        DrawXAxisLabels(canvas, data, coordinates, baselineY);

        // 7. Draw X-axis legend
        if (!string.IsNullOrEmpty(data.XAxis.Legend))
            DrawXAxisLegend(canvas, data.XAxis.Legend, chartAreaWidth, baselineY);

        // 8. Draw axis lines LAST (on top of everything to prevent overlap)
        DrawAxisLines(canvas, chartAreaWidth, baselineY, topY);
    }

    /// <summary>
    /// Draws the chart-specific data elements (bars, lines, area, dots, etc.).
    /// Derived classes must implement this to provide their specific drawing logic.
    /// This is called after grid lines and axis labels, but before axis lines are drawn on top.
    /// </summary>
    /// <param name="canvas">The canvas to draw to</param>
    /// <param name="data">The chart data to render</param>
    /// <param name="coordinates">Pre-calculated coordinates for all data points</param>
    /// <param name="chartAreaWidth">Width of the chart drawing area</param>
    /// <param name="chartAreaHeight">Height of the chart drawing area</param>
    /// <param name="baselineY">Y coordinate of the x-axis (bottom of chart area)</param>
    /// <param name="topY">Y coordinate of the top of the chart area</param>
    protected abstract void DrawChartElements(ICanvas canvas, ChartData data, List<(float x, float y)> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY);

    /// <summary>
    /// Draws value labels above data points. Can be overridden by derived classes.
    /// Default implementation does nothing (derived classes can override if they show value labels).
    /// </summary>
    protected virtual void DrawValueLabels(ICanvas canvas, ChartData data, List<(float x, float y)> coordinates)
    {
        // Default: no value labels
        // Derived classes can override this to show value labels above data points
    }

    /// <summary>
    /// Draws horizontal grid lines across the chart area.
    /// </summary>
    protected static void DrawGridLines(ICanvas canvas, ChartData data, float chartAreaWidth, float chartAreaHeight,
        float baselineY, float topY)
    {
        int stepCount = data.YAxis.StepCount;
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
    protected static void DrawAxisLines(ICanvas canvas, float chartAreaWidth, float baselineY, float topY)
    {
        // Y-axis (vertical line on the left)
        canvas.DrawLine(LeftPadding, topY, LeftPadding, baselineY, AxisColor, AxisLineWidth);

        // X-axis (horizontal line at the bottom)
        canvas.DrawLine(LeftPadding, baselineY, LeftPadding + chartAreaWidth, baselineY, AxisColor, AxisLineWidth);
    }

    /// <summary>
    /// Draws Y-axis value labels.
    /// </summary>
    protected void DrawYAxisLabels(ICanvas canvas, ChartData data, float chartAreaHeight, float baselineY, float topY)
    {
        int stepCount = data.YAxis.StepCount;
        if (stepCount <= 0) stepCount = 5;

        decimal minValue = data.YAxis.MinValue;
        decimal maxValue = data.YAxis.MaxValue;
        decimal range = maxValue - minValue;
        decimal stepValue = range / stepCount;

        float stepHeight = chartAreaHeight / stepCount;

        for (int i = 0; i <= stepCount; i++)
        {
            float y = baselineY - i * stepHeight;
            decimal value = minValue + i * stepValue;

            // Format the value with prefix/suffix
            string label = $"{data.YAxis.Prefix}{value:N0}{data.YAxis.Suffix}";

            // Draw label to the left of the Y-axis
            canvas.DrawText(LeftPadding - 8, y - AxisLabelFontSize / 2, label, AxisColor, AxisLabelFontSize, FontFamily,
                TextAlignment.Right);
        }
    }

    /// <summary>
    /// Draws the Y-axis legend (rotated text would be ideal, but we'll use horizontal for now).
    /// </summary>
    protected void DrawYAxisLegend(ICanvas canvas, string legend, float chartAreaHeight, float topY)
    {
        // Position the legend to the left of the Y-axis labels
        float x = 10;
        float y = topY + chartAreaHeight / 2;

        // Note: PDF doesn't easily support rotated text without transforms
        // For now, we'll draw it horizontally at a slight offset
        canvas.DrawText(x, y, legend, AxisColor, LegendFontSize, FontFamily, TextAlignment.Left);
    }

    /// <summary>
    /// Draws the X-axis legend centered below the axis labels.
    /// </summary>
    protected void DrawXAxisLegend(ICanvas canvas, string legend, float chartAreaWidth, float baselineY)
    {
        float x = LeftPadding + chartAreaWidth / 2;
        float y = baselineY + 50;

        canvas.DrawText(x, y, legend, AxisColor, LegendFontSize, FontFamily, TextAlignment.Center);
    }

    /// <summary>
    /// Draws X-axis labels (data point labels below each point).
    /// </summary>
    protected void DrawXAxisLabels(ICanvas canvas, ChartData data, List<(float x, float y)> coordinates,
        float baselineY)
    {
        float labelY = baselineY + 15;

        for (int i = 0; i < data.DataPoints.Count; i++)
        {
            (float x, _) = coordinates[i];
            string label = data.DataPoints[i].Label;

            canvas.DrawText(x, labelY, label, AxisColor, AxisLabelFontSize, FontFamily, TextAlignment.Center);
        }
    }

    /// <summary>
    /// Calculates coordinates for all data points based on chart dimensions.
    /// </summary>
    protected static List<(float x, float y)> CalculateCoordinates(ChartData data, float chartAreaWidth,
        float chartAreaHeight, float baselineY)
    {
        int dataPointCount = data.DataPoints.Count;
        List<(float x, float y)> coordinates = new();

        // Calculate horizontal spacing
        float totalSpace = chartAreaWidth / dataPointCount;
        float spacing = totalSpace;

        for (int i = 0; i < dataPointCount; i++)
        {
            // Center each data point in its allocated space
            float x = LeftPadding + i * spacing + spacing / 2;

            // Calculate Y based on value
            decimal normalizedValue = data.DataPoints[i].Value / data.YAxis.MaxValue;
            float y = baselineY - (float)(normalizedValue * (decimal)chartAreaHeight);

            coordinates.Add((x, y));
        }

        return coordinates;
    }

    /// <summary>
    /// Calculates the chart layout dimensions.
    /// </summary>
    protected (float chartAreaWidth, float chartAreaHeight, float baselineY, float topY) CalculateLayout()
    {
        float chartAreaWidth = Width - LeftPadding - RightPadding;
        float chartAreaHeight = Height - TitleAreaHeight - BottomPadding;
        float baselineY = TitleAreaHeight + chartAreaHeight;
        float topY = TitleAreaHeight;

        return (chartAreaWidth, chartAreaHeight, baselineY, topY);
    }
}