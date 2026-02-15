using System.Collections.Generic;
using BonniePDF.Drawing;
using BonniePDF.Models;

namespace BonniePDF.Charts;

/// <summary>
/// Line chart that renders connected data points with straight lines.
/// Now supports multiple series for comparing datasets.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class LineChart : CartesianChartBase
{
    private const string DefaultLineColorHex = "#4f46e5";
    private const float DefaultDataPointRadius = 5;

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
    /// Width of the line stroke for single-series charts (backward compatibility).
    /// </summary>
    public float LineWidth { get; set; } = 2f;

    /// <summary>
    /// Whether to show circular markers at data points for single-series charts (backward compatibility).
    /// </summary>
    public bool ShowDataPointMarkers { get; set; } = true;

    /// <summary>
    /// Draws the line chart elements for a single series (connecting line and optional data point markers).
    /// </summary>
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        List<(float x, float y)> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        // Get series-specific styling or use defaults
        float lineWidth = series.Style?.LineWidth > 0 ? series.Style.LineWidth : DefaultLineWidth;
        bool showMarkers = series.Style?.ShowMarkers ?? DefaultShowMarkers;
        float markerRadius = series.Style?.MarkerRadius > 0 ? series.Style.MarkerRadius : DefaultMarkerRadius;

        // Draw the connecting line
        DrawConnectingLine(canvas, coordinates, series.Color, lineWidth);

        // Draw data point markers if enabled
        if (showMarkers)
            DrawDataPointMarkers(canvas, coordinates, series.Color, markerRadius);
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
    /// Draws the connecting line between all data points for a series.
    /// </summary>
    private void DrawConnectingLine(ICanvas canvas, List<(float x, float y)> coordinates, Color color, float lineWidth)
    {
        if (coordinates.Count < 2) return;

        for (int i = 0; i < coordinates.Count - 1; i++)
        {
            (float x1, float y1) = coordinates[i];
            (float x2, float y2) = coordinates[i + 1];

            canvas.DrawLine(x1, y1, x2, y2, color, lineWidth);
        }
    }

    /// <summary>
    /// Draws circular markers at each data point for a series.
    /// </summary>
    private void DrawDataPointMarkers(ICanvas canvas, List<(float x, float y)> coordinates, Color color, float radius)
    {
        foreach ((float x, float y) in coordinates)
            canvas.FillCircle(x, y, radius, color);
    }

    /// <summary>
    /// Gets the default color for the series when converting from old API (DataPoints) to new API (Series).
    /// Returns the chart's LineColor property for backward compatibility.
    /// </summary>
    protected override Color GetDefaultSeriesColor() => LineColor;

    #region Backward Compatibility - Obsolete Methods

    /// <summary>
    /// Draws the line chart elements (connecting line and optional data point markers).
    /// OBSOLETE: Use DrawSeriesElements instead for multi-series support.
    /// </summary>
    [System.Obsolete("Use DrawSeriesElements instead for multi-series support")]
    protected override void DrawChartElements(ICanvas canvas, ChartData data, List<(float x, float y)> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        // For backward compatibility, treat as single series
        var series = data.Series.Count > 0 ? data.Series[0] : new ChartSeries { Color = LineColor };
        DrawSeriesElements(canvas, data, series, coordinates, chartAreaWidth, chartAreaHeight, baselineY, topY);
    }

    #endregion
}