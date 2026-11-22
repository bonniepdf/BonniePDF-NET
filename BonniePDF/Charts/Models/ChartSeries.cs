// Generated chart source. Do not edit here; the source manifest records its provenance.
using System.Collections.Generic;

namespace BonniePDF;

/// <summary>
/// Represents a single data series in a chart.
/// </summary>
public class ChartSeries
{
    /// <summary>Series name/label used in legends.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Color to use for this series (lines, bars, markers, etc.)</summary>
    public Color Color
    {
        get => _color ?? DefaultColor;
        set => _color = value;
    }

    private static readonly Color DefaultColor = Color.FromHex("#4f46e5");

    private Color? _color;

    /// <summary>Whether the author gave this series a colour; an unset series takes its chart's colour.</summary>
    internal bool HasColor => _color is not null;

    /// <summary>Data points for this series.</summary>
    public IList<ChartDataPoint> DataPoints { get; set; } = new List<ChartDataPoint>();

    /// <summary>Series-specific styling options.</summary>
    public ChartSeriesStyle? Style { get; set; } = new();

    /// <summary>Renderer role when used in <see cref="ComboChart"/>: <c>bar</c> (default), <c>line</c>, or <c>area</c>.</summary>
    public string SeriesType { get; set; } = "bar";

    /// <summary>Value axis binding when used in <see cref="ComboChart"/>: <c>primary</c> (default) or <c>secondary</c>.</summary>
    public string ValueAxis { get; set; } = "primary";
}
