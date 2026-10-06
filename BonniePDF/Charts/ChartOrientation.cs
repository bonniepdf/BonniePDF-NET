namespace BonniePDF;

/// <summary>
/// Controls the orientation of a Cartesian chart.
/// </summary>
public enum ChartOrientation
{
    /// <summary>
    /// Categories run along the X-axis (bottom) and values along the Y-axis (left). Default.
    /// </summary>
    Vertical,

    /// <summary>
    /// Categories run along the Y-axis (left) and values along the X-axis (bottom).
    /// Bars, stems, and filled areas extend horizontally from the left axis baseline.
    /// </summary>
    Horizontal
}
