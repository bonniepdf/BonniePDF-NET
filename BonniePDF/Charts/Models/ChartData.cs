// Generated chart source. Do not edit here; the source manifest records its provenance.
using System.Collections.Generic;

namespace BonniePDF;

/// <summary>
/// Data model for chart visualization.
/// Supports single-series and multi-series charts.
/// </summary>
public class ChartData
{
    /// <summary>Chart title displayed at the top</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional subtitle displayed beneath the chart title</summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>Multiple data series to display in the chart.</summary>
    public IList<ChartSeries> Series { get; set; } = new List<ChartSeries>();

    /// <summary>Y-axis (vertical) configuration</summary>
    public AxisOptions YAxis { get; set; } = new();

    /// <summary>X-axis (horizontal) configuration</summary>
    public AxisOptions XAxis { get; set; } = new();
}
