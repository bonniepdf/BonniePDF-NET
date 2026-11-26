// Generated chart source. Do not edit here; the source manifest records its provenance.
namespace BonniePDF;

/// <summary>
/// Options for configuring chart axis display
/// </summary>
public class AxisOptions
{
    /// <summary>Prefix for value labels (e.g., "£", "$")</summary>
    public string Prefix { get; set; } = "";

    /// <summary>Suffix for value labels (e.g., "%", "k")</summary>
    public string Suffix { get; set; } = "";

    /// <summary>Highest value on the value axis. Must be greater than <see cref="MinValue"/> when a chart with series is drawn.</summary>
    public decimal MaxValue { get; set; } = 10000;

    internal bool HasMaxValue => MaxValue != 10000m;

    /// <summary>Lowest value on the value axis (default: 0).</summary>
    public decimal MinValue { get; set; }

    internal bool HasMinValue => MinValue != 0m;

    /// <summary>Number of grid lines/steps on the axis</summary>
    public int StepCount { get; set; } = 5;

    /// <summary>Legend/title for this axis (e.g., "Sales (£)", "Month")</summary>
    public string Legend { get; set; } = "";

    /// <summary>Whether to show grid lines for this axis</summary>
    public bool ShowGridLines { get; set; } = true;

    /// <summary>Whether to render the legend vertically (rotated 90 degrees) instead of horizontally.</summary>
    public bool VerticalLegend { get; set; }

    /// <summary>Compute nice axis bounds and tick spacing from plotted data using Heckbert scaling.</summary>
    public bool AutoScale { get; set; }

    /// <summary>Include zero in the auto-scaled range; null defers to the chart family's default.</summary>
    public bool? IncludeZero { get; set; }

    /// <summary>Fractional headroom (0..0.5) beyond extreme values before rounding; null defers to chart default.</summary>
    public double? Headroom { get; set; }

    /// <summary>Number format string (<c>G</c>, <c>F&lt;n&gt;</c>, <c>N&lt;n&gt;</c>, <c>P&lt;n&gt;</c>, <c>K&lt;n&gt;</c>) for axis tick labels.</summary>
    public string? ValueFormat { get; set; }

    /// <summary>Category-axis label overflow strategy: <c>omit</c> (default), <c>rotate</c>, <c>wrap</c>, or <c>auto</c>.</summary>
    public string LabelOverflow { get; set; } = "omit";
    /// <summary>Where a negative tick's minus sign goes relative to <see cref="Prefix"/>: <c>minusAfterPrefix</c> (default, <c>$-1</c>), <c>minus</c> (<c>-$1</c>) or <c>parentheses</c> (<c>($1)</c>).</summary>
    public string NegativeStyle { get; set; } = "minusAfterPrefix";

    /// <summary><c>linear</c> (default) or <c>log</c>. A logarithmic value axis needs every plotted value and bound above zero; bars then start at the lower bound.</summary>
    public string Scale { get; set; } = "linear";

    /// <summary>Base of a logarithmic axis, above 1 (<c>10</c>). Ticks sit at its integer powers.</summary>
    public double LogBase { get; set; } = 10;

    /// <summary>On a logarithmic axis, draw faint gridlines at 2 to base-1 times each power.</summary>
    public bool ShowMinorGridLines { get; set; }
}
