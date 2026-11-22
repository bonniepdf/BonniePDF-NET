// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;

namespace BonniePDF;

/// <summary>Composable custom theme definition extending a built-in chart preset.</summary>
public sealed class ChartThemeDefinition
{
    public string Extends { get; set; } = "modern";
    public Color? BackgroundColor { get; set; }
    public float? TitleX { get; set; }
    public float? TitleY { get; set; }
    public Color? TitleColor { get; set; }
    public float? TitleFontSize { get; set; }
    public string? TitleFontFamily { get; set; }
    public TextAlignment? TitleAlignment { get; set; }
    public Color? SubtitleColor { get; set; }
    public Color? AxisColor { get; set; }
    public Color? GridLineColor { get; set; }
    public string? GridLineStyle { get; set; }
    public Color? LegendTextColor { get; set; }
    public bool? AxisFrame { get; set; }
    public float? AxisLineWidth { get; set; }
    public IReadOnlyList<Color>? SeriesPalette { get; set; }
    public IReadOnlyList<string>? SeriesPatterns { get; set; }
    public IReadOnlyList<string>? SeriesMarkerShapes { get; set; }
    public IReadOnlyList<float[]?>? SeriesDashPatterns { get; set; }
    public string? FontFamily { get; set; }
    public bool? MeasuredLabels { get; set; }
    public bool? FlowLegend { get; set; }
    public double? BarCornerRadius { get; set; }
    public bool? StyledGeometry { get; set; }
    public bool? PdfTextBaseline { get; set; }
    public bool? TabularNums { get; set; }
    public bool? RoundedLegendSwatches { get; set; }
}

/// <summary>
/// Everything a chart theme decides. Charts read these values rather than testing the theme name,
/// so each theme is described once, here.
/// </summary>
internal sealed class ChartTheme
{
    private const string LibrarySeriesColor = "#4f46e5";

    internal static readonly ChartTheme Legacy = new()
    {
        BackgroundColor = null,
        TitleX = null,
        TitleY = 35,
        TitleColor = Color.FromHex("#333333"),
        TitleFontSize = 20,
        TitleFontFamily = StandardFonts.Helvetica,
        TitleAlignment = TextAlignment.Center,
        SubtitleColor = Color.FromHex("#666666"),
        AxisColor = Color.FromHex("#666666"),
        GridLineColor = Color.FromHex("#e0e0e0"),
        GridLineStyle = "solid",
        LegendTextColor = Color.FromHex("#333333"),
        AxisFrame = true,
        AxisLineWidth = 1,
        SeriesPalette = [],
        SeriesPatterns = [],
        SeriesMarkerShapes = [],
        SeriesDashPatterns = [],
        FontFamily = StandardFonts.Helvetica,
        MeasuredLabels = false,
        FlowLegend = false,
        BarCornerRadius = 0,
        StyledGeometry = false,
        PdfTextBaseline = false,
        TabularNums = false,
        RoundedLegendSwatches = false,
    };

    internal static readonly ChartTheme Modern = new()
    {
        BackgroundColor = null,
        TitleX = 32,
        TitleY = 20,
        TitleColor = Color.FromHex("#0f172a"),
        TitleFontSize = 18,
        TitleFontFamily = StandardFonts.HelveticaBold,
        TitleAlignment = TextAlignment.Left,
        SubtitleColor = Color.FromHex("#64748b"),
        AxisColor = Color.FromHex("#64748b"),
        GridLineColor = Color.FromHex("#e2e8f0"),
        GridLineStyle = "solid",
        LegendTextColor = Color.FromHex("#334155"),
        AxisFrame = false,
        AxisLineWidth = 0.5f,
        SeriesPalette = [Color.FromHex("#2563eb"), Color.FromHex("#0d9488"), Color.FromHex("#7c3aed"), Color.FromHex("#d97706"), Color.FromHex("#e11d48"), Color.FromHex("#0284c7")],
        SeriesPatterns = [],
        SeriesMarkerShapes = [],
        SeriesDashPatterns = [],
        FontFamily = StandardFonts.Helvetica,
        MeasuredLabels = true,
        FlowLegend = true,
        BarCornerRadius = 4,
        StyledGeometry = true,
        PdfTextBaseline = true,
        TabularNums = true,
        RoundedLegendSwatches = true,
    };

