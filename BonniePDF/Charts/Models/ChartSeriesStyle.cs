namespace BonniePDF;

/// <summary>
/// Series-specific styling options for fine-grained control over appearance.
/// </summary>
public class ChartSeriesStyle
{
    /// <summary>
    /// Line width for Line and Area charts (default: 2f).
    /// </summary>
    public float LineWidth { get; set; } = 2f;

    /// <summary>
    /// Fill opacity for Area charts (0.0 to 1.0, default: 0.3f).
    /// </summary>
    public float FillOpacity { get; set; } = 0.3f;

    /// <summary>
    /// Marker radius for Line, Lollipop, and DotPlot charts (default: 5f).
    /// </summary>
    public float MarkerRadius { get; set; } = 5f;

    /// <summary>
    /// Whether to show circular markers at data points in Line charts (default: true).
    /// </summary>
    public bool ShowMarkers { get; set; } = true;

    /// <summary>
    /// Stem width for Lollipop charts (default: 2f).
    /// </summary>
    public float StemWidth { get; set; } = 2f;
}
