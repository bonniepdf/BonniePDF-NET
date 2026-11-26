// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Renders cumulative variance bridges (P&amp;L, cash flow, ARR movement) with delta, subtotal, and total bars.
/// </summary>
public class WaterfallChart : CartesianChartBase
{
    private Color? _positiveColor;
    private Color? _negativeColor;
    private Color? _totalColor;

    /// <summary>Override colour for positive delta bars.</summary>
    public Color? PositiveColor
    {
        get => _positiveColor;
        set => _positiveColor = value;
    }

    /// <summary>Override colour for negative delta bars.</summary>
    public Color? NegativeColor
    {
        get => _negativeColor;
        set => _negativeColor = value;
    }

    /// <summary>Override colour for total and subtotal bars.</summary>
    public Color? TotalColor
    {
        get => _totalColor;
        set => _totalColor = value;
    }

    private Color EffectivePositiveColor => _positiveColor ?? Appearance.PositiveColor ?? (Appearance.StyledGeometry ? Color.FromHex("#0d9488") : Color.FromHex("#16a34a"));
    private Color EffectiveNegativeColor => _negativeColor ?? Appearance.NegativeColor ?? (Appearance.StyledGeometry ? Color.FromHex("#e11d48") : Color.FromHex("#dc2626"));
    private Color EffectiveTotalColor => _totalColor ?? Appearance.TotalColor ?? (Appearance.StyledGeometry ? Color.FromHex("#2563eb") : Color.FromHex("#4f46e5"));

    /// <summary>Corner rounding and gradient style applied to waterfall bars.</summary>
    public ChartBarStyle BarStyle { get; set; } = new();
    /// <summary>Whether to draw dashed connector lines between adjacent bars.</summary>
    public bool ShowConnectors { get; set; } = true;
    /// <summary>Fraction of each category slot occupied by the bar (0 to 1).</summary>
    public float BarWidthRatio { get; set; } = 0.6f;
    /// <summary>Whether to prefix positive delta labels with <c>+</c>.</summary>
    public bool ShowSignOnDeltas { get; set; } = true;

    internal override bool UsesStyledGeometry => base.UsesStyledGeometry || (BarStyle.CornerRadius ?? 0) > 0 || BarStyle.Gradient is not null || ShowConnectors;
    protected override bool DefaultAutoScale => true;

    public WaterfallChart()
    {
        SvgShapeRendering = "crispEdges";
        AutoLayout = true;
        ShowLegend = false;
    }

    public WaterfallChart(ChartData data) : this()
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    private readonly record struct WaterfallBar(ChartDataPoint Point, decimal Start, decimal End, decimal DisplayValue, bool IsTotal);

    private static List<WaterfallBar> ComputeBars(ChartData data)
    {
        List<WaterfallBar> bars = new();
        if (data.Series.Count == 0) return bars;
        decimal running = 0;
        foreach (ChartDataPoint dp in data.Series[0].DataPoints)
        {
            if (dp.Kind is "total" or "subtotal")
            {
                decimal val = dp.HasExplicitValue ? dp.Value : running;
                bars.Add(new(dp, 0, val, val, true));
                running = val;
            }
            else
            {
                decimal start = running;
                decimal end = running + dp.Value;
                bars.Add(new(dp, start, end, dp.Value, false));
                running = end;
            }
        }
        return bars;
    }

    protected override (double Min, double Max)? GetValueDataRange(ChartData data)
    {
        var bars = ComputeBars(data);
        if (bars.Count == 0) return null;
        double min = 0, max = 0;
        foreach (var b in bars)
        {
            min = Math.Min(min, Math.Min((double)b.Start, (double)b.End));
            max = Math.Max(max, Math.Max((double)b.Start, (double)b.End));
        }
        return (min, max);
    }

    internal override bool SupportsDepth3d => true;