    internal static readonly ChartTheme Dark = new()
    {
        BackgroundColor = Color.FromHex("#0f172a"),
        TitleX = 32,
        TitleY = 20,
        TitleColor = Color.FromHex("#f8fafc"),
        TitleFontSize = 18,
        TitleFontFamily = StandardFonts.HelveticaBold,
        TitleAlignment = TextAlignment.Left,
        SubtitleColor = Color.FromHex("#94a3b8"),
        AxisColor = Color.FromHex("#94a3b8"),
        GridLineColor = Color.FromHex("#1e293b"),
        GridLineStyle = "solid",
        LegendTextColor = Color.FromHex("#e2e8f0"),
        AxisFrame = false,
        AxisLineWidth = 0.5f,
        SeriesPalette = [Color.FromHex("#60a5fa"), Color.FromHex("#2dd4bf"), Color.FromHex("#a78bfa"), Color.FromHex("#fbbf24"), Color.FromHex("#fb7185"), Color.FromHex("#38bdf8")],
        SeriesPatterns = [],
        SeriesMarkerShapes = [],
        SeriesDashPatterns = [],
        FontFamily = StandardFonts.Helvetica,
        MeasuredLabels = true,
        FlowLegend = true,
        BarCornerRadius = 4,
        StyledGeometry = true,
        PdfTextBaseline = true,
        TabularNums = true,
        RoundedLegendSwatches = true,
    };

    internal static readonly ChartTheme Print = new()
    {
        BackgroundColor = Color.FromHex("#ffffff"),
        TitleX = 32,
        TitleY = 20,
        TitleColor = Color.FromHex("#000000"),
        TitleFontSize = 18,
        TitleFontFamily = StandardFonts.HelveticaBold,
        TitleAlignment = TextAlignment.Left,
        SubtitleColor = Color.FromHex("#404040"),
        AxisColor = Color.FromHex("#1f2937"),
        GridLineColor = Color.FromHex("#d1d5db"),
        GridLineStyle = "dashed",
        LegendTextColor = Color.FromHex("#111827"),
        AxisFrame = false,
        AxisLineWidth = 0.75f,
        SeriesPalette = [Color.FromHex("#111827"), Color.FromHex("#374151"), Color.FromHex("#4b5563"), Color.FromHex("#6b7280"), Color.FromHex("#9ca3af"), Color.FromHex("#1f2937")],
        SeriesPatterns = ["none", "diagonal", "dots", "reverseDiagonal", "horizontal", "crosshatch"],
        SeriesMarkerShapes = ["circle", "square", "diamond", "triangle"],
        SeriesDashPatterns = [null, [6f, 3f], [2f, 2f], [8f, 3f, 2f, 3f]],
        FontFamily = StandardFonts.Helvetica,
        MeasuredLabels = true,
        FlowLegend = true,
        BarCornerRadius = 2,
        StyledGeometry = true,
        PdfTextBaseline = true,
        TabularNums = true,
        RoundedLegendSwatches = true,
    };

    internal static bool IsValidPreset(string? name) => name is "legacy" or "modern" or "dark" or "print";

    internal static ChartTheme For(string name) => name switch
    {
        "modern" => Modern,
        "dark" => Dark,
        "print" => Print,
        "legacy" => Legacy,
        _ => throw new ArgumentException("Invalid chart theme.", nameof(name)),
    };

