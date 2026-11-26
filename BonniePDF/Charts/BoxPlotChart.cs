// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>A box plot: the distribution of each category as a box, whiskers and outliers. Points carry <c>Samples</c> or <c>Stats</c>.</summary>
public class BoxPlotChart : CartesianChartBase
{
    /// <summary><c>tukey</c> (default): the furthest samples within 1.5 x IQR of the box. <c>minMax</c>, or <c>percentile</c>: the 5th and 95th percentiles.</summary>
    public string Whiskers { get; set; } = "tukey";
    /// <summary>Draw each mean as a diamond.</summary>
    public bool ShowMean { get; set; }
    /// <summary>The fraction of a category slot that the boxes of its series fill.</summary>
    public float BoxWidthRatio { get; set; } = 0.5f;
    /// <summary>The label above each box when value labels are shown; <c>{min}</c>, <c>{q1}</c>, <c>{median}</c>, <c>{q3}</c>, <c>{max}</c> and <c>{mean}</c> are replaced.</summary>
    public string ValueLabelTemplate { get; set; } = "{median}";

    /// <summary>Creates an empty box plot.</summary>
    public BoxPlotChart() { ShowValueLabels = false; }

    /// <summary>Creates a box plot for <paramref name="data"/>.</summary>
    public BoxPlotChart(ChartData data) : base(data) { ShowValueLabels = false; }

    internal override bool SupportsLogAxis => true;

    /// <inheritdoc />
    protected override bool DefaultAutoScale => true;

    /// <inheritdoc />
    protected override bool DefaultIncludeZero => false;

    private sealed record Summary(double Low, double Q1, double Median, double Q3, double High, double? Mean, List<double> Outliers);

    /// <summary>Linear-interpolation quantile of sorted values (Hyndman and Fan type 7).</summary>
    internal static double QuantileType7(IReadOnlyList<double> sorted, double p)
    {
        int n = sorted.Count;
        double h = (n - 1) * p;
        int f = (int)Math.Floor(h);
        return f + 1 < n ? sorted[f] + (h - f) * (sorted[f + 1] - sorted[f]) : sorted[f];
    }

    private Summary Summarize(ChartDataPoint point)
    {
        if (point.Samples is { Count: > 0 } samples)
        {
            List<double> sorted = samples.Select(v => (double)v).Order().ToList();
            double q1 = QuantileType7(sorted, 0.25), median = QuantileType7(sorted, 0.5), q3 = QuantileType7(sorted, 0.75);
            double lowLimit = sorted[0], highLimit = sorted[^1];
            if (string.Equals(Whiskers, "tukey", StringComparison.Ordinal)) { lowLimit = q1 - 1.5 * (q3 - q1); highLimit = q3 + 1.5 * (q3 - q1); }
            else if (string.Equals(Whiskers, "percentile", StringComparison.Ordinal)) { lowLimit = QuantileType7(sorted, 0.05); highLimit = QuantileType7(sorted, 0.95); }
            List<double> inside = sorted.Where(v => v >= lowLimit && v <= highLimit).ToList();
            double low = string.Equals(Whiskers, "percentile", StringComparison.Ordinal) ? lowLimit : inside[0];
            double high = string.Equals(Whiskers, "percentile", StringComparison.Ordinal) ? highLimit : inside[^1];
            return new Summary(low, q1, median, q3, high, sorted.Sum() / sorted.Count,
                string.Equals(Whiskers, "minMax", StringComparison.Ordinal) ? [] : sorted.Where(v => v < lowLimit || v > highLimit).ToList());
        }
        if (point.Stats is { } s)
        {
            if (!(s.Min <= s.Q1 && s.Q1 <= s.Median && s.Median <= s.Q3 && s.Q3 <= s.Max)) throw new InvalidOperationException("Box plot stats must satisfy min <= q1 <= median <= q3 <= max.");
            return new Summary((double)s.Min, (double)s.Q1, (double)s.Median, (double)s.Q3, (double)s.Max, s.Mean is { } m ? (double)m : null, (s.Outliers ?? []).Select(v => (double)v).ToList());
        }
        throw new InvalidOperationException("Every box plot point needs samples or stats.");
    }

    /// <inheritdoc />
    protected override (double Min, double Max)? GetValueDataRange(ChartData data)
    {
        double min = double.PositiveInfinity, max = double.NegativeInfinity;
        foreach (ChartSeries series in data.Series)
        {
            foreach (ChartDataPoint p in series.DataPoints)
            {
                Summary s = Summarize(p);
                foreach (double v in s.Outliers.Append(s.Low).Append(s.High)) { min = Math.Min(min, v); max = Math.Max(max, v); }
            }
        }
        return min <= max ? (min, max) : null;
    }

