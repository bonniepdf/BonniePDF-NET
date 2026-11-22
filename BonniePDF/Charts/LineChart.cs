// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Line chart that renders connected data points with straight, smooth, or stepped lines.
/// </summary>
public class LineChart : CartesianChartBase
{
    private const string DefaultLineColorHex = "#4f46e5";
    private const float DefaultDataPointRadius = 5;

    public LineChart() { }
    public LineChart(ChartData data) : base(data) { }

    public float DefaultLineWidth { get; set; } = 2f;
    public bool Sparkline { get; set; }
    public float SparklineFillOpacity { get; set; }
    public bool SparklineShowEndpoints { get; set; }
    public bool SparklineHighlightMinMax { get; set; }
    public decimal? SparklineReferenceValue { get; set; }

    public bool DefaultShowMarkers { get; set; } = true;
    public float DefaultMarkerRadius { get; set; } = DefaultDataPointRadius;
    public string DefaultMarkerShape { get; set; } = "circle";
    public string DefaultMarkerFill { get; set; } = "solid";
    public float DefaultMarkerHaloWidth { get; set; }
    public string Curve { get; set; } = "linear";
    public Color LineColor { get; set; } = Color.FromHex(DefaultLineColorHex);

    protected override bool SupportsInlineLegend => true;

    protected override void DrawLegendSwatch(ICanvas canvas, float x, float y, float size, ChartSeries series)
    {
        if (!Appearance.RoundedLegendSwatches)
        {
            base.DrawLegendSwatch(canvas, x, y, size, series);
            return;
        }
        Color color = ResolveSeriesColor(series);
        int idx = (_renderData ?? Data).Series.IndexOf(series);
        float[]? dash = ResolveSeriesDashPattern(series, idx);
        float midY = y + size / 2;
        if (dash is { Length: > 0 })
        {
            double[] dashD = Array.ConvertAll(dash, v => (double)v);
            ChartVector.Stroke(canvas, new ChartStrokePlan([new("M", [x, midY]), new("L", [x + size + 2, midY])], color, 2, "butt", "miter", 10, dashD));
        }
        else
        {
            canvas.DrawLine(x, midY, x + size + 2, midY, color, 2);
        }
        if (series.Style?.ShowMarkers ?? DefaultShowMarkers)
        {
            string shape = ResolveSeriesMarkerShape(series, idx, DefaultMarkerShape);
            ChartMarkers.Draw(canvas, x + (size + 2) / 2, midY, 2.8f, color, shape, series.Style?.MarkerFill ?? DefaultMarkerFill, 0, EffectiveBackgroundColor ?? Color.White, 1.2f);
        }
    }

