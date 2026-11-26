// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// <summary>Direct slice label placement (<c>none</c>, <c>inside</c> or <c>callout</c>).</summary>
    public string SliceLabelMode { get; set; } = "none";

    /// <summary>The label placement drawn: a tilted pie writes its shares inside the slices unless <c>Pie3d.Labels</c> is <see langword="false"/>.</summary>
    private string EffectiveSliceLabelMode => SliceLabelMode is "none" && Pie3d is { Labels: true } ? "inside" : SliceLabelMode;

    private List<int> _explodedSlices = [];
    /// <summary>Indices of the slices pulled out from the centre, along the middle of each slice.</summary>
    public IList<int> ExplodedSlices
    {
        get => _explodedSlices;
        set
        {
            if (value.Any(i => i < 0)) throw new ArgumentOutOfRangeException(nameof(value), "explodedSlices must hold slice indices: integers from 0.");
            _explodedSlices = [.. value];
        }
    }

    private float _explodeOffset = 16;
    /// <summary>How far exploded slices move out, in points (<c>16</c>).</summary>
    public float ExplodeOffset
    {
        get => _explodeOffset;
        set
        {
            if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value), "explodeOffset must be finite and nonnegative.");
            _explodeOffset = value;
        }
    }

    /// <summary>Whether any slice is pulled out.</summary>
    private bool IsExploded => _explodedSlices.Count > 0 && _explodeOffset > 0;

    /// <summary>The layout of slice <paramref name="i"/>: the pie's, moved out along the slice's middle when the slice is exploded.</summary>
    private PieLayout SliceLayout(int i, IList<(float StartAngle, float EndAngle)> angles, PieLayout layout)
    {
        if (!IsExploded || !_explodedSlices.Contains(i)) return layout;
        double mid = (angles[i].StartAngle + (double)angles[i].EndAngle) / 2 * Math.PI / 180;
        return layout with
        {
            CenterX = (float)(layout.CenterX + _explodeOffset * Math.Cos(mid)),
            CenterY = (float)(layout.CenterY + _explodeOffset * Math.Sin(mid) * layout.YScale),
        };
    }

    private ChartPie3d? _pie3d;
    /// <summary>Tilts the pie and gives its slices side walls: at most eight slices. Flat pies are easier to read accurately.</summary>
    public ChartPie3d? Pie3d
    {
        get => _pie3d;
        set
        {
            value?.Resolve();
            _pie3d = value;
        }
    }
    public bool ShowLegend { get; set; } = true;
    public bool ShowTotal { get; set; } = true;
    public string TotalPosition { get; set; } = "legend";
    public bool SliceGradient { get; set; }

    private bool EffectiveSliceGradient => SliceGradient || string.Equals(Appearance.AutoFill, "tint", StringComparison.Ordinal);

    private IReadOnlyList<ChartResolvedEffect> EffectiveSliceEffects => ChartEffect.ResolveAll(SliceEffects) ?? Appearance.SliceEffects;

    /// <summary>Shadow, glow or gloss for every slice.</summary>
    public IList<ChartEffect> SliceEffects { get; set; } = new List<ChartEffect>();

    internal override bool UsesStyledGeometry => base.UsesStyledGeometry || Pie3d is not null || EffectiveSliceGradient || EffectiveSliceEffects.Count > 0 || !string.Equals(Appearance.MarkOutline, "none", StringComparison.Ordinal) || SliceBorderWidth > 0 || SliceLabelMode is "callout";

    private bool TotalInCentre => IsDonut && string.Equals(TotalPosition, "center", StringComparison.Ordinal);

    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        if (data.Series.Count == 0 || data.Series[0].DataPoints.Count == 0) return;

        IList<ChartDataPoint> dataPoints = data.Series[0].DataPoints;
        if (Pie3d is not null && dataPoints.Count > ChartPie3d.MaxSlices) throw new InvalidOperationException($"A pie3d chart takes at most {ChartPie3d.MaxSlices} slices.");
        decimal total = dataPoints.Sum(dp => dp.Value);
        PieLayout layout = CalculateLayout(canvas, dataPoints, total, !string.IsNullOrEmpty(data.Title), !string.IsNullOrEmpty(data.Subtitle));
        var angles = CalculateSliceAngles(dataPoints, total);
        var colors = GetColors(dataPoints.Count, dataPoints);

        foreach (int i in _explodedSlices)
            if (i >= dataPoints.Count) throw new InvalidOperationException($"explodedSlices names slice {i}, but the pie has {dataPoints.Count} slices.");
        if (Pie3d is not null && IsExploded)
        {
            // Exploded slices are separate solids: paint them back to front, each with its walls and then its top.
            static double Front((float StartAngle, float EndAngle) a) =>
                a.StartAngle <= 90 && a.EndAngle >= 90 ? 1 : Math.Max(Math.Sin(a.StartAngle * Math.PI / 180), Math.Sin(a.EndAngle * Math.PI / 180));
            List<int> order = Enumerable.Range(0, angles.Count).OrderBy(i => Front(angles[i])).ThenBy(i => i).ToList();
            DrawPieSlices(canvas, dataPoints, angles, colors, layout, data.Series[0].Name, order, i => DrawSliceSolid(canvas, i, angles, colors, layout));
        }
        else
        {
            DrawPieWalls(canvas, angles, colors, layout);
            DrawPieSlices(canvas, dataPoints, angles, colors, layout, data.Series[0].Name);
        }

        string labelMode = EffectiveSliceLabelMode;
        if (labelMode is "inside")
            DrawInsideSliceLabels(canvas, dataPoints, angles, colors, layout, total);
        else if (labelMode is "callout")
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

        (double yScale, float depth) = Pie3d?.Resolve() ?? (1.0, 0f);
        float maxRadiusFromHeight = Pie3d is null ? availableHeight / 2f : (float)((availableHeight - depth) / (2 * yScale));
        float pieRadius = MathF.Min(availableWidthForPie / 2f, maxRadiusFromHeight);
        if (IsExploded) pieRadius -= _explodeOffset;
        pieRadius = MathF.Max(pieRadius, 20f);
        float pieCenterY = titleBand + TopPadding + availableHeight / 2f;
        float blockCenterY = pieCenterY;
        if (Pie3d is not null)
        {
            double blockHeight = 2.0 * pieRadius * yScale + depth;
            double blockTop = titleBand + TopPadding + (availableHeight - blockHeight) / 2.0;
            pieCenterY = (float)(blockTop + pieRadius * yScale);
            blockCenterY = (float)(blockTop + blockHeight / 2.0);
        }
        float pieAreaWidth = Width - (ShowLegend ? legendWidth + LegendGapFromPie + RightPadding : RightPadding) - LeftPadding;
        float pieCenterX = LeftPadding + pieAreaWidth / 2f;
        float legendX = pieCenterX + pieRadius + calloutReserve + LegendGapFromPie;
        float totalLegendHeight = dataPoints.Count * LegendItemHeight;
        float legendStartY = blockCenterY - totalLegendHeight / 2f;

        float holeRatio = !IsDonut
            ? 0f
            : _donutHoleRatio ?? (TotalInCentre && ShowTotal ? CentreTotalHoleRatio : DefaultDonutHoleRatio);

        return new PieLayout(pieCenterX, pieCenterY, pieRadius, pieRadius * holeRatio, legendX, legendStartY, titleBand + TopPadding, Height - BottomPadding - captionBand, yScale, depth);
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

    private List<Color> GetColors(int count, IList<ChartDataPoint>? dataPoints = null)
    {
        IList<string> src = Colors ?? DefaultColors;
        List<Color> result = new(count);
        for (int i = 0; i < count; i++)
        {
            // A point's own fill or colour wins over the palette; a gradient fill stands in as its middle colour.
            ChartDataPoint? dp = dataPoints is not null && i < dataPoints.Count ? dataPoints[i] : null;
            Color? own = dp?.Fill?.FallbackColor ?? dp?.Color;
            result.Add(own ?? Color.FromHex(src[i % src.Count]));
        }
        return result;
    }

    private void DrawPieSlices(ICanvas canvas, IList<ChartDataPoint> dataPoints,
        IList<(float StartAngle, float EndAngle)> angles, IList<Color> colors, PieLayout pieLayout, string seriesName,
        IReadOnlyList<int>? order = null, Action<int>? beforeSlice = null)
    {
        decimal total = dataPoints.Sum(dp => dp.Value);
        Color borderColor = EffectiveBackgroundColor ?? Color.White;
        foreach (int i in order ?? Enumerable.Range(0, dataPoints.Count).ToList())
        {
            var (startAngle, endAngle) = angles[i];
            if (endAngle <= startAngle) continue;
            beforeSlice?.Invoke(i);
            PieLayout layout = SliceLayout(i, angles, pieLayout);
            int sliceIndex = i;
            ChartVector.WithGroup(canvas, () => new ChartGroupInfo("mark", SeriesIndex: 0, PointIndex: sliceIndex, Name: dataPoints[sliceIndex].Label,
                Description: DescribeMark("{label}: {value} ({percent})", new Dictionary<string, object>
                {
                    ["series"] = seriesName,
                    ["label"] = dataPoints[sliceIndex].Label,
                    ["value"] = FormatSliceValue(dataPoints[sliceIndex]),
                    ["percent"] = FormatSlicePercent(dataPoints[sliceIndex].Value, total),
                    ["raw"] = dataPoints[sliceIndex].Value,
                }),
                Anchor: new ChartGroupAnchor(layout.CenterX, layout.CenterY, "both")), () =>
            {
                Color color = colors[i];
                IList<Point> path = IsDonut
                    ? CreateAnnularSectorPath(startAngle, endAngle, layout)
                    : CreatePieSlicePath(startAngle, endAngle, layout);
                ChartFill? pointFill = dataPoints[i].Fill;
                if (pointFill is { IsSolid: false })
                {
                    ChartBox box = new(layout.CenterX - layout.Radius, layout.CenterY - (float)(layout.Radius * layout.YScale), layout.Radius * 2, (float)(layout.Radius * 2 * layout.YScale));
                    List<ChartPathCommand> fillCmds = new(path.Count + 1);
                    for (int n = 0; n < path.Count; n++) fillCmds.Add(new(n == 0 ? "M" : "L", [path[n].X, path[n].Y]));
                    fillCmds.Add(new("Z", []));
                    ChartResolvedFill resolved = pointFill.Resolve(FillBoxes(box));
                    ChartVector.Fill(canvas, new ChartPathPlan(fillCmds, resolved.Color, resolved.Gradient, resolved.Radial));
                }
                else if (EffectiveSliceGradient)
                {
                    ChartPathPlan gp = GradientSlice(path, startAngle, endAngle, color, layout);
                    ChartVector.Fill(canvas, EffectiveSliceEffects.Count > 0 ? gp with { Effects = EffectiveSliceEffects } : gp);
                }
                else if (EffectiveSliceEffects.Count > 0)
                {
                    List<ChartPathCommand> effectCmds = new(path.Count + 1);
                    for (int n = 0; n < path.Count; n++) effectCmds.Add(new(n == 0 ? "M" : "L", [path[n].X, path[n].Y]));
                    effectCmds.Add(new("Z", []));
                    ChartVector.Fill(canvas, new ChartPathPlan(effectCmds, color, null, null, EffectiveSliceEffects));
                }
                else canvas.FillPath(path, color);

                if (Pie3d is not null && IsExploded && _explodedSlices.Contains(i))
                {
                    // A lit bevel along the top edges of a pulled-out slice, so the top reads as a separate surface from its walls.
                    List<ChartPathCommand> edge = new(path.Count + 1);
                    for (int n = 0; n < path.Count; n++) edge.Add(new(n == 0 ? "M" : "L", [path[n].X, path[n].Y]));
                    edge.Add(new("Z", []));
                    ChartVector.Stroke(canvas, new ChartStrokePlan(edge, EdgeHighlight(color), 1, "butt", "round"));
                }

                if (string.Equals(Appearance.MarkOutline, "darker", StringComparison.Ordinal))
                {
                    List<ChartPathCommand> outline = new(path.Count + 1);
                    for (int n = 0; n < path.Count; n++) outline.Add(new(n == 0 ? "M" : "L", [path[n].X, path[n].Y]));
                    outline.Add(new("Z", []));
                    ChartVector.Stroke(canvas, new ChartStrokePlan(outline, ChartTint.Darken(color), 1, "butt", "round"));
                }

                if (SliceBorderWidth > 0)
                {
                    List<ChartPathCommand> cmds = new(path.Count + 1);
                    for (int n = 0; n < path.Count; n++) cmds.Add(new(n == 0 ? "M" : "L", [path[n].X, path[n].Y]));
                    cmds.Add(new("Z", []));
                    ChartVector.Stroke(canvas, new ChartStrokePlan(cmds, borderColor, SliceBorderWidth, "butt", "round"));
                }
            });
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
            PieLayout slice = SliceLayout(i, angles, layout);
            float x = slice.CenterX + (float)(midRadius * Math.Cos(midRad));
            float y = slice.CenterY + (float)(midRadius * Math.Sin(midRad) * layout.YScale) - fontSize / 2f;
            canvas.DrawText(x, y, text, ChartValueScale.ContrastTextColor(colors[i]), fontSize, font, TextAlignment.Center);
        }
    }

    private void DrawCalloutLabels(ICanvas canvas, IList<ChartDataPoint> dataPoints,
        IList<(float StartAngle, float EndAngle)> angles, PieLayout layout, decimal total)
    {
        string font = ResolveFontFamily();
        const float fontSize = 9;
        const float rowHeight = fontSize + 4;
        List<(string Text, double MidRad, float AnchorY, float Y, float Cx, float Cy)> right = new();
        List<(string Text, double MidRad, float AnchorY, float Y, float Cx, float Cy)> left = new();

        for (int i = 0; i < dataPoints.Count; i++)
        {
            ChartDataPoint dp = dataPoints[i];
            if (dp.Value <= 0) continue;
            var (startAngle, endAngle) = angles[i];
            double midRad = (startAngle + endAngle) / 2.0 * Math.PI / 180.0;
            PieLayout slice = SliceLayout(i, angles, layout);
            float anchorY = slice.CenterY + (float)(layout.Radius * Math.Sin(midRad) * layout.YScale);
            string text = $"{dp.Label} {FormatSlicePercent(dp.Value, total)}";
            if (Math.Cos(midRad) >= 0) right.Add((text, midRad, anchorY, anchorY, slice.CenterX, slice.CenterY));
            else left.Add((text, midRad, anchorY, anchorY, slice.CenterX, slice.CenterY));
        }

        void Relax(List<(string Text, double MidRad, float AnchorY, float Y, float Cx, float Cy)> items, bool isRight)
        {
            items.Sort((a, b) => a.AnchorY.CompareTo(b.AnchorY));
            for (int i = 1; i < items.Count; i++)
                if (items[i].Y < items[i - 1].Y + rowHeight)
                    items[i] = items[i] with { Y = items[i - 1].Y + rowHeight };
            if (items.Count > 0 && items[^1].Y > layout.PlotBottom - rowHeight / 2f)
            {
                items[^1] = items[^1] with { Y = layout.PlotBottom - rowHeight / 2f };
                for (int i = items.Count - 2; i >= 0; i--)
                    if (items[i].Y > items[i + 1].Y - rowHeight)
                        items[i] = items[i] with { Y = items[i + 1].Y - rowHeight };
                if (items[0].Y < layout.PlotTop + rowHeight / 2f)
                    items[0] = items[0] with { Y = layout.PlotTop + rowHeight / 2f };
            }
            float dir = isRight ? 1f : -1f;
            float endX = layout.CenterX + dir * (layout.Radius + 18);
            foreach (var item in items)
            {
                double sx = item.Cx + (layout.Radius + 2) * Math.Cos(item.MidRad);
                double sy = item.Cy + (layout.Radius + 2) * Math.Sin(item.MidRad) * layout.YScale;
                double mx = item.Cx + (layout.Radius + 10) * Math.Cos(item.MidRad);
                double my = item.Cy + (layout.Radius + 10) * Math.Sin(item.MidRad) * layout.YScale;
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
                layout.CenterY + inner * Math.Sin(middle) * layout.YScale,
                layout.CenterX + layout.Radius * Math.Cos(middle),
                layout.CenterY + layout.Radius * Math.Sin(middle) * layout.YScale,
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
            path.Add(new Point(layout.CenterX + (float)(layout.Radius * Math.Cos(radians)), layout.CenterY + (float)(layout.Radius * Math.Sin(radians) * layout.YScale)));
        }
        return path;
    }

    /// <summary>The highlight along the top edges of an exploded slice: each channel 40 % of the way to white, in integers.</summary>
    private static Color EdgeHighlight(Color color)
    {
        string hex = color.ToHexString();
        int Channel(int offset)
        {
            int c = int.Parse(hex.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return c + ((255 - c) * 40 + 50) / 100;
        }
        return new Color((byte)Channel(1), (byte)Channel(3), (byte)Channel(5));
    }

    /// <summary>
    /// The colour of a wall whose outward normal points at <paramref name="normalDeg"/> (screen angles, 90° towards the viewer).
    /// Light comes from the front left: a wall facing it is shaded 12 %, a wall facing away 38 %, plus <paramref name="extra"/>.
    /// Integer channel maths, so every implementation gives the same colour.
    /// </summary>
    private static Color WallShade(Color color, double normalDeg, int extra = 0)
    {
        double lambert = Math.Max(0, Math.Cos((normalDeg - 125) * Math.PI / 180));
        int pct = (int)Math.Round(38 - 26 * lambert, MidpointRounding.AwayFromZero) + extra;
        string hex = color.ToHexString();
        int Channel(int offset)
        {
            int c = int.Parse(hex.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return c - (c * pct + 50) / 100;
        }
        return new Color((byte)Channel(1), (byte)Channel(3), (byte)Channel(5));
    }

    /// <summary>
    /// Fills the wall that hangs from the rim arc at <paramref name="radius"/> between two angles. The outside of the rim
    /// faces away from the centre, the inside of the hole (<paramref name="inside"/>) towards it; the light changes along the
    /// curve, so the wall is a horizontal gradient sampled every 15° or less. Across the front or back half of the rim,
    /// x follows the angle one to one.
    /// </summary>
    private static void DrawRimWall(ICanvas canvas, PieLayout layout, float radius, float from, float to, Color color, bool inside)
    {
        int segments = Math.Max((int)Math.Ceiling((to - from) / 5), 1);
        List<Point> rim = [];
        for (int i = 0; i <= segments; i++)
        {
            double radians = (from + (double)(to - from) * i / segments) * Math.PI / 180;
            rim.Add(new Point(layout.CenterX + (float)(radius * Math.Cos(radians)), layout.CenterY + (float)(radius * Math.Sin(radians) * layout.YScale)));
        }
        List<Point> outline = new(rim);
        for (int i = rim.Count - 1; i >= 0; i--) outline.Add(new Point(rim[i].X, rim[i].Y + layout.Depth));
        List<ChartPathCommand> commands = new(outline.Count + 1);
        for (int n = 0; n < outline.Count; n++) commands.Add(new(n == 0 ? "M" : "L", [outline[n].X, outline[n].Y]));
        commands.Add(new("Z", []));
        Color Shade(double angle) => WallShade(color, inside ? angle + 180 : angle, inside ? 12 : 0);
        double XAt(double angle) => layout.CenterX + radius * Math.Cos(angle * Math.PI / 180);
        double x0 = XAt(from), x1 = XAt(to);
        if (Math.Abs(x1 - x0) < 0.5)
        {
            ChartVector.Fill(canvas, new ChartPathPlan(commands, Shade((from + (double)to) / 2), null));
            return;
        }
        int count = Math.Max((int)Math.Ceiling((to - from) / 15.0), 1);
        List<ChartGradientStop> stops = [];
        for (int k = 0; k <= count; k++)
        {
            double angle = from + (double)(to - from) * k / count;
            stops.Add(new ChartGradientStop((XAt(angle) - x0) / (x1 - x0), Shade(angle)));
        }
        ChartVector.Fill(canvas, new ChartPathPlan(commands, Shade((from + (double)to) / 2),
            new ChartResolvedGradient(x0, layout.CenterY, x1, layout.CenterY, stops[0].Color, stops[^1].Color, stops)));
    }

    /// <summary>The parts of each slice (or only slice <paramref name="only"/>) that fall between two angles.</summary>
    private static List<(float From, float To, Color Color)> Spans(IList<(float StartAngle, float EndAngle)> angles, IList<Color> colors, float from, float to, int? only = null)
    {
        List<(float, float, Color)> result = [];
        for (int i = 0; i < angles.Count; i++)
        {
            if (only is { } o && i != o) continue;
            (float startAngle, float endAngle) = angles[i];
            float a = Math.Max(startAngle, from);
            float b = Math.Min(endAngle, to);
            if (endAngle > startAngle && b > a) result.Add((a, b, colors[i]));
        }
        return result;
    }

    /// <summary>How far a span of the far half of the hole is from the back (270°): the back is painted first.</summary>
    private static double BackDistance((float From, float To, Color Color) w)
    {
        double mid = (w.From + (double)w.To) / 2;
        return Math.Abs((mid < 0 ? mid + 360 : mid) - 270);
    }

    /// <summary>
    /// Draws the side walls of a tilted pie. A donut first gets the inside of its hole: the far half of the hole's rim
    /// (screen angles 180° to 360°), dropped by the depth and shaded twice, which the top faces then frame. Every pie then
    /// gets the outside of the near half of its rim (0° to 180°), shaded once.
    /// </summary>
    private void DrawPieWalls(ICanvas canvas, IList<(float StartAngle, float EndAngle)> angles, IList<Color> colors, PieLayout layout)
    {
        if (Pie3d is null || layout.Depth <= 0) return;
        if (IsDonut && layout.DonutHoleRadius > 0)
        {
            // Slice angles run from -90° to 270°, so the far half of the hole is -90° to 0° and 180° to 270°.
            foreach ((float from, float to, Color color) in Spans(angles, colors, -90, 0).Concat(Spans(angles, colors, 180, 270)).OrderBy(BackDistance).ToList())
                DrawRimWall(canvas, layout, layout.DonutHoleRadius, from, to, color, true);
        }
        // Outermost angles first, so the wall at the bottom of the pie is painted last.
        foreach ((float from, float to, Color color) in Spans(angles, colors, 0, 180).OrderByDescending(w => Math.Abs((w.From + (double)w.To) / 2 - 90)).ToList())
            DrawRimWall(canvas, layout, layout.Radius, from, to, color, false);
    }

    /// <summary>
    /// Draws the walls of one slice of an exploded tilted pie, before its top: the inside of the hole, the two cut faces
    /// along its edges (which the gap now shows), then the outside of its rim.
    /// </summary>
    private void DrawSliceSolid(ICanvas canvas, int i, IList<(float StartAngle, float EndAngle)> angles, IList<Color> colors, PieLayout pieLayout)
    {
        if (pieLayout.Depth <= 0) return;
        PieLayout layout = SliceLayout(i, angles, pieLayout);
        Color color = colors[i];
        float inner = IsDonut ? layout.DonutHoleRadius : 0;
        if (inner > 0)
        {
            foreach ((float from, float to, Color _) in Spans(angles, colors, -90, 0, i).Concat(Spans(angles, colors, 180, 270, i)).OrderBy(BackDistance).ToList())
                DrawRimWall(canvas, layout, inner, from, to, color, true);
        }
        (float startAngle, float endAngle) = angles[i];
        if (endAngle - startAngle < 360)
        {
            // A cut face's outward normal points away from the slice: back from its start edge, on from its end edge.
            foreach ((float angle, double normal) in new[] { (startAngle, startAngle - 90.0), (endAngle, endAngle + 90.0) })
            {
                double t = angle * Math.PI / 180;
                double cos = Math.Cos(t), sin = Math.Sin(t) * layout.YScale;
                double x1 = layout.CenterX + inner * cos, y1 = layout.CenterY + inner * sin;
                double x2 = layout.CenterX + layout.Radius * cos, y2 = layout.CenterY + layout.Radius * sin;
                List<ChartPathCommand> commands = [new("M", [x1, y1]), new("L", [x2, y2]), new("L", [x2, y2 + layout.Depth]), new("L", [x1, y1 + layout.Depth]), new("Z", [])];
                ChartVector.Fill(canvas, new ChartPathPlan(commands, WallShade(color, normal), null));
            }
        }
        foreach ((float from, float to, Color _) in Spans(angles, colors, 0, 180, i))
            DrawRimWall(canvas, layout, layout.Radius, from, to, color, false);
    }

    private static IList<Point> CreateAnnularSectorPath(float startAngle, float endAngle, PieLayout layout)
    {
        List<Point> path = new();
        int segments = Math.Max((int)Math.Ceiling((endAngle - startAngle) / 5), 1);
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + (endAngle - startAngle) * i / segments;
            float radians = (float)(angle * Math.PI / 180);
            path.Add(new Point(layout.CenterX + (float)(layout.Radius * Math.Cos(radians)), layout.CenterY + (float)(layout.Radius * Math.Sin(radians) * layout.YScale)));
        }
        for (int i = segments; i >= 0; i--)
        {
            float angle = startAngle + (endAngle - startAngle) * i / segments;
            float radians = (float)(angle * Math.PI / 180);
            path.Add(new Point(layout.CenterX + (float)(layout.DonutHoleRadius * Math.Cos(radians)), layout.CenterY + (float)(layout.DonutHoleRadius * Math.Sin(radians) * layout.YScale)));
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
        float PlotBottom,
        double YScale = 1.0,
        float Depth = 0f);
}
