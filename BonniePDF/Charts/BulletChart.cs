// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>Qualitative background range band on a <see cref="BulletChart"/>.</summary>
public sealed class BulletRange
{
    /// <summary>Upper threshold value of this qualitative range band.</summary>
    public decimal Max { get; set; }
    /// <summary>Optional fill colour for this range band.</summary>
    public Color? Color { get; set; }
}

/// <summary>
/// Compact KPI bullet chart with qualitative background bands, primary measure bar, and target marker.
/// </summary>
public class BulletChart : ChartBase
{
    private static readonly string[] DefaultRangeColors = ["#94a3b8", "#cbd5e1", "#e2e8f0"];

    /// <summary>Primary measured KPI value.</summary>
    public decimal Value { get; set; }
    /// <summary>Optional comparative target value drawn as a perpendicular marker.</summary>
    public decimal? Target { get; set; }
    /// <summary>Qualitative background bands ordered from lower to upper thresholds.</summary>
    public IList<BulletRange> Ranges { get; set; } = [];
    /// <summary>Minimum scale value (default 0).</summary>
    public decimal MinValue { get; set; }
    /// <summary>Optional explicit maximum scale value; inferred from data and ranges when omitted.</summary>
    public decimal? MaxValue { get; set; }
    /// <summary>Fill colour of the primary measure bar.</summary>
    public Color MeasureColor { get; set; } = Color.FromHex("#2563eb");
    /// <summary>Fill colour of the comparative target marker.</summary>
    public Color TargetColor { get; set; } = Color.FromHex("#0f172a");
    /// <summary>Bar orientation (<c>Horizontal</c> default or <c>Vertical</c>).</summary>
    public ChartOrientation Orientation { get; set; } = ChartOrientation.Horizontal;
    /// <summary>Whether to draw numeric scale ticks beneath or beside the bar.</summary>
    public bool ShowScale { get; set; }
    /// <summary>Whether to draw the formatted value label beside the bar.</summary>
    public bool ShowValueLabel { get; set; }
    /// <summary>Number format for scale and value labels.</summary>
    public string ValueFormat { get; set; } = "G";
    /// <summary>Prefix prepended to formatted values.</summary>
    public string ValuePrefix { get; set; } = "";
    /// <summary>Suffix appended to formatted values.</summary>
    public string ValueSuffix { get; set; } = "";
    /// <summary>Metric label drawn beside the bullet bar.</summary>
    public string Label { get; set; } = "";

    public BulletChart()
    {
        Width = 320;
        Height = 44;
    }

    public BulletChart(ChartData data) : base(data)
    {
        Width = 320;
        Height = 44;
    }

    private decimal ResolveMax()
    {
        if (MaxValue.HasValue) return MaxValue.Value;
        decimal m = Math.Max(Value, Target ?? MinValue);
        foreach (var r in Ranges) if (r.Max > m) m = r.Max;
        return m > MinValue ? m : MinValue + 100m;
    }

    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        decimal maxV = ResolveMax();
        ChartValueScale.Check(MinValue, maxV);
        string font = ResolveFontFamily();
        bool horizontal = Orientation == ChartOrientation.Horizontal;
        float titlePad = !string.IsNullOrEmpty(data.Title) ? 36f : 0f;
        float scaleBand = ShowScale ? 16f : 0f;
        float captionPad = !string.IsNullOrEmpty(Caption) ? 16f : 0f;

        float leftPad = 8f, rightPad = 8f;
        if (horizontal && !string.IsNullOrEmpty(Label))
            leftPad = MathF.Min(Width * 0.35f, MathF.Ceiling(canvas.MeasureText(Label, 10, font).Width + 16));
        string formattedVal = $"{ValuePrefix}{ChartNumberFormat.Format(Value, ValueFormat)}{ValueSuffix}";
        if (horizontal && ShowValueLabel)
            rightPad = MathF.Min(Width * 0.25f, MathF.Ceiling(canvas.MeasureText(formattedVal, 10, font).Width + 14));

        float plotX = leftPad;
        float plotY = titlePad + 6f;
        float plotW = MathF.Max(10f, Width - leftPad - rightPad);
        float plotH = MathF.Max(10f, Height - plotY - 6f - scaleBand - captionPad);

        if (horizontal && !string.IsNullOrEmpty(Label))
            canvas.DrawText(plotX - 8, plotY + plotH / 2f - 5, Label, Appearance.TitleColor, 10, font, TextAlignment.Right);

        var sortedRanges = Ranges.Where(r => r.Max > MinValue).OrderByDescending(r => r.Max).ToList();
        if (sortedRanges.Count == 0)
        {
            canvas.FillRect(plotX, plotY, plotW, plotH, Color.FromHex("#e2e8f0"));
        }
        else
        {
            for (int i = 0; i < sortedRanges.Count; i++)
            {
                var r = sortedRanges[i];
                float frac = Math.Clamp((float)ChartValueScale.Fraction(r.Max, MinValue, maxV), 0f, 1f);
                Color col = r.Color ?? Color.FromHex(DefaultRangeColors[(sortedRanges.Count - 1 - i) % DefaultRangeColors.Length]);
                if (horizontal) canvas.FillRect(plotX, plotY, frac * plotW, plotH, col);
                else canvas.FillRect(plotX, plotY + (1f - frac) * plotH, plotW, frac * plotH, col);
            }
        }

        float valFrac = Math.Clamp((float)ChartValueScale.Fraction(Value, MinValue, maxV), 0f, 1f);
        if (horizontal)
        {
            float mh = plotH * 0.42f;
            canvas.FillRect(plotX, plotY + (plotH - mh) / 2f, valFrac * plotW, mh, MeasureColor);
        }
        else
        {
            float mw = plotW * 0.42f;
            float mh = valFrac * plotH;
            canvas.FillRect(plotX + (plotW - mw) / 2f, plotY + plotH - mh, mw, mh, MeasureColor);
        }

        if (Target.HasValue)
        {
            float tFrac = Math.Clamp((float)ChartValueScale.Fraction(Target.Value, MinValue, maxV), 0f, 1f);
            if (horizontal)
            {
                float tx = plotX + tFrac * plotW;
                float pad = plotH * 0.14f;
                canvas.DrawLine(tx, plotY + pad, tx, plotY + plotH - pad, TargetColor, 2f);
            }
            else
            {
                float ty = plotY + (1f - tFrac) * plotH;
                float pad = plotW * 0.14f;
                canvas.DrawLine(plotX + pad, ty, plotX + plotW - pad, ty, TargetColor, 2f);
            }
        }

        if (horizontal && ShowValueLabel)
            canvas.DrawText(plotX + plotW + 6, plotY + plotH / 2f - 5, formattedVal, Appearance.TitleColor, 10, font, TextAlignment.Left);

        if (ShowScale && horizontal)
        {
            const int ticks = 4;
            for (int i = 0; i <= ticks; i++)
            {
                float tx = plotX + (i / (float)ticks) * plotW;
                decimal v = MinValue + (i / (decimal)ticks) * (maxV - MinValue);
                string lbl = $"{ValuePrefix}{ChartNumberFormat.Format(v, ValueFormat)}{ValueSuffix}";
                canvas.DrawText(tx, plotY + plotH + 4, lbl, Appearance.AxisColor, 8, font, TextAlignment.Center);
            }
        }
    }
}
