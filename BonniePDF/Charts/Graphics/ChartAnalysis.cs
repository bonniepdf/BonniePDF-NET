// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Linq;
namespace BonniePDF;

/// <summary>Error bars for every point of a series. A point's own <c>ErrorPlus</c> and <c>ErrorMinus</c> take precedence.</summary>
public sealed class ChartErrorBars
{
    /// <summary><c>fixed</c>: <see cref="Amount"/>. <c>percent</c>: <see cref="Amount"/> percent of the value. <c>stdDev</c>: <see cref="Amount"/> sample standard deviations of the series. <c>stdError</c>: <see cref="Amount"/> standard errors.</summary>
    public string Type { get; set; } = "fixed";
    /// <summary>The size, as <see cref="Type"/> describes.</summary>
    public double Amount { get; set; }
    /// <summary>Which sides are drawn: <c>both</c> (default), <c>plus</c> or <c>minus</c>.</summary>
    public string Direction { get; set; } = "both";
    /// <summary>Width of the end caps in points (<c>6</c>).</summary>
    public float CapWidth { get; set; } = 6;
    /// <summary>Line colour. Unset uses the theme's text colour.</summary>
    public Color? Color { get; set; }
    /// <summary>Line width in points (<c>1</c>).</summary>
    public float Width { get; set; } = 1;

    private static readonly string[] ErrorBarTypes = ["fixed", "percent", "stdDev", "stdError"];
    private static readonly string[] ErrorBarDirections = ["both", "plus", "minus"];

    internal void Validate()
    {
        if (!ErrorBarTypes.Contains(Type, StringComparer.Ordinal)) throw new InvalidOperationException($"Unknown error bar type '{Type}'.");
        if (!(Amount >= 0) || !double.IsFinite(Amount)) throw new InvalidOperationException("Error bar amount must be finite and nonnegative.");
        if (!ErrorBarDirections.Contains(Direction, StringComparer.Ordinal)) throw new InvalidOperationException($"Unknown error bar direction '{Direction}'.");
        if (!(CapWidth >= 0)) throw new InvalidOperationException("Error bar capWidth must be nonnegative.");
        if (!(Width > 0)) throw new InvalidOperationException("Error bar width must be positive.");
    }
}

/// <summary>A bar between the first and the last series at each category, coloured by direction.</summary>
public sealed class ChartUpDownBars
{
    /// <summary>Colour where the last series is at or above the first. Unset uses the theme's positive colour.</summary>
    public Color? UpColor { get; set; }
    /// <summary>Colour where it is below. Unset uses the theme's negative colour.</summary>
    public Color? DownColor { get; set; }
    /// <summary>The fraction of a category slot the bar fills (<c>0.3</c>).</summary>
    public float WidthRatio { get; set; } = 0.3f;
}

/// <summary>A grid of the plotted values beneath a vertical category chart.</summary>
public sealed class ChartDataTable
{
    /// <summary>Draw the table (<see langword="true"/>).</summary>
    public bool Show { get; set; } = true;
    /// <summary>Put each series' legend swatch in its row, and hide the legend (<see langword="true"/>).</summary>
    public bool ShowLegendKeys { get; set; } = true;
    /// <summary>Number format of the cells. Unset uses the chart's value label format.</summary>
    public string? Format { get; set; }
}

/// <summary>A volume pane beneath the price pane of a <see cref="CandlestickChart"/>.</summary>
public sealed class CandlestickVolume
{
    /// <summary>The series holding the volumes: an <see cref="int"/> index or a <see cref="string"/> name. One point per candle, in the same order.</summary>
    public required object Series { get; set; }
    /// <summary>The pane's height as a fraction of the chart height (<c>0.2</c>).</summary>
    public float PaneRatio { get; set; } = 0.2f;
}

/// <summary>A regression line over one series of a category chart; <c>x</c> is the category number from 1.</summary>
public sealed class ChartTrendline
{
    /// <summary>The series: an <see cref="int"/> index or a <see cref="string"/> name.</summary>
    public required object Series { get; set; }
    /// <summary><c>linear</c>, <c>exponential</c>, <c>logarithmic</c>, <c>power</c>, <c>polynomial</c> or <c>movingAverage</c>.</summary>
    public required string Type { get; set; }
    /// <summary>Points averaged by a <c>movingAverage</c> (at least 2).</summary>
    public int Period { get; set; } = 2;
    /// <summary>Order of a <c>polynomial</c>, from 2 to 4 (<c>2</c>).</summary>
    public int Order { get; set; } = 2;
    /// <summary>What to write beside the line: <c>none</c> (default), <c>equation</c>, <c>r2</c> or <c>both</c>.</summary>
    public string Label { get; set; } = "none";
    /// <summary>Line colour. Unset uses the series colour.</summary>
    public Color? Color { get; set; }
    /// <summary>Alternating dash and gap lengths in points (<c>6, 4</c>).</summary>
    public float[]? DashPattern { get; set; }

    private static readonly string[] TrendlineTypes = ["linear", "exponential", "logarithmic", "power", "polynomial", "movingAverage"];
    private static readonly string[] TrendlineLabels = ["none", "equation", "r2", "both"];

    internal void Validate()
    {
        if (!TrendlineTypes.Contains(Type, StringComparer.Ordinal)) throw new InvalidOperationException($"Unknown trendline type '{Type}'.");
        if (!TrendlineLabels.Contains(Label, StringComparer.Ordinal)) throw new InvalidOperationException($"Unknown trendline label '{Label}'.");
    }
}
