namespace BonniePDF.Charts.Models;

/// <summary>
/// Represents a single data point in a chart
/// </summary>
public class ChartDataPoint
{
    /// <summary>
    /// Gets or sets the label for the data point.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the numeric value for the data point.
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Number format for <see cref="FormattedValue"/>: <c>G</c> (default), <c>F&lt;n&gt;</c>, <c>N&lt;n&gt;</c> or <c>P&lt;n&gt;</c>.
    /// Culture-invariant; see <see cref="ChartNumberFormat"/>. Anything else throws when the chart is drawn.
    /// </summary>
    public string LabelFormat { get; set; } = "G";

    /// <summary>
    /// Gets the formatted value string based on Value and LabelFormat.
    /// </summary>
    public string FormattedValue => ChartNumberFormat.Format(Value, LabelFormat);
}