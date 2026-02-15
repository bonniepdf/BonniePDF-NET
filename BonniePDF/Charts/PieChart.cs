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
/// All layout is computed dynamically from Width and Height, so the chart
/// adapts correctly to any size or StretchToFit mode.
/// </summary>
public class PieChart : ChartBase
{
    // Layout proportions (relative to chart dimensions)
    private const float TitleAreaHeight = 60;
    private const float TopPadding = 20; // padding between title and pie top
    private const float BottomPadding = 20; // padding at bottom
    private const float LeftPadding = 20; // padding on left of pie
    private const float LegendGapFromPie = 30; // gap between pie right edge and legend
    private const float RightPadding = 20; // padding on right of legend

    // Legend item layout
    private const float LegendItemHeight = 24;
    private const float LegendBoxSize = 14;
    private const float LegendBoxTextGap = 8;
    private const float LegendFontSize = 11;

    // Donut hole proportions
    private const float DonutHoleRatio = 0.33f; // donut hole radius as fraction of pie radius

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
    /// Format string for the total value displayed in the donut hole.
    /// When null (default), uses the LabelFormat from the first data point.
    /// Set to a custom format string (e.g. "C0", "N0", "'$'#,##0") to override.
    /// </summary>
    public string? TotalValueFormat { get; set; } = null;

    /// <summary>
    /// Whether to show the legend.
    /// Default is true.
    /// </summary>
    public bool ShowLegend { get; set; } = true;

    /// <summary>
    /// Draws the pie chart content.
    /// All positions are computed dynamically from Width and Height.
    /// </summary>
    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        if (data.DataPoints.Count == 0) return;

        // Calculate total value
        decimal total = data.DataPoints.Sum(dp => dp.Value);

        // Calculate dynamic layout
        var layout = CalculateLayout(data.DataPoints.Count);

        // Calculate start angle for each slice
        List<(float startAngle, float endAngle)> angles = CalculateSliceAngles(data.DataPoints, total);

        // Get colors to use
        List<Color> colors = GetColors(data.DataPoints.Count);

        // Draw pie slices
        DrawPieSlices(canvas, data.DataPoints, angles, colors, layout);

        // Draw donut hole if enabled
        if (IsDonut) DrawDonutHole(canvas, total, data.DataPoints, layout);

