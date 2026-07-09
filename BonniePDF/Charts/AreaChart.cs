using System.Collections.Generic;
using BonniePDF.Charts.Models;
using BonniePDF.Core;
using BonniePDF.Drawing;

namespace BonniePDF.Charts;

/// <summary>
/// Area chart that renders a filled polygon with a line overlay.
/// Now supports multiple series with transparency for overlapping areas.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class AreaChart : CartesianChartBase
{
    private const string DefaultAreaColorHex = "#4f46e5";

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
    /// In horizontal mode the area fill extends leftward from the line to the value-axis baseline.
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

        DrawAreaFill(canvas, coordinates, baselineY, series.Color, fillOpacity);
        DrawConnectingLine(canvas, coordinates, series.Color, lineWidth);
    }

    /// <summary>
    /// Gets the vertical offset for value labels (simple spacing above area/line).
    /// </summary>
    protected override float GetLabelVerticalOffset(ChartSeries series) => 7;

    /// <summary>
    /// Draws the area fill as a single closed polygon spanning from the axis baseline to the line.
    /// </summary>
    private void DrawAreaFill(ICanvas canvas, IList<Point> coordinates, float baselineY, Color color, float opacity)
    {
        if (coordinates.Count < 2) return;

        Color fillColor = color.WithAlpha(opacity);
        Point first = coordinates[0];
        Point last = coordinates[^1];

        if (Orientation == ChartOrientation.Horizontal)
        {
            List<Point> path = [new(LeftPadding, first.Y), .. coordinates, new(LeftPadding, last.Y)];
            canvas.FillPath(path, fillColor);
        }
        else
        {
            List<Point> path = [new(first.X, baselineY), .. coordinates, new(last.X, baselineY)];
            canvas.FillPath(path, fillColor);
        }
    }

    /// <summary>
    /// Draws the connecting line on top of the area fill for a series.
    /// </summary>
    private static void DrawConnectingLine(ICanvas canvas, IList<Point> coordinates, Color color, float lineWidth)
    {
        if (coordinates.Count < 2) return;

        for (int i = 0; i < coordinates.Count - 1; i++)
        {
            var p1 = coordinates[i];
            var p2 = coordinates[i + 1];
            canvas.DrawLine(p1.X, p1.Y, p2.X, p2.Y, color, lineWidth);
        }
    }

    /// <summary>
    /// Gets the default color for the series when converting from old API (DataPoints) to new API (Series).
    /// Returns the chart's AreaColor property for backward compatibility.
    /// </summary>
    protected override Color GetDefaultSeriesColor() => AreaColor;
}
