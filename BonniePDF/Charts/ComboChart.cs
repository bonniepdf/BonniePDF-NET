// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Renders mixed bar, area, and line series on primary and optional secondary Y axes.
/// </summary>
public class ComboChart : CartesianChartBase
{
    private decimal _secMin;
    private decimal _secMax = 100;
    private string _secTickFormat = "N0";
    private bool _hasSecondary;

    /// <summary>Optional right-hand secondary Y axis options for series bound to <c>secondary</c>.</summary>
    public AxisOptions? SecondaryYAxis { get; set; }
    /// <summary>Corner rounding and gradient style applied to bar series.</summary>
    public ChartBarStyle BarStyle { get; set; } = new();
    /// <summary>Fraction of each category slot occupied by bar series (0 to 1).</summary>
    public float BarWidthRatio { get; set; } = 0.6f;
    /// <summary>Relative gap between grouped bar series within a category (0 to 1).</summary>
    public float GroupGapRatio { get; set; } = 0.1f;

    internal override bool UsesStyledGeometry => true;
    protected override bool DefaultAutoScale => true;
    protected override bool DefaultClipToPlotArea => true;
    protected override bool SupportsInlineLegend => true;

    public ComboChart()
    {
        AutoLayout = true;
    }

    public ComboChart(ChartData data) : this()
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    private bool UsesSecondary(ChartData data) =>
        SecondaryYAxis is not null || data.Series.Any(s => s.ValueAxis is "secondary");

