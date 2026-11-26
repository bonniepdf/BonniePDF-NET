// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>A qualitative range band on a <see cref="GaugeChart"/>.</summary>
public sealed class GaugeBand
{
    public decimal Max { get; set; }
    public Color? Color { get; set; }
}

/// <summary>A radial KPI gauge with threshold bands, ticks, optional target notch, and needle or marker.</summary>
public class GaugeChart : ChartBase
{
    public decimal Value { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; } = 100m;
    public IList<GaugeBand> Bands { get; set; } = [];
    public decimal? Target { get; set; }
    public float Arc { get; set; } = 180;
    public string Needle { get; set; } = "needle";
    public bool ShowScale { get; set; } = true;
    public string ValueTemplate { get; set; } = "{value}";
    public string ValueFormat { get; set; } = "G";
    public string Unit { get; set; } = "";

    internal override bool UsesStyledGeometry => true;

    public GaugeChart() { AutoLayout = true; }
    public GaugeChart(ChartData data) : base(data) { AutoLayout = true; }

    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        double minV = (double)MinValue;
        double maxV = (double)MaxValue;
        double val = (double)Value;
        if (!(maxV > minV))
            throw new InvalidOperationException("Gauge maxValue must be greater than minValue.");
        if (!(Arc >= 90 && Arc <= 300)) throw new InvalidOperationException("Gauge arc must be between 90 and 300 degrees.");

        string font = ResolveFontFamily();
        string boldFont = string.Equals(font, StandardFonts.Helvetica, StringComparison.Ordinal) ? StandardFonts.HelveticaBold : font;
        double topEdge = (data.Title.Length > 0 ? 56 : 18) + (data.Subtitle.Length > 0 ? 18 : 0);
        double bottomEdge = Height - 16 - (Caption.Length > 0 ? 16 : 0);
        double availW = Math.Max(80, Width - 48);
        double availH = Math.Max(60, bottomEdge - topEdge);
        double arcRad = Arc * Math.PI / 180.0;
        double startAngle = -Math.PI / 2 - arcRad / 2;
        double endAngle = -Math.PI / 2 + arcRad / 2;
        double extraBelow = Arc > 180 ? Math.Sin((arcRad - Math.PI) / 2) : 0;
        double scalePad = ShowScale ? 22 : 8;
        double radius = Math.Max(24, Math.Min((availW - scalePad * 2) / 2, (availH - scalePad - 24) / (1 + extraBelow)));
        double thickness = Math.Max(10, radius * 0.22);
        double innerR = radius - thickness;
        double cx = Width / 2.0;
        double cy = topEdge + scalePad + radius;
        Color bg = EffectiveBackgroundColor ?? Color.White;
        Color primary = Appearance.SeriesPalette.Count > 0 ? Appearance.SeriesPalette[0] : Color.FromHex("#2563eb");
        double span = maxV - minV;
        double AngleFor(double v) => startAngle + Math.Max(0, Math.Min(1, (v - minV) / span)) * arcRad;

        if (Bands.Count > 0)
        {
            double prev = minV;
            for (int i = 0; i < Bands.Count; i++)
            {
                GaugeBand band = Bands[i];
                double hi = Math.Max(prev, Math.Min(maxV, (double)band.Max));
                if (hi > prev)
                {
                    Color color = band.Color ?? ChartTint.Dim(primary, bg, 0.25 + (double)i / Math.Max(1, Bands.Count) * 0.55);
                    ChartVector.Fill(canvas, new ChartPathPlan(ChartArc.RingSectorCommands(cx, cy, innerR, radius, AngleFor(prev), AngleFor(hi)), color, null));
                }
                prev = hi;
            }
            if (prev < maxV)
            {
                ChartVector.Fill(canvas, new ChartPathPlan(ChartArc.RingSectorCommands(cx, cy, innerR, radius, AngleFor(prev), endAngle), ChartTint.Dim(Appearance.AxisColor, bg, 0.2), null));
            }
        }
        else
        {
            ChartVector.Fill(canvas, new ChartPathPlan(ChartArc.RingSectorCommands(cx, cy, innerR, radius, startAngle, endAngle), ChartTint.Dim(primary, bg, 0.16), null));
            double va0 = AngleFor(val);
            if (va0 > startAngle + 1e-4)
                ChartVector.Fill(canvas, new ChartPathPlan(ChartArc.RingSectorCommands(cx, cy, innerR, radius, startAngle, va0), primary, null));
        }

