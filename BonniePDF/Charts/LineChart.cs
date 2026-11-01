// Generated chart source. Do not edit here; the source manifest records its provenance.
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Line chart that renders connected data points with straight lines.
/// Now supports multiple series for comparing datasets.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class LineChart : CartesianChartBase
{
    private const string DefaultLineColorHex = "#4f46e5";
    private const float DefaultDataPointRadius = 5;

    public LineChart()
    {
    }

    /// <summary>Creates a line chart bound to its document data.</summary>
    public LineChart(ChartData data) : base(data)
    {
    }

    /// <summary>
    /// Default line width for all series (can be overridden per series via Style).
    /// </summary>
    public float DefaultLineWidth { get; set; } = 2f;

    /// <summary>
    /// Whether to show data point markers by default (can be overridden per series via Style).
    /// </summary>
    public bool DefaultShowMarkers { get; set; } = true;

    /// <summary>
    /// Default marker radius for all series (can be overridden per series via Style).
    /// </summary>
    public float DefaultMarkerRadius { get; set; } = DefaultDataPointRadius;

    /// <summary>
    /// Default color for single-series charts (backward compatibility).
    /// </summary>
    public Color LineColor { get; set; } = Color.FromHex(DefaultLineColorHex);

    /// <summary>
    /// Draws the line chart elements for a single series (connecting line and optional data point markers).
    /// </summary>
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        // Get series-specific styling or use defaults
        float lineWidth = series.Style?.LineWidth > 0 ? series.Style.LineWidth : DefaultLineWidth;
        bool showMarkers = series.Style?.ShowMarkers ?? DefaultShowMarkers;
        float markerRadius = series.Style?.MarkerRadius > 0 ? series.Style.MarkerRadius : DefaultMarkerRadius;

        // Draw the connecting line
        DrawConnectingLine(canvas, coordinates, ResolveSeriesColor(series), lineWidth);

        // Draw data point markers if enabled
        if (showMarkers)
            DrawDataPointMarkers(canvas, coordinates, ResolveSeriesColor(series), markerRadius);
    }

    /// <summary>
    /// Gets the vertical offset for value labels (accounts for data point marker radius).
    /// </summary>
    protected override float GetLabelVerticalOffset(ChartSeries series)
    {
        float markerRadius = series.Style?.MarkerRadius > 0 ? series.Style.MarkerRadius : DefaultMarkerRadius;
        return markerRadius + 7;
    }

    /// <summary>
    /// Draws circular markers at each data point for a series.
    /// </summary>
    private static void DrawDataPointMarkers(ICanvas canvas, IList<Point> coordinates, Color color, float radius)
    {
        foreach (Point p in coordinates)
            canvas.FillCircle(p.X, p.Y, radius, color);
    }

    /// <summary>
    /// Gets the default color for the series when converting from old API (DataPoints) to new API (Series).
    /// Returns the chart's LineColor property for backward compatibility.
    /// </summary>
    protected override Color GetDefaultSeriesColor() => LineColor;
}
