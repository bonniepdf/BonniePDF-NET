// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>A funnel: the stages of a process, each as wide as its value, with the share that survives each step.</summary>
public class FunnelChart : ChartBase
{
    /// <summary><c>trapezoid</c> (default): each stage narrows to the next one's width. <c>bars</c>: centred bars, one width per stage.</summary>
    public string Shape { get; set; } = "trapezoid";
    /// <summary>Gap between stages in points.</summary>
    public float StageGap { get; set; } = 2;
    /// <summary>The narrowest a stage may be, as a fraction of the widest (<c>0.12</c>).</summary>
    public float NeckWidth { get; set; } = 0.12f;
    /// <summary>The text inside each stage; <c>{label}</c> and <c>{value}</c> are replaced.</summary>
    public string LabelTemplate { get; set; } = "{label}  {value}";
    /// <summary>The text beside each stage after the first; <c>{conversion}</c> is its share of the previous stage, <c>{conversion:P0}</c> formats it. Empty draws none.</summary>
    public string ConversionTemplate { get; set; } = "{conversion:P0}";
    /// <summary>Number format of <c>{value}</c>.</summary>
    public string ValueFormat { get; set; } = "G";

    internal override bool UsesStyledGeometry => true;

    /// <summary>Creates an empty funnel chart.</summary>
    public FunnelChart() { AutoLayout = true; }

    /// <summary>Creates a funnel chart for <paramref name="data"/>.</summary>
    public FunnelChart(ChartData data) : base(data) { AutoLayout = true; }

    /// <inheritdoc />
    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        IList<ChartDataPoint> points = data.Series.Count > 0 ? data.Series[0].DataPoints : [];
        int n = points.Count;
        if (n == 0) return;
        if (points.Any(p => p.Value < 0)) throw new InvalidOperationException("Funnel values must be finite and nonnegative.");
        if (!(NeckWidth >= 0 && NeckWidth <= 1)) throw new InvalidOperationException("Funnel neckWidth must be from 0 to 1.");
        string font = ResolveFontFamily();
        decimal max = points.Max(p => p.Value);
        bool showConversion = ConversionTemplate.Length > 0 && n > 1;
        string[] conversions = new string[n];
        for (int i = 1; i < n; i++)
            conversions[i] = ChartTemplate.Apply(ConversionTemplate, new Dictionary<string, object> { ["conversion"] = points[i - 1].Value > 0 ? (double)points[i].Value / (double)points[i - 1].Value : 0.0 });
        float band = 0;
        if (showConversion) { for (int i = 1; i < n; i++) band = Math.Max(band, canvas.MeasureText(conversions[i], 10, font).Width); band += 12; }
        const float left = 24, right = 24;
        float top = (data.Title.Length > 0 ? 64 : 18) + (data.Subtitle.Length > 0 ? 18 : 0);
        float bottom = Height - 16 - (Caption.Length > 0 ? 16 : 0);
        float funnelW = Math.Max(40, Width - left - right - band);
        float cx = left + funnelW / 2;
        float gap = Math.Max(0, StageGap);
        float stageH = (bottom - top - gap * (n - 1)) / n;
        float WidthOf(decimal v) => funnelW * Math.Max(NeckWidth, max > 0 ? (float)(v / max) : 0);
        IReadOnlyList<ChartResolvedEffect> effects = Appearance.BarEffects;
        for (int i = 0; i < n; i++)
        {
            ChartDataPoint p = points[i];
            float y = top + i * (stageH + gap);
            float topW = WidthOf(p.Value);
            float bottomW = string.Equals(Shape, "trapezoid", StringComparison.Ordinal) && i < n - 1 ? WidthOf(points[i + 1].Value) : topW;
            Color color = ChartCategoryColors.For(Appearance, p, i);
            List<ChartPathCommand> commands =
            [
                new("M", [cx - topW / 2, y]), new("L", [cx + topW / 2, y]),
                new("L", [cx + bottomW / 2, y + stageH]), new("L", [cx - bottomW / 2, y + stageH]), new("Z", []),
            ];
            ChartVector.Fill(canvas, new ChartPathPlan(commands, color, null, null, effects.Count > 0 ? effects : null));
            string label = ChartTemplate.Apply(LabelTemplate.Replace("{value}", "{value:" + ValueFormat + "}", StringComparison.Ordinal), new Dictionary<string, object> { ["label"] = p.Label, ["value"] = p.Value });
            const float size = 11;
            bool fits = canvas.MeasureText(label, size, font).Width + 8 <= (topW + bottomW) / 2;
            canvas.DrawText(cx, y + stageH / 2 - size / 2, label, fits ? ChartValueScale.ContrastTextColor(color) : Appearance.LegendTextColor, size, font, TextAlignment.Center);
            if (showConversion && i > 0) canvas.DrawText(left + funnelW + 8, y + stageH / 2 - 5, conversions[i], Appearance.AxisColor, 10, font, TextAlignment.Left);
        }
    }
}
