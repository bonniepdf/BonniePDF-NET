// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>Open-high-low-close prices per period, as candles or OHLC bars. Reads the first series; its points carry <c>Open</c>, <c>High</c>, <c>Low</c> and <c>Close</c>.</summary>
public class CandlestickChart : CartesianChartBase
{
    /// <summary><c>candle</c> (default): a body from open to close with wicks. <c>ohlc</c>: a vertical line with open and close ticks.</summary>
    public string Style { get; set; } = "candle";
    /// <summary>Draw rising candles hollow.</summary>
    public bool HollowUp { get; set; }
    /// <summary>The fraction of a category slot that a candle body fills (<c>0.6</c>).</summary>
    public float BodyWidthRatio { get; set; } = 0.6f;
    /// <summary>Colour of rising periods (close at or above open). Unset uses the theme's positive colour.</summary>
    public Color? UpColor { get; set; }
    /// <summary>Colour of falling periods. Unset uses the theme's negative colour.</summary>
    public Color? DownColor { get; set; }

    /// <summary>Creates an empty candlestick chart.</summary>
    public CandlestickChart() { ShowValueLabels = false; ShowLegend = false; }

    /// <summary>Creates a candlestick chart for <paramref name="data"/>.</summary>
    public CandlestickChart(ChartData data) : base(data) { ShowValueLabels = false; ShowLegend = false; }

    internal override bool SupportsTimeAxis => true;

    /// <summary>A volume pane beneath the price pane, sharing its columns.</summary>
    public CandlestickVolume? Volume { get; set; }

    internal override float GetExtraBottomBand(ChartData data) => Volume is null ? 0 : Height * Volume.PaneRatio + 22;

    /// <inheritdoc />
    protected override void DrawAfterSeries(ICanvas canvas, ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (Volume is not { } v) return;
        float ratio = v.PaneRatio;
        if (!(ratio > 0 && ratio <= 0.5f)) throw new InvalidOperationException("Candlestick volume paneRatio must be above 0 and at most 0.5.");
        int index = v.Series is int n ? n : v.Series is string name ? data.Series.ToList().FindIndex(s => string.Equals(s.Name, name, StringComparison.Ordinal)) : -1;
        if (index <= 0 || index >= data.Series.Count) throw new InvalidOperationException($"Candlestick volume names a missing series '{v.Series}'.");
        IList<ChartDataPoint> prices = data.Series[0].DataPoints;
        IList<ChartDataPoint> volumes = data.Series[index].DataPoints;
        if (volumes.Count != prices.Count) throw new InvalidOperationException("Candlestick volume needs one point per candle.");
        float paneHeight = Height * ratio;
        float top = baselineY + 32;
        float bottom = top + paneHeight;
        decimal maxVolume = volumes.Max(p => p.Value);
        if (!(maxVolume > 0)) return;
        float slot = chartAreaWidth / Math.Max(1, prices.Count);
        float width = Math.Max(1, slot * BodyWidthRatio);
        string family = EffectiveFontFamily;
        canvas.DrawLine(_plotLeft, bottom, _plotLeft + chartAreaWidth, bottom, ThemeAxisColor, 1);
        canvas.DrawText(_plotLeft - 8, top - 5, ChartNumberFormat.Format(maxVolume, "K1"), ThemeAxisColor, 9, family, TextAlignment.Right);
        canvas.DrawText(_plotLeft - 8, top + paneHeight / 2 - 5, "Volume", ThemeAxisColor, 9, family, TextAlignment.Right);
        for (int i = 0; i < prices.Count; i++)
        {
            var o = Ohlc(prices[i]);
            Color color = (o.Close >= o.Open ? Up : Down).WithAlpha(0.6f);
            float cx = TimeAxisActive ? TimeX(prices[i], chartAreaWidth) : _plotLeft + slot * (i + 0.5f);
            float h = (float)(volumes[i].Value / maxVolume) * paneHeight;
            canvas.FillRect(cx - width / 2, bottom - h, width, h, color);
        }
    }

    internal override bool SupportsLogAxis => true;

    /// <inheritdoc />
    protected override bool DefaultAutoScale => true;

    /// <inheritdoc />
    protected override bool DefaultIncludeZero => false;

    private Color Up => UpColor ?? Appearance.PositiveColor ?? Color.FromHex("#15803d");
    private Color Down => DownColor ?? Appearance.NegativeColor ?? Color.FromHex("#b91c1c");

