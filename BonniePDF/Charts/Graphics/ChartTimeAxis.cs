// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
namespace BonniePDF;

/// <summary>A time axis: where it starts and ends, its tick unit and the tick label format.</summary>
public sealed class ChartTimeAxis
{
    /// <summary>The left edge. Unset uses the earliest date.</summary>
    public DateTimeOffset? MinDate { get; set; }
    /// <summary>The right edge. Unset uses the latest date.</summary>
    public DateTimeOffset? MaxDate { get; set; }
    /// <summary><c>auto</c> (default), <c>year</c>, <c>quarter</c>, <c>month</c>, <c>week</c>, <c>day</c> or <c>hour</c>.</summary>
    public string Unit { get; set; } = "auto";
    /// <summary>Tick label format: <c>yyyy</c>, <c>yy</c>, <c>Q</c>, <c>MMM</c>, <c>MM</c>, <c>dd</c>, <c>d</c>, <c>HH</c>, <c>mm</c> and <c>'literal'</c>. Unset uses the unit's default.</summary>
    public string? Format { get; set; }
    /// <summary>Draw vertical gridlines at the ticks (default <see langword="true"/>).</summary>
    public bool ShowGridLines { get; set; } = true;
}
