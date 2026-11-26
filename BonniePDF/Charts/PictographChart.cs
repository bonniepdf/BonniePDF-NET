// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>Category rows of repeated unit vector icons with fractional trailing-icon clipping.</summary>
public class PictographChart : ChartBase
{
    public string Icon { get; set; } = "person";
    public decimal UnitValue { get; set; } = 10m;
    public float IconSize { get; set; } = 18;
    public float IconGap { get; set; } = 4;
    public int MaxIconsPerRow { get; set; } = 10;
    public bool ShowTrack { get; set; }
    public int TrackTotal { get; set; } = 10;
    public bool ShowValueLabels { get; set; } = true;
    public string ValueFormat { get; set; } = "G";
    public string ValueSuffix { get; set; } = "";

    internal override bool UsesStyledGeometry => true;

    public PictographChart() { AutoLayout = true; }
    public PictographChart(ChartData data) : base(data) { AutoLayout = true; }

    private static List<List<ChartPathCommand>> IconPaths(string kind, double x, double y, double s)
    {
        if (string.Equals(kind, "circle", StringComparison.Ordinal)) return [ChartArc.CircleCommands(x + s / 2, y + s / 2, s * 0.44)];
        if (string.Equals(kind, "box", StringComparison.Ordinal)) return [ChartArc.RoundedRectCommands(x + s * 0.08, y + s * 0.08, s * 0.84, s * 0.84, s * 0.16)];
        if (string.Equals(kind, "person", StringComparison.Ordinal))
        {
            List<ChartPathCommand> head = ChartArc.CircleCommands(x + s * 0.5, y + s * 0.22, s * 0.16);
            List<ChartPathCommand> body =
            [
                new("M", [x + s * 0.22, y + s * 0.94]),
                new("L", [x + s * 0.22, y + s * 0.56]),
                new("C", [x + s * 0.22, y + s * 0.44, x + s * 0.34, y + s * 0.42, x + s * 0.5, y + s * 0.42]),
                new("C", [x + s * 0.66, y + s * 0.42, x + s * 0.78, y + s * 0.44, x + s * 0.78, y + s * 0.56]),
                new("L", [x + s * 0.78, y + s * 0.94]),
                new("Z", []),
            ];
            return [head, body];
        }
        if (string.Equals(kind, "star", StringComparison.Ordinal))
        {
            List<ChartPathCommand> cmds = [];
            for (int i = 0; i < 10; i++)
            {
                double a = -Math.PI / 2 + i * Math.PI / 5;
                double r = i % 2 == 0 ? s * 0.46 : s * 0.2;
                cmds.Add(new(i == 0 ? "M" : "L", [x + s * 0.5 + r * Math.Cos(a), y + s * 0.52 + r * Math.Sin(a)]));
            }
            cmds.Add(new("Z", []));
            return [cmds];
        }
        if (string.Equals(kind, "heart", StringComparison.Ordinal))
        {
            return [[
                new("M", [x + s * 0.5, y + s * 0.88]),
                new("C", [x + s * 0.1, y + s * 0.6, x + s * 0.06, y + s * 0.24, x + s * 0.28, y + s * 0.16]),
                new("C", [x + s * 0.42, y + s * 0.12, x + s * 0.5, y + s * 0.26, x + s * 0.5, y + s * 0.32]),
                new("C", [x + s * 0.5, y + s * 0.26, x + s * 0.58, y + s * 0.12, x + s * 0.72, y + s * 0.16]),
                new("C", [x + s * 0.94, y + s * 0.24, x + s * 0.9, y + s * 0.6, x + s * 0.5, y + s * 0.88]),
                new("Z", []),
            ]];
        }
        if (string.Equals(kind, "check", StringComparison.Ordinal))
        {
            return [[
                new("M", [x + s * 0.14, y + s * 0.54]),
                new("L", [x + s * 0.38, y + s * 0.78]),
                new("L", [x + s * 0.86, y + s * 0.24]),
                new("L", [x + s * 0.76, y + s * 0.14]),
                new("L", [x + s * 0.38, y + s * 0.58]),
                new("L", [x + s * 0.24, y + s * 0.44]),
                new("Z", []),
            ]];
        }
        return [[
            new("M", [x + s * 0.18, y + s * 0.08]),
            new("L", [x + s * 0.62, y + s * 0.08]),
            new("L", [x + s * 0.84, y + s * 0.3]),
            new("L", [x + s * 0.84, y + s * 0.92]),
            new("L", [x + s * 0.18, y + s * 0.92]),
            new("Z", []),
        ]];
    }

    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        IList<ChartDataPoint> points = data.Series.Count > 0 ? data.Series[0].DataPoints : [];
        int n = points.Count;
        if (n == 0) return;
        double unitVal = (double)UnitValue;
        if (!(unitVal > 0)) throw new InvalidOperationException("Pictograph unitValue must be positive.");
        if (points.Any(p => p.Value < 0)) throw new InvalidOperationException("Pictograph values must be finite and nonnegative.");

