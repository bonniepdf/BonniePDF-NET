// Generated chart source. Do not edit here; the source manifest records its provenance.
using System.Collections.Generic;

namespace BonniePDF;

/// <summary>Precomputed statistics for one box of a box plot.</summary>
public class ChartBoxStats
{
    /// <summary>The lowest whisker end.</summary>
    public decimal Min { get; set; }
    /// <summary>The first quartile.</summary>
    public decimal Q1 { get; set; }
    /// <summary>The median.</summary>
    public decimal Median { get; set; }
    /// <summary>The third quartile.</summary>
    public decimal Q3 { get; set; }
    /// <summary>The highest whisker end.</summary>
    public decimal Max { get; set; }
    /// <summary>Drawn as a diamond when the chart shows means.</summary>
    public decimal? Mean { get; set; }
    /// <summary>Values beyond the whiskers, drawn as hollow circles.</summary>
    public IList<decimal>? Outliers { get; set; }
}

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

    /// <summary>Colour of this point's mark (a bar, slice or bubble), overriding the series colour.</summary>
    public Color? Color { get; set; }

    /// <summary>Fill of this point's mark (a colour or a gradient), overriding <see cref="Color"/> and the series style.</summary>
    public ChartFill? Fill { get; set; }

    /// <summary>Lower bound of a range: range area, candlestick, error bars.</summary>
    public decimal? Low { get; set; }
    /// <summary>Upper bound of a range: range area, candlestick, error bars.</summary>
    public decimal? High { get; set; }
    /// <summary>Opening value of a candlestick.</summary>
    public decimal? Open { get; set; }
    /// <summary>Closing value of a candlestick.</summary>
    public decimal? Close { get; set; }
    /// <summary>Raw observations for a box plot.</summary>
    public IList<decimal>? Samples { get; set; }
    /// <summary>Precomputed box statistics for a box plot; <see cref="Samples"/> wins when both are set.</summary>
    public ChartBoxStats? Stats { get; set; }
    /// <summary>When the point happened, on a time axis (UTC).</summary>
    public System.DateTimeOffset? Date { get; set; }

    /// <summary>Error bar amount above the value (absolute).</summary>
    public decimal? ErrorPlus { get; set; }
    /// <summary>Error bar amount below the value (absolute).</summary>
    public decimal? ErrorMinus { get; set; }

    /// <summary>Gets the formatted value string based on Value and LabelFormat.</summary>
    public string FormattedValue => ChartNumberFormat.Format(Value, LabelFormat);
}
