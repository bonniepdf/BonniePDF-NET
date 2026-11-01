// Generated chart source. Do not edit here; the source manifest records its provenance.
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Area chart that renders a filled polygon with a line overlay.
/// Now supports multiple series with transparency for overlapping areas.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class AreaChart : CartesianChartBase
{
    private const string DefaultAreaColorHex = "#4f46e5";

    public AreaChart()
    {
    }

    /// <summary>Creates an area chart bound to its document data.</summary>
    public AreaChart(ChartData data) : base(data)
    {
    }

    /// <summary>
    /// Default fill opacity for all series (can be overridden per series via Style).
    /// </summary>
    public float DefaultFillOpacity { get; set; } = 0.3f;

    /// <summary>
    /// Default line width for all series (can be overridden per series via Style).
    /// </summary>
    public float DefaultLineWidth { get; set; } = 2f;

    /// <summary>
    /// Color for the area fill and line (backward compatibility for single-series charts).
    /// </summary>
    public Color AreaColor { get; set; } = Color.FromHex(DefaultAreaColorHex);

    /// <summary>
    /// Opacity for the area fill (backward compatibility for single-series charts).
    /// </summary>
    public float FillOpacity { get; set; } = 0.3f;

    /// <summary>
    /// Width of the line stroke (backward compatibility for single-series charts).
    /// </summary>
    public float LineWidth { get; set; } = 2f;

    /// <summary>
    /// Draws the area chart elements for a single series (area fill and connecting line).
    /// The fill spans from the line to the value origin, in either orientation.
    /// </summary>
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        float fillOpacity = series.Style?.FillOpacity > 0 && series.Style.FillOpacity != DefaultFillOpacity
            ? series.Style.FillOpacity
            : data.Series.Count == 1
                ? FillOpacity
                : DefaultFillOpacity;
        float lineWidth = series.Style?.LineWidth > 0 && series.Style.LineWidth != DefaultLineWidth
            ? series.Style.LineWidth
            : data.Series.Count == 1
                ? LineWidth
                : DefaultLineWidth;

        DrawAreaFill(canvas, coordinates, ValueOrigin(data, chartAreaWidth, chartAreaHeight, baselineY), ResolveSeriesColor(series), fillOpacity);
        DrawConnectingLine(canvas, coordinates, ResolveSeriesColor(series), lineWidth);
    }

    /// <summary>
    /// Gets the vertical offset for value labels (simple spacing above area/line).
    /// </summary>
    protected override float GetLabelVerticalOffset(ChartSeries series) => 7;

    /// <summary>
    /// Draws the area fill as a single closed polygon spanning from the value origin to the line.
    /// </summary>
    private void DrawAreaFill(ICanvas canvas, IList<Point> coordinates, float origin, Color color, float opacity)
    {
        if (coordinates.Count < 2) return;

        Color fillColor = color.WithAlpha(opacity);
        Point first = coordinates[0];
        Point last = coordinates[^1];

        if (Orientation == ChartOrientation.Horizontal)
        {
            List<Point> path = [new(origin, first.Y), .. coordinates, new(origin, last.Y)];
            canvas.FillPath(path, fillColor);
        }
        else
        {
            List<Point> path = [new(first.X, origin), .. coordinates, new(last.X, origin)];
            canvas.FillPath(path, fillColor);
        }
    }

    /// <summary>
    /// Gets the default color for the series when converting from old API (DataPoints) to new API (Series).
    /// Returns the chart's AreaColor property for backward compatibility.
    /// </summary>
    protected override Color GetDefaultSeriesColor() => AreaColor;
}
