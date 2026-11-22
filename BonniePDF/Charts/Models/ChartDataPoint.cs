// Generated chart source. Do not edit here; the source manifest records its provenance.
namespace BonniePDF;

/// <summary>
/// Represents a single data point in a chart
/// </summary>
public class ChartDataPoint
{
    private decimal _value;

    /// <summary>Gets or sets the label for the data point.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the numeric value for the data point.</summary>
    public decimal Value
    {
        get => _value;
        set { _value = value; HasExplicitValue = true; }
    }

    internal bool HasExplicitValue { get; private set; }

    /// <summary>
    /// Number format for <see cref="FormattedValue"/>: <c>G</c> (default), <c>F&lt;n&gt;</c>, <c>N&lt;n&gt;</c>, <c>P&lt;n&gt;</c> or <c>K&lt;n&gt;</c>.
    /// Culture-invariant; see <see cref="ChartNumberFormat"/>. Anything else throws when the chart is drawn.
    /// </summary>
    public string LabelFormat { get; set; } = "G";

    internal bool HasExplicitLabelFormat => !string.Equals(LabelFormat, "G", System.StringComparison.Ordinal);

    /// <summary>Continuous X coordinate for <see cref="ScatterChart"/>; ignored by categorical charts.</summary>
    public decimal? X { get; set; }

    /// <summary>Positive bubble size magnitude for <see cref="ScatterChart"/>; ignored by other charts.</summary>
    public decimal? Size { get; set; }

    /// <summary>Point role for <see cref="WaterfallChart"/>: <c>delta</c> (default), <c>total</c>, or <c>subtotal</c>.</summary>
    public string Kind { get; set; } = "delta";

    /// <summary>Gets the formatted value string based on Value and LabelFormat.</summary>
    public string FormattedValue => ChartNumberFormat.Format(Value, LabelFormat);
}