    private (double Centre, double Thickness) SlotOf(ChartData data, ChartSeries series, int index, float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        bool vertical = Orientation != ChartOrientation.Horizontal;
        int categories = data.Series.Max(s => s.DataPoints.Count);
        double axisStart = vertical ? _plotLeft : baselineY - chartAreaHeight;
        double slot = (vertical ? chartAreaWidth : chartAreaHeight) / (double)Math.Max(1, categories);
        double thickness = slot * BoxWidthRatio / data.Series.Count;
        int s = data.Series.IndexOf(series);
        return (axisStart + index * slot + (slot - slot * BoxWidthRatio) / 2 + s * thickness + thickness / 2, thickness);
    }

    private double HeightOf(double value, float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        double f = (double)Fraction((decimal)value);
        return Orientation == ChartOrientation.Horizontal ? _plotLeft + f * chartAreaWidth : baselineY - f * chartAreaHeight;
    }

    /// <inheritdoc />
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series, IList<Point> coordinates, float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (!(BoxWidthRatio > 0 && BoxWidthRatio <= 1)) throw new InvalidOperationException("boxWidthRatio must be above 0 and at most 1.");
        bool vertical = Orientation != ChartOrientation.Horizontal;
        Point At(double c, double h) => vertical ? new Point((float)c, (float)h) : new Point((float)h, (float)c);
        Color color = ResolveSeriesColor(series);
        Color bg = EffectiveBackgroundColor ?? Color.White;
        for (int i = 0; i < series.DataPoints.Count; i++)
        {
            Summary s = Summarize(series.DataPoints[i]);
            (double centre, double thickness) = SlotOf(data, series, i, chartAreaWidth, chartAreaHeight, baselineY);
            double H(double v) => HeightOf(v, chartAreaWidth, chartAreaHeight, baselineY);
            void Line(Point a, Point b, float width) => canvas.DrawLine(a.X, a.Y, b.X, b.Y, color, width);
            Line(At(centre, H(s.Low)), At(centre, H(s.Q1)), 1.5f);
            Line(At(centre, H(s.Q3)), At(centre, H(s.High)), 1.5f);
            Line(At(centre - thickness / 4, H(s.Low)), At(centre + thickness / 4, H(s.Low)), 1.5f);
            Line(At(centre - thickness / 4, H(s.High)), At(centre + thickness / 4, H(s.High)), 1.5f);
            Point[] corners = [At(centre - thickness / 2, H(s.Q3)), At(centre + thickness / 2, H(s.Q3)), At(centre + thickness / 2, H(s.Q1)), At(centre - thickness / 2, H(s.Q1))];
            List<ChartPathCommand> commands = corners.Select((p, k) => new ChartPathCommand(k == 0 ? "M" : "L", [p.X, p.Y])).ToList();
            commands.Add(new ChartPathCommand("Z", []));
            ChartVector.Fill(canvas, new ChartPathPlan(commands, color.WithAlpha(0.25f), null));
            ChartVector.Stroke(canvas, new ChartStrokePlan(commands, color, 1.5, "butt", "miter"));
            Line(At(centre - thickness / 2, H(s.Median)), At(centre + thickness / 2, H(s.Median)), 2.5f);
            if (ShowMean && s.Mean is { } mean)
            {
                Point m = At(centre, H(mean));
                ChartMarkers.Draw(canvas, m.X, m.Y, 4, color, "diamond", "solid", 1.5f, bg, 1.5f);
            }
            foreach (double v in s.Outliers)
            {
                Point o = At(centre, H(v));
                ChartMarkers.Draw(canvas, o.X, o.Y, 3, color, "circle", "hollow", 0, bg, 1.25f);
            }
        }
    }

    /// <inheritdoc />
    protected override void DrawValueLabels(ICanvas canvas, ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (!ShowValueLabels) return;
        bool vertical = Orientation != ChartOrientation.Horizontal;
        string family = EffectiveFontFamily;
        foreach (ChartSeries series in data.Series)
        {
            for (int i = 0; i < series.DataPoints.Count; i++)
            {
                Summary s = Summarize(series.DataPoints[i]);
                (double centre, _) = SlotOf(data, series, i, chartAreaWidth, chartAreaHeight, baselineY);
                string text = ChartTemplate.Apply(ValueLabelTemplate, new Dictionary<string, object> { ["min"] = s.Low, ["q1"] = s.Q1, ["median"] = s.Median, ["q3"] = s.Q3, ["max"] = s.High, ["mean"] = s.Mean ?? 0.0 });
                double h = HeightOf(s.High, chartAreaWidth, chartAreaHeight, baselineY);
                if (vertical) DrawPlacedLabel(canvas, (float)centre, (float)(h - 6 - LabelFontSize), text, Appearance.LegendTextColor, LabelFontSize, family, TextAlignment.Center);
                else DrawPlacedLabel(canvas, (float)(h + 6), (float)(centre - LabelFontSize / 2), text, Appearance.LegendTextColor, LabelFontSize, family, TextAlignment.Left);
            }
        }
    }
}
