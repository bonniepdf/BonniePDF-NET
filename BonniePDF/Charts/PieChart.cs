// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Pie and donut chart with modern theme support, gradient slices, centre totals, and optional callout labels.
/// </summary>
public class PieChart : ChartBase
{
    public PieChart() { }
    public PieChart(ChartData data) : base(data) { }

    private const float TitleAreaHeight = 60;
    private const float TopPadding = 20;
    private const float BottomPadding = 20;
    private const float LeftPadding = 20;
    private const float LegendGapFromPie = 30;
    private const float RightPadding = 20;

    private const float LegendItemHeight = 24;
    private const float LegendBoxSize = 14;
    private const float LegendBoxTextGap = 8;
    private const float LegendFontSize = 11;
    private const float TotalRowGap = 8;

    private const float DefaultDonutHoleRatio = 0.33f;
    private const float CentreTotalHoleRatio = 0.56f;
    private const float CentreValueMaxSize = 22f;
    private const float CentreValueMinSize = 7f;
    private const float CentreLabelGap = 3f;
    private const double RoundedK = 0.5522847498307933;
    private static readonly Color CentreValueColor = Color.FromHex("#111827");

    private static readonly string[] DefaultColors =
    {
        "#4f46e5", "#7c3aed", "#a855f7", "#d946ef", "#ec4899", "#f43f5e",
        "#f97316", "#eab308", "#84cc16", "#22c55e", "#14b8a6", "#06b6d4"
    };

    private float? _donutHoleRatio;

    public IList<string>? Colors { get; set; }
    public bool IsDonut { get; set; } = true;

    public float? DonutHoleRatio
    {
        get => _donutHoleRatio;
        set
        {
            if (value.HasValue && (!float.IsFinite(value.Value) || value.Value < 0f || value.Value > 0.85f))
                throw new ArgumentOutOfRangeException(nameof(value), "DonutHoleRatio must be finite and between 0 and 0.85.");
            _donutHoleRatio = value;
        }
    }

    public float SliceBorderWidth { get; set; }
    public string TotalLabel { get; set; } = "Total";
    public string? TotalValueFormat { get; set; }
    public string? ValueFormat { get; set; }
    public string PercentFormat { get; set; } = "F0";
    public string LegendFormat { get; set; } = "labelValue";
    public string SliceLabelMode { get; set; } = "none";
    public bool ShowLegend { get; set; } = true;
    public bool ShowTotal { get; set; } = true;
    public string TotalPosition { get; set; } = "legend";
    public bool SliceGradient { get; set; }

    internal override bool UsesStyledGeometry => base.UsesStyledGeometry || SliceGradient || SliceBorderWidth > 0 || SliceLabelMode is "callout";

    private bool TotalInCentre => IsDonut && string.Equals(TotalPosition, "center", StringComparison.Ordinal);

    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        if (data.Series.Count == 0 || data.Series[0].DataPoints.Count == 0) return;

        IList<ChartDataPoint> dataPoints = data.Series[0].DataPoints;
        decimal total = dataPoints.Sum(dp => dp.Value);
        PieLayout layout = CalculateLayout(canvas, dataPoints, total, !string.IsNullOrEmpty(data.Title), !string.IsNullOrEmpty(data.Subtitle));
        var angles = CalculateSliceAngles(dataPoints, total);
        var colors = GetColors(dataPoints.Count);

        DrawPieSlices(canvas, dataPoints, angles, colors, layout);

        if (SliceLabelMode is "inside")
            DrawInsideSliceLabels(canvas, dataPoints, angles, colors, layout, total);
        else if (SliceLabelMode is "callout")
            DrawCalloutLabels(canvas, dataPoints, angles, layout, total);

