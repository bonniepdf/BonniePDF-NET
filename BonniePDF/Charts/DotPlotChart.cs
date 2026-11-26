// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Dot plot chart that renders circular or shaped data points with value labels.
/// </summary>
public class DotPlotChart : CartesianChartBase
{
    internal override bool SupportsAnnotations => true;

    private const string DefaultColorHex = "#4f46e5";
    private const float DefaultDotRadiusValue = 10;

    public DotPlotChart() { }
    public DotPlotChart(ChartData data) : base(data) { }

    public float DefaultDotRadius { get; set; } = DefaultDotRadiusValue;
    public Color DotColor { get; set; } = Color.FromHex(DefaultColorHex);
    public string DefaultMarkerShape { get; set; } = "circle";
    public string DefaultMarkerFill { get; set; } = "solid";
    public float DefaultMarkerHaloWidth { get; set; }

    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        float dotRadius = series.Style?.MarkerRadius > 0 ? series.Style.MarkerRadius : DefaultDotRadius;
        int seriesIdx = data.Series.IndexOf(series);
        Color color = ResolveSeriesColor(series);
        string shape = ResolveSeriesMarkerShape(series, seriesIdx, DefaultMarkerShape);
        string fill = series.Style?.MarkerFill ?? DefaultMarkerFill;
        float halo = series.Style?.MarkerHaloWidth ?? DefaultMarkerHaloWidth;
        Color bg = EffectiveBackgroundColor ?? Color.White;
        foreach (Point p in coordinates)
            ChartMarkers.Draw(canvas, p.X, p.Y, dotRadius, color, shape, fill, halo, bg, 2f);
    }

    protected override float GetLabelVerticalOffset(ChartSeries series)
    {
        float dotRadius = series.Style?.MarkerRadius > 0 ? series.Style.MarkerRadius : DefaultDotRadius;
        return dotRadius + 7;
    }

    protected override Color GetDefaultSeriesColor() => DotColor;
}
