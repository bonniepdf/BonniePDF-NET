// Generated chart source. Do not edit here; the source manifest records its provenance.
using System.Collections.Generic;

namespace BonniePDF;

/// <summary>
/// Represents a single data series in a chart.
/// Similar to datasets in Chart.js or series in ApexCharts.
/// </summary>
public class ChartSeries
{
    /// <summary>
    /// Series name/label (e.g., "Year Sales", "Product A", "Q1 Revenue")
    /// Used in legends and tooltips.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Color to use for this series (lines, bars, markers, etc.)
    /// </summary>
    public Color Color
    {
        get => _color ?? DefaultColor;
        set => _color = value;
    }

    private static readonly Color DefaultColor = Color.FromHex("#4f46e5");

    private Color? _color;

    /// <summary>Whether the author gave this series a colour; an unset series takes its chart's colour.</summary>
    internal bool HasColor => _color is not null;

    /// <summary>
    /// Data points for this series.
    /// Each data point contains a label, value, and optional format string.
    /// </summary>
    public IList<ChartDataPoint> DataPoints { get; set; } = new List<ChartDataPoint>();

    /// <summary>
    /// Series-specific styling options.
    /// Allows customization of line width, opacity, marker size, etc.
    /// </summary>
    public ChartSeriesStyle? Style { get; set; } = new();
}
