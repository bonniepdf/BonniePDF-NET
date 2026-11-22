// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Base class for Cartesian charts (charts with X and Y axes).
/// </summary>
public abstract class CartesianChartBase : ChartBase
{
    private readonly List<ChartLabelBox> _labelBoxes = new();
    protected ChartData? _renderData;
    protected float _plotLeft = LeftPadding;
    protected float _plotRight = RightPadding;
    protected decimal _effectiveMin;
    protected decimal _effectiveMax = 10000;
    protected int _effectiveStepCount = 5;
    protected string _effectiveTickFormat = "N0";

    protected void DrawPlacedLabel(ICanvas canvas, float x, float y, string text, Color color, float size, string family, TextAlignment align)
    {
        if (Appearance.MeasuredLabels) ChartLabels.Draw(canvas, _labelBoxes, Width, Height, x, y, text, color, size, family, align);
        else canvas.DrawText(x, y, text, color, size, family, align);
    }

    protected Color ThemeAxisColor => Appearance.AxisColor;
    protected Color ThemeGridColor => Appearance.GridLineColor;

    protected CartesianChartBase() { }
    protected CartesianChartBase(ChartData data) : base(data) { }

    protected const float TitleAreaHeight = 60;
    protected const float BottomPadding = 70;
    protected const float TopPadding = 20;
    protected const float AxisLabelBand = 30;
    protected const float LeftPadding = 80;
    protected const float RightPadding = 40;
    protected const float AxisLineWidth = 1f;
    protected const float GridLineWidth = 0.5f;
    protected const float AxisLabelFontSize = 10;
    protected const float LegendFontSize = 11;

    protected static readonly Color AxisColor = ChartTheme.Legacy.AxisColor;
    protected static readonly Color GridLineColor = ChartTheme.Legacy.GridLineColor;

    public AxisOptions XAxis
    {
        get => Data.XAxis;
        set => Data.XAxis = value ?? throw new ArgumentNullException(nameof(value));
    }

