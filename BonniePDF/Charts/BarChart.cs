// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Bar chart that renders vertical bars with value labels above each bar.
/// Now supports multiple series.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class BarChart : CartesianChartBase
{
    protected override void DrawLegendSwatch(ICanvas canvas, float x, float y, float size, ChartSeries series)
    { ChartBars.Draw(canvas, x, y, size, size, ResolveSeriesColor(series), BarStyle, Appearance, false, x, y, size, size); }
    /// <summary>Value-end rounding and optional opaque linear gradient.</summary>
    public ChartBarStyle BarStyle { get; set; } = new();
    internal override bool UsesStyledGeometry => base.UsesStyledGeometry || (BarStyle.CornerRadius ?? 0) > 0 || BarStyle.Gradient is not null;
    private const string BarColorHex = "#4f46e5";

    /// <summary>
    /// Color for the bars (backward compatibility).
    /// </summary>
    public Color BarColor { get; set; } = Color.FromHex(BarColorHex);

    /// <summary>
    /// Width of bars as percentage of available space (0.0 to 1.0, default: 0.6).
    /// </summary>
    public float BarWidthRatio { get; set; } = 0.6f;

    /// <summary>Bars and gridlines are axis-aligned, so crisp edges render sharper than antialiased.</summary>
    public BarChart()
    {
        SvgShapeRendering = "crispEdges";
    }

    /// <summary>Creates a bar chart bound to its document data.</summary>
    public BarChart(ChartData data) : this()
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <summary>
    /// Draws the bar chart elements for a single series.
    /// In vertical mode draws upward bars; in horizontal mode draws rightward bars.
    /// </summary>
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        int dataPointCount = series.DataPoints.Count;
        float origin = ValueOrigin(data, chartAreaWidth, chartAreaHeight, baselineY);

        if (Orientation == ChartOrientation.Horizontal)
        {
            float totalBarSpace = chartAreaHeight / dataPointCount;
            float barThickness = Math.Min(totalBarSpace * BarWidthRatio, 50);
            DrawBars(canvas, coordinates, barThickness, origin, ResolveSeriesColor(series), chartAreaWidth, chartAreaHeight, topY);
        }
        else
        {
            float totalBarSpace = chartAreaWidth / dataPointCount;
            float barWidth = Math.Min(totalBarSpace * BarWidthRatio, 50);
            DrawBars(canvas, coordinates, barWidth, origin, ResolveSeriesColor(series), chartAreaWidth, chartAreaHeight, topY);
        }
    }

    /// <summary>
    /// Gets the offset for value labels (spacing above bars in vertical mode, to the right in horizontal).
    /// </summary>
    protected override float GetLabelVerticalOffset(ChartSeries series) => 7;

    /// <summary>
    /// Draws bars for each data point.
    /// Bars run from the value origin to each point, up or right for values above it.
    /// </summary>
    private void DrawBars(ICanvas canvas, IList<Point> coordinates, float thickness, float origin, Color color, float plotWidth, float plotHeight, float plotY)
    {
        foreach (Point p in coordinates)
        {
            if (Orientation == ChartOrientation.Horizontal)
            {
                float barWidth = p.X - origin;
                ChartBars.Draw(canvas, origin, p.Y - thickness / 2, barWidth, thickness, color, BarStyle, Appearance, true, LeftPadding, plotY, plotWidth, plotHeight);
            }
            else
            {
                float barHeight = origin - p.Y;
                float barX = p.X - thickness / 2;
                ChartBars.Draw(canvas, barX, p.Y, thickness, barHeight, color, BarStyle, Appearance, false, LeftPadding, plotY, plotWidth, plotHeight);
            }
        }
    }

    /// <summary>
    /// Gets the default color for the series when converting from old API (DataPoints) to new API (Series).
    /// Returns the chart's BarColor property for backward compatibility.
    /// </summary>
    protected override Color GetDefaultSeriesColor() => BarColor;
}
