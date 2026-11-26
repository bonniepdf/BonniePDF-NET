// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>A radar (spider) chart: one spoke per category, one closed shape per series.</summary>
public class RadarChart : ChartBase
{
    private const double Kappa = 4.0 / 3 * 0.41421356237309503;

    /// <summary><c>polygon</c> (default) or <c>circle</c> rings.</summary>
    public string GridShape { get; set; } = "polygon";
    /// <summary>Opacity of each series' fill, <c>0..1</c>. <c>0</c> draws outlines only.</summary>
    public float FillOpacity { get; set; } = 0.2f;
    /// <summary><c>linear</c> (default) or <c>smooth</c> (a closed Catmull-Rom curve).</summary>
    public string Curve { get; set; } = "linear";
    /// <summary>Draw a marker at each value.</summary>
    public bool ShowMarkers { get; set; } = true;
    /// <summary>Draw the value beside each marker.</summary>
    public bool ShowValueLabels { get; set; }
    /// <summary>Draw a legend when there is more than one series.</summary>
    public bool ShowLegend { get; set; } = true;
    /// <summary>Outline width in points.</summary>
    public float LineWidth { get; set; } = 2;

    internal override bool UsesStyledGeometry => true;

    /// <summary>Creates an empty radar chart.</summary>
    public RadarChart() { AutoLayout = true; }

    /// <summary>Creates a radar chart for <paramref name="data"/>.</summary>
    public RadarChart(ChartData data) : base(data) { AutoLayout = true; }

    private static double Clamp01(decimal v) => Math.Max(0, Math.Min(1, (double)v));

    /// <inheritdoc />
    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        if (data.Series.Count == 0) return;
        List<string> categories = [];
        foreach (ChartSeries s in data.Series) if (s.DataPoints.Count > categories.Count) categories = s.DataPoints.Select(p => p.Label).ToList();
        int n = categories.Count;
        if (n < 3) throw new InvalidOperationException("A radar chart needs at least three categories.");
        if (!(FillOpacity >= 0 && FillOpacity <= 1)) throw new InvalidOperationException("Radar fillOpacity must be from 0 to 1.");

        AxisOptions axis = data.YAxis;
        decimal min = axis.MinValue, max = axis.MaxValue;
        int steps = axis.StepCount > 0 ? axis.StepCount : 5;
        string tickFormat = axis.ValueFormat ?? "N0";
        if (axis.AutoScale || !axis.HasMaxValue)
        {
            List<decimal> values = data.Series.SelectMany(s => s.DataPoints.Select(p => p.Value)).ToList();
            NiceScaleResult nice = ChartValueScale.ComputeNiceScale((double)values.Min(), (double)values.Max(), axis.StepCount, axis.IncludeZero ?? true, axis.Headroom ?? 0,
                axis.HasMinValue ? (double)axis.MinValue : null, axis.HasMaxValue ? (double)axis.MaxValue : null);
            min = nice.MinValue; max = nice.MaxValue; steps = nice.StepCount; tickFormat = axis.ValueFormat ?? nice.AutoFormat;
        }

        string font = ResolveFontFamily();
        const float labelSize = 10;
        float widest = 0;
        foreach (string c in categories) widest = Math.Max(widest, canvas.MeasureText(c, labelSize, font).Width);
        float top = (data.Title.Length > 0 ? 56 : 18) + (data.Subtitle.Length > 0 ? 18 : 0);
        float legendBand = ShowLegend && data.Series.Count > 1 ? 28 : 0;
        float bottom = Height - 12 - legendBand - (Caption.Length > 0 ? 16 : 0);
        double cx = Width / 2.0;
        double cy = (top + bottom) / 2.0;
        double radius = Math.Max(20, Math.Min(Width / 2.0 - widest - 20, (bottom - top) / 2.0 - labelSize - 10));
        double Angle(int i) => -Math.PI / 2 + 2 * Math.PI * i / n;
        Point At(int i, double r) => new((float)(cx + r * Math.Cos(Angle(i))), (float)(cy + r * Math.Sin(Angle(i))));