        if (ShowLegend) DrawLegend(canvas, dataPoints, colors, layout, total);
        if (ShowTotal && TotalInCentre) DrawCentreTotal(canvas, dataPoints, layout, total);
    }

    private string FormatSliceValue(ChartDataPoint dp)
    {
        if (dp.HasExplicitLabelFormat) return dp.FormattedValue;
        return !string.IsNullOrEmpty(ValueFormat) ? ChartNumberFormat.Format(dp.Value, ValueFormat) : dp.FormattedValue;
    }

    private string FormatSlicePercent(decimal value, decimal total)
    {
        decimal pct = total > 0 ? (value / total) * 100m : 0m;
        return $"{ChartNumberFormat.Format(pct, PercentFormat)}%";
    }

    private string FormatLegendEntry(ChartDataPoint dp, decimal total) => LegendFormat switch
    {
        "labelOnly" => dp.Label,
        "labelPercent" => $"{dp.Label} ({FormatSlicePercent(dp.Value, total)})",
        "labelValuePercent" => $"{dp.Label} - {FormatSliceValue(dp)} ({FormatSlicePercent(dp.Value, total)})",
        _ => $"{dp.Label} - {FormatSliceValue(dp)}",
    };

    private void DrawCentreTotal(ICanvas canvas, IList<ChartDataPoint> dataPoints, PieLayout layout, decimal total)
    {
        string value = ChartNumberFormat.Format(total, TotalValueFormat);
        float maxWidth = layout.DonutHoleRadius * 1.6f;
        float size = MathF.Min(CentreValueMaxSize, MathF.Floor(layout.DonutHoleRadius * 0.45f));
        string boldFont = ResolveTitleFontFamily();
        string regularFont = ResolveFontFamily();
        while (size > CentreValueMinSize && canvas.MeasureText(value, size, boldFont).Width > maxWidth)
            size -= 1f;
        float labelSize = MathF.Max(CentreValueMinSize, MathF.Round(size * 0.55f, MidpointRounding.AwayFromZero));
        bool hasLabel = !string.IsNullOrEmpty(TotalLabel);
        float blockHeight = size + (hasLabel ? CentreLabelGap + labelSize : 0);
        float top = layout.CenterY - blockHeight / 2f;
        Color valueColor = Appearance.BackgroundColor.HasValue ? Appearance.TitleColor : CentreValueColor;
        canvas.DrawText(layout.CenterX, top, value, valueColor, size, boldFont, TextAlignment.Center);
        if (hasLabel)
            canvas.DrawText(layout.CenterX, top + size + CentreLabelGap, TotalLabel, Appearance.LegendTextColor, labelSize, regularFont, TextAlignment.Center);
    }

    private PieLayout CalculateLayout(ICanvas canvas, IList<ChartDataPoint> dataPoints, decimal total, bool hasTitle, bool hasSubtitle)
    {
        float titleBand = (hasTitle ? TitleAreaHeight : 0) + (hasSubtitle ? 18 : 0);
        float captionBand = !string.IsNullOrEmpty(Caption) ? 16 : 0;
        float availableHeight = Height - titleBand - TopPadding - BottomPadding - captionBand;
        string font = ResolveFontFamily();

        float legendWidth = 0;
        if (ShowLegend)
        {
            if (AutoLayout)
            {
                float maxW = 0;
                foreach (var dp in dataPoints)
                {
                    float w = canvas.MeasureText(FormatLegendEntry(dp, total), LegendFontSize, font).Width;
                    if (w > maxW) maxW = w;
                }
                if (ShowTotal && !TotalInCentre)
                {
                    float tw = canvas.MeasureText($"{TotalLabel}: {ChartNumberFormat.Format(total, TotalValueFormat)}", LegendFontSize, font).Width;
                    if (tw > maxW) maxW = tw;
                }
                legendWidth = MathF.Max(80, MathF.Min(Width * 0.45f, MathF.Ceiling(LegendBoxSize + LegendBoxTextGap + maxW + 6)));
            }
            else
            {
                legendWidth = LegendBoxSize + LegendBoxTextGap + 150;
            }
        }

        float calloutReserve = 0;
        if (SliceLabelMode is "callout")
        {
            float maxLbl = 0;
            foreach (var dp in dataPoints)
            {
                float w = canvas.MeasureText($"{dp.Label} {FormatSlicePercent(dp.Value, total)}", 9, font).Width;
                if (w > maxLbl) maxLbl = w;
            }
            calloutReserve = MathF.Min(Width * 0.25f, maxLbl + 24);
        }

        float availableWidthForPie = Width - LeftPadding - RightPadding - calloutReserve * 2;
        if (ShowLegend) availableWidthForPie -= legendWidth + LegendGapFromPie;

        float pieRadius = MathF.Max(MathF.Min(availableWidthForPie / 2f, availableHeight / 2f), 20f);
        float pieCenterY = titleBand + TopPadding + availableHeight / 2f;
        float pieAreaWidth = Width - (ShowLegend ? legendWidth + LegendGapFromPie + RightPadding : RightPadding) - LeftPadding;
        float pieCenterX = LeftPadding + pieAreaWidth / 2f;
        float legendX = pieCenterX + pieRadius + calloutReserve + LegendGapFromPie;
        float totalLegendHeight = dataPoints.Count * LegendItemHeight;
        float legendStartY = pieCenterY - totalLegendHeight / 2f;

        float holeRatio = !IsDonut
            ? 0f
            : _donutHoleRatio ?? (TotalInCentre && ShowTotal ? CentreTotalHoleRatio : DefaultDonutHoleRatio);

        return new PieLayout(pieCenterX, pieCenterY, pieRadius, pieRadius * holeRatio, legendX, legendStartY, titleBand + TopPadding, Height - BottomPadding - captionBand);
    }

    private static List<(float StartAngle, float EndAngle)> CalculateSliceAngles(IList<ChartDataPoint> dataPoints, decimal total)
    {
        List<(float, float)> angles = new(dataPoints.Count);
        float current = -90f;
        foreach (ChartDataPoint dp in dataPoints)
        {
            float slice = total > 0 ? (float)(360 * (double)(dp.Value / total)) : 0f;
            angles.Add((current, current + slice));
            current += slice;
        }
        return angles;
    }

    private List<Color> GetColors(int count)
    {
        IList<string> src = Colors ?? DefaultColors;
        List<Color> result = new(count);
        for (int i = 0; i < count; i++) result.Add(Color.FromHex(src[i % src.Count]));
        return result;
    }

    private void DrawPieSlices(ICanvas canvas, IList<ChartDataPoint> dataPoints,
        IList<(float StartAngle, float EndAngle)> angles, IList<Color> colors, PieLayout layout)
    {
        Color borderColor = EffectiveBackgroundColor ?? Color.White;
        for (int i = 0; i < dataPoints.Count; i++)
        {
            var (startAngle, endAngle) = angles[i];
            if (endAngle <= startAngle) continue;
            Color color = colors[i];
            IList<Point> path = IsDonut
                ? CreateAnnularSectorPath(startAngle, endAngle, layout)
                : CreatePieSlicePath(startAngle, endAngle, layout);
            if (SliceGradient) ChartVector.Fill(canvas, GradientSlice(path, startAngle, endAngle, color, layout));
            else canvas.FillPath(path, color);

            if (SliceBorderWidth > 0)
            {
                List<ChartPathCommand> cmds = new(path.Count + 1);
                for (int n = 0; n < path.Count; n++) cmds.Add(new(n == 0 ? "M" : "L", [path[n].X, path[n].Y]));
                cmds.Add(new("Z", []));
                ChartVector.Stroke(canvas, new ChartStrokePlan(cmds, borderColor, SliceBorderWidth, "butt", "round"));
            }
        }
    }

    private void DrawInsideSliceLabels(ICanvas canvas, IList<ChartDataPoint> dataPoints,
        IList<(float StartAngle, float EndAngle)> angles, IList<Color> colors, PieLayout layout, decimal total)
    {
        string font = ResolveFontFamily();
        const float fontSize = 9;
        float midRadius = IsDonut ? (layout.DonutHoleRadius + layout.Radius) / 2f : layout.Radius * 0.65f;
        for (int i = 0; i < dataPoints.Count; i++)
        {
            ChartDataPoint dp = dataPoints[i];
            if (dp.Value <= 0) continue;
            var (startAngle, endAngle) = angles[i];
            double spanRad = (endAngle - startAngle) * Math.PI / 180.0;
            string text = FormatSlicePercent(dp.Value, total);
            Size measured = canvas.MeasureText(text, fontSize, font);
            double chord = 2 * midRadius * Math.Sin(Math.Min(Math.PI, spanRad) / 2.0);
            if (chord < measured.Width + 4) continue;
            double midRad = (startAngle + endAngle) / 2.0 * Math.PI / 180.0;
            float x = layout.CenterX + (float)(midRadius * Math.Cos(midRad));
            float y = layout.CenterY + (float)(midRadius * Math.Sin(midRad)) - fontSize / 2f;
            canvas.DrawText(x, y, text, ChartValueScale.ContrastTextColor(colors[i]), fontSize, font, TextAlignment.Center);
        }
    }

    private void DrawCalloutLabels(ICanvas canvas, IList<ChartDataPoint> dataPoints,
        IList<(float StartAngle, float EndAngle)> angles, PieLayout layout, decimal total)
    {
        string font = ResolveFontFamily();
        const float fontSize = 9;
        const float rowHeight = fontSize + 4;
        List<(string Text, double MidRad, float AnchorY, float Y)> right = new();
        List<(string Text, double MidRad, float AnchorY, float Y)> left = new();

        for (int i = 0; i < dataPoints.Count; i++)
        {
            ChartDataPoint dp = dataPoints[i];
            if (dp.Value <= 0) continue;
            var (startAngle, endAngle) = angles[i];
            double midRad = (startAngle + endAngle) / 2.0 * Math.PI / 180.0;
            float anchorY = layout.CenterY + (float)(layout.Radius * Math.Sin(midRad));
            string text = $"{dp.Label} {FormatSlicePercent(dp.Value, total)}";
            if (Math.Cos(midRad) >= 0) right.Add((text, midRad, anchorY, anchorY));
            else left.Add((text, midRad, anchorY, anchorY));
        }

        void Relax(List<(string Text, double MidRad, float AnchorY, float Y)> items, bool isRight)
        {
            items.Sort((a, b) => a.AnchorY.CompareTo(b.AnchorY));
            for (int i = 1; i < items.Count; i++)
                if (items[i].Y < items[i - 1].Y + rowHeight)
                    items[i] = (items[i].Text, items[i].MidRad, items[i].AnchorY, items[i - 1].Y + rowHeight);
            if (items.Count > 0 && items[^1].Y > layout.PlotBottom - rowHeight / 2f)
            {
                items[^1] = (items[^1].Text, items[^1].MidRad, items[^1].AnchorY, layout.PlotBottom - rowHeight / 2f);
                for (int i = items.Count - 2; i >= 0; i--)
                    if (items[i].Y > items[i + 1].Y - rowHeight)
                        items[i] = (items[i].Text, items[i].MidRad, items[i].AnchorY, items[i + 1].Y - rowHeight);
                if (items[0].Y < layout.PlotTop + rowHeight / 2f)
                    items[0] = (items[0].Text, items[0].MidRad, items[0].AnchorY, layout.PlotTop + rowHeight / 2f);
            }
            float dir = isRight ? 1f : -1f;
            float endX = layout.CenterX + dir * (layout.Radius + 18);
            foreach (var item in items)
            {
                double sx = layout.CenterX + (layout.Radius + 2) * Math.Cos(item.MidRad);
                double sy = layout.CenterY + (layout.Radius + 2) * Math.Sin(item.MidRad);
                double mx = layout.CenterX + (layout.Radius + 10) * Math.Cos(item.MidRad);
                double my = layout.CenterY + (layout.Radius + 10) * Math.Sin(item.MidRad);
                ChartVector.Stroke(canvas, new ChartStrokePlan(
                    [new("M", [sx, sy]), new("L", [mx, my]), new("L", [endX, item.Y])],
                    Appearance.AxisColor, 0.75, "butt", "round"));
                canvas.DrawText(endX + dir * 4, item.Y - fontSize / 2f, item.Text, Appearance.LegendTextColor, fontSize, font,
                    isRight ? TextAlignment.Left : TextAlignment.Right);
            }
        }

        Relax(right, true);
        Relax(left, false);
    }

    private ChartPathPlan GradientSlice(IList<Point> path, float startAngle, float endAngle, Color color, PieLayout layout)
    {
        double middle = (startAngle + endAngle) / 2.0 * Math.PI / 180.0;
        double inner = IsDonut ? layout.DonutHoleRadius : 0.0;
        List<ChartPathCommand> cmds = new(path.Count + 1);
        for (int n = 0; n < path.Count; n++) cmds.Add(new(n == 0 ? "M" : "L", [path[n].X, path[n].Y]));
        cmds.Add(new("Z", []));
        return new ChartPathPlan(
            cmds,
            color,
            new ChartResolvedGradient(
                layout.CenterX + inner * Math.Cos(middle),
                layout.CenterY + inner * Math.Sin(middle),
                layout.CenterX + layout.Radius * Math.Cos(middle),
                layout.CenterY + layout.Radius * Math.Sin(middle),
                ChartTint.Tint(color),
                color));
    }

    private static IList<Point> CreatePieSlicePath(float startAngle, float endAngle, PieLayout layout)
    {
        List<Point> path = [new Point(layout.CenterX, layout.CenterY)];
        int segments = Math.Max((int)Math.Ceiling((endAngle - startAngle) / 5), 1);
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + (endAngle - startAngle) * i / segments;
            float radians = (float)(angle * Math.PI / 180);
            path.Add(new Point(layout.CenterX + (float)(layout.Radius * Math.Cos(radians)), layout.CenterY + (float)(layout.Radius * Math.Sin(radians))));
        }
        return path;
    }

    private static IList<Point> CreateAnnularSectorPath(float startAngle, float endAngle, PieLayout layout)
    {
        List<Point> path = new();
        int segments = Math.Max((int)Math.Ceiling((endAngle - startAngle) / 5), 1);
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + (endAngle - startAngle) * i / segments;
            float radians = (float)(angle * Math.PI / 180);
            path.Add(new Point(layout.CenterX + (float)(layout.Radius * Math.Cos(radians)), layout.CenterY + (float)(layout.Radius * Math.Sin(radians))));
        }
        for (int i = segments; i >= 0; i--)
        {
            float angle = startAngle + (endAngle - startAngle) * i / segments;
            float radians = (float)(angle * Math.PI / 180);
            path.Add(new Point(layout.CenterX + (float)(layout.DonutHoleRadius * Math.Cos(radians)), layout.CenterY + (float)(layout.DonutHoleRadius * Math.Sin(radians))));
        }
        return path;
    }

    private void DrawLegendSwatch(ICanvas canvas, float x, float y, Color color)
    {
        if (!Appearance.RoundedLegendSwatches)
        {
            canvas.FillRect(x, y, LegendBoxSize, LegendBoxSize, color);
            return;
        }
        const double s = 12, r = 3, c = RoundedK * r;
        double ox = x + 1, oy = y + 1;
        ChartVector.Fill(canvas, new ChartPathPlan(
        [
            new("M", [ox + r, oy]),
            new("L", [ox + s - r, oy]),
            new("C", [ox + s - r + c, oy, ox + s, oy + r - c, ox + s, oy + r]),
            new("L", [ox + s, oy + s - r]),
            new("C", [ox + s, oy + s - r + c, ox + s - r + c, oy + s, ox + s - r, oy + s]),
            new("L", [ox + r, oy + s]),
            new("C", [ox + r - c, oy + s, ox, oy + s - r + c, ox, oy + s - r]),
            new("L", [ox, oy + r]),
            new("C", [ox, oy + r - c, ox + r - c, oy, ox + r, oy]),
            new("Z", []),
        ], color, null));
    }

    private void DrawLegend(ICanvas canvas, IList<ChartDataPoint> dataPoints, IList<Color> colors, PieLayout layout, decimal total)
    {
        Color textColor = Appearance.LegendTextColor;
        string font = ResolveFontFamily();

        for (int i = 0; i < dataPoints.Count; i++)
        {
            ChartDataPoint dp = dataPoints[i];
            float y = layout.LegendStartY + i * LegendItemHeight;
            DrawLegendSwatch(canvas, layout.LegendX, y, colors[i]);
            string labelText = FormatLegendEntry(dp, total);
            float textX = layout.LegendX + LegendBoxSize + LegendBoxTextGap;
            float textY = y + LegendBoxSize / 2f - LegendFontSize / 2f;
            canvas.DrawText(textX, textY, labelText, textColor, LegendFontSize, font, TextAlignment.Left);
        }

        if (ShowTotal && !TotalInCentre)
        {
            string totalText = $"{TotalLabel}: {ChartNumberFormat.Format(total, TotalValueFormat)}";
            float textX = layout.LegendX + LegendBoxSize + LegendBoxTextGap;
            float totalY = layout.LegendStartY + dataPoints.Count * LegendItemHeight + TotalRowGap;
            canvas.DrawText(textX, totalY, totalText, textColor, LegendFontSize, font, TextAlignment.Left);
        }
    }

    private sealed record PieLayout(
        float CenterX,
        float CenterY,
        float Radius,
        float DonutHoleRadius,
        float LegendX,
        float LegendStartY,
        float PlotTop,
        float PlotBottom);
}