        string font = ResolveFontFamily();
        Color bg = EffectiveBackgroundColor ?? Color.White;
        double topEdge = (data.Title.Length > 0 ? 56 : 16) + (data.Subtitle.Length > 0 ? 18 : 0);
        double bottomEdge = Height - 16 - (Caption.Length > 0 ? 16 : 0);
        double labelW = Math.Min(Width * 0.3, points.Max(p => (double)canvas.MeasureText(p.Label, 11, font).Width) + 16);
        string[] valueLabels = points.Select(p => ChartNumberFormat.ApplyAffixes(ChartNumberFormat.Format(p.Value, ValueFormat), "", ValueSuffix)).ToArray();
        double valW = ShowValueLabels ? valueLabels.Max(t => (double)canvas.MeasureText(t, 11, font).Width) + 16 : 12;
        double startX = 16 + labelW;
        double availW = Math.Max(40, Width - startX - valW - 12);
        int maxCols = Math.Max(1, Math.Min(MaxIconsPerRow, (int)Math.Floor((availW + IconGap) / (IconSize + IconGap))));
        double rowH = (bottomEdge - topEdge) / n;
        double size = Math.Min(IconSize, Math.Max(10, rowH - 8));

        void DrawIcon(double x, double y, Color color, double? clipFraction = null)
        {
            List<List<ChartPathCommand>> shapes = IconPaths(Icon, x, y, size);
            if (clipFraction is { } cf && cf < 0.999)
            {
                ChartVector.PushClipRect(canvas, (float)x, (float)y, (float)(size * cf), (float)size);
                foreach (List<ChartPathCommand> cmds in shapes) ChartVector.Fill(canvas, new ChartPathPlan(cmds, color, null));
                ChartVector.PopClip(canvas);
            }
            else
            {
                foreach (List<ChartPathCommand> cmds in shapes) ChartVector.Fill(canvas, new ChartPathPlan(cmds, color, null));
            }
        }

        for (int i = 0; i < n; i++)
        {
            ChartDataPoint p = points[i];
            double cy = topEdge + rowH * (i + 0.5);
            double iy = cy - size / 2;
            Color color = ChartCategoryColors.For(Appearance, p, i);
            Color trackColor = ChartTint.Dim(color, bg, 0.16);
            canvas.DrawText((float)(startX - 10), (float)(cy - 5.5), p.Label, Appearance.LegendTextColor, 11, font, TextAlignment.Right);

            double units = Math.Min(maxCols, (double)p.Value / unitVal);
            int full = (int)Math.Floor(units);
            double frac = units - full;
            int slotCount = ShowTrack ? Math.Min(maxCols, Math.Max(TrackTotal, (int)Math.Ceiling(units))) : (int)Math.Ceiling(units);

            for (int k = 0; k < slotCount; k++)
            {
                double ix = startX + k * (size + IconGap);
                if (ShowTrack) DrawIcon(ix, iy, trackColor);
                if (k < full) DrawIcon(ix, iy, color);
                else if (k == full && frac > 0.02) DrawIcon(ix, iy, color, frac);
            }

            if (ShowValueLabels)
            {
                double endX = startX + slotCount * (size + IconGap) + 6;
                canvas.DrawText((float)endX, (float)(cy - 5.5), valueLabels[i], Appearance.AxisColor, 11, font, TextAlignment.Left);
            }
        }
    }
}
