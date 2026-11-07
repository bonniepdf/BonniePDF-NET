// Generated chart source. Do not edit here; the source manifest records its provenance.
namespace BonniePDF;

/// <summary>A value-axis line, or a band when an end value is supplied.</summary>
public sealed class ChartReference
{
    /// <summary>The line value or first band endpoint.</summary>
    public decimal Value { get; set; }

    /// <summary>The second band endpoint.</summary>
    public decimal? EndValue { get; set; }

    /// <summary>The line or band colour.</summary>
    public required Color Color { get; set; }

    /// <summary>Reference line thickness in points.</summary>
    public float Thickness { get; set; } = 1;

    /// <summary>Optional alternating dash and gap lengths, in points.</summary>
    public float[]? DashPattern { get; set; }
}