    public AxisOptions YAxis
    {
        get => Data.YAxis;
        set => Data.YAxis = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string FontFamily { get; set; } = StandardFonts.Helvetica;
    public bool ShowValueLabels { get; set; } = true;
    public float LabelFontSize { get; set; } = 9;
    public string? ValueLabelFormat { get; set; }
    public string ValueLabelPrefix { get; set; } = "";
    public string ValueLabelSuffix { get; set; } = "";
    public string ValueLabelPosition { get; set; } = "outside";
    public bool ShowLegend { get; set; } = true;
    public string LegendPosition { get; set; } = "bottom";
    public ChartOrientation Orientation { get; set; } = ChartOrientation.Vertical;
    public IList<ChartReference> References { get; set; } = [];
    public bool? ClipToPlotArea { get; set; }

    protected virtual bool DefaultAutoScale => false;
    protected virtual bool DefaultIncludeZero => true;
    protected virtual bool DefaultClipToPlotArea => false;
    protected virtual bool SupportsInlineLegend => false;
    protected bool EffectiveClipToPlotArea => ClipToPlotArea ?? DefaultClipToPlotArea;
    protected string EffectiveFontFamily => ResolveFontFamily(FontFamily);

    private protected virtual bool DrawCompact(ICanvas canvas, ChartData data) => false;

    protected virtual (double Min, double Max)? GetValueDataRange(ChartData data)
    {
        double min = double.PositiveInfinity;
        double max = double.NegativeInfinity;
        foreach (var series in data.Series)
        {
            foreach (var dp in series.DataPoints)
            {
                double v = (double)dp.Value;
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }
        return min <= max ? (min, max) : null;
    }

    protected virtual void ResolveValueScale(ChartData data)
    {
        AxisOptions axis = data.YAxis;
        bool useAuto = axis.AutoScale || (DefaultAutoScale && !axis.HasMaxValue);
        if (useAuto)
        {
            var range = GetValueDataRange(data);
            if (range.HasValue)
            {
                var nice = ChartValueScale.ComputeNiceScale(
                    range.Value.Min,
                    range.Value.Max,
                    axis.StepCount,
                    axis.IncludeZero ?? DefaultIncludeZero,
                    axis.Headroom ?? (ShowValueLabels ? 0.05 : 0),
                    axis.HasMinValue ? (double)axis.MinValue : null,
                    axis.HasMaxValue ? (double)axis.MaxValue : null);
                _effectiveMin = nice.MinValue;
                _effectiveMax = nice.MaxValue;
                _effectiveStepCount = nice.StepCount;
                _effectiveTickFormat = axis.ValueFormat ?? nice.AutoFormat;
                return;
            }
        }
        _effectiveMin = axis.MinValue;
        _effectiveMax = axis.MaxValue;
        _effectiveStepCount = axis.StepCount > 0 ? axis.StepCount : 5;
        _effectiveTickFormat = axis.ValueFormat ?? "N0";
    }

    protected string FormatPointValue(ChartDataPoint dp, decimal? overrideValue = null)
    {
        decimal val = overrideValue ?? dp.Value;
        string fmt = dp.HasExplicitLabelFormat ? dp.LabelFormat : (ValueLabelFormat ?? dp.LabelFormat);
        return $"{ValueLabelPrefix}{ChartNumberFormat.Format(val, fmt)}{ValueLabelSuffix}";
    }

    protected virtual float GetExtraRightMargin(ICanvas canvas, ChartData data) => 0f;

    protected virtual void DrawAfterSeries(ICanvas canvas, ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY, float topY) { }

    protected bool ShouldDrawLegend(ChartData data) => ShowLegend && LegendPosition is not "none" && data.Series.Count > 1;

    protected string EffectiveLegendPosition() => LegendPosition is "inline" && !SupportsInlineLegend ? "top" : LegendPosition;

    protected sealed override void DrawContent(ICanvas canvas, ChartData data)
    {
        ChartData? previousData = _renderData;
        float prevLeft = _plotLeft;
        float prevRight = _plotRight;
        try
        {
            _labelBoxes.Clear();
            _renderData = data;
            if (data.Series.Count == 0) return;
            ResolveValueScale(data);
            ChartValueScale.Check(_effectiveMin, _effectiveMax);

            if (DrawCompact(canvas, data)) return;
            var layout = CalculateLayoutWithCanvas(canvas, data);
            _plotLeft = layout.PlotLeft;
            _plotRight = layout.PlotRight;

            DrawReferences(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY, bands: true);

            if (data.YAxis.ShowGridLines && Appearance.GridLineStyle is not "none")
                DrawGridLines(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY, layout.TopY);

            DrawYAxisLabels(canvas, data, layout.ChartAreaHeight, layout.BaselineY, layout.TopY);

            if (!string.IsNullOrEmpty(data.YAxis.Legend))
                DrawYAxisLegend(canvas, data.YAxis, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.TopY, layout.BaselineY);

            if (EffectiveClipToPlotArea)
            {
                ChartVector.PushClipRect(canvas, _plotLeft, layout.TopY, layout.ChartAreaWidth, layout.ChartAreaHeight);
                try
                {
                    DrawAllSeries(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY, layout.TopY);
                }
                finally { ChartVector.PopClip(canvas); }
            }
            else
            {
                DrawAllSeries(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY, layout.TopY);
            }

            DrawAfterSeries(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY, layout.TopY);
            DrawReferences(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY, bands: false);

            if (ShowValueLabels)
                DrawValueLabels(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY, layout.TopY);

            DrawXAxisLabels(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY);

            if (!string.IsNullOrEmpty(data.XAxis.Legend))
                DrawXAxisLegend(canvas, data.XAxis.Legend, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY);

            if (Appearance.AxisFrame) DrawAxisFrameLines(canvas, layout.ChartAreaWidth, layout.BaselineY, layout.TopY);
            else
            {
                float baseline = Orientation == ChartOrientation.Horizontal ? layout.BaselineY : ValueOrigin(data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY);
                canvas.DrawLine(_plotLeft, baseline, _plotLeft + layout.ChartAreaWidth, baseline, ThemeAxisColor, Appearance.AxisLineWidth);
            }

            if (ShouldDrawLegend(data))
                DrawLegendInPosition(canvas, data, layout);
        }
        finally
        {
            _renderData = previousData;
            _plotLeft = prevLeft;
            _plotRight = prevRight;
            _labelBoxes.Clear();
        }
    }

    protected void DrawReferences(
        ICanvas canvas, ChartData data, float width, float height, float baseline, bool bands,
        IList<ChartReference>? customRefs = null, decimal? customMin = null, decimal? customMax = null, bool? forceHorizontal = null)
    {
        bool horizontal = forceHorizontal ?? (Orientation == ChartOrientation.Horizontal);
        decimal min = customMin ?? _effectiveMin;
        decimal max = customMax ?? _effectiveMax;
        IList<ChartReference> list = customRefs ?? References;
        foreach (ChartReference reference in list)
        {
            if (reference.EndValue.HasValue != bands) continue;
            float a = (float)ChartValueScale.Fraction(reference.Value, min, max);
            if (reference.EndValue is decimal end)
            {
                float b = (float)ChartValueScale.Fraction(end, min, max);
                float low = MathF.Max(0, MathF.Min(a, b));
                float high = MathF.Min(1, MathF.Max(a, b));
                if (!(high > low)) continue;
                float rx = horizontal ? _plotLeft + low * width : _plotLeft;
                float ry = horizontal ? baseline - height : baseline - high * height;
                float rw = horizontal ? (high - low) * width : width;
                float rh = horizontal ? height : (high - low) * height;
                canvas.FillRect(rx, ry, rw, rh, reference.Color);
                if (!string.IsNullOrEmpty(reference.Label))
                    DrawReferenceLabel(canvas, reference, rx, ry, rw, rh, true, horizontal);
            }
            else if (a >= 0 && a <= 1 && float.IsFinite(reference.Thickness) && reference.Thickness > 0)
            {
                float x = horizontal ? _plotLeft + a * width : _plotLeft;
                float y = baseline - (horizontal ? height : a * height);
                float len = horizontal ? height : width;
                DrawReferenceLine(canvas, x, y, len, horizontal, reference);
                if (!string.IsNullOrEmpty(reference.Label))
                    DrawReferenceLabel(canvas, reference, x, y, horizontal ? 0 : len, horizontal ? len : 0, false, horizontal);
            }
        }
    }

    private void DrawReferenceLabel(ICanvas canvas, ChartReference reference, float x, float y, float w, float h, bool isBand, bool verticalLine)
    {
        string label = reference.Label!;
        const float fontSize = 8.5f;
        string family = EffectiveFontFamily;
        Size measured = canvas.MeasureText(label, fontSize, family);
        string pos = string.IsNullOrEmpty(reference.LabelPosition) ? "end" : reference.LabelPosition;
        string placement = reference.LabelPlacement ?? (isBand ? "inside" : "above");
        Color textColor = reference.LabelColor ?? (reference.Color.ScA < 0.5f
            ? new Color(reference.Color.R, reference.Color.G, reference.Color.B, (byte)255)
            : reference.Color);

        float tx, ty;
        TextAlignment align;

        if (isBand)
        {
            if (pos is "start") { tx = x + 6; align = TextAlignment.Left; }
            else if (pos is "center") { tx = x + w / 2; align = TextAlignment.Center; }
            else { tx = x + w - 6; align = TextAlignment.Right; }
            ty = placement is "below" ? y + h + 3 : placement is "above" ? y - fontSize - 3 : y + 4;
        }
        else if (verticalLine)
        {
            tx = placement is "below" ? x - 4 : x + 4;
            align = placement is "below" ? TextAlignment.Right : TextAlignment.Left;
            ty = pos is "start" ? y + h - fontSize - 4 : pos is "center" ? y + h / 2 - fontSize / 2 : y + 4;
        }
        else
        {
            if (pos is "start") { tx = x + 4; align = TextAlignment.Left; }
            else if (pos is "center") { tx = x + w / 2; align = TextAlignment.Center; }
            else { tx = x + w - 4; align = TextAlignment.Right; }
            ty = placement is "below" ? y + 4 : y - fontSize - 4;
        }

        if (reference.LabelBackgroundColor.HasValue)
        {
            float px = align == TextAlignment.Right ? tx - measured.Width - 4 : align == TextAlignment.Center ? tx - measured.Width / 2 - 4 : tx - 4;
            float py = ty - 2;
            float pw = measured.Width + 8;
            float ph = fontSize + 4;
            const double r = 2;
            List<ChartPathCommand> cmds =
            [
                new("M", [px + r, py]),
                new("L", [px + pw - r, py]),
                new("L", [px + pw, py + r]),
                new("L", [px + pw, py + ph - r]),
                new("L", [px + pw - r, py + ph]),
                new("L", [px + r, py + ph]),
                new("L", [px, py + ph - r]),
                new("L", [px, py + r]),
                new("Z", []),
            ];
            ChartVector.Fill(canvas, new ChartPathPlan(cmds, reference.LabelBackgroundColor.Value, null));
        }

        canvas.DrawText(tx, ty, label, textColor, fontSize, family, align);
    }

    private static void DrawReferenceLine(ICanvas canvas, float x, float y, float length, bool vertical, ChartReference reference)
    {
        void Draw(float from, float to)
        {
            if (vertical) canvas.DrawLine(x, y + from, x, y + to, reference.Color, reference.Thickness);
            else canvas.DrawLine(x + from, y, x + to, y, reference.Color, reference.Thickness);
        }

        if (reference.DashPattern is not { Length: > 0 } pattern)
        {
            Draw(0, length);
            return;
        }

        foreach (float dash in pattern)
        {
            if (!float.IsFinite(dash) || dash <= 0 || length / dash > 100_000)
                throw new ArgumentOutOfRangeException(nameof(reference), "Reference dash lengths must be finite and positive and allow at most 100,000 segments.");
        }
        float from = 0;
        for (int i = 0; from < length; i++)
        {
            float to = MathF.Min(length, from + pattern[i % pattern.Length]);
            if (i % 2 == 0) Draw(from, to);
            from = to;
        }
    }

    protected virtual void DrawAllSeries(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        foreach (ChartSeries series in data.Series)
        {
            IList<Point> coordinates = CalculateSeriesCoordinates(series, data, chartAreaWidth, chartAreaHeight, baselineY);
            DrawSeriesElements(canvas, data, series, coordinates, chartAreaWidth, chartAreaHeight, baselineY, topY);
        }
    }

    protected IList<Point> CalculateSeriesCoordinates(
        ChartSeries series, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        int dataPointCount = series.DataPoints.Count;
        List<Point> coordinates = new();
        if (dataPointCount == 0) return coordinates;

        if (Orientation == ChartOrientation.Horizontal)
        {
            float topY = baselineY - chartAreaHeight;
            float totalSpace = chartAreaHeight / dataPointCount;
            for (int i = 0; i < dataPointCount; i++)
            {
                float y = topY + i * totalSpace + totalSpace / 2;
                decimal normalizedValue = ChartValueScale.Fraction(series.DataPoints[i].Value, _effectiveMin, _effectiveMax);
                float x = _plotLeft + (float)(normalizedValue * (decimal)chartAreaWidth);
                coordinates.Add(new Point(x, y));
            }
        }
        else
        {
            float totalSpace = chartAreaWidth / dataPointCount;
            for (int i = 0; i < dataPointCount; i++)
            {
                float x = _plotLeft + i * totalSpace + totalSpace / 2;
                decimal normalizedValue = ChartValueScale.Fraction(series.DataPoints[i].Value, _effectiveMin, _effectiveMax);
                float y = baselineY - (float)(normalizedValue * (decimal)chartAreaHeight);
                coordinates.Add(new Point(x, y));
            }
        }

        return coordinates;
    }

    internal float ValueOrigin(ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        decimal fraction = ChartValueScale.OriginFraction(_effectiveMin, _effectiveMax);
        return Orientation == ChartOrientation.Horizontal
            ? _plotLeft + (float)(fraction * (decimal)chartAreaWidth)
            : baselineY - (float)(fraction * (decimal)chartAreaHeight);
    }

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

    protected void DrawSeriesLine(ICanvas canvas, IList<Point> coordinates, Color color, float lineWidth, string curve = "linear", float[]? dashPattern = null)
    {
        if (coordinates.Count < 2) return;
        bool hasDash = dashPattern is { Length: > 0 };
        if (curve is "linear" && !hasDash)
        {
            DrawConnectingLine(canvas, coordinates, color, lineWidth);
            return;
        }
        double[]? dashDouble = null;
        if (hasDash)
        {
            dashDouble = new double[dashPattern!.Length];
            for (int i = 0; i < dashPattern.Length; i++) dashDouble[i] = dashPattern[i];
            ChartCurves.ValidateDashPattern(dashDouble, ChartCurves.PolylineLength((IReadOnlyList<Point>)coordinates));
        }
        var cmds = ChartCurves.BuildCurveCommands((IReadOnlyList<Point>)coordinates, curve, Orientation == ChartOrientation.Horizontal);
        ChartVector.Stroke(canvas, new ChartStrokePlan(cmds, color, lineWidth, hasDash ? "butt" : "round", "round", 10, dashDouble));
    }

    protected abstract void DrawSeriesElements(
        ICanvas canvas, ChartData data, ChartSeries series, IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY);

    protected void DrawBarValueLabel(
        ICanvas canvas, float barX, float barY, float barW, float barH, string text, Color barColor,
        bool below, float offset, float pointX, float pointY, bool outsideAlignCenter = true)
    {
        string family = EffectiveFontFamily;
        if (ValueLabelPosition is "inside" or "auto")
        {
            Size measured = canvas.MeasureText(text, LabelFontSize, family);
            float absW = MathF.Abs(barW);
            float absH = MathF.Abs(barH);
            if (absW >= measured.Width + 6 && absH >= LabelFontSize + 6)
            {
                float rx = barW < 0 ? barX + barW : barX;
                float ry = barH < 0 ? barY + barH : barY;
                Color contrast = ChartValueScale.ContrastTextColor(barColor);
                canvas.DrawText(rx + absW / 2, ry + absH / 2 - LabelFontSize / 2, text, contrast, LabelFontSize, family, TextAlignment.Center);
                return;
            }
            if (ValueLabelPosition is "inside") return;
        }

        if (Orientation == ChartOrientation.Horizontal)
        {
            DrawPlacedLabel(canvas, below ? pointX - offset : pointX + offset, pointY - LabelFontSize / 2, text, barColor, LabelFontSize, family, below ? TextAlignment.Right : TextAlignment.Left);
        }
        else
        {
            DrawPlacedLabel(canvas, pointX, below ? pointY + offset : pointY - offset - LabelFontSize, text, barColor, LabelFontSize, family, outsideAlignCenter ? TextAlignment.Center : TextAlignment.Left);
        }
    }

    protected virtual void DrawValueLabels(
        ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (!ShowValueLabels) return;
        string family = EffectiveFontFamily;

        foreach (ChartSeries series in data.Series)
        {
            IList<Point> coordinates = CalculateSeriesCoordinates(series, data, chartAreaWidth, chartAreaHeight, baselineY);
            Color labelColor = ResolveSeriesColor(series);
            float offset = GetLabelVerticalOffset(series);

            for (int i = 0; i < series.DataPoints.Count; i++)
            {
                Point point = coordinates[i];
                string text = FormatPointValue(series.DataPoints[i]);
                bool below = ChartValueScale.IsBelowOrigin(series.DataPoints[i].Value, _effectiveMin, _effectiveMax);

                if (Orientation == ChartOrientation.Horizontal)
                {
                    DrawPlacedLabel(canvas, below ? point.X - offset : point.X + offset, point.Y - LabelFontSize / 2, text, labelColor, LabelFontSize, family, below ? TextAlignment.Right : TextAlignment.Left);
                }
                else
                {
                    DrawPlacedLabel(canvas, point.X, below ? point.Y + offset : point.Y - offset - LabelFontSize, text, labelColor, LabelFontSize, family, TextAlignment.Center);
                }
            }
        }
    }

    protected virtual float GetLabelVerticalOffset(ChartSeries series) => 7;

    protected virtual void DrawLegendSwatch(ICanvas canvas, float x, float y, float size, ChartSeries series) =>
        canvas.FillRect(x, y, size, size, ResolveSeriesColor(series));

    private void DrawLegendInPosition(ICanvas canvas, ChartData data, (float PlotLeft, float PlotRight, float ChartAreaWidth, float ChartAreaHeight, float BaselineY, float TopY) layout)
    {
        string pos = EffectiveLegendPosition();
        if (pos is "none") return;
        if (pos is "bottom") { DrawLegend(canvas, data, layout.ChartAreaWidth); return; }
        string family = EffectiveFontFamily;

        if (pos is "top")
        {
            ChartTheme t = Appearance;
            float y = !string.IsNullOrEmpty(data.Title) ? t.TitleY + t.TitleFontSize + (!string.IsNullOrEmpty(data.Subtitle) ? 19 : 8) : 10;
            float x = _plotLeft;
            foreach (ChartSeries series in data.Series)
            {
                float w = canvas.MeasureText(series.Name, 11, family).Width + 32;
                if (x + w > Width - 16 && x > _plotLeft) { x = _plotLeft; y += 18; }
                DrawLegendSwatch(canvas, x, y, 12, series);
                canvas.DrawText(x + 18, y, series.Name, ThemeAxisColor, 11, family, TextAlignment.Left);
                x += w;
            }
            return;
        }
        if (pos is "right")
        {
            const float rowH = 20;
            float totalH = data.Series.Count * rowH;
            float startY = layout.TopY + MathF.Max(0, (layout.ChartAreaHeight - totalH) / 2);
            float x = _plotLeft + layout.ChartAreaWidth + 16;
            for (int i = 0; i < data.Series.Count; i++)
            {
                ChartSeries series = data.Series[i];
                float y = startY + i * rowH;
                DrawLegendSwatch(canvas, x, y, 12, series);
                canvas.DrawText(x + 18, y, series.Name, ThemeAxisColor, 11, family, TextAlignment.Left);
            }
            return;
        }
        if (pos is "inline")
        {
            List<(ChartSeries Series, float TargetY, float Y)> items = new();
            foreach (ChartSeries series in data.Series)
            {
                var coords = CalculateSeriesCoordinates(series, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY);
                if (coords.Count == 0) continue;
                Point last = coords[^1];
                items.Add((series, last.Y - 5.5f, last.Y - 5.5f));
            }
            items.Sort((a, b) => a.TargetY.CompareTo(b.TargetY));
            const float minGap = 14;
            for (int i = 1; i < items.Count; i++)
            {
                if (items[i].Y < items[i - 1].Y + minGap) items[i] = (items[i].Series, items[i].TargetY, items[i - 1].Y + minGap);
            }
            float maxY = layout.BaselineY - 11;
            if (items.Count > 0 && items[^1].Y > maxY)
            {
                items[^1] = (items[^1].Series, items[^1].TargetY, maxY);
                for (int i = items.Count - 2; i >= 0; i--)
                {
                    if (items[i].Y > items[i + 1].Y - minGap) items[i] = (items[i].Series, items[i].TargetY, items[i + 1].Y - minGap);
                }
            }
            float x = _plotLeft + layout.ChartAreaWidth + 8;
            foreach (var item in items)
                canvas.DrawText(x, item.Y, item.Series.Name, ResolveSeriesColor(item.Series), 10, family, TextAlignment.Left);
        }
    }

    protected virtual void DrawLegend(ICanvas canvas, ChartData data, float chartAreaWidth)
    {
        string family = EffectiveFontFamily;
        float captionLift = !string.IsNullOrEmpty(Caption) ? 16 : 0;
        if (Appearance.FlowLegend)
        {
            float x = _plotLeft, y = Height - 42 - captionLift;
            foreach (ChartSeries series in data.Series)
            {
                float w = canvas.MeasureText(series.Name, 11, family).Width + 32;
                if (x + w > Width - 16 && x > _plotLeft) { x = _plotLeft; y += 18; }
                if (y + 12 > Height - 4 - captionLift) break;
                DrawLegendSwatch(canvas, x, y, 12, series);
                canvas.DrawText(x + 18, y, series.Name, ThemeAxisColor, 11, family, TextAlignment.Left);
                x += w;
            }
            return;
        }
        float legendY = Height - 40 - captionLift;
        float legendStartX = _plotLeft;
        const float legendItemWidth = 150;
        const float colorBoxSize = 12;

        for (int i = 0; i < data.Series.Count; i++)
        {
            ChartSeries series = data.Series[i];
            float x = legendStartX + i * legendItemWidth;
            DrawLegendSwatch(canvas, x, legendY, colorBoxSize, series);
            canvas.DrawText(x + colorBoxSize + 5, legendY + colorBoxSize / 2 - LegendFontSize / 2, series.Name, ThemeAxisColor, LegendFontSize, family, TextAlignment.Left);
        }
    }

    protected virtual void DrawGridLines(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        int stepCount = _effectiveStepCount > 0 ? _effectiveStepCount : 5;
        bool dashed = Appearance.GridLineStyle is "dashed";

        if (Orientation == ChartOrientation.Horizontal)
        {
            float stepWidth = chartAreaWidth / stepCount;
            for (int i = 1; i <= stepCount; i++)
            {
                float x = _plotLeft + i * stepWidth;
                if (dashed)
                    ChartVector.Stroke(canvas, new ChartStrokePlan([new("M", [x, topY]), new("L", [x, baselineY])], ThemeGridColor, GridLineWidth, "butt", "miter", 10, [3, 3]));
                else
                    canvas.DrawLine(x, topY, x, baselineY, ThemeGridColor, GridLineWidth);
            }
        }
        else
        {
            float stepHeight = chartAreaHeight / stepCount;
            for (int i = 1; i <= stepCount; i++)
            {
                float y = baselineY - i * stepHeight;
                float x1 = _plotLeft;
                float x2 = _plotLeft + chartAreaWidth;
                if (dashed)
                    ChartVector.Stroke(canvas, new ChartStrokePlan([new("M", [x1, y]), new("L", [x2, y])], ThemeGridColor, GridLineWidth, "butt", "miter", 10, [3, 3]));
                else
                    canvas.DrawLine(x1, y, x2, y, ThemeGridColor, GridLineWidth);
            }
        }
    }

    protected void DrawAxisFrameLines(ICanvas canvas, float chartAreaWidth, float baselineY, float topY)
    {
        canvas.BeginCrispEdges();
        canvas.DrawLine(_plotLeft, topY, _plotLeft, baselineY, ThemeAxisColor, AxisLineWidth);
        canvas.DrawLine(_plotLeft, baselineY, _plotLeft + chartAreaWidth, baselineY, ThemeAxisColor, AxisLineWidth);
        canvas.EndCrispEdges();
    }

    protected static void DrawAxisLines(ICanvas canvas, float chartAreaWidth, float baselineY, float topY)
    {
        canvas.BeginCrispEdges();
        canvas.DrawLine(LeftPadding, topY, LeftPadding, baselineY, AxisColor, AxisLineWidth);
        canvas.DrawLine(LeftPadding, baselineY, LeftPadding + chartAreaWidth, baselineY, AxisColor, AxisLineWidth);
        canvas.EndCrispEdges();
    }

    protected virtual void DrawYAxisLabels(ICanvas canvas, ChartData data,
        float chartAreaHeight, float baselineY, float topY)
    {
        int stepCount = _effectiveStepCount > 0 ? _effectiveStepCount : 5;
        decimal minValue = _effectiveMin;
        decimal maxValue = _effectiveMax;
        decimal stepValue = (maxValue - minValue) / stepCount;
        string family = EffectiveFontFamily;
        string fmt = _effectiveTickFormat;

        if (Orientation == ChartOrientation.Horizontal)
        {
            float chartAreaWidth = Width - _plotLeft - _plotRight;
            float stepWidth = chartAreaWidth / stepCount;
            for (int i = 0; i <= stepCount; i++)
            {
                float x = _plotLeft + i * stepWidth;
                decimal value = minValue + i * stepValue;
                string label = $"{data.YAxis.Prefix}{ChartNumberFormat.Format(value, fmt)}{data.YAxis.Suffix}";
                canvas.DrawText(x, baselineY + 8, label, ThemeAxisColor, AxisLabelFontSize, family, TextAlignment.Center);
            }
        }
        else
        {
            float stepHeight = chartAreaHeight / stepCount;
            for (int i = 0; i <= stepCount; i++)
            {
                float y = baselineY - i * stepHeight;
                decimal value = minValue + i * stepValue;
                string label = $"{data.YAxis.Prefix}{ChartNumberFormat.Format(value, fmt)}{data.YAxis.Suffix}";
                canvas.DrawText(_plotLeft - 8, y - AxisLabelFontSize / 2, label, ThemeAxisColor, AxisLabelFontSize, family, TextAlignment.Right);
            }
        }
    }

    protected virtual void DrawYAxisLegend(ICanvas canvas, AxisOptions yAxis,
        float chartAreaWidth, float chartAreaHeight, float topY, float baselineY)
    {
        string family = EffectiveFontFamily;
        if (Orientation == ChartOrientation.Horizontal)
        {
            float x = _plotLeft + chartAreaWidth / 2;
            float y = Height - 15 - (!string.IsNullOrEmpty(Caption) ? 16 : 0);
            canvas.DrawText(x, y, yAxis.Legend, ThemeAxisColor, LegendFontSize, family, TextAlignment.Center);
        }
        else if (yAxis.VerticalLegend)
        {
            const float x = 15;
            float textWidth = canvas.MeasureText(yAxis.Legend, LegendFontSize, family).Width;
            float y = topY + chartAreaHeight / 2 + textWidth / 2 - LegendFontSize;
            canvas.DrawTextRotated(x, y, yAxis.Legend, ThemeAxisColor, LegendFontSize, family, 90);
        }
        else
        {
            const float x = 10;
            float y = topY + chartAreaHeight / 2;
            canvas.DrawText(x, y, yAxis.Legend, ThemeAxisColor, LegendFontSize, family, TextAlignment.Left);
        }
    }

    protected virtual void DrawXAxisLegend(ICanvas canvas, string legend,
        float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        string family = EffectiveFontFamily;
        if (Orientation == ChartOrientation.Horizontal)
        {
            const float x = 15;
            float textWidth = canvas.MeasureText(legend, LegendFontSize, family).Width;
            float y = TitleAreaHeight + chartAreaHeight / 2 + textWidth / 2 - LegendFontSize;
            canvas.DrawTextRotated(x, y, legend, ThemeAxisColor, LegendFontSize, family, 90);
        }
        else
        {
            float x = _plotLeft + chartAreaWidth / 2;
            float labelsBottom = baselineY + 15 + AxisLabelFontSize;
            float marginBottom = baselineY + BottomPadding;
            float y = (labelsBottom + marginBottom) / 2 - LegendFontSize / 2;
            canvas.DrawText(x, y, legend, ThemeAxisColor, LegendFontSize, family, TextAlignment.Center);
        }
    }

    private static List<string> WrapLabelTwoLines(ICanvas canvas, string text, float maxWidth, float fontSize, string family)
    {
        if (canvas.MeasureText(text, fontSize, family).Width <= maxWidth) return [text];
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 1)
        {
            string truncated = text;
            while (truncated.Length > 1 && canvas.MeasureText(truncated + "...", fontSize, family).Width > maxWidth)
                truncated = truncated[..^1];
            return [truncated + "..."];
        }
        int bestSplit = 1;
        float bestMax = float.PositiveInfinity;
        for (int s = 1; s < words.Length; s++)
        {
            float w1 = canvas.MeasureText(string.Join(" ", words[..s]), fontSize, family).Width;
            float w2 = canvas.MeasureText(string.Join(" ", words[s..]), fontSize, family).Width;
            float m = MathF.Max(w1, w2);
            if (m < bestMax) { bestMax = m; bestSplit = s; }
        }
        return [string.Join(" ", words[..bestSplit]), string.Join(" ", words[bestSplit..])];
    }

    protected string ResolveEffectiveOverflow(ICanvas canvas, ChartData data, float chartAreaWidth)
    {
        string mode = data.XAxis.LabelOverflow;
        if (mode is not "auto") return mode;
        var pts = data.Series.Count > 0 ? data.Series[0].DataPoints : null;
        if (pts is null || pts.Count == 0) return "omit";
        float slot = chartAreaWidth / pts.Count;
        string family = EffectiveFontFamily;
        bool collides = false;
        bool allFitTwoLines = true;
        foreach (var dp in pts)
        {
            if (canvas.MeasureText(dp.Label, AxisLabelFontSize, family).Width > slot - 4)
            {
                collides = true;
                var lines = WrapLabelTwoLines(canvas, dp.Label, slot - 4, AxisLabelFontSize, family);
                if (lines.Exists(l => l.EndsWith("...", StringComparison.Ordinal) || canvas.MeasureText(l, AxisLabelFontSize, family).Width > slot - 2))
                    allFitTwoLines = false;
            }
        }
        if (!collides) return "omit";
        return allFitTwoLines ? "wrap" : "rotate";
    }

    protected virtual void DrawXAxisLabels(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        if (data.Series.Count == 0) return;
        ChartSeries firstSeries = data.Series[0];
        int dataPointCount = firstSeries.DataPoints.Count;
        if (dataPointCount == 0) return;
        string family = EffectiveFontFamily;

        if (Orientation == ChartOrientation.Horizontal)
        {
            float topY = baselineY - chartAreaHeight;
            float totalSpace = chartAreaHeight / dataPointCount;
            for (int i = 0; i < dataPointCount; i++)
            {
                float y = topY + i * totalSpace + totalSpace / 2;
                string label = firstSeries.DataPoints[i].Label;
                DrawPlacedLabel(canvas, _plotLeft - 8, y - AxisLabelFontSize / 2, label, ThemeAxisColor, AxisLabelFontSize, family, TextAlignment.Right);
            }
        }
        else
        {
            float labelY = baselineY + 15;
            float totalSpace = chartAreaWidth / dataPointCount;
            string overflow = ResolveEffectiveOverflow(canvas, data, chartAreaWidth);

            for (int i = 0; i < dataPointCount; i++)
            {
                float x = _plotLeft + i * totalSpace + totalSpace / 2;
                string label = firstSeries.DataPoints[i].Label;
                if (overflow is "rotate")
                {
                    float w = canvas.MeasureText(label, AxisLabelFontSize, family).Width;
                    float dx = w * 0.70710678f;
                    canvas.DrawTextRotated(x - dx * 0.5f, labelY + dx * 0.5f, label, ThemeAxisColor, AxisLabelFontSize, family, 45);
                }
                else if (overflow is "wrap")
                {
                    var lines = WrapLabelTwoLines(canvas, label, totalSpace - 4, AxisLabelFontSize, family);
                    for (int li = 0; li < lines.Count; li++)
                        canvas.DrawText(x, labelY + li * (AxisLabelFontSize + 2), lines[li], ThemeAxisColor, AxisLabelFontSize, family, TextAlignment.Center);
                }
                else
                {
                    DrawPlacedLabel(canvas, x, labelY, label, ThemeAxisColor, AxisLabelFontSize, family, TextAlignment.Center);
                }
            }
        }
    }

    protected virtual Color GetDefaultSeriesColor() => Color.FromHex("#4f46e5");

    protected Color ResolveSeriesColor(ChartSeries series) =>
        series.HasColor ? series.Color : Appearance.SeriesColor(GetDefaultSeriesColor(), (_renderData ?? Data).Series.IndexOf(series));

    protected string ResolveSeriesPattern(ChartSeries series, int index)
    {
        if (!string.IsNullOrEmpty(series.Style?.Pattern)) return series.Style!.Pattern!;
        return Appearance.SeriesPatterns.Count > 0 ? Appearance.SeriesPatterns[Math.Max(0, index) % Appearance.SeriesPatterns.Count] : "none";
    }

    protected string ResolveSeriesMarkerShape(ChartSeries series, int index, string fallback = "circle")
    {
        if (!string.IsNullOrEmpty(series.Style?.MarkerShape)) return series.Style!.MarkerShape!;
        return Appearance.SeriesMarkerShapes.Count > 0 ? Appearance.SeriesMarkerShapes[Math.Max(0, index) % Appearance.SeriesMarkerShapes.Count] : fallback;
    }

    protected float[]? ResolveSeriesDashPattern(ChartSeries series, int index)
    {
        if (series.Style?.DashPattern is not null) return series.Style.DashPattern;
        return Appearance.SeriesDashPatterns.Count > 0 ? Appearance.SeriesDashPatterns[Math.Max(0, index) % Appearance.SeriesDashPatterns.Count] : null;
    }

    protected bool ReservesBottomLegendBand(ChartData data)
    {
        if (ShouldDrawLegend(data) && EffectiveLegendPosition() is "bottom") return true;
        if (Orientation == ChartOrientation.Horizontal) return !string.IsNullOrEmpty(data.YAxis.Legend);
        return !string.IsNullOrEmpty(data.XAxis.Legend);
    }

    protected (float chartAreaWidth, float chartAreaHeight, float baselineY, float topY) CalculateLayout(ChartData data)
    {
        float topBand = !string.IsNullOrEmpty(data.Title) ? TitleAreaHeight : TopPadding;
        float bottomBand = ReservesBottomLegendBand(data) ? BottomPadding : AxisLabelBand;

        float chartAreaWidth = Width - LeftPadding - RightPadding;
        float chartAreaHeight = Height - topBand - bottomBand;
        float baselineY = topBand + chartAreaHeight;
        float topY = topBand;
        return (chartAreaWidth, chartAreaHeight, baselineY, topY);
    }

    private (float PlotLeft, float PlotRight, float ChartAreaWidth, float ChartAreaHeight, float BaselineY, float TopY) CalculateLayoutWithCanvas(ICanvas canvas, ChartData data)
    {
        float extraRight = GetExtraRightMargin(canvas, data);
        bool hasEnhancedLayout = AutoLayout || !string.IsNullOrEmpty(data.Subtitle) || !string.IsNullOrEmpty(Caption)
            || LegendPosition is not "bottom" || data.XAxis.LabelOverflow is not "omit" || extraRight > 0;

        if (!hasEnhancedLayout)
        {
            var (cw, ch, by, ty) = CalculateLayout(data);
            return (LeftPadding, RightPadding, cw, ch, by, ty);
        }

        string family = EffectiveFontFamily;
        float plotLeft = LeftPadding;
        float plotRight = MathF.Max(RightPadding, extraRight);

        if (AutoLayout)
        {
            float maxLeftText = 0;
            if (Orientation == ChartOrientation.Horizontal)
            {
                if (data.Series.Count > 0)
                {
                    foreach (var dp in data.Series[0].DataPoints)
                    {
                        float w = canvas.MeasureText(dp.Label, AxisLabelFontSize, family).Width;
                        if (w > maxLeftText) maxLeftText = w;
                    }
                }
            }
            else
            {
                int steps = _effectiveStepCount > 0 ? _effectiveStepCount : 5;
                decimal stepVal = (_effectiveMax - _effectiveMin) / steps;
                for (int i = 0; i <= steps; i++)
                {
                    decimal v = _effectiveMin + i * stepVal;
                    string lbl = $"{data.YAxis.Prefix}{ChartNumberFormat.Format(v, _effectiveTickFormat)}{data.YAxis.Suffix}";
                    float w = canvas.MeasureText(lbl, AxisLabelFontSize, family).Width;
                    if (w > maxLeftText) maxLeftText = w;
                }
            }
            bool hasLeftLegend = Orientation == ChartOrientation.Horizontal ? !string.IsNullOrEmpty(data.XAxis.Legend) : !string.IsNullOrEmpty(data.YAxis.Legend);
            plotLeft = MathF.Max(24, MathF.Min(Width * 0.35f, MathF.Ceiling(maxLeftText + 16 + (hasLeftLegend ? 24 : 0))));
            plotRight = MathF.Max(20, extraRight);
            if (Orientation == ChartOrientation.Horizontal && ShowValueLabels)
            {
                float maxValW = 0;
                foreach (var s in data.Series)
                    foreach (var dp in s.DataPoints)
                    {
                        float w = canvas.MeasureText(FormatPointValue(dp), LabelFontSize, family).Width;
                        if (w > maxValW) maxValW = w;
                    }
                plotRight = MathF.Max(plotRight, MathF.Min(Width * 0.25f, MathF.Ceiling(maxValW + 20)));
            }
        }

        string legendPos = ShouldDrawLegend(data) ? EffectiveLegendPosition() : "none";
        if (legendPos is "right" or "inline")
        {
            float maxNameW = 0;
            foreach (var s in data.Series)
            {
                float w = canvas.MeasureText(s.Name, 11, family).Width;
                if (w > maxNameW) maxNameW = w;
            }
            float needed = MathF.Min(Width * 0.35f, MathF.Ceiling(maxNameW + (legendPos is "right" ? 40 : 20)));
            plotRight = MathF.Max(plotRight, needed);
        }

        float topBand = !string.IsNullOrEmpty(data.Title) ? TitleAreaHeight : TopPadding;
        if (!string.IsNullOrEmpty(data.Subtitle)) topBand += !string.IsNullOrEmpty(data.Title) ? 18 : 24;
        if (legendPos is "top") topBand += 24;

        float bottomBand = ReservesBottomLegendBand(data) ? BottomPadding : AxisLabelBand;
        if (!string.IsNullOrEmpty(Caption)) bottomBand += 16;

        float prelimW = MathF.Max(40, Width - plotLeft - plotRight);
        if (Orientation == ChartOrientation.Vertical && data.XAxis.LabelOverflow is not "omit")
        {
            string eff = ResolveEffectiveOverflow(canvas, data, prelimW);
            if (eff is "rotate")
            {
                float maxLbl = 0;
                if (data.Series.Count > 0)
                    foreach (var dp in data.Series[0].DataPoints)
                    {
                        float w = canvas.MeasureText(dp.Label, AxisLabelFontSize, family).Width;
                        if (w > maxLbl) maxLbl = w;
                    }
                bottomBand += MathF.Min(60, MathF.Ceiling(maxLbl * 0.70710678f * 0.65f));
            }
            else if (eff is "wrap")
            {
                bottomBand += 14;
            }
        }

        float maxHorizMargin = Width * 0.6f;
        if (plotLeft + plotRight > maxHorizMargin)
        {
            float scale = maxHorizMargin / (plotLeft + plotRight);
            plotLeft *= scale;
            plotRight *= scale;
        }
        float maxVertMargin = Height * 0.6f;
        if (topBand + bottomBand > maxVertMargin)
        {
            float scale = maxVertMargin / (topBand + bottomBand);
            topBand *= scale;
            bottomBand *= scale;
        }

        float cw2 = Width - plotLeft - plotRight;
        float ch2 = Height - topBand - bottomBand;
        return (plotLeft, plotRight, cw2, ch2, topBand + ch2, topBand);
    }
}
