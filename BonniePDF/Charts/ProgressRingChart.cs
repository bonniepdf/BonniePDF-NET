// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>One ring of a <see cref="ProgressRingChart"/>.</summary>
public sealed class ProgressRing
{
    public decimal Value { get; set; }
    public decimal? Max { get; set; }
    public string? Label { get; set; }
    public Color? Color { get; set; }
    public Color? TrackColor { get; set; }
}

/// <summary>Concentric circular progress rings with centre summary and optional legend.</summary>
public class ProgressRingChart : ChartBase
{
    public IList<ProgressRing> Rings { get; set; } = [];
    public float Thickness { get; set; } = 0.14f;
    public float RingGap { get; set; } = 0.04f;
    public float StartAngle { get; set; } = -90;
    public bool RoundCaps { get; set; } = true;
    public string CenterTemplate { get; set; } = "{value:P0}";
    public string CenterSubtext { get; set; } = "";
    public bool ShowLegend { get; set; } = true;

    internal override bool UsesStyledGeometry => true;

    public ProgressRingChart() { AutoLayout = true; }
    public ProgressRingChart(ChartData data) : base(data) { AutoLayout = true; }

    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        int n = Rings.Count;
        if (n == 0) return;
        if (n > 6) throw new InvalidOperationException("ProgressRingChart supports at most 6 rings.");
        string font = ResolveFontFamily();
        string boldFont = string.Equals(font, StandardFonts.Helvetica, StringComparison.Ordinal) ? StandardFonts.HelveticaBold : font;
        Color bg = EffectiveBackgroundColor ?? Color.White;
        double topEdge = (data.Title.Length > 0 ? 56 : 16) + (data.Subtitle.Length > 0 ? 18 : 0);
        double bottomEdge = Height - 16 - (Caption.Length > 0 ? 16 : 0);
        bool showLeg = ShowLegend && Rings.Any(r => !string.IsNullOrEmpty(r.Label));
        double legendW = showLeg ? Math.Min(Width * 0.4, 160) : 0;
        double availW = Math.Max(60, Width - 32 - legendW);
        double availH = Math.Max(60, bottomEdge - topEdge);
        double outerR = Math.Max(24, Math.Min(availW, availH) / 2 - 4);
        double cx = 16 + availW / 2;
        double cy = topEdge + availH / 2;
        double strokeW = Math.Max(4, outerR * Thickness);
        double gapW = outerR * RingGap;
        double startRad = StartAngle * Math.PI / 180.0;
        string lineCap = RoundCaps ? "round" : "butt";

        List<Color> ringColors = [];
        for (int i = 0; i < n; i++)
        {
            ProgressRing ring = Rings[i];
            double max = (double)(ring.Max ?? 100m);
            double rVal = (double)ring.Value;
            if (!(max > 0))
                throw new InvalidOperationException("Progress ring values must be finite and max must be positive.");
            double r = outerR - strokeW / 2 - i * (strokeW + gapW);
            if (r <= strokeW / 2) break;
            Color color = ring.Color ?? ChartCategoryColors.For(Appearance, null, i);
            ringColors.Add(color);
            Color track = ring.TrackColor ?? ChartTint.Dim(color, bg, 0.16);
            ChartVector.Stroke(canvas, new ChartStrokePlan(ChartArc.CircleCommands(cx, cy, r), track, strokeW));
            double frac = Math.Max(0, Math.Min(1, rVal / max));
            if (frac >= 0.9999)
                ChartVector.Stroke(canvas, new ChartStrokePlan(ChartArc.CircleCommands(cx, cy, r), color, strokeW));
            else if (frac > 1e-5)
                ChartVector.Stroke(canvas, new ChartStrokePlan(ChartArc.ArcStrokeCommands(cx, cy, r, startRad, startRad + frac * 2 * Math.PI), color, strokeW, lineCap));
        }

        ProgressRing first = Rings[0];
        double firstMax = (double)(first.Max ?? 100m);
        double innerHole = Math.Max(12, outerR - n * (strokeW + gapW));
        if (CenterTemplate.Length > 0 && innerHole >= 16)
        {
            string mainText = ChartTemplate.Apply(CenterTemplate, new Dictionary<string, object> { ["value"] = (double)first.Value / firstMax, ["raw"] = first.Value });
            double mainSize = Math.Max(11, Math.Min(22, Math.Floor(innerHole * 0.45)));
            bool hasSub = CenterSubtext.Length > 0;
            double mainY = hasSub ? cy - mainSize * 0.75 : cy - mainSize / 2;
            canvas.DrawText((float)cx, (float)mainY, mainText, Appearance.LegendTextColor, (float)mainSize, boldFont, TextAlignment.Center);
            if (hasSub)
                canvas.DrawText((float)cx, (float)(mainY + mainSize + 3), CenterSubtext, Appearance.AxisColor, (float)Math.Max(8, Math.Round(mainSize * 0.5, MidpointRounding.AwayFromZero)), font, TextAlignment.Center);
        }

        if (showLeg)
        {
            double legX = 24 + availW;
            const double rowH = 22;
            double startY = cy - n * rowH / 2;
            for (int i = 0; i < ringColors.Count; i++)
            {
                ProgressRing ring = Rings[i];
                double max = (double)(ring.Max ?? 100m);
                double y = startY + i * rowH;
                ChartVector.Fill(canvas, new ChartPathPlan(ChartArc.CircleCommands(legX + 5, y + 6, 4.5), ringColors[i], null));
                int pct = (int)Math.Round((double)ring.Value / max * 100, MidpointRounding.AwayFromZero);
                string text = $"{ring.Label ?? $"Ring {i + 1}"} ({pct}%)";
                canvas.DrawText((float)(legX + 15), (float)(y + 1), text, Appearance.LegendTextColor, 10, font, TextAlignment.Left);
            }
        }
    }
}
