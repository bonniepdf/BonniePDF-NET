// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// A histogram: raw observations (the <c>Value</c> of each point of each series) grouped into bins and drawn as touching bars.
/// Several series draw as translucent overlapping distributions.
/// </summary>
public class HistogramChart : BarChart
{
    private int _outOfRange;

    /// <summary>Explicit ascending bin edges (at least two). Values outside them are dropped and counted in <see cref="OutOfRangeCount"/>.</summary>
    public IList<decimal>? Bins { get; set; }
    /// <summary>Width of every bin.</summary>
    public decimal? BinWidth { get; set; }
    /// <summary>Number of equal bins between the data minimum and maximum.</summary>
    public int? BinCount { get; set; }
    /// <summary><c>count</c> (default), <c>percent</c> of the observations, or <c>density</c> (count over total times bin width).</summary>
    public string Normalize { get; set; } = "count";

    /// <summary>Observations the last draw dropped because they fell outside explicit <see cref="Bins"/>.</summary>
    public int OutOfRangeCount => _outOfRange;

    /// <summary>Draw each series' cumulative share, from 0 to 100 %, as a line over the bars with its own axis on the right.</summary>
    public bool CumulativeLine { get; set; }

    private List<double[]> _cumulative = [];

    /// <inheritdoc />
    protected override float GetExtraRightMargin(ICanvas canvas, ChartData data) => CumulativeLine ? 44 : 0;

    /// <inheritdoc />
    protected override void DrawAfterSeries(ICanvas canvas, ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (!CumulativeLine) return;
        string family = EffectiveFontFamily;
        for (int p = 0; p <= 100; p += 25)
            canvas.DrawText(_plotLeft + chartAreaWidth + 8, baselineY - p / 100f * chartAreaHeight - 5, p.ToString(System.Globalization.CultureInfo.InvariantCulture) + "%", Appearance.AxisColor, 10, family, TextAlignment.Left);
        for (int s = 0; s < data.Series.Count; s++)
        {
            Color color = ResolveSeriesColor(data.Series[s]).WithAlpha(1f);
            double[] values = s < _cumulative.Count ? _cumulative[s] : [];
            float slot = chartAreaWidth / Math.Max(1, values.Length);
            List<Point> pts = values.Select((v, i) => new Point(_plotLeft + slot * (i + 0.5f), baselineY - (float)(v / 100) * chartAreaHeight)).ToList();
            for (int i = 1; i < pts.Count; i++) canvas.DrawLine(pts[i - 1].X, pts[i - 1].Y, pts[i].X, pts[i].Y, color, 2);
            foreach (Point p in pts) canvas.FillCircle(p.X, p.Y, 3, color);
        }
    }

    internal override bool SupportsAnnotations => true;

    /// <summary>Creates an empty histogram.</summary>
    public HistogramChart() { Init(); }

    /// <summary>Creates a histogram for <paramref name="data"/>.</summary>
    public HistogramChart(ChartData data) : base(data) { Init(); }

    private void Init()
    {
        BarWidthRatio = 1;
        ShowValueLabels = false;
    }

    /// <inheritdoc />
    protected override bool DefaultAutoScale => true;

    private sealed record Bin(double Lower, double Upper, int[] Counts);

