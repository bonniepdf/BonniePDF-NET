// Generated chart source. Do not edit here; the source manifest records its provenance.
using System.Collections.Generic;
namespace BonniePDF;

/// <summary>The colour of each category in charts that colour every category.</summary>
internal static class ChartCategoryColors
{
    private static readonly Color[] Defaults =
    [
        Color.FromHex("#4f46e5"), Color.FromHex("#7c3aed"), Color.FromHex("#a855f7"), Color.FromHex("#d946ef"), Color.FromHex("#ec4899"), Color.FromHex("#f43f5e"),
        Color.FromHex("#f97316"), Color.FromHex("#eab308"), Color.FromHex("#84cc16"), Color.FromHex("#22c55e"), Color.FromHex("#14b8a6"), Color.FromHex("#06b6d4"),
    ];

    /// <summary>The point's own colour, else the theme palette, else the library cycle.</summary>
    internal static Color For(ChartTheme theme, ChartDataPoint? point, int index)
    {
        Color? own = point?.Fill?.FallbackColor ?? point?.Color;
        if (own is { } c) return c;
        IReadOnlyList<Color> palette = theme.SeriesPalette.Count > 0 ? theme.SeriesPalette : Defaults;
        return palette[index % palette.Count];
    }
}
