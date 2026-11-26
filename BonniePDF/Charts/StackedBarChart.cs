// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Renders multiple series stacked per category, in absolute or 100% normalized mode.
/// </summary>
public class StackedBarChart : CartesianChartBase
{
    /// <summary>Corner rounding and gradient style applied to stacked bar segments.</summary>
    public ChartBarStyle BarStyle { get; set; } = new();
    /// <summary>Stacking mode: <c>absolute</c> (default) or 100% normalized <c>percent</c>.</summary>
    public string Mode { get; set; } = "absolute";
    /// <summary>Fraction of each category slot occupied by the bar (0 to 1).</summary>
    public float BarWidthRatio { get; set; } = 0.6f;
    /// <summary>Whether to draw the category total beyond the top of each stacked bar.</summary>
    public bool ShowStackTotals { get; set; }
    /// <summary>Number format for category stack totals.</summary>
    public string? StackTotalFormat { get; set; }
    /// <summary>Visual gap between adjacent stacked segments in points.</summary>
    public float SegmentGap { get; set; }

    internal override bool UsesStyledGeometry => base.UsesStyledGeometry || (BarStyle.CornerRadius ?? 0) > 0 || BarStyle.Gradient is not null;
    protected override bool DefaultAutoScale => true;

    public StackedBarChart()
    {
        SvgShapeRendering = "crispEdges";
        AutoLayout = true;
        ValueLabelPosition = "inside";
    }

    public StackedBarChart(ChartData data) : this()
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    protected override void DrawLegendSwatch(ICanvas canvas, float x, float y, float size, ChartSeries series)
    {
        Color color = ResolveSeriesColor(series);
        int idx = (_renderData ?? Data).Series.IndexOf(series);
        string pat = ResolveSeriesPattern(series, idx);
        ChartBars.Draw(canvas, x, y, size, size, color, BarStyle, Appearance, false, x, y, size, size);
        if (pat is not "none") ChartPatterns.FillRect(canvas, x, y, size, size, new ChartPatternFill(pat, Color.White));
    }

    private static int CategoryCount(ChartData data)
    {
        int max = 0;
        foreach (var s in data.Series) if (s.DataPoints.Count > max) max = s.DataPoints.Count;
        return max;
    }

    private decimal CategoryTotal(ChartData data, int catIndex)
    {
        decimal sum = 0;
        foreach (var s in data.Series)
        {
            if (catIndex < s.DataPoints.Count)
            {
                decimal v = s.DataPoints[catIndex].Value;
                if (Mode is "percent" && v < 0)
                    throw new ArgumentOutOfRangeException(nameof(data), "StackedBarChart in percent mode requires nonnegative values.");
                sum += v;
            }
        }
        return sum;
    }

    protected override (double Min, double Max)? GetValueDataRange(ChartData data)
    {
        int count = CategoryCount(data);
        if (count == 0) return null;
        if (Mode is "percent") return (0, 100);
        double min = 0, max = 0;
        for (int i = 0; i < count; i++)
        {
            double pos = 0, neg = 0;
            foreach (var s in data.Series)
            {
                if (i < s.DataPoints.Count)
                {
                    double v = (double)s.DataPoints[i].Value;
                    if (v >= 0) pos += v; else neg += v;
                }
            }
            if (pos > max) max = pos;
            if (neg < min) min = neg;
        }
        return (min, max);
    }

    protected override void ResolveValueScale(ChartData data)
    {
        if (Mode is "percent")
        {
            _effectiveMin = 0;
            _effectiveMax = 100;
            _effectiveStepCount = data.YAxis.StepCount > 0 ? data.YAxis.StepCount : 5;
            _effectiveTickFormat = data.YAxis.ValueFormat ?? "N0";
            return;
        }
        base.ResolveValueScale(data);
    }

    internal override bool SupportsDepth3d => true;