    protected override void DrawAllSeries(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        var bars = ComputeBars(data);
        if (bars.Count == 0) return;
        bool horizontal = Orientation == ChartOrientation.Horizontal;
        float slot = (horizontal ? chartAreaHeight : chartAreaWidth) / bars.Count;
        float thickness = MathF.Min(slot * BarWidthRatio, 50f);

        for (int i = 0; i < bars.Count; i++)
        {
            var b = bars[i];
            Color color = b.IsTotal ? EffectiveTotalColor : (b.DisplayValue >= 0 ? EffectivePositiveColor : EffectiveNegativeColor);
            float fStart = (float)ChartValueScale.Fraction(b.Start, _effectiveMin, _effectiveMax);
            float fEnd = (float)ChartValueScale.Fraction(b.End, _effectiveMin, _effectiveMax);
            float center = (horizontal ? topY : _plotLeft) + i * slot + slot / 2f;

            if (horizontal)
            {
                float xStart = _plotLeft + fStart * chartAreaWidth;
                float xEnd = _plotLeft + fEnd * chartAreaWidth;
                MarkGroup(canvas, data, data.Series[0], i, new ChartGroupAnchor(xStart, center, "x"), () =>
                {
                    DrawBarDepth(canvas, xStart, center - thickness / 2f, xEnd - xStart, thickness, color);
                    ChartBars.Draw(canvas, xStart, center - thickness / 2f, xEnd - xStart, thickness, color, BarStyle, Appearance, true, _plotLeft, topY, chartAreaWidth, chartAreaHeight);
                }, b.DisplayValue);
                if (ShowConnectors && i < bars.Count - 1)
                {
                    float nextCenter = topY + (i + 1) * slot + slot / 2f;
                    ChartVector.Stroke(canvas, new ChartStrokePlan([new("M", [xEnd, center + thickness / 2f]), new("L", [xEnd, nextCenter - thickness / 2f])], ThemeAxisColor, 0.75, "butt", "miter", 10, [3, 3]));
                }
            }
            else
            {
                float yStart = baselineY - fStart * chartAreaHeight;
                float yEnd = baselineY - fEnd * chartAreaHeight;
                MarkGroup(canvas, data, data.Series[0], i, new ChartGroupAnchor(center, yStart, "y"), () =>
                {
                    DrawBarDepth(canvas, center - thickness / 2f, yEnd, thickness, yStart - yEnd, color);
                    ChartBars.Draw(canvas, center - thickness / 2f, yEnd, thickness, yStart - yEnd, color, BarStyle, Appearance, false, _plotLeft, topY, chartAreaWidth, chartAreaHeight);
                }, b.DisplayValue);
                if (ShowConnectors && i < bars.Count - 1)
                {
                    float nextCenter = _plotLeft + (i + 1) * slot + slot / 2f;
                    ChartVector.Stroke(canvas, new ChartStrokePlan([new("M", [center + thickness / 2f, yEnd]), new("L", [nextCenter - thickness / 2f, yEnd])], ThemeAxisColor, 0.75, "butt", "miter", 10, [3, 3]));
                }
            }
        }
    }

    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series, IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY) { }

    protected override void DrawValueLabels(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (!ShowValueLabels) return;
        var bars = ComputeBars(data);
        if (bars.Count == 0) return;
        bool horizontal = Orientation == ChartOrientation.Horizontal;
        float slot = (horizontal ? chartAreaHeight : chartAreaWidth) / bars.Count;
        float thickness = MathF.Min(slot * BarWidthRatio, 50f);

        for (int i = 0; i < bars.Count; i++)
        {
            var b = bars[i];
            Color color = b.IsTotal ? EffectiveTotalColor : (b.DisplayValue >= 0 ? EffectivePositiveColor : EffectiveNegativeColor);
            string raw = FormatPointValue(b.Point, b.DisplayValue);
            string text = !b.IsTotal && ShowSignOnDeltas && b.DisplayValue > 0 && !raw.StartsWith('+') ? $"+{raw}" : raw;
            float fStart = (float)ChartValueScale.Fraction(b.Start, _effectiveMin, _effectiveMax);
            float fEnd = (float)ChartValueScale.Fraction(b.End, _effectiveMin, _effectiveMax);
            float center = (horizontal ? topY : _plotLeft) + i * slot + slot / 2f;
            bool below = b.End < b.Start;

            if (horizontal)
            {
                float xStart = _plotLeft + fStart * chartAreaWidth;
                float xEnd = _plotLeft + fEnd * chartAreaWidth;
                DrawBarValueLabel(canvas, xStart, center - thickness / 2f, xEnd - xStart, thickness, text, color, below, 6, xEnd, center);
            }
            else
            {
                float yStart = baselineY - fStart * chartAreaHeight;
                float yEnd = baselineY - fEnd * chartAreaHeight;
                DrawBarValueLabel(canvas, center - thickness / 2f, yEnd, thickness, yStart - yEnd, text, color, below, 6, center, yEnd);
            }
        }
    }
}
