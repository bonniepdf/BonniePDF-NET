// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>A stacked pyramid chart: hierarchical tiers expanding from apex to base (or inverted).</summary>
public class PyramidChart : ChartBase
{
    public float StageGap { get; set; } = 2;
    public bool Inverted { get; set; }
    public string LabelTemplate { get; set; } = "{label}  {value}";
    public string ShareTemplate { get; set; } = "{share:P0}";
    public string ValueFormat { get; set; } = "G";

    internal override bool UsesStyledGeometry => true;

    public PyramidChart() { AutoLayout = true; }
    public PyramidChart(ChartData data) : base(data) { AutoLayout = true; }

    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        IList<ChartDataPoint> points = data.Series.Count > 0 ? data.Series[0].DataPoints : [];
        int n = points.Count;
        if (n == 0) return;
        if (points.Any(p => p.Value < 0)) throw new InvalidOperationException("Pyramid values must be finite and nonnegative.");
        double total = points.Sum(p => (double)p.Value);
        if (!(total > 0)) return;
        string font = ResolveFontFamily();
        bool showShare = ShareTemplate.Length > 0;
        string[] shares = points.Select(p => ChartTemplate.Apply(ShareTemplate, new Dictionary<string, object> { ["share"] = (double)p.Value / total })).ToArray();
        double shareW = showShare ? shares.Max(s => (double)canvas.MeasureText(s, 10, font).Width) + 16 : 0;
        const double left = 24, right = 24;
        double top = (data.Title.Length > 0 ? 60 : 18) + (data.Subtitle.Length > 0 ? 18 : 0);
        double bottom = Height - 16 - (Caption.Length > 0 ? 16 : 0);
        double pyrW = Math.Max(60, Width - left - right - shareW);
        double cx = left + pyrW / 2;
        double gap = Math.Max(0, (double)StageGap);
        double totalH = Math.Max(40, bottom - top);
        double usableH = Math.Max(20, totalH - gap * (n - 1));
        double minSliceH = Math.Min(18, usableH / (n * 1.5));
        double[] rawH = points.Select(p => Math.Max(minSliceH, (double)p.Value / total * usableH)).ToArray();
        double sumRaw = rawH.Sum();
        double[] heights = rawH.Select(h => h / sumRaw * usableH).ToArray();
        IReadOnlyList<ChartResolvedEffect> effects = Appearance.BarEffects;
        double WidthAt(double yPos)
        {
            double frac = Math.Max(0, Math.Min(1, (yPos - top) / totalH));
            return pyrW * (Inverted ? 1 - frac : frac);
        }

        double y = top;
        for (int i = 0; i < n; i++)
        {
            ChartDataPoint p = points[i];
            double h = heights[i];
            double topW = WidthAt(y);
            double bottomW = WidthAt(y + h);
            Color color = ChartCategoryColors.For(Appearance, p, i);
            List<ChartPathCommand> commands =
            [
                new("M", [cx - topW / 2, y]),
                new("L", [cx + topW / 2, y]),
                new("L", [cx + bottomW / 2, y + h]),
                new("L", [cx - bottomW / 2, y + h]),
                new("Z", []),
            ];
            ChartVector.Fill(canvas, new ChartPathPlan(commands, color, null, null, effects.Count > 0 ? effects : null));
            string label = ChartTemplate.Apply(LabelTemplate.Replace("{value}", "{value:" + ValueFormat + "}", StringComparison.Ordinal), new Dictionary<string, object> { ["label"] = p.Label, ["value"] = p.Value });
            double midW = (topW + bottomW) / 2;
            bool fits = canvas.MeasureText(label, 10, font).Width + 10 <= midW && h >= 14;
            if (fits)
                canvas.DrawText((float)cx, (float)(y + h / 2 - 5), label, ChartValueScale.ContrastTextColor(color), 10, font, TextAlignment.Center);
            else
                canvas.DrawText((float)(cx + midW / 2 + 8), (float)(y + h / 2 - 5), label, Appearance.LegendTextColor, 10, font, TextAlignment.Left);
            if (showShare)
                canvas.DrawText((float)(left + pyrW + 10), (float)(y + h / 2 - 5), shares[i], Appearance.AxisColor, 10, font, TextAlignment.Left);
            y += h + gap;
        }
    }
}
