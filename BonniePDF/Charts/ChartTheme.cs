// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;

namespace BonniePDF;

/// <summary>
/// Everything a chart theme decides. Charts read these values rather than testing the theme name,
/// so each theme is described once, here.
/// </summary>
internal sealed class ChartTheme
{
    /// <summary>The library's default series colour; a theme palette replaces only this colour.</summary>
    private const string LibrarySeriesColor = "#4f46e5";

    internal static readonly ChartTheme Legacy = new()
    {
        TitleX = null,
        TitleY = 35,
        TitleColor = Color.FromHex("#333333"),
        TitleFontSize = 20,
        TitleFontFamily = StandardFonts.Helvetica,
        TitleAlignment = TextAlignment.Center,
        AxisColor = Color.FromHex("#666666"),
        GridLineColor = Color.FromHex("#e0e0e0"),
        AxisFrame = true,
        AxisLineWidth = 1,
        SeriesPalette = [],
        MeasuredLabels = false,
        FlowLegend = false,
        BarCornerRadius = 0,
        StyledGeometry = false,
        PdfTextBaseline = false,
    };

    internal static readonly ChartTheme Modern = new()
    {
        TitleX = 32,
        TitleY = 20,
        TitleColor = Color.FromHex("#0f172a"),
        TitleFontSize = 18,
        TitleFontFamily = StandardFonts.HelveticaBold,
        TitleAlignment = TextAlignment.Left,
        AxisColor = Color.FromHex("#64748b"),
        GridLineColor = Color.FromHex("#e2e8f0"),
        AxisFrame = false,
        AxisLineWidth = 0.5f,
        SeriesPalette = [Color.FromHex("#2563eb"), Color.FromHex("#0d9488"), Color.FromHex("#7c3aed"), Color.FromHex("#d97706"), Color.FromHex("#e11d48"), Color.FromHex("#0284c7")],
        MeasuredLabels = true,
        FlowLegend = true,
        BarCornerRadius = 4,
        StyledGeometry = true,
        PdfTextBaseline = true,
    };

    /// <summary>The theme for a validated <see cref="ChartBase.Theme"/> name.</summary>
    internal static ChartTheme For(string name) => name == "modern" ? Modern : Legacy;

    /// <summary>Left edge of a left-aligned title; <c>null</c> centres the title on the chart.</summary>
    internal float? TitleX { get; private init; }
    internal float TitleY { get; private init; }
    internal Color TitleColor { get; private init; }
    internal float TitleFontSize { get; private init; }
    internal string TitleFontFamily { get; private init; } = StandardFonts.Helvetica;
    internal TextAlignment TitleAlignment { get; private init; }

    /// <summary>Axis lines, tick labels, axis legends and legend text.</summary>
    internal Color AxisColor { get; private init; }
    internal Color GridLineColor { get; private init; }

    /// <summary>Draws both axis lines with crisp edges; otherwise only the category baseline.</summary>
    internal bool AxisFrame { get; private init; }
    internal float AxisLineWidth { get; private init; }

    /// <summary>Replaces the library's default series colour, cycled by series index. Empty keeps it.</summary>
    internal IReadOnlyList<Color> SeriesPalette { get; private init; } = [];

    /// <summary>Places value and category labels by measured bounds and omits overlapping ones.</summary>
    internal bool MeasuredLabels { get; private init; }

    /// <summary>Wraps legend entries by measured width instead of fixed-width columns.</summary>
    internal bool FlowLegend { get; private init; }

    /// <summary>Bar corner radius when the bar style sets none.</summary>
    internal double BarCornerRadius { get; private init; }

    /// <summary>Draws bars as resolved vector paths and requests antialiased SVG shapes.</summary>
    internal bool StyledGeometry { get; private init; }

    /// <summary>Standalone SVG follows the PDF text-baseline and rotation conventions.</summary>
    internal bool PdfTextBaseline { get; private init; }

    /// <summary>The colour of the series at <paramref name="index"/> when the chart supplies <paramref name="color"/> because the author set none.</summary>
    internal Color SeriesColor(Color color, int index) =>
        SeriesPalette.Count > 0 && color.ToHexString() == LibrarySeriesColor ? SeriesPalette[Math.Max(0, index) % SeriesPalette.Count] : color;
}
