// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Base class for Cartesian charts (charts with X and Y axes).
/// Provides common functionality for drawing axes, grid lines, labels, and legends.
/// Uses the Template Method pattern to ensure consistent drawing order across all derived charts.
/// </summary>
public abstract class CartesianChartBase : ChartBase
{
    private readonly List<ChartLabelBox> _labelBoxes = new();
    private ChartData? _renderData;
    protected void DrawPlacedLabel(ICanvas canvas, float x, float y, string text, Color color, float size, string family, TextAlignment align)
    {
        if (Appearance.MeasuredLabels) ChartLabels.Draw(canvas, _labelBoxes, Width, Height, x, y, text, color, size, family, align);
        else canvas.DrawText(x, y, text, color, size, family, align);
    }

    private Color ThemeAxisColor => Appearance.AxisColor;
    private Color ThemeGridColor => Appearance.GridLineColor;

    protected CartesianChartBase()
    {
    }

    protected CartesianChartBase(ChartData data) : base(data)
    {
    }

    // Layout constants (relative to chart height)
    protected const float TitleAreaHeight = 60; // Space for title at top
    protected const float BottomPadding = 70; // Space for x-axis labels and legend

    /// <summary>
    /// Top band reserved when <see cref="ChartData.Title"/> is empty. Just enough
    /// headroom for value labels sitting above the tallest data point, so a
    /// titleless chart's plot area stretches up instead of leaving the title gap empty.
    /// </summary>
    protected const float TopPadding = 20;

    /// <summary>
    /// Bottom band reserved when no legend is drawn there. Holds only the
    /// category/value tick labels (<see cref="DrawXAxisLabels"/> at
    /// <c>baselineY + 15</c>, font <see cref="AxisLabelFontSize"/>), so a
    /// legend-free chart's plot area stretches down instead of reserving the full
    /// <see cref="BottomPadding"/> band.
    /// </summary>
    protected const float AxisLabelBand = 30;

    protected const float LeftPadding = 80; // Space for y-axis labels and legend
    protected const float RightPadding = 40; // Right margin
    protected const float AxisLineWidth = 1f;
    protected const float GridLineWidth = 0.5f;
    protected const float AxisLabelFontSize = 10;
    protected const float LegendFontSize = 11;

    // Legacy styling constants; the active theme supplies the colours actually drawn.
    protected static readonly Color AxisColor = ChartTheme.Legacy.AxisColor;
    protected static readonly Color GridLineColor = ChartTheme.Legacy.GridLineColor;

