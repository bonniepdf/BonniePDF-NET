using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Pie chart that renders pie slices with a legend on the right side.
/// Supports donut style with a transparent center hole.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// All layout is computed dynamically from Width and Height, so the chart
/// adapts correctly to any size or StretchToFit mode.
/// </summary>
public class PieChart : ChartBase
{
    public PieChart()
    {
    }

    /// <summary>Creates a pie chart bound to its document data.</summary>
    public PieChart(ChartData data) : base(data)
    {
    }

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
    private const float TotalRowGap = 8; // gap between last legend item and total row

    // Donut hole proportions
    private const float DonutHoleRatio = 0.33f; // donut hole radius as fraction of pie radius

    private const string FontFamily = StandardFonts.Helvetica;

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
    public IList<string>? Colors { get; set; }

    /// <summary>
    /// Whether to render as a donut chart with a transparent center hole.
    /// Default is true.
    /// </summary>
    public bool IsDonut { get; set; } = true;

    /// <summary>
    /// Label prefix for the total row in the legend (e.g. "Total").
    /// Default is "Total".
    /// </summary>
    public string TotalLabel { get; set; } = "Total";

    /// <summary>
    /// Font size for the total label in the donut hole.
    /// </summary>
    /// <remarks>No longer used - the total is now shown in the legend. Kept for API compatibility.</remarks>
    public float TotalLabelFontSize { get; set; } = 11;

    /// <summary>
    /// Font size for the total value in the donut hole.
    /// </summary>
    /// <remarks>No longer used - the total is now shown in the legend. Kept for API compatibility.</remarks>
    public float TotalValueFontSize { get; set; } = 16;

    /// <summary>
    /// Number format for the total: <c>G</c> (default), <c>F&lt;n&gt;</c>, <c>N&lt;n&gt;</c> or <c>P&lt;n&gt;</c>.
    /// Culture-invariant; see <see cref="ChartNumberFormat"/>. Anything else throws when the chart is drawn.
    /// </summary>
    public string? TotalValueFormat { get; set; }

    /// <summary>
    /// Whether to show the legend.
    /// Default is true.
    /// </summary>
    public bool ShowLegend { get; set; } = true;

    /// <summary>
    /// Whether to show the total value as a summary row below the legend items.
    /// Default is true.
    /// </summary>
    public bool ShowTotal { get; set; } = true;

    /// <summary>
    /// Draws the pie chart content.
    /// All positions are computed dynamically from Width and Height.
    /// </summary>
    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        if (data.Series.Count == 0 || data.Series[0].DataPoints.Count == 0) return;

        IList<ChartDataPoint> dataPoints = data.Series[0].DataPoints;

        // Calculate total value
        decimal total = dataPoints.Sum(dp => dp.Value);

        // Calculate dynamic layout
        var layout = CalculateLayout(dataPoints.Count, !string.IsNullOrEmpty(data.Title));

        // Calculate start angle for each slice
        List<(float startAngle, float endAngle)> angles = CalculateSliceAngles(dataPoints, total);

        // Get colors to use
        List<Color> colors = GetColors(dataPoints.Count);

        // Draw pie slices (annular sectors when IsDonut, full wedges otherwise)
        DrawPieSlices(canvas, dataPoints, angles, colors, layout);