        if (ShowScale)
        {
            const int steps = 4;
            for (int i = 0; i <= steps; i++)
            {
                double v = minV + span * i / steps;
                double a = AngleFor(v);
                canvas.DrawLine(
                    (float)(cx + (radius + 2) * Math.Cos(a)), (float)(cy + (radius + 2) * Math.Sin(a)),
                    (float)(cx + (radius + 7) * Math.Cos(a)), (float)(cy + (radius + 7) * Math.Sin(a)),
                    Appearance.AxisColor, 1);
                double lx = cx + (radius + 16) * Math.Cos(a);
                double ly = cy + (radius + 16) * Math.Sin(a) - 4.5;
                canvas.DrawText((float)lx, (float)ly, ChartNumberFormat.Format(ChartTemplate.ToDecimal(v), ValueFormat), Appearance.AxisColor, 9, font, TextAlignment.Center);
            }
        }

        if (Target is { } t)
        {
            double ta = AngleFor((double)t);
            ChartVector.Stroke(canvas, new ChartStrokePlan(
                [new("M", [cx + (innerR - 4) * Math.Cos(ta), cy + (innerR - 4) * Math.Sin(ta)]), new("L", [cx + (radius + 4) * Math.Cos(ta), cy + (radius + 4) * Math.Sin(ta)])],
                Appearance.LegendTextColor, 2));
        }

        double va = AngleFor(val);
        if (string.Equals(Needle, "marker", StringComparison.Ordinal))
        {
            double mr = (innerR + radius) / 2;
            double mx = cx + mr * Math.Cos(va);
            double my = cy + mr * Math.Sin(va);
            ChartVector.Fill(canvas, new ChartPathPlan(ChartArc.CircleCommands(mx, my, thickness * 0.45), bg, null));
            ChartVector.Fill(canvas, new ChartPathPlan(ChartArc.CircleCommands(mx, my, thickness * 0.3), Appearance.LegendTextColor, null));
        }
        else
        {
            double tipR = radius - 3;
            double baseW = Math.Max(3, thickness * 0.22);
            double perp = va + Math.PI / 2;
            List<ChartPathCommand> needleCmds =
            [
                new("M", [cx + tipR * Math.Cos(va), cy + tipR * Math.Sin(va)]),
                new("L", [cx + baseW * Math.Cos(perp), cy + baseW * Math.Sin(perp)]),
                new("L", [cx - baseW * Math.Cos(perp), cy - baseW * Math.Sin(perp)]),
                new("Z", []),
            ];
            ChartVector.Fill(canvas, new ChartPathPlan(needleCmds, Appearance.LegendTextColor, null));
            ChartVector.Fill(canvas, new ChartPathPlan(ChartArc.CircleCommands(cx, cy, baseW * 1.35), Appearance.LegendTextColor, null));
            ChartVector.Fill(canvas, new ChartPathPlan(ChartArc.CircleCommands(cx, cy, baseW * 0.55), bg, null));
        }

        ChartVector.Stroke(canvas, new ChartStrokePlan(ChartArc.ArcStrokeCommands(cx, cy, innerR, startAngle, endAngle), ChartTint.Dim(Appearance.AxisColor, bg, 0.15), 0.75));

        string formatted = ChartTemplate.Apply(ValueTemplate.Replace("{value}", "{value:" + ValueFormat + "}", StringComparison.Ordinal), new Dictionary<string, object> { ["value"] = Value }) + Unit;
        double textY = string.Equals(Needle, "needle", StringComparison.Ordinal) ? cy + Math.Max(12, radius * 0.18) : cy - 10;
        double fontSize = Math.Max(14, Math.Min(24, Math.Round(radius * 0.24, MidpointRounding.AwayFromZero)));
        canvas.DrawText((float)cx, (float)textY, formatted, Appearance.LegendTextColor, (float)fontSize, boldFont, TextAlignment.Center);
    }
}
