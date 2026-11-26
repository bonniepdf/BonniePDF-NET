// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>A Nightingale rose (variable-radius polar area) chart: equal-angle petals whose radius encodes magnitude.</summary>
public class NightingaleRoseChart : ChartBase
{
    public float InnerRadiusRatio { get; set; } = 0.12f;
    /// <summary>When true (default), radius scales by sqrt(value/max) so petal area is proportional to value.</summary>
    public bool AreaScale { get; set; } = true;
    public bool ShowGridCircles { get; set; } = true;
    public bool ShowLabels { get; set; } = true;
    public bool ShowValueLabels { get; set; }
    public string ValueFormat { get; set; } = "G";

    internal override bool UsesStyledGeometry => true;

    public NightingaleRoseChart() { AutoLayout = true; }
    public NightingaleRoseChart(ChartData data) : base(data) { AutoLayout = true; }

    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        IList<ChartDataPoint> points = data.Series.Count > 0 ? data.Series[0].DataPoints : [];
        int n = points.Count;
        if (n == 0) return;
        if (points.Any(p => p.Value < 0)) throw new InvalidOperationException("NightingaleRose values must be finite and nonnegative.");
        double maxVal = (double)points.Max(p => p.Value);
        if (!(maxVal > 0)) return;
        string font = ResolveFontFamily();
        Color bg = EffectiveBackgroundColor ?? Color.White;
        double topEdge = (data.Title.Length > 0 ? 56 : 16) + (data.Subtitle.Length > 0 ? 18 : 0);
        double bottomEdge = Height - 16 - (Caption.Length > 0 ? 16 : 0);
        double labelMargin = ShowLabels ? 36 : 12;
        double outerR = Math.Max(24, Math.Min(Width / 2.0 - labelMargin, (bottomEdge - topEdge) / 2 - labelMargin));
        double innerR = outerR * Math.Max(0, Math.Min(0.6, (double)InnerRadiusRatio));
        double cx = Width / 2.0;
        double cy = topEdge + (bottomEdge - topEdge) / 2;

        if (ShowGridCircles)
        {
            Color gridColor = ChartTint.Dim(Appearance.AxisColor, bg, 0.25);
            for (int step = 1; step <= 4; step++)
            {
                double gr = innerR + (outerR - innerR) * step / 4;
                ChartVector.Stroke(canvas, new ChartStrokePlan(ChartArc.CircleCommands(cx, cy, gr), gridColor, 0.75));
            }
        }

        double sector = 2 * Math.PI / n;
        for (int i = 0; i < n; i++)
        {
            ChartDataPoint p = points[i];
            double a0 = -Math.PI / 2 + i * sector;
            double a1 = a0 + sector;
            double ratio = (double)p.Value / maxVal;
            double scaled = AreaScale ? Math.Sqrt(ratio) : ratio;
            double r = innerR + (outerR - innerR) * scaled;
            Color color = ChartCategoryColors.For(Appearance, p, i);
            List<ChartPathCommand> cmds = ChartArc.RingSectorCommands(cx, cy, innerR, r, a0, a1);
            ChartVector.Fill(canvas, new ChartPathPlan(cmds, color, null));
            ChartVector.Stroke(canvas, new ChartStrokePlan(cmds, bg, 1.25, "butt", "round", 10));

            double mid = (a0 + a1) / 2;
            if (ShowLabels)
            {
                double lr = outerR + 14;
                double lx = cx + lr * Math.Cos(mid);
                double ly = cy + lr * Math.Sin(mid) - 5;
                double cos = Math.Cos(mid);
                TextAlignment align = cos > 0.2 ? TextAlignment.Left : cos < -0.2 ? TextAlignment.Right : TextAlignment.Center;
                canvas.DrawText((float)lx, (float)ly, p.Label, Appearance.LegendTextColor, 10, font, align);
            }
            if (ShowValueLabels && r - innerR > 18)
            {
                double vr = innerR + (r - innerR) * 0.65;
                canvas.DrawText((float)(cx + vr * Math.Cos(mid)), (float)(cy + vr * Math.Sin(mid) - 4.5), ChartNumberFormat.Format(p.Value, ValueFormat), bg, 9, font, TextAlignment.Center);
            }
        }
    }
}
