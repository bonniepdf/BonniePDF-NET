// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Renders continuous numeric X/Y scatter plots with optional area-mapped bubble sizing and OLS linear trendlines.
/// </summary>
public class ScatterChart : CartesianChartBase
{
    private decimal _xMin;
    private decimal _xMax = 100;
    private int _xStepCount = 5;
    private string _xTickFormat = "N0";
    private double _minSize;
    private double _maxSize;
    private bool _hasBubbleSize;

    /// <summary>Default radius of scatter point markers in points.</summary>
    public float DefaultMarkerRadius { get; set; } = 4.5f;
    /// <summary>Default geometric shape of scatter markers.</summary>
    public string DefaultMarkerShape { get; set; } = "circle";
    /// <summary>Default fill mode of scatter markers (<c>solid</c> or <c>hollow</c>).</summary>
    public string DefaultMarkerFill { get; set; } = "solid";
    /// <summary>Minimum bubble radius in points when <c>Size</c> values are provided.</summary>
    public float MinBubbleRadius { get; set; } = 4f;
    /// <summary>Maximum bubble radius in points when <c>Size</c> values are provided.</summary>
    public float MaxBubbleRadius { get; set; } = 24f;
    /// <summary>Fill opacity for area-scaled bubbles (0 to 1).</summary>
    public float BubbleFillOpacity { get; set; } = 0.65f;
    /// <summary>Optional ordinary least-squares trendline per series (<c>none</c> or <c>linear</c>).</summary>
    public string ShowTrendline { get; set; } = "none";
    /// <summary>Optional reference lines and shaded bands on the horizontal X axis.</summary>
    public IList<ChartReference> XReferences { get; set; } = [];
    /// <summary>Whether to draw vertical gridlines along the continuous X axis.</summary>
    public bool ShowVerticalGridLines { get; set; } = true;

    internal override bool UsesStyledGeometry => true;
    protected override bool DefaultAutoScale => true;
    protected override bool DefaultIncludeZero => false;
    protected override bool DefaultClipToPlotArea => true;

    public ScatterChart()
    {
        AutoLayout = true;
        ShowValueLabels = false;
    }

    public ScatterChart(ChartData data) : this()
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    private static decimal PointX(ChartSeries series, int index) => series.DataPoints[index].X ?? index;

    protected override void ResolveValueScale(ChartData data)
    {
        base.ResolveValueScale(data);

        double xMin = double.PositiveInfinity, xMax = double.NegativeInfinity;
        double sMin = double.PositiveInfinity, sMax = double.NegativeInfinity;
        bool hasSize = false;
        foreach (var s in data.Series)
        {
            for (int i = 0; i < s.DataPoints.Count; i++)
            {
                double x = (double)PointX(s, i);
                if (x < xMin) xMin = x;
                if (x > xMax) xMax = x;
                if (s.DataPoints[i].Size is decimal sz)
                {
                    if (sz <= 0) throw new ArgumentOutOfRangeException(nameof(data), "ScatterChart bubble sizes must be finite and positive.");
                    hasSize = true;
                    double sd = (double)sz;
                    if (sd < sMin) sMin = sd;
                    if (sd > sMax) sMax = sd;
                }
            }
        }
        _hasBubbleSize = hasSize;
        _minSize = hasSize ? sMin : 0;
        _maxSize = hasSize ? sMax : 0;

        AxisOptions xAxis = data.XAxis;
        bool useAutoX = xAxis.AutoScale || !xAxis.HasMaxValue;
        if (useAutoX && xMin <= xMax)
        {
            var nice = ChartValueScale.ComputeNiceScale(
                xMin, xMax, xAxis.StepCount, xAxis.IncludeZero ?? false, xAxis.Headroom ?? 0.05,
                xAxis.HasMinValue ? (double)xAxis.MinValue : null,
                xAxis.HasMaxValue ? (double)xAxis.MaxValue : null);
            _xMin = nice.MinValue;
            _xMax = nice.MaxValue;
            _xStepCount = nice.StepCount;
            _xTickFormat = xAxis.ValueFormat ?? nice.AutoFormat;
        }
        else
        {
            _xMin = xAxis.MinValue;
            _xMax = xAxis.MaxValue;
            _xStepCount = xAxis.StepCount > 0 ? xAxis.StepCount : 5;
            _xTickFormat = xAxis.ValueFormat ?? "N0";
        }
        ChartValueScale.Check(_xMin, _xMax);
    }