    protected override (double Min, double Max)? GetValueDataRange(ChartData data)
    {
        double min = double.PositiveInfinity, max = double.NegativeInfinity;
        foreach (var s in data.Series)
        {
            if (s.ValueAxis is "secondary") continue;
            foreach (var dp in s.DataPoints)
            {
                double v = (double)dp.Value;
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }
        return min <= max ? (min, max) : base.GetValueDataRange(data);
    }

    protected override void ResolveValueScale(ChartData data)
    {
        if (Orientation == ChartOrientation.Horizontal)
            throw new NotSupportedException("ComboChart only supports vertical orientation.");
        base.ResolveValueScale(data);
        _hasSecondary = UsesSecondary(data);
        if (!_hasSecondary) return;

        AxisOptions secAxis = SecondaryYAxis ?? new AxisOptions { AutoScale = true };
        bool useAuto = secAxis.AutoScale || !secAxis.HasMaxValue;
        if (useAuto)
        {
            double min = double.PositiveInfinity, max = double.NegativeInfinity;
            foreach (var s in data.Series)
            {
                if (s.ValueAxis is not "secondary") continue;
                foreach (var dp in s.DataPoints)
                {
                    double v = (double)dp.Value;
                    if (v < min) min = v;
                    if (v > max) max = v;
                }
            }
            if (min <= max)
            {
                var nice = ChartValueScale.ComputeNiceScale(
                    min, max, _effectiveStepCount, secAxis.IncludeZero ?? true,
                    secAxis.Headroom ?? (ShowValueLabels ? 0.05 : 0),
                    secAxis.HasMinValue ? (double)secAxis.MinValue : null,
                    secAxis.HasMaxValue ? (double)secAxis.MaxValue : null);
                _secMin = nice.MinValue;
                _secMax = nice.MaxValue;
                _secTickFormat = secAxis.ValueFormat ?? nice.AutoFormat;
                ChartValueScale.Check(_secMin, _secMax);
                return;
            }
        }
        _secMin = secAxis.MinValue;
        _secMax = secAxis.MaxValue;
        _secTickFormat = secAxis.ValueFormat ?? "N0";
        ChartValueScale.Check(_secMin, _secMax);
    }

    protected override float GetExtraRightMargin(ICanvas canvas, ChartData data)
    {
        if (!_hasSecondary) return 0f;
        AxisOptions secAxis = SecondaryYAxis ?? new AxisOptions();
        int steps = _effectiveStepCount > 0 ? _effectiveStepCount : 5;
        decimal stepVal = (_secMax - _secMin) / steps;
        string family = EffectiveFontFamily;
        float maxW = 0;
        for (int i = 0; i <= steps; i++)
        {
            string lbl = $"{secAxis.Prefix}{ChartNumberFormat.Format(_secMin + i * stepVal, _secTickFormat)}{secAxis.Suffix}";
            float w = canvas.MeasureText(lbl, AxisLabelFontSize, family).Width;
            if (w > maxW) maxW = w;
        }
        return MathF.Ceiling(maxW + 18 + (!string.IsNullOrEmpty(secAxis.Legend) ? 22 : 0));
    }

    private List<Point> SeriesCoords(ChartSeries series, float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        int n = series.DataPoints.Count;
        List<Point> pts = new(n);
        if (n == 0) return pts;
        decimal min = series.ValueAxis is "secondary" ? _secMin : _effectiveMin;
        decimal max = series.ValueAxis is "secondary" ? _secMax : _effectiveMax;
        float slot = chartAreaWidth / n;
        for (int i = 0; i < n; i++)
        {
            float x = _plotLeft + i * slot + slot / 2f;
            float y = baselineY - (float)(ChartValueScale.Fraction(series.DataPoints[i].Value, min, max) * (decimal)chartAreaHeight);
            pts.Add(new Point(x, y));
        }
        return pts;
    }

    protected override void DrawAllSeries(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        // 1. Area series
        for (int sIdx = 0; sIdx < data.Series.Count; sIdx++)
        {
            ChartSeries series = data.Series[sIdx];
            if (series.SeriesType is not "area") continue;
            var coords = SeriesCoords(series, chartAreaWidth, chartAreaHeight, baselineY);
            if (coords.Count < 2) continue;
            decimal min = series.ValueAxis is "secondary" ? _secMin : _effectiveMin;
            decimal max = series.ValueAxis is "secondary" ? _secMax : _effectiveMax;
            float origin = baselineY - (float)(ChartValueScale.OriginFraction(min, max) * (decimal)chartAreaHeight);
            Color color = ResolveSeriesColor(series);
            float opacity = series.Style?.FillOpacity ?? 0.3f;
            string curve = series.Style?.Curve ?? "linear";
            var curveCmds = ChartCurves.BuildCurveCommands(coords, curve, false, moveToFirst: false);
            List<ChartPathCommand> cmds = [new("M", [coords[0].X, origin])];
            cmds.AddRange(curveCmds);
            cmds.Add(new("L", [coords[^1].X, origin]));
            cmds.Add(new("Z", []));
            ChartVector.Fill(canvas, new ChartPathPlan(cmds, color.WithAlpha(opacity), null));
            DrawSeriesLine(canvas, coords, color, series.Style?.LineWidth ?? 2f, curve, ResolveSeriesDashPattern(series, sIdx));
        }

        // 2. Bar series
        List<ChartSeries> barSeries = data.Series.Where(s => s.SeriesType is "bar").ToList();
        for (int bIdx = 0; bIdx < barSeries.Count; bIdx++)
        {
            ChartSeries series = barSeries[bIdx];
            int count = series.DataPoints.Count;
            if (count == 0) continue;
            var coords = SeriesCoords(series, chartAreaWidth, chartAreaHeight, baselineY);
            decimal min = series.ValueAxis is "secondary" ? _secMin : _effectiveMin;
            decimal max = series.ValueAxis is "secondary" ? _secMax : _effectiveMax;
            float origin = baselineY - (float)(ChartValueScale.OriginFraction(min, max) * (decimal)chartAreaHeight);
            Color color = ResolveSeriesColor(series);
            float totalGroupSpace = chartAreaWidth / count;
            float groupPadding = totalGroupSpace * GroupGapRatio;
            float barGroupSize = totalGroupSpace - groupPadding;
            float thickness = (barGroupSize / barSeries.Count) * BarWidthRatio;

            for (int i = 0; i < count; i++)
            {
                Point p = coords[i];
                float groupStart = _plotLeft + i * totalGroupSpace + groupPadding / 2f;
                float barX = groupStart + bIdx * (barGroupSize / barSeries.Count) + (barGroupSize / barSeries.Count - thickness) / 2f;
                ChartBars.Draw(canvas, barX, p.Y, thickness, origin - p.Y, color, BarStyle, Appearance, false, _plotLeft, topY, chartAreaWidth, chartAreaHeight);
            }
        }

        // 3. Line series + markers
        for (int sIdx = 0; sIdx < data.Series.Count; sIdx++)
        {
            ChartSeries series = data.Series[sIdx];
            if (series.SeriesType is not "line") continue;
            var coords = SeriesCoords(series, chartAreaWidth, chartAreaHeight, baselineY);
            Color color = ResolveSeriesColor(series);
            float lw = series.Style?.LineWidth ?? 2f;
            string curve = series.Style?.Curve ?? "linear";
            DrawSeriesLine(canvas, coords, color, lw, curve, ResolveSeriesDashPattern(series, sIdx));
            if (series.Style?.ShowMarkers ?? true)
            {
                float radius = series.Style?.MarkerRadius ?? 4f;
                string shape = ResolveSeriesMarkerShape(series, sIdx, "circle");
                string fill = series.Style?.MarkerFill ?? "hollow";
                float halo = series.Style?.MarkerHaloWidth ?? 0f;
                Color bg = EffectiveBackgroundColor ?? Color.White;
                foreach (Point p in coords)
                    ChartMarkers.Draw(canvas, p.X, p.Y, radius, color, shape, fill, halo, bg, lw);
            }
        }
    }

    protected override void DrawAfterSeries(ICanvas canvas, ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (!_hasSecondary) return;
        AxisOptions secAxis = SecondaryYAxis ?? new AxisOptions();
        int steps = _effectiveStepCount > 0 ? _effectiveStepCount : 5;
        float stepH = chartAreaHeight / steps;
        decimal stepVal = (_secMax - _secMin) / steps;
        float rightX = _plotLeft + chartAreaWidth;
        string family = EffectiveFontFamily;

        for (int i = 0; i <= steps; i++)
        {
            float y = baselineY - i * stepH;
            string lbl = $"{secAxis.Prefix}{ChartNumberFormat.Format(_secMin + i * stepVal, _secTickFormat)}{secAxis.Suffix}";
            canvas.DrawText(rightX + 8, y - AxisLabelFontSize / 2f, lbl, ThemeAxisColor, AxisLabelFontSize, family, TextAlignment.Left);
        }
        if (!string.IsNullOrEmpty(secAxis.Legend))
        {
            float tw = canvas.MeasureText(secAxis.Legend, LegendFontSize, family).Width;
            float ly = topY + chartAreaHeight / 2f - tw / 2f;
            canvas.DrawTextRotated(Width - 14, ly, secAxis.Legend, ThemeAxisColor, LegendFontSize, family, -90);
        }
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
            var coords = SeriesCoords(series, chartAreaWidth, chartAreaHeight, baselineY);
            Color color = ResolveSeriesColor(series);
            for (int i = 0; i < series.DataPoints.Count; i++)
            {
                Point p = coords[i];
                string text = FormatPointValue(series.DataPoints[i]);
                DrawPlacedLabel(canvas, p.X, p.Y - 10 - LabelFontSize, text, color, LabelFontSize, family, TextAlignment.Center);
            }
        }
    }
}