    private protected override bool DrawCompact(ICanvas canvas, ChartData data)
    {
        if (!Sparkline) return false;
        bool horizontal = Orientation == ChartOrientation.Horizontal;
        if (SparklineReferenceValue.HasValue)
        {
            float frac = Math.Clamp((float)ChartValueScale.Fraction(SparklineReferenceValue.Value, _effectiveMin, _effectiveMax), 0f, 1f);
            float ry = (1f - frac) * Height;
            ChartVector.Stroke(canvas, new ChartStrokePlan([new("M", [0, ry]), new("L", [Width, ry])], ThemeAxisColor, 0.75, "butt", "miter", 10, [2, 2]));
        }
        for (int sIdx = 0; sIdx < data.Series.Count; sIdx++)
        {
            ChartSeries series = data.Series[sIdx];
            float lineWidth = series.Style?.LineWidth > 0 ? series.Style.LineWidth : DefaultLineWidth;
            float extra = (SparklineShowEndpoints || SparklineHighlightMinMax) ? 2f : 0f;
            float inset = MathF.Min(lineWidth / 2f + extra, MathF.Min(Width / 2f, Height / 2f));
            List<Point> points = new(series.DataPoints.Count);
            for (int i = 0; i < series.DataPoints.Count; i++)
            {
                float category = series.DataPoints.Count > 1 ? (float)i / (series.DataPoints.Count - 1) : 0.5f;
                float fraction = Math.Clamp((float)ChartValueScale.Fraction(series.DataPoints[i].Value, _effectiveMin, _effectiveMax), 0f, 1f);
                points.Add(new Point(
                    inset + (horizontal ? fraction : category) * (Width - 2 * inset),
                    inset + (horizontal ? category : 1 - fraction) * (Height - 2 * inset)));
            }
            Color color = ResolveSeriesColor(series);
            string curve = series.Style?.Curve ?? Curve;
            float[]? dash = ResolveSeriesDashPattern(series, sIdx);
            if (SparklineFillOpacity > 0 && points.Count >= 2)
            {
                var curveCmds = ChartCurves.BuildCurveCommands(points, curve, horizontal, moveToFirst: false);
                List<ChartPathCommand> cmds = new();
                if (horizontal)
                {
                    cmds.Add(new("M", [inset, points[0].Y]));
                    cmds.AddRange(curveCmds);
                    cmds.Add(new("L", [inset, points[^1].Y]));
                    cmds.Add(new("Z", []));
                }
                else
                {
                    cmds.Add(new("M", [points[0].X, Height - inset]));
                    cmds.AddRange(curveCmds);
                    cmds.Add(new("L", [points[^1].X, Height - inset]));
                    cmds.Add(new("Z", []));
                }
                ChartVector.Fill(canvas, new ChartPathPlan(cmds, color.WithAlpha(SparklineFillOpacity), null));
            }
            DrawSeriesLine(canvas, points, color, lineWidth, curve, dash);
            if (SparklineShowEndpoints && points.Count > 0)
            {
                canvas.FillCircle(points[0].X, points[0].Y, 2.5f, color);
                if (points.Count > 1) canvas.FillCircle(points[^1].X, points[^1].Y, 2.5f, color);
            }
            if (SparklineHighlightMinMax && points.Count > 0)
            {
                int minIdx = 0, maxIdx = 0;
                for (int i = 1; i < series.DataPoints.Count; i++)
                {
                    if (series.DataPoints[i].Value < series.DataPoints[minIdx].Value) minIdx = i;
                    if (series.DataPoints[i].Value > series.DataPoints[maxIdx].Value) maxIdx = i;
                }
                canvas.FillCircle(points[minIdx].X, points[minIdx].Y, 2.5f, Color.FromHex("#e11d48"));
                canvas.FillCircle(points[maxIdx].X, points[maxIdx].Y, 2.5f, Color.FromHex("#0d9488"));
            }
        }
        return true;
    }

    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        float lineWidth = series.Style?.LineWidth > 0 ? series.Style.LineWidth : DefaultLineWidth;
        bool showMarkers = series.Style?.ShowMarkers ?? DefaultShowMarkers;
        float markerRadius = series.Style?.MarkerRadius > 0 ? series.Style.MarkerRadius : DefaultMarkerRadius;
        int seriesIdx = data.Series.IndexOf(series);
        Color color = ResolveSeriesColor(series);
        string curve = series.Style?.Curve ?? Curve;
        float[]? dash = ResolveSeriesDashPattern(series, seriesIdx);

        DrawSeriesLine(canvas, coordinates, color, lineWidth, curve, dash);

        if (showMarkers)
        {
            string shape = ResolveSeriesMarkerShape(series, seriesIdx, DefaultMarkerShape);
            string fill = series.Style?.MarkerFill ?? DefaultMarkerFill;
            float halo = series.Style?.MarkerHaloWidth ?? DefaultMarkerHaloWidth;
            Color bg = EffectiveBackgroundColor ?? Color.White;
            foreach (Point p in coordinates)
                ChartMarkers.Draw(canvas, p.X, p.Y, markerRadius, color, shape, fill, halo, bg, lineWidth);
        }
    }

    protected override float GetLabelVerticalOffset(ChartSeries series)
    {
        float markerRadius = series.Style?.MarkerRadius > 0 ? series.Style.MarkerRadius : DefaultMarkerRadius;
        return markerRadius + 7;
    }

    protected override Color GetDefaultSeriesColor() => LineColor;
}