        Color gridColor = Appearance.GridLineColor;
        for (int k = 1; k <= steps; k++)
        {
            double r = radius * k / steps;
            List<ChartPathCommand> commands = [];
            if (string.Equals(GridShape, "circle", StringComparison.Ordinal))
            {
                double c = Kappa * r;
                commands.AddRange([
                    new("M", [cx + r, cy]),
                    new("C", [cx + r, cy + c, cx + c, cy + r, cx, cy + r]),
                    new("C", [cx - c, cy + r, cx - r, cy + c, cx - r, cy]),
                    new("C", [cx - r, cy - c, cx - c, cy - r, cx, cy - r]),
                    new("C", [cx + c, cy - r, cx + r, cy - c, cx + r, cy]),
                    new("Z", []),
                ]);
            }
            else
            {
                for (int i = 0; i < n; i++) { Point p = At(i, r); commands.Add(new(i == 0 ? "M" : "L", [p.X, p.Y])); }
                commands.Add(new("Z", []));
            }
            ChartVector.Stroke(canvas, new ChartStrokePlan(commands, gridColor, 0.5));
        }
        for (int i = 0; i < n; i++) { Point p = At(i, radius); canvas.DrawLine((float)cx, (float)cy, p.X, p.Y, gridColor, 0.5f); }
        for (int k = 1; k <= steps; k++)
        {
            decimal v = min + (max - min) * k / steps;
            canvas.DrawText((float)(cx + 4), (float)(cy - radius * k / steps + 2), ChartNumberFormat.Format(v, tickFormat), Appearance.AxisColor, 9, font, TextAlignment.Left);
        }
        for (int i = 0; i < n; i++)
        {
            Point p = At(i, radius + 8);
            double cos = Math.Cos(Angle(i)), sin = Math.Sin(Angle(i));
            TextAlignment align = cos > 0.1 ? TextAlignment.Left : cos < -0.1 ? TextAlignment.Right : TextAlignment.Center;
            float y = sin < -0.1 ? p.Y - labelSize : sin > 0.1 ? p.Y : p.Y - labelSize / 2;
            canvas.DrawText(p.X, y, categories[i], Appearance.AxisColor, labelSize, font, align);
        }

        Color background = EffectiveBackgroundColor ?? Color.White;
        for (int s = 0; s < data.Series.Count; s++)
        {
            ChartSeries series = data.Series[s];
            Color color = series.HasColor ? series.Color : ChartCategoryColors.For(Appearance, null, s);
            Point[] pts = new Point[n];
            for (int i = 0; i < n; i++)
            {
                decimal v = i < series.DataPoints.Count ? series.DataPoints[i].Value : min;
                pts[i] = At(i, radius * Clamp01(ChartValueScale.Fraction(v, min, max)));
            }
            List<ChartPathCommand> commands = [new("M", [pts[0].X, pts[0].Y])];
            if (string.Equals(Curve, "smooth", StringComparison.Ordinal))
            {
                for (int i = 0; i < n; i++)
                {
                    Point p0 = pts[(i + n - 1) % n], p1 = pts[i], p2 = pts[(i + 1) % n], p3 = pts[(i + 2) % n];
                    commands.Add(new("C", [p1.X + (p2.X - p0.X) / 6.0, p1.Y + (p2.Y - p0.Y) / 6.0, p2.X - (p3.X - p1.X) / 6.0, p2.Y - (p3.Y - p1.Y) / 6.0, p2.X, p2.Y]));
                }
            }
            else for (int i = 1; i < n; i++) commands.Add(new("L", [pts[i].X, pts[i].Y]));
            commands.Add(new("Z", []));
            if (FillOpacity > 0) ChartVector.Fill(canvas, new ChartPathPlan(commands, color.WithAlpha(FillOpacity), null));
            ChartVector.Stroke(canvas, new ChartStrokePlan(commands, color, LineWidth, LineJoin: "round"));
            for (int i = 0; i < n; i++)
            {
                if (ShowMarkers) ChartMarkers.Draw(canvas, pts[i].X, pts[i].Y, 3.5f, color, "circle", "solid", 1.5f, background, LineWidth);
                if (ShowValueLabels && i < series.DataPoints.Count)
                {
                    Point o = At(i, radius * Clamp01(ChartValueScale.Fraction(series.DataPoints[i].Value, min, max)) + 12);
                    canvas.DrawText(o.X, o.Y - 4.5f, ChartNumberFormat.Format(series.DataPoints[i].Value, tickFormat), Appearance.LegendTextColor, 9, font, TextAlignment.Center);
                }
            }
        }

        if (legendBand > 0)
        {
            const float size = 10;
            float[] widths = data.Series.Select(s => canvas.MeasureText(s.Name, size, font).Width + 18).ToArray();
            float x = (Width - (widths.Sum() + 8 * (widths.Length - 1))) / 2;
            float y = Height - 12 - legendBand + 8;
            for (int i = 0; i < data.Series.Count; i++)
            {
                ChartSeries s = data.Series[i];
                Color color = s.HasColor ? s.Color : ChartCategoryColors.For(Appearance, null, i);
                canvas.FillRect(x, y + 1, 8, 8, color);
                canvas.DrawText(x + 14, y, s.Name, Appearance.LegendTextColor, size, font, TextAlignment.Left);
                x += widths[i] + 8;
            }
        }
    }
}