    private static (double Open, double High, double Low, double Close) Ohlc(ChartDataPoint point)
    {
        if (point.Open is not { } open || point.High is not { } high || point.Low is not { } low || point.Close is not { } close)
            throw new InvalidOperationException("Candlestick points need open, high, low and close.");
        if (!(low <= Math.Min(open, close) && Math.Max(open, close) <= high)) throw new InvalidOperationException("Candlestick prices must satisfy low <= open, close <= high.");
        return ((double)open, (double)high, (double)low, (double)close);
    }

    /// <inheritdoc />
    protected override (double Min, double Max)? GetValueDataRange(ChartData data)
    {
        if (data.Series.Count == 0 || data.Series[0].DataPoints.Count == 0) return null;
        double min = double.PositiveInfinity, max = double.NegativeInfinity;
        foreach (ChartDataPoint p in data.Series[0].DataPoints)
        {
            var o = Ohlc(p);
            min = Math.Min(min, o.Low);
            max = Math.Max(max, o.High);
        }
        return (min, max);
    }

    /// <inheritdoc />
    protected override void DrawAllSeries(ICanvas canvas, ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (data.Series.Count == 0) return;
        DrawSeriesElements(canvas, data, data.Series[0], [], chartAreaWidth, chartAreaHeight, baselineY, topY);
    }

    /// <inheritdoc />
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series, IList<Point> coordinates, float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (!(BodyWidthRatio > 0 && BodyWidthRatio <= 1)) throw new InvalidOperationException("bodyWidthRatio must be above 0 and at most 1.");
        bool vertical = Orientation != ChartOrientation.Horizontal;
        Point At(double c, double h) => vertical ? new Point((float)c, (float)h) : new Point((float)h, (float)c);
        int count = series.DataPoints.Count;
        double slot = (vertical ? chartAreaWidth : chartAreaHeight) / (double)Math.Max(1, count);
        double axisStart = vertical ? _plotLeft : baselineY - chartAreaHeight;
        double H(double v)
        {
            double f = (double)Fraction((decimal)v);
            return vertical ? baselineY - f * chartAreaHeight : _plotLeft + f * chartAreaWidth;
        }
        Color bg = EffectiveBackgroundColor ?? Color.White;
        for (int i = 0; i < count; i++)
        {
            var o = Ohlc(series.DataPoints[i]);
            double c = TimeAxisActive ? TimeX(series.DataPoints[i], chartAreaWidth) : axisStart + slot * (i + 0.5);
            bool rising = o.Close >= o.Open;
            Color color = rising ? Up : Down;
            void Line(Point a, Point b, float width) => canvas.DrawLine(a.X, a.Y, b.X, b.Y, color, width);
            if (string.Equals(Style, "ohlc", StringComparison.Ordinal))
            {
                double tick = Math.Max(2, slot * BodyWidthRatio / 2);
                Line(At(c, H(o.Low)), At(c, H(o.High)), 1.5f);
                Line(At(c - tick, H(o.Open)), At(c, H(o.Open)), 1.5f);
                Line(At(c, H(o.Close)), At(c + tick, H(o.Close)), 1.5f);
                continue;
            }
            double body = Math.Max(1, slot * BodyWidthRatio);
            Line(At(c, H(o.Low)), At(c, H(o.High)), 1.25f);
            double top = Math.Min(H(o.Open), H(o.Close));
            double bottom = Math.Max(H(o.Open), H(o.Close));
            double hh = Math.Max(1, bottom - top);
            Point[] corners = vertical
                ? [At(c - body / 2, top), At(c + body / 2, top), At(c + body / 2, top + hh), At(c - body / 2, top + hh)]
                : [At(top, c - body / 2), At(top + hh, c - body / 2), At(top + hh, c + body / 2), At(top, c + body / 2)];
            List<ChartPathCommand> commands = corners.Select((p, k) => new ChartPathCommand(k == 0 ? "M" : "L", [p.X, p.Y])).ToList();
            commands.Add(new ChartPathCommand("Z", []));
            ChartVector.Fill(canvas, new ChartPathPlan(commands, rising && HollowUp ? bg : color, null));
            ChartVector.Stroke(canvas, new ChartStrokePlan(commands, color, 1.25, "butt", "miter", 10));
        }
    }
}
