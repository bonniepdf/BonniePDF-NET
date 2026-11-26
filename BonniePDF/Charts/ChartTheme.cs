// Generated chart source. Do not edit here; the source manifest records its provenance.
using System.Linq;
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
    /// <summary>Whether a reference label with no explicit colour must meet 4.5:1 contrast against the chart background.</summary>
    public bool? AdaptiveReferenceLabels { get; set; }
    /// <summary>Chart background (a colour or gradient). A chart's own <c>BackgroundFill</c> or <c>BackgroundColor</c> wins.</summary>
    public ChartFill? BackgroundFill { get; set; }
    /// <summary>Fill of the plot rectangle of Cartesian charts.</summary>
    public ChartFill? PlotAreaFill { get; set; }
    /// <summary>Colour of a border around the plot rectangle.</summary>
    public Color? PlotAreaBorderColor { get; set; }
    /// <summary>Corner radius of the chart card. A chart's own <c>BorderRadius</c> greater than zero wins.</summary>
    public float? CardRadius { get; set; }
    /// <summary>Border colour of the chart card. A chart's own <c>BorderColor</c> wins.</summary>
    public Color? CardBorderColor { get; set; }
    /// <summary>Border width of the chart card. A chart's own <c>BorderWidth</c> greater than zero wins.</summary>
    public float? CardBorderWidth { get; set; }
    /// <summary>Distance between the chart edge and its card, leaving room for the card's shadow.</summary>
    public float? CardInset { get; set; }
    /// <summary>Effects on the chart card.</summary>
    public IList<ChartEffect>? CardEffects { get; set; }
    /// <summary>Default effects on bars.</summary>
    public IList<ChartEffect>? BarEffects { get; set; }
    /// <summary>Default effects on line strokes.</summary>
    public IList<ChartEffect>? LineEffects { get; set; }
    /// <summary>Default effects on pie slices.</summary>
    public IList<ChartEffect>? SliceEffects { get; set; }
    /// <summary>How a series colour becomes a fill: <c>solid</c> (default) or <c>tint</c> (a gradient from a lighter tint at the value end).</summary>
    public string? AutoFill { get; set; }
    /// <summary><c>darker</c> outlines bars and slices in a darker shade of their colour.</summary>
    public string? MarkOutline { get; set; }
    /// <summary>Ordered colours for lines and markers of series without a colour, when fills use a paler palette.</summary>
    public IReadOnlyList<Color>? StrokePalette { get; set; }
    /// <summary>Colour of increases (waterfall deltas, growth annotations).</summary>
    public Color? PositiveColor { get; set; }
    /// <summary>Colour of decreases.</summary>
    public Color? NegativeColor { get; set; }
    /// <summary>Colour of totals.</summary>
    public Color? TotalColor { get; set; }
}