    private float BubbleRadius(decimal? size, float fallback)
    {
        if (!_hasBubbleSize || !size.HasValue) return fallback;
        double minR = Math.Max(1.0, MinBubbleRadius);
        double maxR = Math.Max(minR, MaxBubbleRadius);
        if (_maxSize == _minSize) return (float)((minR + maxR) / 2.0);
        double t = ((double)size.Value - _minSize) / (_maxSize - _minSize);
        return (float)Math.Sqrt(minR * minR + t * (maxR * maxR - minR * minR));
    }

    protected override void DrawGridLines(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        base.DrawGridLines(canvas, data, chartAreaWidth, chartAreaHeight, baselineY, topY);
        if (!ShowVerticalGridLines) return;
        int steps = _xStepCount > 0 ? _xStepCount : 5;
        float stepW = chartAreaWidth / steps;
        for (int i = 1; i <= steps; i++)
        {
            float x = _plotLeft + i * stepW;
            canvas.DrawLine(x, topY, x, baselineY, ThemeGridColor, GridLineWidth);
        }
    }

    protected override void DrawAllSeries(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (XReferences.Count > 0)
            DrawReferences(canvas, data, chartAreaWidth, chartAreaHeight, baselineY, true, XReferences, _xMin, _xMax, true);

        for (int sIdx = 0; sIdx < data.Series.Count; sIdx++)
        {
            ChartSeries series = data.Series[sIdx];
            Color color = ResolveSeriesColor(series);
            string shape = ResolveSeriesMarkerShape(series, sIdx, DefaultMarkerShape);
            string fill = series.Style?.MarkerFill ?? DefaultMarkerFill;
            float baseR = series.Style is not null && series.Style.MarkerRadius != 5f ? series.Style.MarkerRadius : DefaultMarkerRadius;
            Color bg = EffectiveBackgroundColor ?? Color.White;

            List<(double Xv, double Yv, float Px, float Py, float R, bool IsBubble)> items = new(series.DataPoints.Count);
            for (int i = 0; i < series.DataPoints.Count; i++)
            {
                ChartDataPoint dp = series.DataPoints[i];
                decimal xv = PointX(series, i);
                float px = _plotLeft + (float)(ChartValueScale.Fraction(xv, _xMin, _xMax) * (decimal)chartAreaWidth);
                float py = baselineY - (float)(ChartValueScale.Fraction(dp.Value, _effectiveMin, _effectiveMax) * (decimal)chartAreaHeight);
                float r = BubbleRadius(dp.Size, baseR);
                items.Add(((double)xv, (double)dp.Value, px, py, r, dp.Size.HasValue));
            }

            var sorted = new List<(double Xv, double Yv, float Px, float Py, float R, bool IsBubble)>(items);
            sorted.Sort((a, b) => b.R.CompareTo(a.R));
            foreach (var pt in sorted)
            {
                if (pt.IsBubble)
                {
                    canvas.FillCircle(pt.Px, pt.Py, pt.R, color.WithAlpha(BubbleFillOpacity));
                    ChartMarkers.Draw(canvas, pt.Px, pt.Py, pt.R, color, "circle", "hollow", 0, new Color((byte)0, (byte)0, (byte)0, (byte)0), 1f);
                }
                else
                {
                    ChartMarkers.Draw(canvas, pt.Px, pt.Py, pt.R, color, shape, fill, series.Style?.MarkerHaloWidth ?? 0f, bg, 1.5f);
                }
            }

            if (ShowTrendline is "linear" && items.Count >= 2)
            {
                double sumX = 0, sumY = 0, minX = double.PositiveInfinity, maxX = double.NegativeInfinity;
                foreach (var pt in items)
                {
                    sumX += pt.Xv; sumY += pt.Yv;
                    if (pt.Xv < minX) minX = pt.Xv;
                    if (pt.Xv > maxX) maxX = pt.Xv;
                }
                double meanX = sumX / items.Count, meanY = sumY / items.Count;
                double sxx = 0, sxy = 0;
                foreach (var pt in items) { double dx = pt.Xv - meanX; sxx += dx * dx; sxy += dx * (pt.Yv - meanY); }
                if (sxx > 0)
                {
                    double slope = sxy / sxx;
                    double intercept = meanY - slope * meanX;
                    double y1 = slope * minX + intercept;
                    double y2 = slope * maxX + intercept;
                    double x1Px = _plotLeft + ((minX - (double)_xMin) / ((double)_xMax - (double)_xMin)) * chartAreaWidth;
                    double y1Px = baselineY - ((y1 - (double)_effectiveMin) / ((double)_effectiveMax - (double)_effectiveMin)) * chartAreaHeight;
                    double x2Px = _plotLeft + ((maxX - (double)_xMin) / ((double)_xMax - (double)_xMin)) * chartAreaWidth;
                    double y2Px = baselineY - ((y2 - (double)_effectiveMin) / ((double)_effectiveMax - (double)_effectiveMin)) * chartAreaHeight;
                    ChartVector.Stroke(canvas, new ChartStrokePlan([new("M", [x1Px, y1Px]), new("L", [x2Px, y2Px])], color, 1.25, "butt", "miter", 10, [4, 4]));
                }
            }
        }

        if (XReferences.Count > 0)
            DrawReferences(canvas, data, chartAreaWidth, chartAreaHeight, baselineY, false, XReferences, _xMin, _xMax, true);
    }

    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series, IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY) { }

    protected override void DrawValueLabels(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (!ShowValueLabels) return;
        string family = EffectiveFontFamily;
        foreach (ChartSeries series in data.Series)
        {
            Color color = ResolveSeriesColor(series);
            for (int i = 0; i < series.DataPoints.Count; i++)
            {
                ChartDataPoint dp = series.DataPoints[i];
                decimal xv = PointX(series, i);
                float px = _plotLeft + (float)(ChartValueScale.Fraction(xv, _xMin, _xMax) * (decimal)chartAreaWidth);
                float py = baselineY - (float)(ChartValueScale.Fraction(dp.Value, _effectiveMin, _effectiveMax) * (decimal)chartAreaHeight);
                float r = BubbleRadius(dp.Size, DefaultMarkerRadius);
                string text = !string.IsNullOrEmpty(dp.Label) ? dp.Label : FormatPointValue(dp);
                DrawPlacedLabel(canvas, px, py - r - 4 - LabelFontSize, text, color, LabelFontSize, family, TextAlignment.Center);
            }
        }
    }

    protected override void DrawXAxisLabels(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        int steps = _xStepCount > 0 ? _xStepCount : 5;
        float stepW = chartAreaWidth / steps;
        decimal stepVal = (_xMax - _xMin) / steps;
        string family = EffectiveFontFamily;
        for (int i = 0; i <= steps; i++)
        {
            float x = _plotLeft + i * stepW;
            decimal v = _xMin + i * stepVal;
            string label = $"{data.XAxis.Prefix}{ChartNumberFormat.Format(v, _xTickFormat)}{data.XAxis.Suffix}";
            canvas.DrawText(x, baselineY + 12, label, ThemeAxisColor, AxisLabelFontSize, family, TextAlignment.Center);
        }
    }
}
