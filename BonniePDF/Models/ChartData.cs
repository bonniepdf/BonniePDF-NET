using System.Collections.Generic;

namespace BonniePDF.Models;

/// <summary>
/// Data model for chart visualization
/// </summary>
public class ChartData
{
    /// <summary>
    /// Chart title displayed at the top
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Data points to display in the chart
    /// </summary>
    public List<ChartDataPoint> DataPoints { get; set; } = new();

    /// <summary>
    /// Y-axis (vertical) configuration
    /// </summary>
    public AxisOptions YAxis { get; set; } = new();

    /// <summary>
    /// X-axis (horizontal) configuration
    /// </summary>
    public AxisOptions XAxis { get; set; } = new();
}