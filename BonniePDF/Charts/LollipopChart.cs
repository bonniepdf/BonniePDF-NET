// Generated chart source. Do not edit here; the source manifest records its provenance.
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Lollipop chart that renders vertical lines (stems) with circular markers (heads).
/// Now supports multiple series for comparing datasets.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class LollipopChart : CartesianChartBase
{
    private const string DefaultColorHex = "#4f46e5";
    private const float DefaultHeadRadiusValue = 8;

    public LollipopChart()
    {
    }

    /// <summary>Creates a lollipop chart bound to its document data.</summary>
    public LollipopChart(ChartData data) : base(data)
    {
    }

    /// <summary>
    /// Default stem width for all series (can be overridden per series via Style).
    /// </summary>
    public float DefaultStemWidth { get; set; } = 2f;

    /// <summary>
    /// Default head radius for all series (can be overridden per series via Style).
    /// </summary>
    public float DefaultHeadRadius { get; set; } = DefaultHeadRadiusValue;

    /// <summary>
    /// Color for the lollipop stems and heads (backward compatibility).
    /// </summary>
    public Color LollipopColor { get; set; } = Color.FromHex(DefaultColorHex);

    /// <summary>
    /// Draws the lollipop chart elements for a single series (stems and heads).
    /// In vertical mode stems are vertical lines; in horizontal mode they are horizontal lines.
    /// </summary>
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        float stemWidth = series.Style?.StemWidth > 0 ? series.Style.StemWidth : DefaultStemWidth;
        float headRadius = series.Style?.MarkerRadius > 0 ? series.Style.MarkerRadius : DefaultHeadRadius;

        DrawLollipopStems(canvas, coordinates, ValueOrigin(data, chartAreaWidth, chartAreaHeight, baselineY), ResolveSeriesColor(series), stemWidth);
        DrawLollipopHeads(canvas, coordinates, ResolveSeriesColor(series), headRadius);
    }

    /// <summary>
    /// Gets the offset for value labels (accounts for lollipop head radius).
    /// </summary>
    protected override float GetLabelVerticalOffset(ChartSeries series)
    {
        float headRadius = series.Style?.MarkerRadius > 0 ? series.Style.MarkerRadius : DefaultHeadRadius;
        return headRadius + 7;
    }

    /// <summary>
    /// Draws lollipop stems from the value origin to each data point: vertical lines in vertical
    /// mode and horizontal lines in horizontal mode.
    /// </summary>
    private void DrawLollipopStems(ICanvas canvas, IList<Point> coordinates,
        float origin, Color color, float stemWidth)
    {
        foreach (Point p in coordinates)
        {
            if (Orientation == ChartOrientation.Horizontal)
                canvas.DrawLine(origin, p.Y, p.X, p.Y, color, stemWidth);
            else
                canvas.DrawLine(p.X, origin, p.X, p.Y, color, stemWidth);
        }
    }

    /// <summary>
    /// Draws the lollipop heads (circles at data points) for a series.
    /// </summary>
    private static void DrawLollipopHeads(ICanvas canvas, IList<Point> coordinates, Color color, float radius)
    {
        foreach (Point p in coordinates)
            canvas.FillCircle(p.X, p.Y, radius, color);
    }

    /// <summary>
    /// Gets the default color for the series when converting from old API (DataPoints) to new API (Series).
    /// Returns the chart's LollipopColor property for backward compatibility.
    /// </summary>
    protected override Color GetDefaultSeriesColor() => LollipopColor;
}
