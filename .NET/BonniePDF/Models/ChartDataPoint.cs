namespace BonniePDF.Models;

/// <summary>
/// Represents a single data point in a chart
/// </summary>
public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }

    /// <summary>
    /// Standard .NET format string to use for FormattedValue (e.g., "C0", "P1", "N2").
    /// Defaults to "G" (General).
    /// </summary>
    public string LabelFormat { get; set; } = "G";

    /// <summary>
    /// Gets the formatted value string based on Value and LabelFormat.
    /// </summary>
    public string FormattedValue => Value.ToString(LabelFormat);
}