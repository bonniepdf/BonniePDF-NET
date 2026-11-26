// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>A band between a low and a high value per category: confidence intervals, forecasts and min-max envelopes. Points carry <c>Low</c> and <c>High</c>.</summary>
public class RangeAreaChart : CartesianChartBase
{
    /// <summary>Opacity of the band fill, <c>0..1</c>.</summary>
    public float FillOpacity { get; set; } = 0.25f;
    /// <summary>Draw a line along each edge of the band.</summary>
    public bool ShowBoundaries { get; set; } = true;
    /// <summary>Width of the boundary lines in points.</summary>
    public float LineWidth { get; set; } = 1.5f;

    /// <summary>Creates an empty range area chart.</summary>
    public RangeAreaChart() { ShowValueLabels = false; }

    /// <summary>Creates a range area chart for <paramref name="data"/>.</summary>
    public RangeAreaChart(ChartData data) : base(data) { ShowValueLabels = false; }

    internal override bool SupportsLogAxis => true;

    /// <inheritdoc />
    protected override bool DefaultAutoScale => true;

    /// <inheritdoc />
    protected override bool DefaultIncludeZero => false;

    private static (double Low, double High) Bounds(ChartDataPoint point)
    {
        if (point.Low is not { } low || point.High is not { } high) throw new InvalidOperationException("Range area points need low and high.");
        if (!(low <= high)) throw new InvalidOperationException("Range area points must satisfy low <= high.");
        return ((double)low, (double)high);
    }

    /// <inheritdoc />
    protected override (double Min, double Max)? GetValueDataRange(ChartData data)
    {
        double min = double.PositiveInfinity, max = double.NegativeInfinity;
        foreach (ChartSeries s in data.Series)
            foreach (ChartDataPoint p in s.DataPoints)
            {
                var b = Bounds(p);
                min = Math.Min(min, b.Low);
                max = Math.Max(max, b.High);
            }
        return min <= max ? (min, max) : null;
    }

    /// <inheritdoc />
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series, IList<Point> coordinates, float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (!(FillOpacity >= 0 && FillOpacity <= 1)) throw new InvalidOperationException("Range area fillOpacity must be from 0 to 1.");
        bool vertical = Orientation != ChartOrientation.Horizontal;
        int count = series.DataPoints.Count;
        if (count < 2) return;
        int categories = data.Series.Max(s => s.DataPoints.Count);
        double slot = (vertical ? chartAreaWidth : chartAreaHeight) / (double)Math.Max(1, categories);
        double axisStart = vertical ? _plotLeft : baselineY - chartAreaHeight;
        double H(double v)
        {
            double f = (double)Fraction((decimal)v);
            return vertical ? baselineY - f * chartAreaHeight : _plotLeft + f * chartAreaWidth;
        }
        (double, double) At(int i, double value)
        {
            double c = axisStart + slot * (i + 0.5);
            return vertical ? (c, H(value)) : (H(value), c);
        }
        Color color = ResolveSeriesColor(series);
        List<(double X, double Y)> highs = series.DataPoints.Select((p, i) => At(i, Bounds(p).High)).ToList();
        List<(double X, double Y)> lows = series.DataPoints.Select((p, i) => At(i, Bounds(p).Low)).ToList();
        List<ChartPathCommand> Line(List<(double X, double Y)> pts) => pts.Select((p, i) => new ChartPathCommand(i == 0 ? "M" : "L", [p.X, p.Y])).ToList();
        List<ChartPathCommand> band = [.. Line(highs), .. Enumerable.Reverse(lows).Select(p => new ChartPathCommand("L", [p.X, p.Y])), new ChartPathCommand("Z", [])];
        if (FillOpacity > 0) ChartVector.Fill(canvas, new ChartPathPlan(band, color.WithAlpha(FillOpacity), null));
        if (ShowBoundaries)
        {
            ChartVector.Stroke(canvas, new ChartStrokePlan(Line(highs), color, LineWidth, "butt", "round", 10));
            ChartVector.Stroke(canvas, new ChartStrokePlan(Line(lows), color, LineWidth, "butt", "round", 10));
        }
    }
}
