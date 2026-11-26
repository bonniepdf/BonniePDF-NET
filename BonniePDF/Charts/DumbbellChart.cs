// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>Two or more values per category as dots joined by a line: before and after, plan and actual. Horizontal by default.</summary>
public class DumbbellChart : CartesianChartBase
{
    /// <summary>Order of the categories: <c>none</c>, <c>first</c> (by the first series), <c>last</c>, or <c>difference</c> (largest gap first). Values sort descending.</summary>
    public string SortBy { get; set; } = "none";
    /// <summary>Width of the connector in points.</summary>
    public float ConnectorWidth { get; set; } = 3;
    /// <summary>Radius of the dots in points.</summary>
    public float DotRadius { get; set; } = 5;
    /// <summary>Colour of the connectors. Unset uses the theme's gridline colour.</summary>
    public Color? ConnectorColor { get; set; }

    /// <summary>Creates an empty dumbbell chart.</summary>
    public DumbbellChart() { Orientation = ChartOrientation.Horizontal; }

    /// <summary>Creates a dumbbell chart for <paramref name="data"/>.</summary>
    public DumbbellChart(ChartData data) : base(data) { Orientation = ChartOrientation.Horizontal; }

    internal override bool SupportsLogAxis => true;

    /// <inheritdoc />
    protected override bool DefaultAutoScale => true;

    /// <inheritdoc />
    protected override bool DefaultIncludeZero => false;

    /// <inheritdoc />
    protected override (double Min, double Max)? GetValueDataRange(ChartData data)
    {
        var range = base.GetValueDataRange(data);
        if (range is not { } r) return null;
        double pad = (r.Max - r.Min) * 0.08;
        return (r.Min - pad, r.Max + pad);
    }

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, ChartData data)
    {
        if (string.Equals(SortBy, "none", StringComparison.Ordinal) || data.Series.Count == 0)
        {
            base.Draw(canvas, data);
            return;
        }
        int n = data.Series[0].DataPoints.Count;
        double Key(int i)
        {
            List<double> values = data.Series.Select(s => i < s.DataPoints.Count ? (double)s.DataPoints[i].Value : 0.0).ToList();
            if (string.Equals(SortBy, "first", StringComparison.Ordinal)) return values[0];
            if (string.Equals(SortBy, "last", StringComparison.Ordinal)) return values[^1];
            return values.Max() - values.Min();
        }
        List<int> order = Enumerable.Range(0, n).OrderByDescending(Key).ThenBy(i => i).ToList();
        ChartData sorted = new() { Title = data.Title, Subtitle = data.Subtitle, XAxis = data.XAxis, YAxis = data.YAxis };
        foreach (ChartSeries src in data.Series)
        {
            ChartSeries s = new() { Name = src.Name, Style = src.Style };
            if (src.HasColor) s.Color = src.Color;
            foreach (int i in order) if (i < src.DataPoints.Count) s.DataPoints.Add(src.DataPoints[i]);
            sorted.Series.Add(s);
        }
        base.Draw(canvas, sorted);
    }

    /// <inheritdoc />
    protected override void DrawAllSeries(ICanvas canvas, ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        bool vertical = Orientation != ChartOrientation.Horizontal;
        int categories = data.Series.Max(s => s.DataPoints.Count);
        double slot = (vertical ? chartAreaWidth : chartAreaHeight) / (double)Math.Max(1, categories);
        double axisStart = vertical ? _plotLeft : baselineY - chartAreaHeight;
        double H(decimal v)
        {
            double f = (double)Fraction(v);
            return vertical ? baselineY - f * chartAreaHeight : _plotLeft + f * chartAreaWidth;
        }
        Point At(double c, double h) => vertical ? new Point((float)c, (float)h) : new Point((float)h, (float)c);
        Color bg = EffectiveBackgroundColor ?? Color.White;
        Color connector = ConnectorColor ?? Appearance.GridLineColor;
        for (int i = 0; i < categories; i++)
        {
            List<decimal> values = data.Series.Where(s => i < s.DataPoints.Count).Select(s => s.DataPoints[i].Value).ToList();
            if (values.Count == 0) continue;
            double c = axisStart + slot * (i + 0.5);
            Point a = At(c, H(values.Min())), b = At(c, H(values.Max()));
            canvas.DrawLine(a.X, a.Y, b.X, b.Y, connector, ConnectorWidth);
        }
        foreach (ChartSeries series in data.Series)
        {
            Color color = ResolveSeriesColor(series);
            for (int i = 0; i < series.DataPoints.Count; i++)
            {
                Point pt = At(axisStart + slot * (i + 0.5), H(series.DataPoints[i].Value));
                ChartMarkers.Draw(canvas, pt.X, pt.Y, DotRadius, color, "circle", "solid", 1.5f, bg, 2f);
            }
        }
    }

    /// <inheritdoc />
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series, IList<Point> coordinates, float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        // Everything is drawn by DrawAllSeries, which needs every series of a category at once.
    }

    /// <inheritdoc />
    protected override void DrawValueLabels(ICanvas canvas, ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (!ShowValueLabels) return;
        bool vertical = Orientation != ChartOrientation.Horizontal;
        int categories = data.Series.Max(s => s.DataPoints.Count);
        double slot = (vertical ? chartAreaWidth : chartAreaHeight) / (double)Math.Max(1, categories);
        double axisStart = vertical ? _plotLeft : baselineY - chartAreaHeight;
        double H(decimal v)
        {
            double f = (double)Fraction(v);
            return vertical ? baselineY - f * chartAreaHeight : _plotLeft + f * chartAreaWidth;
        }
        string family = EffectiveFontFamily;
        float size = LabelFontSize;
        float gap = DotRadius + 5;
        for (int i = 0; i < categories; i++)
        {
            var entries = data.Series.Where(s => i < s.DataPoints.Count).Select(s => (S: s, P: s.DataPoints[i])).ToList();
            if (entries.Count == 0) continue;
            var lo = entries.Aggregate((a, b) => b.P.Value < a.P.Value ? b : a);
            var hi = entries.Aggregate((a, b) => b.P.Value > a.P.Value ? b : a);
            double c = axisStart + slot * (i + 0.5);
            void Place((ChartSeries S, ChartDataPoint P) e, int outward)
            {
                string text = FormatPointValue(e.P);
                Color color = RawSeriesStrokeColor(e.S);
                double pos = H(e.P.Value);
                if (vertical) DrawPlacedLabel(canvas, (float)c, (float)(outward > 0 ? pos - gap - size : pos + gap), text, color, size, family, TextAlignment.Center);
                else DrawPlacedLabel(canvas, (float)(outward > 0 ? pos + gap : pos - gap), (float)(c - size / 2), text, color, size, family, outward > 0 ? TextAlignment.Left : TextAlignment.Right);
            }
            Place(lo, -1);
            if (!ReferenceEquals(hi.S, lo.S) || !ReferenceEquals(hi.P, lo.P)) Place(hi, 1);
        }
    }
}