        // Draw legend (including optional total row)
        if (ShowLegend) DrawLegend(canvas, dataPoints, colors, layout, total);
    }

    /// <summary>
    /// Computes all layout metrics from the current Width and Height.
    ///
    /// The title band is only reserved when a title is actually drawn; a titleless
    /// pie centres symmetrically in the box instead of being pushed down by the
    /// empty title gap.
    /// </summary>
    private PieLayout CalculateLayout(int dataPointCount, bool hasTitle)
    {
        float titleBand = hasTitle ? TitleAreaHeight : 0;

        // Available vertical space for the pie (between title and bottom padding)
        float availableHeight = Height - titleBand - TopPadding - BottomPadding;

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
        float pieCenterY = titleBand + TopPadding + availableHeight / 2;

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
    private static List<(float startAngle, float endAngle)> CalculateSliceAngles(IList<ChartDataPoint> dataPoints,
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
        IList<string> sourceColors = Colors ?? new List<string>(DefaultColors);

        for (int i = 0; i < count; i++)
        {
            string colorHex = sourceColors[i % sourceColors.Count];
            colors.Add(Color.FromHex(colorHex));
        }

        return colors;
    }

    /// <summary>
    /// Draws the pie slices.
    /// Uses annular sector paths when IsDonut is true (transparent center hole),
    /// or full triangular wedge paths when IsDonut is false.
    /// </summary>
    private void DrawPieSlices(ICanvas canvas, IList<ChartDataPoint> dataPoints,
        List<(float startAngle, float endAngle)> angles, List<Color> colors, PieLayout layout)
    {
        for (int i = 0; i < dataPoints.Count; i++)
        {
            (float startAngle, float endAngle) = angles[i];
            Color color = colors[i];

            List<Point> path = IsDonut
                ? CreateAnnularSectorPath(startAngle, endAngle, layout)
                : CreatePieSlicePath(startAngle, endAngle, layout);

            canvas.FillPath(path, color);
        }
    }

    /// <summary>
    /// Creates a full triangular wedge path for a pie slice (non-donut mode).
    /// </summary>
    private static List<Point> CreatePieSlicePath(float startAngle, float endAngle, PieLayout layout)
    {
        List<Point> path = new();

        // Start at the center
        path.Add(new Point(layout.CenterX, layout.CenterY));

        // Add arc points
        int segments = Math.Max((int)Math.Ceiling((endAngle - startAngle) / 5), 1);
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + (endAngle - startAngle) * i / segments;
            float radians = (float)(angle * Math.PI / 180);
            float x = layout.CenterX + (float)(layout.Radius * Math.Cos(radians));
            float y = layout.CenterY + (float)(layout.Radius * Math.Sin(radians));
            path.Add(new Point(x, y));
        }

        return path;
    }

    /// <summary>
    /// Creates an annular sector path for a donut slice (donut mode).
    /// The path traces the outer arc forward then the inner arc in reverse,
    /// producing a ring wedge with a genuinely transparent center.
    /// </summary>
    private static List<Point> CreateAnnularSectorPath(float startAngle, float endAngle, PieLayout layout)
    {
        List<Point> path = new();
        int segments = Math.Max((int)Math.Ceiling((endAngle - startAngle) / 5), 1);

        // Outer arc: startAngle → endAngle
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + (endAngle - startAngle) * i / segments;
            float radians = (float)(angle * Math.PI / 180);
            path.Add(new Point(
                layout.CenterX + (float)(layout.Radius * Math.Cos(radians)),
                layout.CenterY + (float)(layout.Radius * Math.Sin(radians))
            ));
        }

        // Inner arc: endAngle → startAngle (reversed)
        for (int i = segments; i >= 0; i--)
        {
            float angle = startAngle + (endAngle - startAngle) * i / segments;
            float radians = (float)(angle * Math.PI / 180);
            path.Add(new Point(
                layout.CenterX + (float)(layout.DonutHoleRadius * Math.Cos(radians)),
                layout.CenterY + (float)(layout.DonutHoleRadius * Math.Sin(radians))
            ));
        }

        return path;
    }

    /// <summary>
    /// Draws the legend on the right side, with an optional total summary row below.
    /// </summary>
    private void DrawLegend(ICanvas canvas, IList<ChartDataPoint> dataPoints, List<Color> colors, PieLayout layout,
        decimal total)
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
            float textY = y + LegendBoxSize / 2 - LegendFontSize / 2;
            canvas.DrawText(textX, textY, labelText, LegendTextColor, LegendFontSize, FontFamily, TextAlignment.Left);
        }

        if (ShowTotal)
        {
            string totalText = $"{TotalLabel}: {ChartNumberFormat.Format(total, TotalValueFormat)}";
            float textX = layout.LegendX + LegendBoxSize + LegendBoxTextGap;
            float totalY = layout.LegendStartY + dataPoints.Count * LegendItemHeight + TotalRowGap;
            canvas.DrawText(textX, totalY, totalText, LegendTextColor, LegendFontSize, FontFamily, TextAlignment.Left);
        }
    }

    /// <summary>
    /// Holds all computed layout positions for a pie chart render pass.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct PieLayout(
        float CenterX,
        float CenterY,
        float Radius,
        float DonutHoleRadius,
        float LegendX,
        float LegendStartY)
    {
        public bool Equals(PieLayout other)
        {
            return Math.Abs(CenterX - other.CenterX) < 0.0001f &&
                   Math.Abs(CenterY - other.CenterY) < 0.0001f &&
                   Math.Abs(Radius - other.Radius) < 0.0001f &&
                   Math.Abs(DonutHoleRadius - other.DonutHoleRadius) < 0.0001f &&
                   Math.Abs(LegendX - other.LegendX) < 0.0001f &&
                   Math.Abs(LegendStartY - other.LegendStartY) < 0.0001f;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(CenterX);
            hash.Add(CenterY);
            hash.Add(Radius);
            hash.Add(DonutHoleRadius);
            hash.Add(LegendX);
            hash.Add(LegendStartY);
            return hash.ToHashCode();
        }
    }
}