/// <summary>
/// Everything a chart theme decides. Charts read these values rather than testing the theme name,
/// so each theme is described once, here.
/// </summary>
internal sealed record ChartTheme
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
        AdaptiveReferenceLabels = false,
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
        AdaptiveReferenceLabels = true,
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
        AdaptiveReferenceLabels = true,
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
        AdaptiveReferenceLabels = true,
    };


    private static IReadOnlyList<Color> Hex(params string[] values) => values.Select(Color.FromHex).ToList();
    private static IReadOnlyList<ChartResolvedEffect> Shadow(double opacity, double blur, double offsetY) =>
        ChartEffect.ResolveAll([new ChartShadowEffect { Opacity = opacity, Blur = blur, OffsetY = offsetY }])!;

    /// <summary>Board decks and annual reports: restrained blue, teal and amber on white, a quiet card with a soft shadow.</summary>
    internal static readonly ChartTheme Executive = Modern with
    {
        SeriesPalette = Hex("#1c398e", "#00786f", "#bb4d00", "#45556c", "#432dd7", "#7e2a0c"),
        TitleColor = Color.FromHex("#0f172b"),
        SubtitleColor = Color.FromHex("#45556c"),
        AxisColor = Color.FromHex("#45556c"),
        GridLineColor = Color.FromHex("#e2e8f0"),
        LegendTextColor = Color.FromHex("#1d293d"),
        BarCornerRadius = 2,
        CardRadius = 10,
        CardBorderColor = Color.FromHex("#e2e8f0"),
        CardBorderWidth = 1,
        CardInset = 12,
        CardEffects = Shadow(0.1, 12, 4),
        PositiveColor = Color.FromHex("#00786f"),
        NegativeColor = Color.FromHex("#c10007"),
        TotalColor = Color.FromHex("#1c398e"),
    };

    /// <summary>Marketing and keynote slides: saturated colours, tinted fills, raised bars and slices.</summary>
    internal static readonly ChartTheme Vivid = Modern with
    {
        SeriesPalette = Hex("#155dfc", "#e60076", "#e17100", "#009966", "#7f22fe", "#e7000b"),
        TitleColor = Color.FromHex("#0f172b"),
        TitleFontSize = 22,
        SubtitleColor = Color.FromHex("#45556c"),
        AxisColor = Color.FromHex("#45556c"),
        GridLineStyle = "none",
        LegendTextColor = Color.FromHex("#1d293d"),
        BarCornerRadius = 6,
        CardRadius = 12,
        CardInset = 14,
        CardEffects = Shadow(0.16, 16, 6),
        AutoFill = "tint",
        BarEffects = Shadow(0.18, 6, 2),
        SliceEffects = Shadow(0.18, 6, 2),
        PositiveColor = Color.FromHex("#009966"),
        NegativeColor = Color.FromHex("#e7000b"),
        TotalColor = Color.FromHex("#155dfc"),
    };

    /// <summary>Friendly internal reports: pale fills outlined in a darker shade, lines in the darker shade, a white plot on a pale card.</summary>
    internal static readonly ChartTheme Pastel = Modern with
    {
        BackgroundColor = Color.FromHex("#f8fafc"),
        PlotAreaFill = ChartFill.Solid(Color.FromHex("#ffffff")),
        PlotAreaBorderColor = Color.FromHex("#e2e8f0"),
        SeriesPalette = Hex("#8ec5ff", "#fda5d5", "#ffd230", "#5ee9b5", "#c4b4ff", "#ffa2a2"),
        StrokePalette = Hex("#1447e6", "#c6005c", "#bb4d00", "#007a55", "#7008e7", "#c10007"),
        MarkOutline = "darker",
        TitleColor = Color.FromHex("#1d293d"),
        SubtitleColor = Color.FromHex("#45556c"),
        AxisColor = Color.FromHex("#45556c"),
        GridLineStyle = "dashed",
        GridLineColor = Color.FromHex("#e2e8f0"),
        LegendTextColor = Color.FromHex("#1d293d"),
        BarCornerRadius = 8,
        CardRadius = 14,
        CardBorderColor = Color.FromHex("#e2e8f0"),
        CardBorderWidth = 1,
        PositiveColor = Color.FromHex("#007a55"),
        NegativeColor = Color.FromHex("#c10007"),
        TotalColor = Color.FromHex("#1447e6"),
    };

    /// <summary>Dark keynote and dashboard slides: a gradient slate surface, bright series colours, glowing lines.</summary>
    internal static readonly ChartTheme Midnight = Modern with
    {
        BackgroundFill = new ChartLinearFill { Angle = 90, Stops = [new() { Offset = 0, Color = Color.FromHex("#0f172b") }, new() { Offset = 1, Color = Color.FromHex("#1d293d") }] },
        SeriesPalette = Hex("#00bcff", "#fb64b6", "#fdc700", "#00d492", "#a684ff", "#ff8904"),
        TitleColor = Color.FromHex("#f8fafc"),
        TitleFontSize = 22,
        SubtitleColor = Color.FromHex("#90a1b9"),
        AxisColor = Color.FromHex("#cad5e2"),
        GridLineColor = Color.FromHex("#314158"),
        LegendTextColor = Color.FromHex("#e2e8f0"),
        BarCornerRadius = 6,
        CardRadius = 12,
        CardInset = 14,
        CardEffects = Shadow(0.4, 18, 6),
        AutoFill = "tint",
        LineEffects = ChartEffect.ResolveAll([new ChartGlowEffect { Opacity = 0.45, Radius = 5 }])!,
        PositiveColor = Color.FromHex("#00d492"),
        NegativeColor = Color.FromHex("#ff6467"),
        TotalColor = Color.FromHex("#00bcff"),
    };

    internal static bool IsValidPreset(string? name) => name is "legacy" or "modern" or "dark" or "print" or "executive" or "vivid" or "pastel" or "midnight";

    internal static ChartTheme For(string name) => name switch
    {
        "modern" => Modern,
        "dark" => Dark,
        "print" => Print,
        "legacy" => Legacy,
        "executive" => Executive,
        "vivid" => Vivid,
        "pastel" => Pastel,
        "midnight" => Midnight,
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
            AdaptiveReferenceLabels = custom.AdaptiveReferenceLabels ?? b.AdaptiveReferenceLabels,
            BackgroundFill = custom.BackgroundFill ?? b.BackgroundFill,
            PlotAreaFill = custom.PlotAreaFill ?? b.PlotAreaFill,
            PlotAreaBorderColor = custom.PlotAreaBorderColor ?? b.PlotAreaBorderColor,
            CardRadius = custom.CardRadius ?? b.CardRadius,
            CardBorderColor = custom.CardBorderColor ?? b.CardBorderColor,
            CardBorderWidth = custom.CardBorderWidth ?? b.CardBorderWidth,
            CardInset = custom.CardInset ?? b.CardInset,
            CardEffects = ChartEffect.ResolveAll(custom.CardEffects) ?? b.CardEffects,
            BarEffects = ChartEffect.ResolveAll(custom.BarEffects) ?? b.BarEffects,
            LineEffects = ChartEffect.ResolveAll(custom.LineEffects) ?? b.LineEffects,
            SliceEffects = ChartEffect.ResolveAll(custom.SliceEffects) ?? b.SliceEffects,
            AutoFill = custom.AutoFill ?? b.AutoFill,
            MarkOutline = custom.MarkOutline ?? b.MarkOutline,
            StrokePalette = custom.StrokePalette ?? b.StrokePalette,
            PositiveColor = custom.PositiveColor ?? b.PositiveColor,
            NegativeColor = custom.NegativeColor ?? b.NegativeColor,
            TotalColor = custom.TotalColor ?? b.TotalColor,
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
    internal bool AdaptiveReferenceLabels { get; private init; }
    internal ChartFill? BackgroundFill { get; private init; }
    internal ChartFill? PlotAreaFill { get; private init; }
    internal Color? PlotAreaBorderColor { get; private init; }
    internal float? CardRadius { get; private init; }
    internal Color? CardBorderColor { get; private init; }
    internal float? CardBorderWidth { get; private init; }
    internal float? CardInset { get; private init; }
    internal IReadOnlyList<ChartResolvedEffect> CardEffects { get; private init; } = [];
    internal IReadOnlyList<ChartResolvedEffect> BarEffects { get; private init; } = [];
    internal IReadOnlyList<ChartResolvedEffect> LineEffects { get; private init; } = [];
    internal IReadOnlyList<ChartResolvedEffect> SliceEffects { get; private init; } = [];
    internal string AutoFill { get; private init; } = "solid";
    internal string MarkOutline { get; private init; } = "none";
    internal IReadOnlyList<Color> StrokePalette { get; private init; } = [];
    internal Color? PositiveColor { get; private init; }
    internal Color? NegativeColor { get; private init; }
    internal Color? TotalColor { get; private init; }

    internal Color SeriesColor(Color color, int index) =>
        SeriesPalette.Count > 0 && string.Equals(color.ToHexString(), LibrarySeriesColor, StringComparison.Ordinal)
            ? SeriesPalette[Math.Max(0, index) % SeriesPalette.Count]
            : color;
}
