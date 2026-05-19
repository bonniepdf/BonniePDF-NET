using System.Collections.Generic;
using BonniePDF.Charts.Models;
using BonniePDF.Core;
using BonniePDF.Drawing;

namespace BonniePDF.Charts;

/// <summary>
/// Dot plot chart that renders circular data points with value labels.
/// Now supports multiple series for comparing datasets.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class DotPlotChart : CartesianChartBase
{
    private const string DefaultColorHex = "#4f46e5";
    private const float DefaultDotRadiusValue = 10;

    /// <summary>
    /// Default dot radius for all series (can be overridden per series via Style).
    /// </summary>
    public float DefaultDotRadius { get; set; } = DefaultDotRadiusValue;

    /// <summary>
    /// Color for the data point circles (backward compatibility).
    /// </summary>
    public Color DotColor { get; set; } = Color.FromHex(DefaultColorHex);

    /// <summary>
    /// Radius of the data point circles (backward compatibility).
    /// </summary>
    public float DotRadius { get; set; } = DefaultDotRadiusValue;

    /// <summary>
    /// Draws the dot plot chart elements for a single series (circular data points).
    /// </summary>
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        // Get series-specific styling or use defaults
        float dotRadius = series.Style?.MarkerRadius > 0 ? series.Style.MarkerRadius : DefaultDotRadius;

        // Draw data point circles
        DrawDataPoints(canvas, coordinates, series.Color, dotRadius);
    }

    /// <summary>
    /// Gets the vertical offset for value labels (accounts for dot radius).
    /// </summary>
    protected override float GetLabelVerticalOffset(ChartSeries series)
    {
        float dotRadius = series.Style?.MarkerRadius > 0 ? series.Style.MarkerRadius : DefaultDotRadius;
        return dotRadius + 7;
    }

    /// <summary>
    /// Draws the data point circles for a series.
    /// </summary>
    private static void DrawDataPoints(ICanvas canvas, IList<Point> coordinates, Color color, float radius)
    {
        foreach (Point p in coordinates)
            canvas.FillCircle(p.X, p.Y, radius, color);
    }

    /// <summary>
    /// Gets the default color for the series when converting from old API (DataPoints) to new API (Series).
    /// Returns the chart's DotColor property for backward compatibility.
    /// </summary>
    protected override Color GetDefaultSeriesColor() => DotColor;
}
