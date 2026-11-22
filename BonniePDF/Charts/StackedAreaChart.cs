// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Renders multiple nonnegative series as cumulative stacked area bands.
/// </summary>
public class StackedAreaChart : CartesianChartBase
{
    /// <summary>Stacking mode: <c>absolute</c> (default) or 100% normalized <c>percent</c>.</summary>
    public string Mode { get; set; } = "absolute";
    /// <summary>Curve interpolation mode for stacked area boundaries.</summary>
    public string Curve { get; set; } = "linear";
    /// <summary>Default fill opacity for stacked area bands (0 to 1).</summary>
    public float DefaultFillOpacity { get; set; } = 0.75f;
    /// <summary>Default stroke width for upper boundary lines in points.</summary>
    public float DefaultLineWidth { get; set; } = 1.5f;

    internal override bool UsesStyledGeometry => true;
    protected override bool DefaultAutoScale => true;
    protected override bool SupportsInlineLegend => true;

    public StackedAreaChart()
    {
        AutoLayout = true;
        ShowValueLabels = false;
    }

    public StackedAreaChart(ChartData data) : this()
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    private static int CategoryCount(ChartData data)
    {
        int max = 0;
        foreach (var s in data.Series) if (s.DataPoints.Count > max) max = s.DataPoints.Count;
        return max;
    }

    private List<decimal[]> ComputeCumulative(ChartData data)
    {
        int count = CategoryCount(data);
        decimal[] totals = new decimal[count];
        foreach (var s in data.Series)
        {
            for (int i = 0; i < count; i++)
            {
                decimal v = i < s.DataPoints.Count ? s.DataPoints[i].Value : 0m;
                if (v < 0) throw new ArgumentOutOfRangeException(nameof(data), "StackedAreaChart requires nonnegative values.");
                totals[i] += v;
            }
        }
        List<decimal[]> layers = new(data.Series.Count);
        decimal[] running = new decimal[count];
        foreach (var s in data.Series)
        {
            decimal[] layer = new decimal[count];
            for (int i = 0; i < count; i++)
            {
                decimal raw = i < s.DataPoints.Count ? s.DataPoints[i].Value : 0m;
                decimal val = Mode is "percent" ? (totals[i] > 0 ? (raw / totals[i]) * 100m : 0m) : raw;
                running[i] += val;
                layer[i] = running[i];
            }
            layers.Add(layer);
        }
        return layers;
    }

    protected override (double Min, double Max)? GetValueDataRange(ChartData data)
    {
        int count = CategoryCount(data);
        if (count == 0) return null;
        if (Mode is "percent") return (0, 100);
        var layers = ComputeCumulative(data);
        double max = 0;
        foreach (decimal v in layers[^1]) if ((double)v > max) max = (double)v;
        return (0, max);
    }

    protected override void ResolveValueScale(ChartData data)
    {
        if (Mode is "percent")
        {
            _effectiveMin = 0;
            _effectiveMax = 100;
            _effectiveStepCount = data.YAxis.StepCount > 0 ? data.YAxis.StepCount : 5;
            _effectiveTickFormat = data.YAxis.ValueFormat ?? "N0";
            return;
        }
        base.ResolveValueScale(data);
    }

    private List<Point> ToPoints(decimal[] values, float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        int n = values.Length;
        bool horizontal = Orientation == ChartOrientation.Horizontal;
        List<Point> pts = new(n);
        float slot = (horizontal ? chartAreaHeight : chartAreaWidth) / n;
        for (int i = 0; i < n; i++)
        {
            float f = (float)ChartValueScale.Fraction(values[i], _effectiveMin, _effectiveMax);
            if (horizontal) pts.Add(new Point(_plotLeft + f * chartAreaWidth, topY + i * slot + slot / 2f));
            else pts.Add(new Point(_plotLeft + i * slot + slot / 2f, baselineY - f * chartAreaHeight));
        }
        return pts;
    }

    protected override void DrawAllSeries(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        int count = CategoryCount(data);
        if (count < 2) return;
        var layers = ComputeCumulative(data);
        bool horizontal = Orientation == ChartOrientation.Horizontal;
        var zeroPoints = ToPoints(new decimal[count], chartAreaWidth, chartAreaHeight, baselineY, topY);
        var prevCmds = ChartCurves.BuildCurveCommands(zeroPoints, Curve, horizontal, moveToFirst: true);

        for (int sIdx = 0; sIdx < data.Series.Count; sIdx++)
        {
            ChartSeries series = data.Series[sIdx];
            var upperPoints = ToPoints(layers[sIdx], chartAreaWidth, chartAreaHeight, baselineY, topY);
            var upperCmds = ChartCurves.BuildCurveCommands(upperPoints, Curve, horizontal, moveToFirst: true);
            var lowerReversed = ChartCurves.ReverseCurveCommands(prevCmds);
            List<ChartPathCommand> bandCmds = new(upperCmds.Count + lowerReversed.Count + 1);
            bandCmds.AddRange(upperCmds);
            bandCmds.AddRange(lowerReversed);
            bandCmds.Add(new("Z", []));

            Color color = ResolveSeriesColor(series);
            float opacity = series.Style is not null && series.Style.FillOpacity != 0.3f ? series.Style.FillOpacity : DefaultFillOpacity;
            float lineWidth = series.Style is not null && series.Style.LineWidth != 2f ? series.Style.LineWidth : DefaultLineWidth;

            ChartVector.Fill(canvas, new ChartPathPlan(bandCmds, color.WithAlpha(opacity), null));
            if (lineWidth > 0)
                DrawSeriesLine(canvas, upperPoints, color, lineWidth, Curve, ResolveSeriesDashPattern(series, sIdx));
            prevCmds = upperCmds;
        }
    }

    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series, IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY) { }
}
