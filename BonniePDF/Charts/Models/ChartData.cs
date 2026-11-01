// Generated chart source. Do not edit here; the source manifest records its provenance.
using System.Collections.Generic;

namespace BonniePDF;

/// <summary>
/// Data model for chart visualization.
/// Supports single-series and multi-series charts.
/// </summary>
public class ChartData
{
    /// <summary>
    /// Chart title displayed at the top
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Multiple data series to display in the chart.
    /// Each series can have its own name, color, and data points.
    /// </summary>
    public IList<ChartSeries> Series { get; set; } = new List<ChartSeries>();

    /// <summary>
    /// Y-axis (vertical) configuration
    /// </summary>
    public AxisOptions YAxis { get; set; } = new();

    /// <summary>
    /// X-axis (horizontal) configuration
    /// </summary>
    public AxisOptions XAxis { get; set; } = new();
}
