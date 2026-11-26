// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>A self-contained KPI tile with hero figure, polarity delta badge, optional trend sparkline, and optional target bar.</summary>
public class StatTileChart : ChartBase
{
    public decimal Value { get; set; }
    public string ValueFormat { get; set; } = "G";
    public string ValuePrefix { get; set; } = "";
    public string ValueSuffix { get; set; } = "";
    public string Label { get; set; } = "";
    public decimal? ComparisonValue { get; set; }
    public string ComparisonLabel { get; set; } = "";
    public string DeltaMode { get; set; } = "percent";
    public string Polarity { get; set; } = "higherIsBetter";
    public IList<decimal> Trend { get; set; } = [];
    public decimal? Target { get; set; }

    internal override bool UsesStyledGeometry => true;

    public StatTileChart() { AutoLayout = true; }
    public StatTileChart(ChartData data) : base(data) { AutoLayout = true; }

    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        double val = (double)Value;
        string font = ResolveFontFamily();
        string boldFont = string.Equals(font, StandardFonts.Helvetica, StringComparison.Ordinal) ? StandardFonts.HelveticaBold : font;
        Color bg = EffectiveBackgroundColor ?? Color.White;
        Color primary = Appearance.SeriesPalette.Count > 0 ? Appearance.SeriesPalette[0] : Color.FromHex("#2563eb");
        const double padX = 20;
        double innerW = Math.Max(80, Width - padX * 2);
        double y = (data.Title.Length > 0 ? 50 : 18) + (data.Subtitle.Length > 0 ? 16 : 0);

        if (Label.Length > 0)
        {
            canvas.DrawText((float)padX, (float)y, Label, Appearance.AxisColor, 11, font, TextAlignment.Left);
            y += 18;
        }

        string heroText = ChartNumberFormat.ApplyAffixes(ChartNumberFormat.Format(Value, ValueFormat), ValuePrefix, ValueSuffix);
        int heroSize = 36;
        while (heroSize > 14 && canvas.MeasureText(heroText, heroSize, boldFont).Width > innerW) heroSize -= 2;
        canvas.DrawText((float)padX, (float)y, heroText, Appearance.LegendTextColor, heroSize, boldFont, TextAlignment.Left);
        y += heroSize + 8;

        if (ComparisonValue is { } cmpDec && cmpDec != Value)
        {
            double cmp = (double)cmpDec;
            double diff = val - cmp;
            bool up = diff > 0;
            bool good = string.Equals(Polarity, "higherIsBetter", StringComparison.Ordinal) ? up : !up;
            Color deltaColor = good
                ? (Appearance.PositiveColor ?? Color.FromHex("#16a34a"))
                : (Appearance.NegativeColor ?? Color.FromHex("#dc2626"));
            double triY = y + 2;
            List<ChartPathCommand> tri = up
                ? [new("M", [padX + 4, triY]), new("L", [padX + 8, triY + 7]), new("L", [padX, triY + 7]), new("Z", [])]
                : [new("M", [padX, triY]), new("L", [padX + 8, triY]), new("L", [padX + 4, triY + 7]), new("Z", [])];
            ChartVector.Fill(canvas, new ChartPathPlan(tri, deltaColor, null));

            string deltaStr = string.Equals(DeltaMode, "percent", StringComparison.Ordinal) && cmp != 0
                ? $"{(up ? "+" : "-")}{ChartNumberFormat.Format(ChartTemplate.ToDecimal(Math.Abs(diff / cmp)), "P1")}"
                : $"{(up ? "+" : "-")}{ChartNumberFormat.ApplyAffixes(ChartNumberFormat.Format(ChartTemplate.ToDecimal(Math.Abs(diff)), ValueFormat), ValuePrefix, ValueSuffix)}";
            canvas.DrawText((float)(padX + 12), (float)y, deltaStr, deltaColor, 11, boldFont, TextAlignment.Left);
            if (ComparisonLabel.Length > 0)
            {
                float dw = canvas.MeasureText(deltaStr, 11, boldFont).Width;
                canvas.DrawText((float)(padX + 18 + dw), (float)(y + 0.5), ComparisonLabel, Appearance.AxisColor, 10, font, TextAlignment.Left);
            }
            y += 22;
        }

        double bottomLimit = Height - 16 - (Caption.Length > 0 ? 16 : 0);
        bool hasTarget = Target is { } tgtDec && tgtDec > 0;
        double targetReserve = hasTarget ? 26 : 0;

        if (Trend.Count >= 2 && bottomLimit - targetReserve - y >= 20)
        {
            double sparkH = Math.Min(54, bottomLimit - targetReserve - y - 6);
            double minT = (double)Trend.Min();
            double maxT = (double)Trend.Max();
            double spanT = maxT > minT ? maxT - minT : 1;
            (double X, double Y)[] pts = new (double, double)[Trend.Count];
            for (int i = 0; i < Trend.Count; i++)
                pts[i] = (padX + innerW * i / (Trend.Count - 1), y + sparkH - ((double)Trend[i] - minT) / spanT * (sparkH - 4) - 2);
            List<ChartPathCommand> areaCmds = [new("M", [pts[0].X, y + sparkH]), new("L", [pts[0].X, pts[0].Y])];
            List<ChartPathCommand> lineCmds = [new("M", [pts[0].X, pts[0].Y])];
            for (int i = 1; i < pts.Length; i++)
            {
                areaCmds.Add(new("L", [pts[i].X, pts[i].Y]));
                lineCmds.Add(new("L", [pts[i].X, pts[i].Y]));
            }
            areaCmds.Add(new("L", [pts[^1].X, y + sparkH]));
            areaCmds.Add(new("Z", []));
            ChartVector.Fill(canvas, new ChartPathPlan(areaCmds, ChartTint.Dim(primary, bg, 0.14), null));
            ChartVector.Stroke(canvas, new ChartStrokePlan(lineCmds, primary, 2, "round", "round"));
            y += sparkH + 10;
        }

        if (hasTarget && Target is { } targetDec)
        {
            double target = (double)targetDec;
            double barY = Math.Min(bottomLimit - 20, Math.Max(y, bottomLimit - 22));
            double frac = Math.Max(0, Math.Min(1, val / target));
            ChartVector.Fill(canvas, new ChartPathPlan(ChartArc.RoundedRectCommands(padX, barY, innerW, 6, 3), ChartTint.Dim(primary, bg, 0.16), null));
            if (frac > 0.005)
                ChartVector.Fill(canvas, new ChartPathPlan(ChartArc.RoundedRectCommands(padX, barY, innerW * frac, 6, 3), primary, null));
            string pctText = $"{ChartNumberFormat.Format(ChartTemplate.ToDecimal(val / target), "P0")} of target ({ChartNumberFormat.ApplyAffixes(ChartNumberFormat.Format(targetDec, ValueFormat), ValuePrefix, ValueSuffix)})";
            canvas.DrawText((float)padX, (float)(barY + 9), pctText, Appearance.AxisColor, 9, font, TextAlignment.Left);
        }
    }
}