    /// <summary>Category-axis presentation owned by this chart.</summary>
    public AxisOptions XAxis
    {
        get => Data.XAxis;
        set => Data.XAxis = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Value-axis presentation owned by this chart.</summary>
    public AxisOptions YAxis
    {
        get => Data.YAxis;
        set => Data.YAxis = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Font family for labels
    /// </summary>
    public string FontFamily { get; set; } = StandardFonts.Helvetica;

    /// <summary>
    /// Whether to show value labels above each data point
    /// </summary>
    public bool ShowValueLabels { get; set; } = true;

    /// <summary>
    /// Font size for value labels above data points
    /// </summary>
    public float LabelFontSize { get; set; } = 9;

    /// <summary>
    /// Whether to show a legend for multiple series (default: true).
    /// </summary>
    public bool ShowLegend { get; set; } = true;

    /// <summary>
    /// Controls whether the chart renders with vertical or horizontal orientation.
    /// In horizontal mode, categories run along the Y-axis (left) and values along the X-axis (bottom).
    /// Setting this on any chart type enables horizontal rendering without needing a separate class.
    /// </summary>
    public ChartOrientation Orientation { get; set; } = ChartOrientation.Vertical;

    /// <summary>
    /// Draws the chart content using a Template Method pattern.
    /// This ensures consistent drawing order across all Cartesian chart types:
    /// 1. Grid lines (behind everything)
    /// 2. Value-axis labels (behind data)
    /// 3. Chart-specific data elements (bars, lines, etc.)
    /// 4. Value labels (on top of data)
    /// 5. Category-axis labels
    /// 6. Axis lines (on top of everything to prevent overlap)
    /// 7. Legend (for multi-series charts)
    /// </summary>
    protected sealed override void DrawContent(ICanvas canvas, ChartData data)
    {
        ChartData? previousData=_renderData;
        try {
        _labelBoxes.Clear();
        _renderData = data;
        if (data.Series.Count == 0) return;
        ChartValueScale.Check(data.YAxis.MinValue, data.YAxis.MaxValue);

        (float chartAreaWidth, float chartAreaHeight, float baselineY, float topY) = CalculateLayout(data);

        // 1. Draw grid lines first (behind everything)
        if (data.YAxis.ShowGridLines)
            DrawGridLines(canvas, data, chartAreaWidth, chartAreaHeight, baselineY, topY);

        // 2. Draw value-axis labels (behind data)
        DrawYAxisLabels(canvas, data, chartAreaHeight, baselineY, topY);

        // 3. Draw value-axis legend
        if (!string.IsNullOrEmpty(data.YAxis.Legend))
            DrawYAxisLegend(canvas, data.YAxis, chartAreaWidth, chartAreaHeight, topY, baselineY);

        // 4. Draw chart-specific data elements
        DrawAllSeries(canvas, data, chartAreaWidth, chartAreaHeight, baselineY, topY);

        // 5. Draw value labels on top of data
        if (ShowValueLabels)
            DrawValueLabels(canvas, data, chartAreaWidth, chartAreaHeight, baselineY, topY);

        // 6. Draw category-axis labels
        DrawXAxisLabels(canvas, data, chartAreaWidth, chartAreaHeight, baselineY);

        // 7. Draw category-axis legend
        if (!string.IsNullOrEmpty(data.XAxis.Legend))
            DrawXAxisLegend(canvas, data.XAxis.Legend, chartAreaWidth, chartAreaHeight, baselineY);

        // 8. Draw axis lines LAST (on top of everything to prevent overlap)
        if (Appearance.AxisFrame) DrawAxisLines(canvas, chartAreaWidth, baselineY, topY);
        else
        {
            float baseline = Orientation == ChartOrientation.Horizontal ? baselineY : ValueOrigin(data, chartAreaWidth, chartAreaHeight, baselineY);
            canvas.DrawLine(LeftPadding, baseline, LeftPadding + chartAreaWidth, baseline, ThemeAxisColor, Appearance.AxisLineWidth);
        }

        // 9. Draw legend for multi-series charts
        if (ShowLegend && data.Series.Count > 1)
            DrawLegend(canvas, data, chartAreaWidth);

        } finally {_renderData=previousData;_labelBoxes.Clear();}
    }

    /// <summary>
    /// Draws all series in the chart.
    /// </summary>
    protected virtual void DrawAllSeries(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        foreach (ChartSeries series in data.Series)
        {
            IList<Point> coordinates = CalculateSeriesCoordinates(
                series, data, chartAreaWidth, chartAreaHeight, baselineY);

            DrawSeriesElements(canvas, data, series, coordinates,
                chartAreaWidth, chartAreaHeight, baselineY, topY);
        }
    }

    /// <summary>
    /// Calculates screen-space coordinates for a series.
    /// In vertical mode: category index → X position, normalised value → Y position.
    /// In horizontal mode: category index → Y position, normalised value → X position.
    /// Derived charts receive pre-computed Point coordinates and draw relative to them,
    /// so most derived chart drawing logic works correctly in both orientations.
    /// </summary>
    protected IList<Point> CalculateSeriesCoordinates(
        ChartSeries series, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        int dataPointCount = series.DataPoints.Count;
        List<Point> coordinates = new();

        if (Orientation == ChartOrientation.Horizontal)
        {
            float topY = baselineY - chartAreaHeight;
            float totalSpace = chartAreaHeight / dataPointCount;

            for (int i = 0; i < dataPointCount; i++)
            {
                float y = topY + i * totalSpace + totalSpace / 2;
                decimal normalizedValue = ChartValueScale.Fraction(series.DataPoints[i].Value, data.YAxis.MinValue, data.YAxis.MaxValue);
                float x = LeftPadding + (float)(normalizedValue * (decimal)chartAreaWidth);
                coordinates.Add(new Point(x, y));
            }
        }
        else
        {
            float totalSpace = chartAreaWidth / dataPointCount;

            for (int i = 0; i < dataPointCount; i++)
            {
                float x = LeftPadding + i * totalSpace + totalSpace / 2;
                decimal normalizedValue = ChartValueScale.Fraction(series.DataPoints[i].Value, data.YAxis.MinValue, data.YAxis.MaxValue);
                float y = baselineY - (float)(normalizedValue * (decimal)chartAreaHeight);
                coordinates.Add(new Point(x, y));
            }
        }

        return coordinates;
    }

    /// <summary>
    /// Where bars, area fills and stems start: value zero limited to the axis range. A y coordinate in
    /// vertical mode and an x coordinate in horizontal mode.
    /// </summary>
    internal float ValueOrigin(ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        decimal fraction = ChartValueScale.OriginFraction(data.YAxis.MinValue, data.YAxis.MaxValue);
        return Orientation == ChartOrientation.Horizontal
            ? LeftPadding + (float)(fraction * (decimal)chartAreaWidth)
            : baselineY - (float)(fraction * (decimal)chartAreaHeight);
    }

    /// <summary>
    /// Draws the connecting line between consecutive data points for a series.
    /// </summary>
    internal static void DrawConnectingLine(ICanvas canvas, IList<Point> coordinates, Color color, float lineWidth)
    {
        if (coordinates.Count < 2) return;

        for (int i = 0; i < coordinates.Count - 1; i++)
        {
            var p1 = coordinates[i];
            var p2 = coordinates[i + 1];
            canvas.DrawLine(p1.X, p1.Y, p2.X, p2.Y, color, lineWidth);
        }
    }

    /// <summary>
    /// Draws the chart-specific elements for a single series.
    /// Derived classes must implement this to provide their specific drawing logic for each series.
    /// </summary>
    /// <param name="canvas">The canvas to draw to</param>
    /// <param name="data">The chart data containing all series</param>
    /// <param name="series">The specific series to draw</param>
    /// <param name="coordinates">Pre-calculated screen-space coordinates for this series' data points</param>
    /// <param name="chartAreaWidth">Width of the chart drawing area</param>
    /// <param name="chartAreaHeight">Height of the chart drawing area</param>
    /// <param name="baselineY">Y coordinate of the bottom of the chart area</param>
    /// <param name="topY">Y coordinate of the top of the chart area</param>
    protected abstract void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY);

    /// <summary>
    /// Draws value labels beyond each point, away from the value origin: above or right of a value at
    /// or above it, below or left of a value below it.
    /// </summary>
    protected virtual void DrawValueLabels(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (!ShowValueLabels) return;

        foreach (ChartSeries series in data.Series)
        {
            IList<Point> coordinates = CalculateSeriesCoordinates(
                series, data, chartAreaWidth, chartAreaHeight, baselineY);

            Color labelColor = ResolveSeriesColor(series);
            float offset = GetLabelVerticalOffset(series);

            for (int i = 0; i < series.DataPoints.Count; i++)
            {
                var point = coordinates[i];
                string text = series.DataPoints[i].FormattedValue;
                bool below = ChartValueScale.IsBelowOrigin(series.DataPoints[i].Value, data.YAxis.MinValue, data.YAxis.MaxValue);

                if (Orientation == ChartOrientation.Horizontal)
                    DrawPlacedLabel(canvas, below ? point.X - offset : point.X + offset, point.Y - LabelFontSize / 2, text,
                        labelColor, LabelFontSize, FontFamily, below ? TextAlignment.Right : TextAlignment.Left);
                else
                    DrawPlacedLabel(canvas, point.X, below ? point.Y + offset : point.Y - offset - LabelFontSize, text,
                        labelColor, LabelFontSize, FontFamily, TextAlignment.Center);
            }
        }
    }

    /// <summary>
    /// Gets the offset (in points) to apply when positioning value labels near data points.
    /// In vertical mode this is the gap above the data point; in horizontal mode to the right.
    /// Default implementation returns 7. Derived classes can override for custom behaviour.
    /// </summary>
    protected virtual float GetLabelVerticalOffset(ChartSeries series) => 7;

    /// <summary>
    /// Draws the legend showing all series names and colors.
    /// </summary>
    protected virtual void DrawLegendSwatch(ICanvas canvas, float x, float y, float size, ChartSeries series) => canvas.FillRect(x, y, size, size, ResolveSeriesColor(series));
    protected virtual void DrawLegend(ICanvas canvas, ChartData data, float chartAreaWidth)
    {
        if (Appearance.FlowLegend)
        {
            float x = LeftPadding, y = Height - 42;
            foreach (var series in data.Series)
            {
                float w = canvas.MeasureText(series.Name, 11, FontFamily).Width + 32;
                if (x + w > Width - 16 && x > LeftPadding) { x = LeftPadding; y += 18; }
                if (y + 12 > Height - 4) break;
                DrawLegendSwatch(canvas, x, y, 12, series);
                canvas.DrawText(x + 18, y, series.Name, ThemeAxisColor, 11, FontFamily, TextAlignment.Left); x += w;
            }
            return;
        }
        float legendY = Height - 40;
        float legendStartX = LeftPadding;
        float legendItemWidth = 150;
        float colorBoxSize = 12;

        for (int i = 0; i < data.Series.Count; i++)
        {
            ChartSeries series = data.Series[i];
            float x = legendStartX + i * legendItemWidth;

            DrawLegendSwatch(canvas, x, legendY, colorBoxSize, series);

            canvas.DrawText(x + colorBoxSize + 5, legendY + colorBoxSize / 2 - LegendFontSize / 2,
                series.Name, ThemeAxisColor, LegendFontSize, FontFamily, TextAlignment.Left);
        }
    }

    /// <summary>
    /// Draws grid lines across the chart area.
    /// In vertical mode: horizontal lines at value intervals.
    /// In horizontal mode: vertical lines at value intervals.
    /// </summary>
    protected void DrawGridLines(ICanvas canvas, ChartData data, float chartAreaWidth, float chartAreaHeight,
        float baselineY, float topY)
    {
        int stepCount = data.YAxis.StepCount;
        if (stepCount <= 0) stepCount = 5;

        if (Orientation == ChartOrientation.Horizontal)
        {
            float stepWidth = chartAreaWidth / stepCount;
            for (int i = 1; i <= stepCount; i++)
            {
                float x = LeftPadding + i * stepWidth;
                canvas.DrawLine(x, topY, x, baselineY, ThemeGridColor, GridLineWidth);
            }
        }
        else
        {
            float stepHeight = chartAreaHeight / stepCount;
            for (int i = 1; i <= stepCount; i++)
            {
                float y = baselineY - i * stepHeight;
                float x1 = LeftPadding;
                float x2 = LeftPadding + chartAreaWidth;
                canvas.DrawLine(x1, y, x2, y, ThemeGridColor, GridLineWidth);
            }
        }
    }

    /// <summary>
    /// Draws the X and Y axis lines.
    /// These are in the same physical positions for both orientations:
    /// the left vertical line and the bottom horizontal line.
    /// </summary>
    protected static void DrawAxisLines(ICanvas canvas, float chartAreaWidth, float baselineY, float topY)
    {
        // The X and Y axis lines are axis-aligned, so render them with crisp edges
        // on every chart type (a sharp, fully-opaque hairline rather than a blurred
        // antialiased one) while leaving any diagonal/curved series geometry smooth.
        canvas.BeginCrispEdges();

        // Left vertical line
        canvas.DrawLine(LeftPadding, topY, LeftPadding, baselineY, AxisColor, AxisLineWidth);

        // Bottom horizontal line
        canvas.DrawLine(LeftPadding, baselineY, LeftPadding + chartAreaWidth, baselineY, AxisColor, AxisLineWidth);

        canvas.EndCrispEdges();
    }

    /// <summary>
    /// Draws value-axis labels.
    /// In vertical mode: numbers on the left side at each step.
    /// In horizontal mode: numbers along the bottom at each step.
    /// </summary>
    protected void DrawYAxisLabels(ICanvas canvas, ChartData data, float chartAreaHeight, float baselineY, float topY)
    {
        int stepCount = data.YAxis.StepCount;
        if (stepCount <= 0) stepCount = 5;

        decimal minValue = data.YAxis.MinValue;
        decimal maxValue = data.YAxis.MaxValue;
        decimal range = maxValue - minValue;
        decimal stepValue = range / stepCount;

        if (Orientation == ChartOrientation.Horizontal)
        {
            float chartAreaWidth = Width - LeftPadding - RightPadding;
            float stepWidth = chartAreaWidth / stepCount;

            for (int i = 0; i <= stepCount; i++)
            {
                float x = LeftPadding + i * stepWidth;
                decimal value = minValue + i * stepValue;
                string label = $"{data.YAxis.Prefix}{ChartNumberFormat.Format(value, "N0")}{data.YAxis.Suffix}";
                canvas.DrawText(x, baselineY + 8, label, ThemeAxisColor, AxisLabelFontSize, FontFamily,
                    TextAlignment.Center);
            }
        }
        else
        {
            float stepHeight = chartAreaHeight / stepCount;

            for (int i = 0; i <= stepCount; i++)
            {
                float y = baselineY - i * stepHeight;
                decimal value = minValue + i * stepValue;
                string label = $"{data.YAxis.Prefix}{ChartNumberFormat.Format(value, "N0")}{data.YAxis.Suffix}";
                canvas.DrawText(LeftPadding - 8, y - AxisLabelFontSize / 2, label, ThemeAxisColor, AxisLabelFontSize,
                    FontFamily, TextAlignment.Right);
            }
        }
    }

    /// <summary>
    /// Draws the value-axis legend.
    /// In vertical mode: on the left side (horizontal or rotated text).
    /// In horizontal mode: centred below the chart as the bottom-axis legend.
    /// </summary>
    protected void DrawYAxisLegend(ICanvas canvas, AxisOptions yAxis,
        float chartAreaWidth, float chartAreaHeight, float topY, float baselineY)
    {
        if (Orientation == ChartOrientation.Horizontal)
        {
            float x = LeftPadding + chartAreaWidth / 2;
            float y = Height - 15;
            canvas.DrawText(x, y, yAxis.Legend, ThemeAxisColor, LegendFontSize, FontFamily, TextAlignment.Center);
        }
        else if (yAxis.VerticalLegend)
        {
            float x = 15;
            float textWidth = canvas.MeasureText(yAxis.Legend, LegendFontSize, FontFamily).Width;
            float y = topY + chartAreaHeight / 2 + textWidth / 2 - LegendFontSize;
            canvas.DrawTextRotated(x, y, yAxis.Legend, ThemeAxisColor, LegendFontSize, FontFamily, 90);
        }
        else
        {
            float x = 10;
            float y = topY + chartAreaHeight / 2;
            canvas.DrawText(x, y, yAxis.Legend, ThemeAxisColor, LegendFontSize, FontFamily, TextAlignment.Left);
        }
    }

    /// <summary>
    /// Draws the category-axis legend.
    /// In vertical mode: centred below the x-axis labels.
    /// In horizontal mode: rotated on the left side.
    /// </summary>
    protected void DrawXAxisLegend(ICanvas canvas, string legend,
        float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        if (Orientation == ChartOrientation.Horizontal)
        {
            float x = 15;
            float textWidth = canvas.MeasureText(legend, LegendFontSize, FontFamily).Width;
            float y = TitleAreaHeight + chartAreaHeight / 2 + textWidth / 2 - LegendFontSize;
            canvas.DrawTextRotated(x, y, legend, ThemeAxisColor, LegendFontSize, FontFamily, 90);
        }
        else
        {
            float x = LeftPadding + chartAreaWidth / 2;
            // Center the legend in the space below the x-axis tick labels (within the
            // bottom-padding band), then shift up by half the font size to account for
            // DrawText's `y` representing the top of the text box rather than its visual center.
            float labelsBottom = baselineY + 15 + AxisLabelFontSize;
            float marginBottom = baselineY + BottomPadding;
            float y = (labelsBottom + marginBottom) / 2 - LegendFontSize / 2;
            canvas.DrawText(x, y, legend, ThemeAxisColor, LegendFontSize, FontFamily, TextAlignment.Center);
        }
    }

    /// <summary>
    /// Draws category-axis labels (data point labels).
    /// In vertical mode: below each point along the X-axis.
    /// In horizontal mode: to the left of each point along the Y-axis.
    /// </summary>
    protected virtual void DrawXAxisLabels(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        if (data.Series.Count == 0) return;

        ChartSeries firstSeries = data.Series[0];
        int dataPointCount = firstSeries.DataPoints.Count;

        if (Orientation == ChartOrientation.Horizontal)
        {
            float topY = baselineY - chartAreaHeight;
            float totalSpace = chartAreaHeight / dataPointCount;

            for (int i = 0; i < dataPointCount; i++)
            {
                float y = topY + i * totalSpace + totalSpace / 2;
                string label = firstSeries.DataPoints[i].Label;
                DrawPlacedLabel(canvas, LeftPadding - 8, y - AxisLabelFontSize / 2, label,
                    ThemeAxisColor, AxisLabelFontSize, FontFamily, TextAlignment.Right);
            }
        }
        else
        {
            float labelY = baselineY + 15;
            float totalSpace = chartAreaWidth / dataPointCount;

            for (int i = 0; i < dataPointCount; i++)
            {
                float x = LeftPadding + i * totalSpace + totalSpace / 2;
                string label = firstSeries.DataPoints[i].Label;
                DrawPlacedLabel(canvas, x, labelY, label, ThemeAxisColor, AxisLabelFontSize, FontFamily, TextAlignment.Center);
            }
        }
    }

    /// <summary>
    /// Gets the default color for the first series.
    /// Derived classes can override this to provide their specific default color.
    /// </summary>
    protected virtual Color GetDefaultSeriesColor() => Color.FromHex("#4f46e5");

    /// <summary>
    /// The colour a series is drawn in: its own when the author set one, otherwise the chart's own colour
    /// (<c>BarColor</c>, <c>LineColor</c>, <c>AreaColor</c>, <c>DotColor</c>, <c>LollipopColor</c>).
    /// </summary>
    protected Color ResolveSeriesColor(ChartSeries series)
    {
        if (series.HasColor) return series.Color;
        Color color = GetDefaultSeriesColor();
        return Appearance.SeriesColor(color, (_renderData ?? Data).Series.IndexOf(series));
    }

    /// <summary>
    /// Reports whether a legend occupies the bottom band for this chart+data, which
    /// requires reserving the full <see cref="BottomPadding"/>. Mirrors where each
    /// legend is actually drawn: the multi-series legend sits at <c>Height - 40</c>;
    /// in horizontal mode the value-axis legend runs along the bottom; in vertical
    /// mode the category-axis legend sits below the tick labels. (A vertical-mode
    /// value-axis legend is drawn on the left, so it does not reserve bottom space.)
    /// </summary>
    protected virtual bool ReservesBottomLegendBand(ChartData data)
    {
        if (ShowLegend && data.Series.Count > 1) return true;
        if (Orientation == ChartOrientation.Horizontal) return !string.IsNullOrEmpty(data.YAxis.Legend);
        return !string.IsNullOrEmpty(data.XAxis.Legend);
    }

    /// <summary>
    /// Calculates the chart layout dimensions.
    ///
    /// The top and bottom bands are content-aware so a chart's plot area fills the
    /// available box instead of reserving space for absent decorations. The title
    /// band collapses to <see cref="TopPadding"/> when there is no title, and the
    /// bottom band collapses to <see cref="AxisLabelBand"/> when no legend is drawn
    /// there. <c>baselineY</c> (and therefore the axis, tick labels, and legends)
    /// depends only on the bottom band, so collapsing the title band shifts nothing
    /// below it — it only lets the bars/series grow taller.
    /// </summary>
    protected (float chartAreaWidth, float chartAreaHeight, float baselineY, float topY) CalculateLayout(ChartData data)
    {
        float topBand = string.IsNullOrEmpty(data.Title) ? TopPadding : TitleAreaHeight;
        float bottomBand = ReservesBottomLegendBand(data) ? BottomPadding : AxisLabelBand;

        float chartAreaWidth = Width - LeftPadding - RightPadding;
        float chartAreaHeight = Height - topBand - bottomBand;
        float baselineY = topBand + chartAreaHeight;
        float topY = topBand;

        return (chartAreaWidth, chartAreaHeight, baselineY, topY);
    }
}
