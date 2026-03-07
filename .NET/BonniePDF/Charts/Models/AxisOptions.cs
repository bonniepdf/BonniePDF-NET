namespace BonniePDF.Charts.Models;

/// <summary>
/// Options for configuring chart axis display
/// </summary>
public class AxisOptions
{
    /// <summary>
    /// Prefix for value labels (e.g., "£", "$")
    /// </summary>
    public string Prefix { get; set; } = "";

    /// <summary>
    /// Suffix for value labels (e.g., "%", "k")
    /// </summary>
    public string Suffix { get; set; } = "";

    /// <summary>
    /// Maximum value for the axis scale
    /// </summary>
    public decimal MaxValue { get; set; } = 10000;

    /// <summary>
    /// Minimum value for the axis scale (default: 0)
    /// </summary>
    public decimal MinValue { get; set; } = 0;

    /// <summary>
    /// Number of grid lines/steps on the axis
    /// </summary>
    public int StepCount { get; set; } = 5;

    /// <summary>
    /// Legend/title for this axis (e.g., "Sales (£)", "Month")
    /// </summary>
    public string Legend { get; set; } = "";

    /// <summary>
    /// Whether to show grid lines for this axis
    /// </summary>
    public bool ShowGridLines { get; set; } = true;

    /// <summary>
    /// Whether to render the legend vertically (rotated 90 degrees) instead of horizontally.
    /// This is particularly useful for Y-axis labels where vertical orientation saves horizontal space.
    /// </summary>
    public bool VerticalLegend { get; set; }
}