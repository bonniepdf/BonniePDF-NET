// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Grouped bar chart that renders multiple bars per category.
/// </summary>
public class GroupedBarChart : CartesianChartBase
{
    protected override void DrawLegendSwatch(ICanvas canvas, float x, float y, float size, ChartSeries series)
    {
        Color color = ResolveSeriesColor(series);
        int idx = (_renderData ?? Data).Series.IndexOf(series);
        string pat = ResolveSeriesPattern(series, idx);
        ChartBars.Draw(canvas, x, y, size, size, color, BarStyle, Appearance, false, x, y, size, size);
        if (pat is not "none") ChartPatterns.FillRect(canvas, x, y, size, size, new ChartPatternFill(pat, Color.White));
    }

    public ChartBarStyle BarStyle { get; set; } = new();
    internal override bool UsesStyledGeometry => base.UsesStyledGeometry || (BarStyle.CornerRadius ?? 0) > 0 || BarStyle.Gradient is not null;
    public float BarWidthRatio { get; set; } = 0.6f;
    public float GroupGapRatio { get; set; } = 0.2f;

    public GroupedBarChart()
    {
        SvgShapeRendering = "crispEdges";
    }

    public GroupedBarChart(ChartData data) : this()
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        int seriesCount = data.Series.Count;
        int categoryCount = series.DataPoints.Count;
        if (categoryCount == 0) return;
        int seriesIndex = data.Series.IndexOf(series);
        float origin = ValueOrigin(data, chartAreaWidth, chartAreaHeight, baselineY);
        Color color = ResolveSeriesColor(series);
        string pat = ResolveSeriesPattern(series, seriesIndex);

        if (Orientation == ChartOrientation.Horizontal)
        {
            float topAreaY = baselineY - chartAreaHeight;
            for (int i = 0; i < coordinates.Count; i++)
            {
                var p = coordinates[i];
                float barWidth = p.X - origin;
                (float barY, float barHeight) = BarSlot(i, seriesIndex, seriesCount, categoryCount, topAreaY, chartAreaHeight);
                ChartBars.Draw(canvas, origin, barY, barWidth, barHeight, color, BarStyle, Appearance, true, _plotLeft, topAreaY, chartAreaWidth, chartAreaHeight);
                if (pat is not "none")
                {
                    float rx = barWidth < 0 ? origin + barWidth : origin;
                    ChartPatterns.FillRect(canvas, rx, barY, MathF.Abs(barWidth), barHeight, new ChartPatternFill(pat, Color.White));
                }
            }
        }
        else
        {
            for (int i = 0; i < coordinates.Count; i++)
            {
                var p = coordinates[i];
                float barHeight = origin - p.Y;
                (float barX, float barWidth) = BarSlot(i, seriesIndex, seriesCount, categoryCount, _plotLeft, chartAreaWidth);
                ChartBars.Draw(canvas, barX, p.Y, barWidth, barHeight, color, BarStyle, Appearance, false, _plotLeft, baselineY - chartAreaHeight, chartAreaWidth, chartAreaHeight);
                if (pat is not "none")
                {
                    float ry = barHeight < 0 ? p.Y + barHeight : p.Y;
                    ChartPatterns.FillRect(canvas, barX, ry, barWidth, MathF.Abs(barHeight), new ChartPatternFill(pat, Color.White));
                }
            }
        }
    }

    private (float Start, float Thickness) BarSlot(int categoryIndex, int seriesIndex, int seriesCount,
        int categoryCount, float axisStart, float axisLength)
    {
        float totalGroupSpace = axisLength / categoryCount;
        float groupPadding = totalGroupSpace * GroupGapRatio;
        float barGroupSize = totalGroupSpace - groupPadding;
        float thickness = (barGroupSize / seriesCount) * BarWidthRatio;
        float groupStart = axisStart + categoryIndex * totalGroupSpace + groupPadding / 2;
        float start = groupStart + seriesIndex * (barGroupSize / seriesCount) + (barGroupSize / seriesCount - thickness) / 2;
        return (start, thickness);
    }

    protected override void DrawValueLabels(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (!ShowValueLabels) return;
        int seriesCount = data.Series.Count;
        float topAreaY = baselineY - chartAreaHeight;
        float origin = ValueOrigin(data, chartAreaWidth, chartAreaHeight, baselineY);

        for (int seriesIndex = 0; seriesIndex < seriesCount; seriesIndex++)
        {
            ChartSeries series = data.Series[seriesIndex];
            int categoryCount = series.DataPoints.Count;
            if (categoryCount == 0) continue;
            IList<Point> coordinates = CalculateSeriesCoordinates(series, data, chartAreaWidth, chartAreaHeight, baselineY);
            float offset = GetLabelVerticalOffset(series);
            Color color = ResolveSeriesColor(series);

            for (int i = 0; i < categoryCount; i++)
            {
                var point = coordinates[i];
                string text = FormatPointValue(series.DataPoints[i]);
                bool below = ChartValueScale.IsBelowOrigin(series.DataPoints[i].Value, _effectiveMin, _effectiveMax);

                if (Orientation == ChartOrientation.Horizontal)
                {
                    (float barY, float barHeight) = BarSlot(i, seriesIndex, seriesCount, categoryCount, topAreaY, chartAreaHeight);
                    DrawBarValueLabel(canvas, origin, barY, point.X - origin, barHeight, text, color, below, offset, point.X, barY + barHeight / 2);
                }
                else
                {
                    (float barX, float barWidth) = BarSlot(i, seriesIndex, seriesCount, categoryCount, _plotLeft, chartAreaWidth);
                    DrawBarValueLabel(canvas, barX, point.Y, barWidth, origin - point.Y, text, color, below, offset, barX + barWidth / 2, point.Y);
                }
            }
        }
    }

    protected override float GetLabelVerticalOffset(ChartSeries series) => 7;
}
