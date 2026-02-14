using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;
using BonniePDF.Models;

namespace BonniePDF.Charts;

/// <summary>
/// Pie chart that renders pie slices with a legend on the right side.
/// Supports donut style with a center hole showing the total value.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class PieChart : ChartBase
{
    // Layout constants
    private const float TitleAreaHeight = 60;
    private const float LegendX = 540;
    private const float LegendStartY = 100;
    private const float LegendItemHeight = 30;
    private const float LegendBoxSize = 16;
    private const float LegendBoxTextGap = 8;
    private const float LegendFontSize = 12;

    // Pie chart constants
    private const float PieCenterX = 300;
    private const float PieCenterY = 260;
    private const float PieRadius = 180;
    private const float DonutHoleRadius = 60;
    private const string FontFamily = "Arial, sans-serif";

    // Styling constants
    private static readonly Color LegendTextColor = Color.FromHex("#333333");
    private static readonly Color TotalLabelColor = Color.FromHex("#666666");
    private static readonly Color TotalValueColor = Color.FromHex("#333333");

    // Default colors matching the SVG
    private static readonly string[] DefaultColors =
    {
        "#4f46e5", "#7c3aed", "#a855f7", "#d946ef", "#ec4899", "#f43f5e",
        "#f97316", "#eab308", "#84cc16", "#22c55e", "#14b8a6", "#06b6d4"
    };

    /// <summary>
    /// Custom colors to use for the pie slices.
    /// If null, uses the default color palette.
    /// </summary>
    public List<string>? Colors { get; set; } = null;

    /// <summary>
    /// Whether to render as a donut chart with a center hole.
    /// Default is true.
    /// </summary>
    public bool IsDonut { get; set; } = true;

    /// <summary>
    /// Label to display above the total value in the donut hole.
    /// Default is "Total".
    /// </summary>
    public string TotalLabel { get; set; } = "Total";

    /// <summary>
    /// Font size for the total label in the donut hole.
    /// </summary>
    public float TotalLabelFontSize { get; set; } = 11;

    /// <summary>
    /// Font size for the total value in the donut hole.
    /// </summary>
    public float TotalValueFontSize { get; set; } = 16;

    /// <summary>
    /// Whether to show the legend.
    /// Default is true.
    /// </summary>
    public bool ShowLegend { get; set; } = true;

    /// <summary>
    /// Draws the pie chart content.
    /// </summary>
    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        if (data.DataPoints.Count == 0) return;

        // Calculate total value
        decimal total = data.DataPoints.Sum(dp => dp.Value);

        // Calculate start angle for each slice
        List<(float startAngle, float endAngle)> angles = CalculateSliceAngles(data.DataPoints, total);

        // Get colors to use
        List<Color> colors = GetColors(data.DataPoints.Count);

        // Draw pie slices
        DrawPieSlices(canvas, data.DataPoints, angles, colors);

        // Draw donut hole if enabled
        if (IsDonut) DrawDonutHole(canvas, total);

        // Draw legend
        if (ShowLegend) DrawLegend(canvas, data.DataPoints, colors);
    }

    /// <summary>
    /// Calculates the start and end angles for each pie slice.
    /// </summary>
    private List<(float startAngle, float endAngle)> CalculateSliceAngles(List<ChartDataPoint> dataPoints,
        decimal total)
    {
        List<(float startAngle, float endAngle)> angles = new();
        float currentAngle = -90; // Start from top (12 o'clock position)

        foreach (ChartDataPoint dataPoint in dataPoints)
        {
            float sliceAngle = (float)(360 * (double)(dataPoint.Value / total));
            float endAngle = currentAngle + sliceAngle;
            angles.Add((currentAngle, endAngle));
            currentAngle = endAngle;
        }

        return angles;
    }

    /// <summary>
    /// Gets the colors to use for the pie slices.
    /// </summary>
    private List<Color> GetColors(int count)
    {
        List<Color> colors = new();
        List<string> sourceColors = Colors ?? new List<string>(DefaultColors);

        for (int i = 0; i < count; i++)
        {
            string colorHex = sourceColors[i % sourceColors.Count];
            colors.Add(Color.FromHex(colorHex));
        }

        return colors;
    }

    /// <summary>
    /// Draws the pie slices.
    /// </summary>
    private void DrawPieSlices(ICanvas canvas, List<ChartDataPoint> dataPoints,
        List<(float startAngle, float endAngle)> angles, List<Color> colors)
    {
        for (int i = 0; i < dataPoints.Count; i++)
        {
            (float startAngle, float endAngle) = angles[i];
            Color color = colors[i];

            // Calculate the path for the pie slice
            List<(float x, float y)> path = CreatePieSlicePath(startAngle, endAngle);

            // Fill the slice
            canvas.FillPath(path, color);
        }
    }

    /// <summary>
    /// Creates a path for a pie slice.
    /// </summary>
    private List<(float x, float y)> CreatePieSlicePath(float startAngle, float endAngle)
    {
        List<(float x, float y)> path = new();

        // Start at the center
        path.Add((PieCenterX, PieCenterY));

        // Add arc points
        int segments = Math.Max((int)Math.Ceiling((endAngle - startAngle) / 5), 1);
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + (endAngle - startAngle) * i / segments;
            float radians = (float)(angle * Math.PI / 180);
            float x = PieCenterX + (float)(PieRadius * Math.Cos(radians));
            float y = PieCenterY + (float)(PieRadius * Math.Sin(radians));
            path.Add((x, y));
        }

        return path;
    }

    /// <summary>
    /// Draws the donut hole with the total value.
    /// </summary>
    private void DrawDonutHole(ICanvas canvas, decimal total)
    {
        // Draw white circle for donut hole
        canvas.FillCircle(PieCenterX, PieCenterY, DonutHoleRadius, Color.White);

        // Draw "Total" label
        string totalLabelText = TotalLabel;
        float totalLabelX = PieCenterX;
        float totalLabelY = PieCenterY - 5;
        canvas.DrawText(totalLabelX, totalLabelY, totalLabelText, TotalLabelColor, TotalLabelFontSize, FontFamily,
            TextAlignment.Center);

        // Draw total value
        string totalValueText = $"£{total:N0}";
        float totalValueX = PieCenterX;
        float totalValueY = PieCenterY + 12;
        canvas.DrawText(totalValueX, totalValueY, totalValueText, TotalValueColor, TotalValueFontSize, FontFamily,
            TextAlignment.Center);
    }

    /// <summary>
    /// Draws the legend on the right side.
    /// </summary>
    private void DrawLegend(ICanvas canvas, List<ChartDataPoint> dataPoints, List<Color> colors)
    {
        for (int i = 0; i < dataPoints.Count; i++)
        {
            ChartDataPoint dataPoint = dataPoints[i];
            Color color = colors[i];
            float y = LegendStartY + i * LegendItemHeight;

            // Draw color box
            canvas.FillRect(LegendX, y, LegendBoxSize, LegendBoxSize, color);

            // Draw label text (vertically centered with the color box)
            string labelText = $"{dataPoint.Label} - {dataPoint.FormattedValue}";
            float textX = LegendX + LegendBoxSize + LegendBoxTextGap;
            canvas.DrawText(textX, y, labelText, LegendTextColor, LegendFontSize, FontFamily, TextAlignment.Left);
        }
    }
}