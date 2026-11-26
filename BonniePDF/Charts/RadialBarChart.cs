// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>Concentric circular bars comparing categories around a shared radial origin.</summary>
public class RadialBarChart : ChartBase
{
    public decimal? MaxValue { get; set; }
    public float MaxAngle { get; set; } = 270;
    public float InnerRadiusRatio { get; set; } = 0.25f;
    public bool RoundCaps { get; set; } = true;
    public bool ShowValueLabels { get; set; } = true;
    public string ValueFormat { get; set; } = "G";
    public string ValuePrefix { get; set; } = "";
    public string ValueSuffix { get; set; } = "";

    internal override bool UsesStyledGeometry => true;

    public RadialBarChart() { AutoLayout = true; }
    public RadialBarChart(ChartData data) : base(data) { AutoLayout = true; }

    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        IList<ChartDataPoint> points = data.Series.Count > 0 ? data.Series[0].DataPoints : [];
        int n = points.Count;
        if (n == 0) return;
        if (points.Any(p => p.Value < 0)) throw new InvalidOperationException("RadialBar values must be finite and nonnegative.");
        if (!(MaxAngle >= 90 && MaxAngle <= 330)) throw new InvalidOperationException("RadialBar maxAngle must be between 90 and 330 degrees.");
        double maxVal = (double)(MaxValue ?? points.Max(p => p.Value));
        if (!(maxVal > 0)) return;
        string font = ResolveFontFamily();
        Color bg = EffectiveBackgroundColor ?? Color.White;
        double topEdge = (data.Title.Length > 0 ? 56 : 16) + (data.Subtitle.Length > 0 ? 18 : 0);
        double bottomEdge = Height - 16 - (Caption.Length > 0 ? 16 : 0);
        double outerR = Math.Max(24, Math.Min(Width / 2.0 - 24, (bottomEdge - topEdge) / 2 - 8));
        double innerR = outerR * Math.Max(0.1, Math.Min(0.7, (double)InnerRadiusRatio));
        double cx = Width / 2.0 + 12;
        double cy = topEdge + (bottomEdge - topEdge) / 2;
        double band = (outerR - innerR) / n;
        double strokeW = Math.Max(3, band * 0.68);
        double startRad = -Math.PI / 2;
        double sweepRad = MaxAngle * Math.PI / 180.0;
        string lineCap = RoundCaps ? "round" : "butt";

        for (int i = 0; i < n; i++)
        {
            ChartDataPoint p = points[i];
            double r = outerR - band * (i + 0.5);
            Color color = ChartCategoryColors.For(Appearance, p, i);
            Color track = ChartTint.Dim(color, bg, 0.14);
            ChartVector.Stroke(canvas, new ChartStrokePlan(ChartArc.ArcStrokeCommands(cx, cy, r, startRad, startRad + sweepRad), track, strokeW, lineCap));
            double frac = Math.Max(0, Math.Min(1, (double)p.Value / maxVal));
            double endRad = startRad + frac * sweepRad;
            if (frac > 1e-4)
                ChartVector.Stroke(canvas, new ChartStrokePlan(ChartArc.ArcStrokeCommands(cx, cy, r, startRad, endRad), color, strokeW, lineCap));
            double labelY = cy - r - 4.5;
            canvas.DrawText((float)(cx - 10), (float)labelY, p.Label, Appearance.LegendTextColor, 9, font, TextAlignment.Right);
            if (ShowValueLabels)
            {
                string valText = ChartNumberFormat.ApplyAffixes(ChartNumberFormat.Format(p.Value, ValueFormat), ValuePrefix, ValueSuffix);
                double endX = cx + r * Math.Cos(endRad);
                double endY = cy + r * Math.Sin(endRad);
                TextAlignment align = Math.Cos(endRad) < -0.15 ? TextAlignment.Right : Math.Cos(endRad) > 0.15 ? TextAlignment.Left : TextAlignment.Center;
                double dx = align == TextAlignment.Left ? 8 : align == TextAlignment.Right ? -8 : 0;
                double dy = Math.Sin(endRad) > 0.5 ? 6 : Math.Sin(endRad) < -0.5 ? -14 : -4.5;
                canvas.DrawText((float)(endX + dx), (float)(endY + dy), valText, Appearance.AxisColor, 9, font, align);
            }
        }
    }
}