    private List<Bin> ComputeBins(ChartData data)
    {
        List<double> all = data.Series.SelectMany(s => s.DataPoints.Select(p => (double)p.Value)).ToList();
        _outOfRange = 0;
        if (all.Count == 0) return [];
        List<double> edges;
        if (Bins is not null)
        {
            if (Bins.Count < 2) throw new InvalidOperationException("Histogram bins need at least two edges.");
            for (int i = 1; i < Bins.Count; i++) if (!(Bins[i] > Bins[i - 1])) throw new InvalidOperationException("Histogram bins must be strictly ascending.");
            edges = Bins.Select(b => (double)b).ToList();
        }
        else
        {
            double lo = all.Min(), hi = all.Max();
            if (BinWidth is { } bw)
            {
                double w = (double)bw;
                if (!(w > 0)) throw new InvalidOperationException("Histogram binWidth must be positive.");
                double start = Math.Floor(lo / w) * w;
                edges = [start];
                while (edges[^1] <= hi && edges.Count < 10001) edges.Add(start + edges.Count * w);
            }
            else if (BinCount is { } count)
            {
                if (count < 1) throw new InvalidOperationException("Histogram binCount must be a positive integer.");
                double span = hi > lo ? hi - lo : 1;
                edges = Enumerable.Range(0, count + 1).Select(i => lo + span * i / count).ToList();
            }
            else
            {
                int k = (int)Math.Ceiling(Math.Log2(all.Count)) + 1;
                NiceScaleResult nice = ChartValueScale.ComputeNiceScale(lo, hi, k, false, 0);
                edges = Enumerable.Range(0, nice.StepCount + 1).Select(i => (double)nice.MinValue + (double)nice.StepSize * i).ToList();
            }
        }
        int nb = edges.Count - 1;
        double width = (edges[nb] - edges[0]) / nb;
        bool uniform = Bins is null;
        List<Bin> bins = Enumerable.Range(0, nb).Select(i => new Bin(edges[i], edges[i + 1], new int[data.Series.Count])).ToList();
        for (int s = 0; s < data.Series.Count; s++)
        {
            foreach (ChartDataPoint p in data.Series[s].DataPoints)
            {
                double v = (double)p.Value;
                if (v < edges[0] || v > edges[nb]) { _outOfRange++; continue; }
                int index;
                if (uniform) index = Math.Min(nb - 1, Math.Max(0, (int)Math.Floor((v - edges[0]) / width + 1e-9)));
                else
                {
                    int lo2 = 0, hi2 = nb;
                    while (lo2 < hi2)
                    {
                        int mid = (lo2 + hi2) >> 1;
                        if (edges[mid + 1] <= v) lo2 = mid + 1; else hi2 = mid;
                    }
                    index = Math.Min(nb - 1, lo2);
                }
                bins[index].Counts[s]++;
            }
        }
        return bins;
    }

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, ChartData data)
    {
        List<Bin> bins = ComputeBins(data);
        double[] totals = data.Series.Select((_, s) => (double)bins.Sum(b => b.Counts[s])).ToArray();
        _cumulative = data.Series.Select((_, s) =>
        {
            double run = 0;
            return bins.Select(b => { run += b.Counts[s]; return totals[s] > 0 ? run / totals[s] * 100 : 0.0; }).ToArray();
        }).ToList();
        bool multiple = data.Series.Count > 1;
        ChartData output = new()
        {
            Title = data.Title,
            Subtitle = data.Subtitle,
            XAxis = data.XAxis,
            YAxis = data.YAxis,
        };
        for (int s = 0; s < data.Series.Count; s++)
        {
            ChartSeries src = data.Series[s];
            ChartSeries series = new() { Name = src.Name, Style = src.Style };
            if (src.HasColor) series.Color = multiple ? src.Color.WithAlpha(0.5f) : src.Color;
            foreach (Bin b in bins)
            {
                double count = b.Counts[s];
                double value = string.Equals(Normalize, "percent", StringComparison.Ordinal) ? (totals[s] > 0 ? count / totals[s] * 100 : 0)
                    : string.Equals(Normalize, "density", StringComparison.Ordinal) ? (totals[s] > 0 ? count / (totals[s] * (b.Upper - b.Lower)) : 0) : count;
                series.DataPoints.Add(new ChartDataPoint
                {
                    Label = ChartNumberFormat.Format(ChartTemplate.ToDecimal(b.Lower), "G") + "–" + ChartNumberFormat.Format(ChartTemplate.ToDecimal(b.Upper), "G"),
                    Value = ChartTemplate.ToDecimal(value),
                });
            }
            output.Series.Add(series);
        }
        base.Draw(canvas, output);
    }
}
