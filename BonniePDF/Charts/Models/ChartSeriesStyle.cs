// Generated chart source. Do not edit here; the source manifest records its provenance.
namespace BonniePDF;

/// <summary>
/// Series-specific styling options for fine-grained control over appearance.
/// </summary>
public class ChartSeriesStyle
{
    /// <summary>Line width for Line and Area charts (default: 2f).</summary>
    public float LineWidth { get; set; } = 2f;

    /// <summary>Fill opacity for Area charts (0.0 to 1.0, default: 0.3f).</summary>
    public float FillOpacity { get; set; } = 0.3f;

    /// <summary>Marker radius for Line, Lollipop, and DotPlot charts (default: 5f).</summary>
    public float MarkerRadius { get; set; } = 5f;

    /// <summary>Whether to show markers at data points in Line charts (default: true).</summary>
    public bool ShowMarkers { get; set; } = true;

    /// <summary>Stem width for Lollipop charts (default: 2f).</summary>
    public float StemWidth { get; set; } = 2f;

    /// <summary>Optional alternating dash and gap lengths in points for line strokes.</summary>
    public float[]? DashPattern { get; set; }

    /// <summary>Optional curve interpolation override: <c>linear</c>, <c>smooth</c>, <c>stepBefore</c>, <c>stepAfter</c>, or <c>stepMiddle</c>.</summary>
    public string? Curve { get; set; }

    /// <summary>Optional marker shape override: <c>circle</c>, <c>square</c>, <c>diamond</c>, or <c>triangle</c>.</summary>
    public string? MarkerShape { get; set; }

    /// <summary>Optional marker fill override: <c>solid</c> or <c>hollow</c>.</summary>
    public string? MarkerFill { get; set; }

    /// <summary>Optional outer halo ring width around markers in points.</summary>
    public float? MarkerHaloWidth { get; set; }

    /// <summary>Optional vector hatch/dot pattern fill for monochrome print accessibility.</summary>
    public string? Pattern { get; set; }
}