        // Draw legend
        if (ShowLegend) DrawLegend(canvas, data.DataPoints, colors, layout);
    }

    /// <summary>
    /// Computes all layout metrics from the current Width and Height.
    /// </summary>
    private PieLayout CalculateLayout(int dataPointCount)
    {
        // Available vertical space for the pie (between title and bottom padding)
        float availableHeight = Height - TitleAreaHeight - TopPadding - BottomPadding;

        // Estimate legend width when legend is shown
        float legendWidth = 0;
        if (ShowLegend)
        {
            // Reserve a reasonable width for legend items
            // LegendBoxSize + gap + ~150pt for text
            legendWidth = LegendBoxSize + LegendBoxTextGap + 150;
        }

        // Available horizontal space for the pie
        float availableWidthForPie = Width - LeftPadding - RightPadding;
        if (ShowLegend)
            availableWidthForPie -= legendWidth + LegendGapFromPie;

        // Pie radius is constrained by both available width and height
        float maxRadiusFromWidth = availableWidthForPie / 2;
        float maxRadiusFromHeight = availableHeight / 2;
        float pieRadius = Math.Min(maxRadiusFromWidth, maxRadiusFromHeight);
        pieRadius = Math.Max(pieRadius, 20); // minimum viable radius

        // Center the pie vertically in the available area
        float pieCenterY = TitleAreaHeight + TopPadding + availableHeight / 2;

        // Center the pie horizontally in its allocated space
        float pieAreaWidth = Width - (ShowLegend ? legendWidth + LegendGapFromPie + RightPadding : RightPadding) - LeftPadding;
        float pieCenterX = LeftPadding + pieAreaWidth / 2;

        // Legend position: to the right of the pie
        float legendX = pieCenterX + pieRadius + LegendGapFromPie;

        // Vertically center the legend items relative to the pie center
        float totalLegendHeight = dataPointCount * LegendItemHeight;
        float legendStartY = pieCenterY - totalLegendHeight / 2;

        // Donut hole radius
        float donutHoleRadius = pieRadius * DonutHoleRatio;

        return new PieLayout(pieCenterX, pieCenterY, pieRadius, donutHoleRadius, legendX, legendStartY);
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
        List<(float startAngle, float endAngle)> angles, List<Color> colors, PieLayout layout)
    {
        for (int i = 0; i < dataPoints.Count; i++)
        {
            (float startAngle, float endAngle) = angles[i];
            Color color = colors[i];

            // Calculate the path for the pie slice
            List<(float x, float y)> path = CreatePieSlicePath(startAngle, endAngle, layout);

            // Fill the slice
            canvas.FillPath(path, color);
        }
    }

    /// <summary>
    /// Creates a path for a pie slice.
    /// </summary>
    private List<(float x, float y)> CreatePieSlicePath(float startAngle, float endAngle, PieLayout layout)
    {
        List<(float x, float y)> path = new();

        // Start at the center
        path.Add((layout.CenterX, layout.CenterY));

        // Add arc points
        int segments = Math.Max((int)Math.Ceiling((endAngle - startAngle) / 5), 1);
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + (endAngle - startAngle) * i / segments;
            float radians = (float)(angle * Math.PI / 180);
            float x = layout.CenterX + (float)(layout.Radius * Math.Cos(radians));
            float y = layout.CenterY + (float)(layout.Radius * Math.Sin(radians));
            path.Add((x, y));
        }

        return path;
    }

    /// <summary>
    /// Draws the donut hole with the total value.
    /// </summary>
    private void DrawDonutHole(ICanvas canvas, decimal total, List<ChartDataPoint> dataPoints, PieLayout layout)
    {
        // Draw white circle for donut hole
        canvas.FillCircle(layout.CenterX, layout.CenterY, layout.DonutHoleRadius, Color.White);

        // Draw "Total" label
        float totalLabelY = layout.CenterY - 5;
        canvas.DrawText(layout.CenterX, totalLabelY, TotalLabel, TotalLabelColor, TotalLabelFontSize, FontFamily,
            TextAlignment.Center);

        // Format total value using the configured format or the first data point's format
        string format = TotalValueFormat ?? (dataPoints.Count > 0 ? dataPoints[0].LabelFormat : "G");
        string totalValueText = total.ToString(format);

        float totalValueY = layout.CenterY + 12;
        canvas.DrawText(layout.CenterX, totalValueY, totalValueText, TotalValueColor, TotalValueFontSize, FontFamily,
            TextAlignment.Center);
    }

    /// <summary>
    /// Draws the legend on the right side.
    /// </summary>
    private void DrawLegend(ICanvas canvas, List<ChartDataPoint> dataPoints, List<Color> colors, PieLayout layout)
    {
        for (int i = 0; i < dataPoints.Count; i++)
        {
            ChartDataPoint dataPoint = dataPoints[i];
            Color color = colors[i];
            float y = layout.LegendStartY + i * LegendItemHeight;

            // Draw color box
            canvas.FillRect(layout.LegendX, y, LegendBoxSize, LegendBoxSize, color);

            // Draw label text (vertically centered with the color box)
            string labelText = $"{dataPoint.Label} - {dataPoint.FormattedValue}";
            float textX = layout.LegendX + LegendBoxSize + LegendBoxTextGap;
            canvas.DrawText(textX, y, labelText, LegendTextColor, LegendFontSize, FontFamily, TextAlignment.Left);
        }
    }

    /// <summary>
    /// Holds all computed layout positions for a pie chart render pass.
    /// </summary>
    private readonly record struct PieLayout(
        float CenterX,
        float CenterY,
        float Radius,
        float DonutHoleRadius,
        float LegendX,
        float LegendStartY);
}
