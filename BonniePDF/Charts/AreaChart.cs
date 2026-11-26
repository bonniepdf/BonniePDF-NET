// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Area chart that renders a filled polygon (or curved vertical fade) with a line overlay.
/// </summary>
public class AreaChart : CartesianChartBase
{
    internal override bool SupportsAnnotations => true;

    internal override bool SupportsTimeAxis => true;

    /// <summary>Thin lines from each point to the category axis.</summary>
    public bool DropLines { get; set; }

    /// <inheritdoc />
    protected override void DrawAllSeries(ICanvas canvas, ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (DropLines) DrawDropLines(canvas, data, chartAreaWidth, chartAreaHeight, baselineY);
        base.DrawAllSeries(canvas, data, chartAreaWidth, chartAreaHeight, baselineY, topY);
    }

    private const string DefaultAreaColorHex = "#4f46e5";

    public AreaChart() { }
    public AreaChart(ChartData data) : base(data) { }

    public float DefaultFillOpacity { get; set; } = 0.3f;
    public float DefaultLineWidth { get; set; } = 2f;
    public Color AreaColor { get; set; } = Color.FromHex(DefaultAreaColorHex);
    public float FillOpacity { get; set; } = 0.3f;
    public float LineWidth { get; set; } = 2f;
    public string Curve { get; set; } = "linear";
    public bool AreaGradient { get; set; }

    internal override bool UsesStyledGeometry => base.UsesStyledGeometry || AreaGradient || Curve is not "linear";
    protected override bool SupportsInlineLegend => true;

    protected override void DrawSeriesElements(ICanvas canvas, ChartData data, ChartSeries series,
        IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        float fillOpacity = series.Style?.FillOpacity > 0 && series.Style.FillOpacity != DefaultFillOpacity
            ? series.Style.FillOpacity
            : (data.Series.Count == 1 ? FillOpacity : DefaultFillOpacity);
        float lineWidth = series.Style?.LineWidth > 0 && series.Style.LineWidth != DefaultLineWidth
            ? series.Style.LineWidth
            : (data.Series.Count == 1 ? LineWidth : DefaultLineWidth);

        float origin = ValueOrigin(data, chartAreaWidth, chartAreaHeight, baselineY);
        Color color = ResolveSeriesColor(series);
        string curve = series.Style?.Curve ?? Curve;
        int seriesIdx = data.Series.IndexOf(series);
        float[]? dash = ResolveSeriesDashPattern(series, seriesIdx);

        DrawAreaFill(canvas, coordinates, origin, color, fillOpacity, curve);
        DrawSeriesLine(canvas, coordinates, ResolveSeriesStrokeColor(series), lineWidth, curve, dash, ResolveLineEffects(series));
    }

    protected override float GetLabelVerticalOffset(ChartSeries series) => 7;

    private void DrawAreaFill(ICanvas canvas, IList<Point> coordinates, float origin, Color color, float opacity, string curve)
    {
        if (coordinates.Count < 2) return;
        bool horizontal = Orientation == ChartOrientation.Horizontal;
        Point first = coordinates[0];
        Point last = coordinates[^1];

        if (!AreaGradient && curve is "linear")
        {
            Color fillColor = color.WithAlpha(opacity);
            List<Point> poly = new(coordinates.Count + 2);
            if (horizontal)
            {
                poly.Add(new Point(origin, first.Y));
                poly.AddRange(coordinates);
                poly.Add(new Point(origin, last.Y));
            }
            else
            {
                poly.Add(new Point(first.X, origin));
                poly.AddRange(coordinates);
                poly.Add(new Point(last.X, origin));
            }
            canvas.FillPath(poly, fillColor);
            return;
        }

        var curveCmds = ChartCurves.BuildCurveCommands((IReadOnlyList<Point>)coordinates, curve, horizontal, moveToFirst: false);
        List<ChartPathCommand> commands = new();
        if (horizontal)
        {
            commands.Add(new("M", [origin, first.Y]));
            commands.AddRange(curveCmds);
            commands.Add(new("L", [origin, last.Y]));
            commands.Add(new("Z", []));
        }
        else
        {
            commands.Add(new("M", [first.X, origin]));
            commands.AddRange(curveCmds);
            commands.Add(new("L", [last.X, origin]));
            commands.Add(new("Z", []));
        }

        if (!AreaGradient)
        {
            ChartVector.Fill(canvas, new ChartPathPlan(commands, color.WithAlpha(opacity), null));
            return;
        }

        Color topColor = color.WithAlpha(opacity);
        Color bottomColor = color.WithAlpha(0.02f);
        float peak = origin;
        foreach (Point p in coordinates)
        {
            if (horizontal)
            {
                if (MathF.Abs(p.X - origin) > MathF.Abs(peak - origin)) peak = p.X;
            }
            else
            {
                if (MathF.Abs(p.Y - origin) > MathF.Abs(peak - origin)) peak = p.Y;
            }
        }
        if (peak == origin) peak = horizontal ? origin + 1 : origin - 1;

        ChartVector.Fill(canvas, new ChartPathPlan(
            commands,
            topColor,
            new ChartResolvedGradient(
                horizontal ? peak : first.X,
                horizontal ? first.Y : peak,
                horizontal ? origin : first.X,
                horizontal ? first.Y : origin,
                topColor,
                bottomColor,
                [new ChartGradientStop(0, topColor), new ChartGradientStop(1, bottomColor)])));
    }

    protected override Color GetDefaultSeriesColor() => AreaColor;
}
