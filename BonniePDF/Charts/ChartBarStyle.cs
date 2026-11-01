// Generated chart source. Do not edit here; the source manifest records its provenance.
namespace BonniePDF;

/// <summary>Opaque two-stop gradient with bar-relative or plot-relative coordinates.</summary>
public sealed class ChartLinearGradient
{
    /// <summary>Opaque colour at the value end (default #93c5fd).</summary>
    public Color StartColor { get; set; } = Color.FromHex("#93c5fd");
    /// <summary>Opaque colour at the baseline (default #2563eb).</summary>
    public Color EndColor { get; set; } = Color.FromHex("#2563eb");
    /// <summary>Coordinate scope: "bar" (default) or "plot".</summary>
    public string Scope { get; set; } = "bar";
}
/// <summary>Explicit settings override theme defaults, including a corner radius of zero.</summary>
public sealed class ChartBarStyle
{
    /// <summary>Value-end radius in points. Null uses the theme; zero keeps square ends.</summary>
    public float? CornerRadius { get; set; }
    /// <summary>Optional opaque linear fill. Null keeps the resolved series colour.</summary>
    public ChartLinearGradient? Gradient { get; set; }
}
