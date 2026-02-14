using System.Collections.Generic;
using BonniePDF.Drawing;
using BonniePDF.Models;

namespace BonniePDF.Charts;

/// <summary>
/// Base class for Cartesian charts (charts with X and Y axes).
/// Provides common functionality for drawing axes, grid lines, labels, and legends.
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