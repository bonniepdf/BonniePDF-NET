// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Lollipop chart that renders stems with circular or shaped marker heads.
/// </summary>
public class LollipopChart : CartesianChartBase
{
    private const string DefaultColorHex = "#4f46e5";
    private const float DefaultHeadRadiusValue = 8;

    public LollipopChart() { }
    public LollipopChart(ChartData data) : base(data) { }

    public float DefaultStemWidth { get; set; } = 2f;
    public float DefaultHeadRadius { get; set; } = DefaultHeadRadiusValue;
    public Color LollipopColor { get; set; } = Color.FromHex(DefaultColorHex);
    public string DefaultMarkerShape { get; set; } = "circle";
    public string DefaultMarkerFill { get; set; } = "solid";
    public float DefaultMarkerHaloWidth { get; set; }

    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        float stemWidth = series.Style?.StemWidth > 0 ? series.Style.StemWidth : DefaultStemWidth;
        float headRadius = series.Style?.MarkerRadius > 0 ? series.Style.MarkerRadius : DefaultHeadRadius;
        int seriesIdx = data.Series.IndexOf(series);
        Color color = ResolveSeriesColor(series);
        float origin = ValueOrigin(data, chartAreaWidth, chartAreaHeight, baselineY);

        foreach (Point p in coordinates)
        {
            if (Orientation == ChartOrientation.Horizontal)
                canvas.DrawLine(origin, p.Y, p.X, p.Y, color, stemWidth);
            else
                canvas.DrawLine(p.X, origin, p.X, p.Y, color, stemWidth);
        }

        string shape = ResolveSeriesMarkerShape(series, seriesIdx, DefaultMarkerShape);
        string fill = series.Style?.MarkerFill ?? DefaultMarkerFill;
        float halo = series.Style?.MarkerHaloWidth ?? DefaultMarkerHaloWidth;
        Color bg = EffectiveBackgroundColor ?? Color.White;
        foreach (Point p in coordinates)
            ChartMarkers.Draw(canvas, p.X, p.Y, headRadius, color, shape, fill, halo, bg, stemWidth);
    }

    protected override float GetLabelVerticalOffset(ChartSeries series)
    {
        float headRadius = series.Style?.MarkerRadius > 0 ? series.Style.MarkerRadius : DefaultHeadRadius;
        return headRadius + 7;
    }

    protected override Color GetDefaultSeriesColor() => LollipopColor;
}
