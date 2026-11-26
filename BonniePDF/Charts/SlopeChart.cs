// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>A slope chart: how each series changes between exactly two categories, labelled at both ends.</summary>
public class SlopeChart : ChartBase
{
    /// <summary>Number format of the values in the end labels.</summary>
    public string ValueFormat { get; set; } = "G";
    /// <summary>Width of the lines in points.</summary>
    public float LineWidth { get; set; } = 2;
    /// <summary>Size of the end labels in points.</summary>
    public float LabelFontSize { get; set; } = 10;

    internal override bool UsesStyledGeometry => true;

    /// <summary>Creates an empty slope chart.</summary>
    public SlopeChart() { AutoLayout = true; }

    /// <summary>Creates a slope chart for <paramref name="data"/>.</summary>
    public SlopeChart(ChartData data) : base(data) { AutoLayout = true; }

    /// <summary>Spreads labels that want the same place: sorted by y, pushed apart to <paramref name="gap"/>, then back up if the last one leaves <paramref name="bottom"/>.</summary>
    internal static double[] SeparateLabels(IReadOnlyList<double> ys, double gap, double bottom)
    {
        var order = ys.Select((y, i) => (Y: y, I: i)).OrderBy(o => o.Y).ThenBy(o => o.I).ToList();
        double[] output = order.Select(o => o.Y).ToArray();
        for (int k = 1; k < output.Length; k++) output[k] = Math.Max(output[k], output[k - 1] + gap);
        if (output.Length > 0 && output[^1] > bottom)
        {
            output[^1] = bottom;
            for (int k = output.Length - 2; k >= 0; k--) output[k] = Math.Min(output[k], output[k + 1] - gap);
        }
        double[] result = new double[ys.Count];
        for (int k = 0; k < order.Count; k++) result[order[k].I] = output[k];
        return result;
    }

    /// <inheritdoc />
    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        IList<ChartSeries> series = data.Series;
        if (series.Count == 0) return;
        if (series.Any(s => s.DataPoints.Count != 2)) throw new InvalidOperationException("A slope chart needs exactly two categories in every series.");
        string font = ResolveFontFamily();
        float size = LabelFontSize;
        List<string> leftTexts = series.Select(s => $"{s.Name}  {ChartNumberFormat.Format(s.DataPoints[0].Value, ValueFormat)}").ToList();
        List<string> rightTexts = series.Select(s => $"{s.Name}  {ChartNumberFormat.Format(s.DataPoints[1].Value, ValueFormat)}").ToList();
        float Widest(List<string> texts) => Math.Min(Width * 0.3f, texts.Max(t => canvas.MeasureText(t, size, font).Width));
        float x0 = 12 + Widest(leftTexts) + 12;
        float x1 = Math.Max(x0 + 60, Width - 12 - Widest(rightTexts) - 12);
        float top = (data.Title.Length > 0 ? 56 : 18) + (data.Subtitle.Length > 0 ? 18 : 0) + 22;
        float bottom = Height - 16 - (Caption.Length > 0 ? 16 : 0);
        List<double> values = series.SelectMany(s => s.DataPoints.Select(p => (double)p.Value)).ToList();
        double lo = values.Min(), hi = values.Max();
        if (!(hi > lo)) { lo -= 1; hi += 1; }
        double YOf(decimal v) => bottom - ((double)v - lo) / (hi - lo) * (bottom - top);
        Color axis = Appearance.AxisColor;
        canvas.DrawLine(x0, top, x0, bottom, axis, 1);
        canvas.DrawLine(x1, top, x1, bottom, axis, 1);
        canvas.DrawText(x0, top - 20, series[0].DataPoints[0].Label, Appearance.LegendTextColor, 10, font, TextAlignment.Center);
        canvas.DrawText(x1, top - 20, series[0].DataPoints[1].Label, Appearance.LegendTextColor, 10, font, TextAlignment.Center);
        Color background = EffectiveBackgroundColor ?? Color.White;
        double gap = size + 2;
        double[] leftY = SeparateLabels(series.Select(s => YOf(s.DataPoints[0].Value)).ToList(), gap, bottom);
        double[] rightY = SeparateLabels(series.Select(s => YOf(s.DataPoints[1].Value)).ToList(), gap, bottom);
        for (int i = 0; i < series.Count; i++)
        {
            ChartSeries s = series[i];
            Color color = s.HasColor ? s.Color : ChartCategoryColors.For(Appearance, null, i);
            double ya = YOf(s.DataPoints[0].Value), yb = YOf(s.DataPoints[1].Value);
            ChartVector.Stroke(canvas, new ChartStrokePlan([new("M", [x0, ya]), new("L", [x1, yb])], color, LineWidth, "round", "miter", 10));
            ChartMarkers.Draw(canvas, x0, (float)ya, 3.5f, color, "circle", "solid", 1.5f, background, LineWidth);
            ChartMarkers.Draw(canvas, x1, (float)yb, 3.5f, color, "circle", "solid", 1.5f, background, LineWidth);
            canvas.DrawText(x0 - 8, (float)(leftY[i] - size / 2), leftTexts[i], color, size, font, TextAlignment.Right);
            canvas.DrawText(x1 + 8, (float)(rightY[i] - size / 2), rightTexts[i], color, size, font, TextAlignment.Left);
        }
    }
}
