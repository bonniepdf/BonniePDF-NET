using System;
using System.Collections.Generic;
using System.Linq;

namespace BonniePDF.Models;

/// <summary>
/// Data model for chart visualization.
/// Supports both single-series (backward compatible) and multi-series charts.
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
    public List<ChartSeries> Series { get; set; } = new();

    /// <summary>
    /// Y-axis (vertical) configuration
    /// </summary>
    public AxisOptions YAxis { get; set; } = new();

    /// <summary>
    /// X-axis (horizontal) configuration
    /// </summary>
    public AxisOptions XAxis { get; set; } = new();

    /// <summary>
    /// Data points to display in the chart (single series).
    /// <para>
    /// <b>BACKWARD COMPATIBILITY:</b> This property provides backward compatibility with existing code.
    /// When set, it automatically creates a single series named "Data".
    /// When read, it returns the data points from the first series (or an empty list if no series exist).
    /// </para>
    /// <para>
    /// <b>Recommended:</b> Use the <see cref="Series"/> property for new code to enable multi-series support.
    /// </para>
    /// </summary>
    [Obsolete("Use Series property instead for multi-series support. This property is maintained for backward compatibility.")]
    public List<ChartDataPoint> DataPoints
    {
        get => Series.FirstOrDefault()?.DataPoints ?? new();
        set
        {
            if (Series.Count == 0)
            {
                Series.Add(new ChartSeries { Name = "Data", DataPoints = value });
            }
            else
            {
                Series[0].DataPoints = value;
            }
        }
    }
}