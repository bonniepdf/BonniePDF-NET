using System.Collections.Generic;
using BonniePDF.Core;

namespace BonniePDF.Charts.Models;

/// <summary>
/// Represents a single data series in a chart.
/// Similar to datasets in Chart.js or series in ApexCharts.
/// </summary>
public class ChartSeries
{
    /// <summary>
    /// Series name/label (e.g., "2024 Sales", "Product A", "Q1 Revenue")
    /// Used in legends and tooltips.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Color to use for this series (lines, bars, markers, etc.)
    /// </summary>
    public Color Color { get; set; } = Color.FromHex("#4f46e5");

    /// <summary>
    /// Data points for this series.
    /// Each data point contains a label, value, and optional format string.
    /// </summary>
    public IList<ChartDataPoint> DataPoints { get; set; } = new List<ChartDataPoint>();

    /// <summary>
    /// Series-specific styling options.
    /// Allows customization of line width, opacity, marker size, etc.
    /// </summary>
    public ChartSeriesStyle Style { get; set; } = new();
}

/// <summary>
/// Series-specific styling options for fine-grained control over appearance.
/// </summary>
public class ChartSeriesStyle
{
    /// <summary>
    /// Line width for Line and Area charts (default: 2f).
    /// </summary>
    public float LineWidth { get; set; } = 2f;

    /// <summary>
    /// Fill opacity for Area charts (0.0 to 1.0, default: 0.3f).
    /// </summary>
    public float FillOpacity { get; set; } = 0.3f;

    /// <summary>
    /// Marker radius for Line, Lollipop, and DotPlot charts (default: 5f).
    /// </summary>
    public float MarkerRadius { get; set; } = 5f;

    /// <summary>
    /// Whether to show circular markers at data points in Line charts (default: true).
    /// </summary>
    public bool ShowMarkers { get; set; } = true;

    /// <summary>
    /// Stem width for Lollipop charts (default: 2f).
    /// </summary>
    public float StemWidth { get; set; } = 2f;
}
