// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Grouped bar chart that renders multiple bars per category.
/// Extends CartesianChartBase and uses standard ChartData model with ChartSeries.
/// Uses the ICanvas interface for rendering, making it backend-agnostic.
/// </summary>
public class GroupedBarChart : CartesianChartBase
{
    protected override void DrawLegendSwatch(ICanvas canvas, float x, float y, float size, ChartSeries series)
    { ChartBars.Draw(canvas, x, y, size, size, ResolveSeriesColor(series), BarStyle, Appearance, false, x, y, size, size); }
    /// <summary>Value-end rounding and optional opaque linear gradient.</summary>
    public ChartBarStyle BarStyle { get; set; } = new();
    internal override bool UsesStyledGeometry => base.UsesStyledGeometry || (BarStyle.CornerRadius ?? 0) > 0 || BarStyle.Gradient is not null;
    /// <summary>
    /// Width of bars as percentage of available space (0.0 to 1.0, default: 0.6).
    /// </summary>
    public float BarWidthRatio { get; set; } = 0.6f;

    /// <summary>
    /// Gap between groups as percentage of available space (0.0 to 1.0, default: 0.2).
    /// </summary>
    public float GroupGapRatio { get; set; } = 0.2f;

    /// <summary>Bars and gridlines are axis-aligned, so crisp edges render sharper than antialiased.</summary>
    public GroupedBarChart()
    {
        SvgShapeRendering = "crispEdges";
    }

    /// <summary>Creates a grouped bar chart bound to its document data.</summary>
    public GroupedBarChart(ChartData data) : this()
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <summary>
    /// Draws the grouped bar chart elements for a single series.
    /// In vertical mode draws grouped vertical bars; in horizontal mode draws grouped horizontal bars.
    /// </summary>
    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        int seriesCount = data.Series.Count;
        int categoryCount = series.DataPoints.Count;
        int seriesIndex = data.Series.IndexOf(series);
        float origin = ValueOrigin(data, chartAreaWidth, chartAreaHeight, baselineY);

        if (Orientation == ChartOrientation.Horizontal)
        {
            float topAreaY = baselineY - chartAreaHeight;

            for (int i = 0; i < coordinates.Count; i++)
            {
                var p = coordinates[i];
                float barWidth = p.X - origin;
                (float barY, float barHeight) = BarSlot(i, seriesIndex, seriesCount, categoryCount, topAreaY, chartAreaHeight);

                ChartBars.Draw(canvas, origin, barY, barWidth, barHeight, ResolveSeriesColor(series), BarStyle, Appearance, true, LeftPadding, topAreaY, chartAreaWidth, chartAreaHeight);
            }
        }
        else
        {
            for (int i = 0; i < coordinates.Count; i++)
            {
                var p = coordinates[i];
                float barHeight = origin - p.Y;
                (float barX, float barWidth) = BarSlot(i, seriesIndex, seriesCount, categoryCount, LeftPadding, chartAreaWidth);

                ChartBars.Draw(canvas, barX, p.Y, barWidth, barHeight, ResolveSeriesColor(series), BarStyle, Appearance, false, LeftPadding, baselineY - chartAreaHeight, chartAreaWidth, chartAreaHeight);
            }
        }
    }

    /// <summary>
    /// Where one bar sits along the category axis: its leading edge and its thickness.
    /// The axis runs from <paramref name="axisStart"/> for <paramref name="axisLength"/>
    /// (left to right in vertical mode, top to bottom in horizontal mode). Bars and their
    /// value labels both take their position from here, so a label always follows its bar.
    /// </summary>
    private (float Start, float Thickness) BarSlot(int categoryIndex, int seriesIndex, int seriesCount,
        int categoryCount, float axisStart, float axisLength)
    {
        float totalGroupSpace = axisLength / categoryCount;
        float groupPadding = totalGroupSpace * GroupGapRatio;
        float barGroupSize = totalGroupSpace - groupPadding;
        float thickness = (barGroupSize / seriesCount) * BarWidthRatio;

        float groupStart = axisStart + categoryIndex * totalGroupSpace + groupPadding / 2;
        float start = groupStart + seriesIndex * (barGroupSize / seriesCount)
                      + (barGroupSize / seriesCount - thickness) / 2;

        return (start, thickness);
    }

    /// <summary>
    /// Draws each value label against its own bar: centred above it in vertical mode, and to the
    /// right of it, centred on its thickness, in horizontal mode. The base implementation places
    /// labels at the middle of the category, which is right for one mark per category but would
    /// print the labels of every series in a group on top of each other.
    /// </summary>
    protected override void DrawValueLabels(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (!ShowValueLabels) return;

        int seriesCount = data.Series.Count;
        float topAreaY = baselineY - chartAreaHeight;

        for (int seriesIndex = 0; seriesIndex < seriesCount; seriesIndex++)
        {
            ChartSeries series = data.Series[seriesIndex];
            int categoryCount = series.DataPoints.Count;
            IList<Point> coordinates = CalculateSeriesCoordinates(
                series, data, chartAreaWidth, chartAreaHeight, baselineY);
            float offset = GetLabelVerticalOffset(series);

            for (int i = 0; i < categoryCount; i++)
            {
                var point = coordinates[i];
                string text = series.DataPoints[i].FormattedValue;
                bool below = ChartValueScale.IsBelowOrigin(series.DataPoints[i].Value, data.YAxis.MinValue, data.YAxis.MaxValue);

                if (Orientation == ChartOrientation.Horizontal)
                {
                    (float barY, float barHeight) = BarSlot(i, seriesIndex, seriesCount, categoryCount, topAreaY, chartAreaHeight);
                    DrawPlacedLabel(canvas, below ? point.X - offset : point.X + offset, barY + barHeight / 2 - LabelFontSize / 2, text,
                        ResolveSeriesColor(series), LabelFontSize, FontFamily, below ? TextAlignment.Right : TextAlignment.Left);
                }
                else
                {
                    (float barX, float barWidth) = BarSlot(i, seriesIndex, seriesCount, categoryCount, LeftPadding, chartAreaWidth);
                    DrawPlacedLabel(canvas, barX + barWidth / 2, below ? point.Y + offset : point.Y - offset - LabelFontSize, text,
                        ResolveSeriesColor(series), LabelFontSize, FontFamily, TextAlignment.Center);
                }
            }
        }
    }

    /// <summary>
    /// Gets the offset for value labels (simple spacing above/beside bars).
    /// </summary>
    protected override float GetLabelVerticalOffset(ChartSeries series) => 7;
}
