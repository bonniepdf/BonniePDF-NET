// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Bar chart that renders vertical or horizontal bars with value labels.
/// </summary>
public class BarChart : CartesianChartBase
{
    internal override bool SupportsAnnotations => true;

    internal override float? AnnotationBarThickness(float slot, int seriesCount) => Math.Min(slot * BarWidthRatio, 50);

    protected override void DrawLegendSwatch(ICanvas canvas, float x, float y, float size, ChartSeries series)
    {
        Color color = ResolveSeriesColor(series);
        int idx = (_renderData ?? Data).Series.IndexOf(series);
        string pat = ResolveSeriesPattern(series, idx);
        ChartBars.Draw(canvas, x, y, size, size, color, BarStyle, Appearance, false, x, y, size, size);
        if (pat is not "none") ChartPatterns.FillRect(canvas, x, y, size, size, new ChartPatternFill(pat, Color.White));
    }

    public ChartBarStyle BarStyle { get; set; } = new();
    internal override bool UsesStyledGeometry => base.UsesStyledGeometry || (BarStyle.CornerRadius ?? 0) > 0 || BarStyle.Gradient is not null;
    private const string BarColorHex = "#4f46e5";

    public Color BarColor { get; set; } = Color.FromHex(BarColorHex);
    public float BarWidthRatio { get; set; } = 0.6f;

    public BarChart()
    {
        SvgShapeRendering = "crispEdges";
    }

    public BarChart(ChartData data) : this()
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        int dataPointCount = series.DataPoints.Count;
        if (dataPointCount == 0) return;
        float origin = ValueOrigin(data, chartAreaWidth, chartAreaHeight, baselineY);
        Color color = ResolveSeriesColor(series);
        int seriesIdx = data.Series.IndexOf(series);
        string pat = ResolveSeriesPattern(series, seriesIdx);

        if (Orientation == ChartOrientation.Horizontal)
        {
            float totalBarSpace = chartAreaHeight / dataPointCount;
            float barThickness = Math.Min(totalBarSpace * BarWidthRatio, 50);
            for (int index = 0; index < coordinates.Count; index++)
            {
                Point p = coordinates[index];
                float barWidth = p.X - origin;
                float barY = p.Y - barThickness / 2;
                var paint = ResolveMarkPaint(series, index < series.DataPoints.Count ? series.DataPoints[index] : null, color);
                MarkGroup(canvas, data, series, index, new ChartGroupAnchor(origin, p.Y, "x"), () =>
                {
                    DrawBarDepth(canvas, origin, barY, barWidth, barThickness, paint.Color);
                    ChartBars.Draw(canvas, origin, barY, barWidth, barThickness, paint.Color, BarStyle, Appearance, true, _plotLeft, topY, chartAreaWidth, chartAreaHeight, paint.Fill is { } hf ? (hf, FillBoxes) : null, ResolveBarEffectsResolved(series, BarStyle.Effects));
                    if (pat is not "none")
                    {
                        float rx = barWidth < 0 ? origin + barWidth : origin;
                        ChartPatterns.FillRect(canvas, rx, barY, MathF.Abs(barWidth), barThickness, new ChartPatternFill(pat, Color.White));
                    }
                });
            }
        }
        else
        {
            float totalBarSpace = chartAreaWidth / dataPointCount;
            float barWidth = Math.Min(totalBarSpace * BarWidthRatio, 50);
            for (int index = 0; index < coordinates.Count; index++)
            {
                Point p = coordinates[index];
                float barHeight = origin - p.Y;
                float barX = p.X - barWidth / 2;
                var paint = ResolveMarkPaint(series, index < series.DataPoints.Count ? series.DataPoints[index] : null, color);
                MarkGroup(canvas, data, series, index, new ChartGroupAnchor(p.X, origin, "y"), () =>
                {
                    DrawBarDepth(canvas, barX, p.Y, barWidth, barHeight, paint.Color);
                    ChartBars.Draw(canvas, barX, p.Y, barWidth, barHeight, paint.Color, BarStyle, Appearance, false, _plotLeft, topY, chartAreaWidth, chartAreaHeight, paint.Fill is { } vf ? (vf, FillBoxes) : null, ResolveBarEffectsResolved(series, BarStyle.Effects));
                    if (pat is not "none")
                    {
                        float ry = barHeight < 0 ? p.Y + barHeight : p.Y;
                        ChartPatterns.FillRect(canvas, barX, ry, barWidth, MathF.Abs(barHeight), new ChartPatternFill(pat, Color.White));
                    }
                });
            }
        }
    }

    protected override void DrawValueLabels(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (!ShowValueLabels) return;
        if (ValueLabelPosition is "outside")
        {
            base.DrawValueLabels(canvas, data, chartAreaWidth, chartAreaHeight, baselineY, topY);
            return;
        }
        float origin = ValueOrigin(data, chartAreaWidth, chartAreaHeight, baselineY);
        foreach (ChartSeries series in data.Series)
        {
            int count = series.DataPoints.Count;
            if (count == 0) continue;
            var coordinates = CalculateSeriesCoordinates(series, data, chartAreaWidth, chartAreaHeight, baselineY);
            Color color = ResolveSeriesColor(series);
            Color labelColor = RawSeriesStrokeColor(series);
            float offset = GetLabelVerticalOffset(series);
            float thickness = Orientation == ChartOrientation.Horizontal
                ? Math.Min((chartAreaHeight / count) * BarWidthRatio, 50)
                : Math.Min((chartAreaWidth / count) * BarWidthRatio, 50);
            for (int i = 0; i < count; i++)
            {
                Point p = coordinates[i];
                string text = FormatPointValue(series.DataPoints[i]);
                bool below = ChartValueScale.IsBelowOrigin(series.DataPoints[i].Value, _effectiveMin, _effectiveMax);
                if (Orientation == ChartOrientation.Horizontal)
                    DrawBarValueLabel(canvas, origin, p.Y - thickness / 2, p.X - origin, thickness, text, DimMark(series, i, color), below, offset, p.X, p.Y, true, DimMark(series, i, labelColor));
                else
                    DrawBarValueLabel(canvas, p.X - thickness / 2, p.Y, thickness, origin - p.Y, text, DimMark(series, i, color), below, offset, p.X, p.Y, true, DimMark(series, i, labelColor));
            }
        }
    }

    protected override float GetLabelVerticalOffset(ChartSeries series) => 7 + (Orientation == ChartOrientation.Horizontal ? DepthShift.Dx : DepthShift.Dy);

    internal override bool SupportsDepth3d => true;
    protected override Color GetDefaultSeriesColor() => BarColor;
}