    internal static ChartTheme Resolve(string presetName, ChartThemeDefinition? custom)
    {
        if (custom is null) return For(presetName);
        ChartTheme b = For(custom.Extends);
        return new ChartTheme
        {
            BackgroundColor = custom.BackgroundColor ?? b.BackgroundColor,
            TitleX = custom.TitleX ?? b.TitleX,
            TitleY = custom.TitleY ?? b.TitleY,
            TitleColor = custom.TitleColor ?? b.TitleColor,
            TitleFontSize = custom.TitleFontSize ?? b.TitleFontSize,
            TitleFontFamily = custom.TitleFontFamily ?? b.TitleFontFamily,
            TitleAlignment = custom.TitleAlignment ?? b.TitleAlignment,
            SubtitleColor = custom.SubtitleColor ?? b.SubtitleColor,
            AxisColor = custom.AxisColor ?? b.AxisColor,
            GridLineColor = custom.GridLineColor ?? b.GridLineColor,
            GridLineStyle = custom.GridLineStyle ?? b.GridLineStyle,
            LegendTextColor = custom.LegendTextColor ?? b.LegendTextColor,
            AxisFrame = custom.AxisFrame ?? b.AxisFrame,
            AxisLineWidth = custom.AxisLineWidth ?? b.AxisLineWidth,
            SeriesPalette = custom.SeriesPalette ?? b.SeriesPalette,
            SeriesPatterns = custom.SeriesPatterns ?? b.SeriesPatterns,
            SeriesMarkerShapes = custom.SeriesMarkerShapes ?? b.SeriesMarkerShapes,
            SeriesDashPatterns = custom.SeriesDashPatterns ?? b.SeriesDashPatterns,
            FontFamily = custom.FontFamily ?? b.FontFamily,
            MeasuredLabels = custom.MeasuredLabels ?? b.MeasuredLabels,
            FlowLegend = custom.FlowLegend ?? b.FlowLegend,
            BarCornerRadius = custom.BarCornerRadius ?? b.BarCornerRadius,
            StyledGeometry = custom.StyledGeometry ?? b.StyledGeometry,
            PdfTextBaseline = custom.PdfTextBaseline ?? b.PdfTextBaseline,
            TabularNums = custom.TabularNums ?? b.TabularNums,
            RoundedLegendSwatches = custom.RoundedLegendSwatches ?? b.RoundedLegendSwatches,
        };
    }

    internal Color? BackgroundColor { get; private init; }
    internal float? TitleX { get; private init; }
    internal float TitleY { get; private init; }
    internal Color TitleColor { get; private init; }
    internal float TitleFontSize { get; private init; }
    internal string TitleFontFamily { get; private init; } = StandardFonts.Helvetica;
    internal TextAlignment TitleAlignment { get; private init; }
    internal Color SubtitleColor { get; private init; }
    internal Color AxisColor { get; private init; }
    internal Color GridLineColor { get; private init; }
    internal string GridLineStyle { get; private init; } = "solid";
    internal Color LegendTextColor { get; private init; }
    internal bool AxisFrame { get; private init; }
    internal float AxisLineWidth { get; private init; }
    internal IReadOnlyList<Color> SeriesPalette { get; private init; } = [];
    internal IReadOnlyList<string> SeriesPatterns { get; private init; } = [];
    internal IReadOnlyList<string> SeriesMarkerShapes { get; private init; } = [];
    internal IReadOnlyList<float[]?> SeriesDashPatterns { get; private init; } = [];
    internal string FontFamily { get; private init; } = StandardFonts.Helvetica;
    internal bool MeasuredLabels { get; private init; }
    internal bool FlowLegend { get; private init; }
    internal double BarCornerRadius { get; private init; }
    internal bool StyledGeometry { get; private init; }
    internal bool PdfTextBaseline { get; private init; }
    internal bool TabularNums { get; private init; }
    internal bool RoundedLegendSwatches { get; private init; }

    internal Color SeriesColor(Color color, int index) =>
        SeriesPalette.Count > 0 && string.Equals(color.ToHexString(), LibrarySeriesColor, StringComparison.Ordinal)
            ? SeriesPalette[Math.Max(0, index) % SeriesPalette.Count]
            : color;
}