    protected override void DrawAllSeries(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        int count = CategoryCount(data);
        if (count == 0) return;
        bool horizontal = Orientation == ChartOrientation.Horizontal;
        float gap = Math.Clamp(SegmentGap, 0f, 4f);
        float slotSpan = (horizontal ? chartAreaHeight : chartAreaWidth) / count;
        float thickness = MathF.Min(slotSpan * BarWidthRatio, 50f);
        ChartBarStyle squareStyle = new() { CornerRadius = 0, Gradient = BarStyle.Gradient };

        for (int i = 0; i < count; i++)
        {
            decimal total = Mode is "percent" ? CategoryTotal(data, i) : 1m;
            if (Mode is "percent" && total <= 0) continue;

            int lastPosIdx = -1, lastNegIdx = -1;
            for (int sIdx = 0; sIdx < data.Series.Count; sIdx++)
            {
                if (i >= data.Series[sIdx].DataPoints.Count) continue;
                decimal v = data.Series[sIdx].DataPoints[i].Value;
                if (v > 0) lastPosIdx = sIdx;
                else if (v < 0) lastNegIdx = sIdx;
            }

            decimal posAcc = 0, negAcc = 0;
            float center = (horizontal ? topY : _plotLeft) + i * slotSpan + slotSpan / 2f;

            for (int sIdx = 0; sIdx < data.Series.Count; sIdx++)
            {
                ChartSeries series = data.Series[sIdx];
                if (i >= series.DataPoints.Count) continue;
                ChartDataPoint dp = series.DataPoints[i];
                if (dp.Value == 0) continue;

                decimal val = Mode is "percent" ? (dp.Value / total) * 100m : dp.Value;
                decimal startVal = val > 0 ? posAcc : negAcc;
                decimal endVal = startVal + val;
                if (val > 0) posAcc = endVal; else negAcc = endVal;

                bool isOuter = (val > 0 && sIdx == lastPosIdx) || (val < 0 && sIdx == lastNegIdx);
                ChartBarStyle style = isOuter ? BarStyle : squareStyle;
                Color color = ResolveSeriesColor(series);
                string pat = ResolveSeriesPattern(series, sIdx);

                float fStart = (float)ChartValueScale.Fraction(startVal, _effectiveMin, _effectiveMax);
                float fEnd = (float)ChartValueScale.Fraction(endVal, _effectiveMin, _effectiveMax);

                if (horizontal)
                {
                    float xStart = _plotLeft + fStart * chartAreaWidth;
                    float xEnd = _plotLeft + fEnd * chartAreaWidth;
                    if (gap > 0 && startVal != 0) xStart += val > 0 ? gap : -gap;
                    float w = xEnd - xStart;
                    float by = center - thickness / 2f;
                    int categoryX = i;
                    MarkGroup(canvas, data, series, categoryX, new ChartGroupAnchor(xStart, center, "x"), () =>
                    {
                        DrawBarDepth(canvas, xStart, by, w, thickness, color);
                        ChartBars.Draw(canvas, xStart, by, w, thickness, color, style, Appearance, true, _plotLeft, topY, chartAreaWidth, chartAreaHeight);
                        if (pat is not "none") ChartPatterns.FillRect(canvas, w < 0 ? xStart + w : xStart, by, MathF.Abs(w), thickness, new ChartPatternFill(pat, Color.White));
                    });
                }
                else
                {
                    float yStart = baselineY - fStart * chartAreaHeight;
                    float yEnd = baselineY - fEnd * chartAreaHeight;
                    if (gap > 0 && startVal != 0) yStart += val > 0 ? -gap : gap;
                    float h = yStart - yEnd;
                    float bx = center - thickness / 2f;
                    int categoryY = i;
                    MarkGroup(canvas, data, series, categoryY, new ChartGroupAnchor(center, yStart, "y"), () =>
                    {
                        DrawBarDepth(canvas, bx, yEnd, thickness, h, color);
                        ChartBars.Draw(canvas, bx, yEnd, thickness, h, color, style, Appearance, false, _plotLeft, topY, chartAreaWidth, chartAreaHeight);
                        if (pat is not "none") ChartPatterns.FillRect(canvas, bx, h < 0 ? yEnd + h : yEnd, thickness, MathF.Abs(h), new ChartPatternFill(pat, Color.White));
                    });
                }
            }
        }
    }

    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series, IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY) { }

    protected override void DrawValueLabels(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        int count = CategoryCount(data);
        if (count == 0) return;
        bool horizontal = Orientation == ChartOrientation.Horizontal;
        float slotSpan = (horizontal ? chartAreaHeight : chartAreaWidth) / count;
        float thickness = MathF.Min(slotSpan * BarWidthRatio, 50f);
        string family = EffectiveFontFamily;

        for (int i = 0; i < count; i++)
        {
            decimal total = Mode is "percent" ? CategoryTotal(data, i) : 1m;
            if (Mode is "percent" && total <= 0) continue;

            decimal posAcc = 0, negAcc = 0;
            float center = (horizontal ? topY : _plotLeft) + i * slotSpan + slotSpan / 2f;

            for (int sIdx = 0; sIdx < data.Series.Count; sIdx++)
            {
                ChartSeries series = data.Series[sIdx];
                if (i >= series.DataPoints.Count) continue;
                ChartDataPoint dp = series.DataPoints[i];
                if (dp.Value == 0) continue;

                decimal val = Mode is "percent" ? (dp.Value / total) * 100m : dp.Value;
                decimal startVal = val > 0 ? posAcc : negAcc;
                decimal endVal = startVal + val;
                if (val > 0) posAcc = endVal; else negAcc = endVal;

                if (ShowValueLabels)
                {
                    float fStart = (float)ChartValueScale.Fraction(startVal, _effectiveMin, _effectiveMax);
                    float fEnd = (float)ChartValueScale.Fraction(endVal, _effectiveMin, _effectiveMax);
                    Color color = ResolveSeriesColor(series);
                    string text = Mode is "percent"
                        ? $"{ChartNumberFormat.Format(val, ValueLabelFormat ?? "F0")}%"
                        : FormatPointValue(dp);

                    if (horizontal)
                    {
                        float xStart = _plotLeft + fStart * chartAreaWidth;
                        float xEnd = _plotLeft + fEnd * chartAreaWidth;
                        DrawBarValueLabel(canvas, xStart, center - thickness / 2f, xEnd - xStart, thickness, text, color, val < 0, 5, xEnd, center);
                    }
                    else
                    {
                        float yStart = baselineY - fStart * chartAreaHeight;
                        float yEnd = baselineY - fEnd * chartAreaHeight;
                        DrawBarValueLabel(canvas, center - thickness / 2f, yEnd, thickness, yStart - yEnd, text, color, val < 0, 5, center, yEnd);
                    }
                }
            }

            if (ShowStackTotals && Mode is "absolute")
            {
                decimal net = posAcc != 0 ? posAcc : negAcc;
                string fmt = StackTotalFormat ?? ValueLabelFormat ?? "G";
                string txt = ChartNumberFormat.ApplyAffixes(ChartNumberFormat.Format(net, fmt), ValueLabelPrefix, ValueLabelSuffix, NegativeStyle);
                float fNet = (float)ChartValueScale.Fraction(net, _effectiveMin, _effectiveMax);
                if (horizontal)
                {
                    float x = _plotLeft + fNet * chartAreaWidth;
                    DrawPlacedLabel(canvas, net < 0 ? x - 6 : x + 6, center - LabelFontSize / 2f, txt, Appearance.TitleColor, LabelFontSize, family, net < 0 ? TextAlignment.Right : TextAlignment.Left);
                }
                else
                {
                    float y = baselineY - fNet * chartAreaHeight;
                    DrawPlacedLabel(canvas, center, net < 0 ? y + 6 : y - 6 - LabelFontSize, txt, Appearance.TitleColor, LabelFontSize, family, TextAlignment.Center);
                }
            }
        }
    }
}
