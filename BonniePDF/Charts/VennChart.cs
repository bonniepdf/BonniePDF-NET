// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>One set of a <see cref="VennChart"/>.</summary>
public sealed class VennSet
{
    public required string Id { get; set; }
    public required string Label { get; set; }
    public decimal Size { get; set; }
    public Color? Color { get; set; }
}

/// <summary>An overlap between 2 or 3 sets of a <see cref="VennChart"/>.</summary>
public sealed class VennIntersection
{
    public IList<string> Sets { get; set; } = [];
    public decimal Size { get; set; }
    public string? Label { get; set; }
}

/// <summary>Area-proportional 2- and 3-set Venn diagram with intersection labels.</summary>
public class VennChart : ChartBase
{
    public IList<VennSet> Sets { get; set; } = [];
    public IList<VennIntersection> Intersections { get; set; } = [];
    public float FillOpacity { get; set; } = 0.45f;
    public float StrokeWidth { get; set; } = 2;
    public bool ShowValues { get; set; } = true;
    public string ValueFormat { get; set; } = "G";

    internal override bool UsesStyledGeometry => true;

    public VennChart() { AutoLayout = true; }
    public VennChart(ChartData data) : base(data) { AutoLayout = true; }

    private static double CircleOverlapArea(double r1, double r2, double d)
    {
        if (d >= r1 + r2) return 0;
        if (d <= Math.Abs(r1 - r2)) return Math.PI * Math.Min(r1, r2) * Math.Min(r1, r2);
        double a1 = Math.Acos(Math.Max(-1, Math.Min(1, (d * d + r1 * r1 - r2 * r2) / (2 * d * r1))));
        double a2 = Math.Acos(Math.Max(-1, Math.Min(1, (d * d + r2 * r2 - r1 * r1) / (2 * d * r2))));
        double s = Math.Max(0, (-d + r1 + r2) * (d + r1 - r2) * (d - r1 + r2) * (d + r1 + r2));
        return r1 * r1 * a1 + r2 * r2 * a2 - 0.5 * Math.Sqrt(s);
    }

    private static double SolveDistance(double r1, double r2, double targetArea)
    {
        double maxOverlap = Math.PI * Math.Min(r1, r2) * Math.Min(r1, r2);
        if (targetArea <= 0) return r1 + r2 + Math.Min(r1, r2) * 0.15;
        if (targetArea >= maxOverlap) return Math.Abs(r1 - r2);
        double lo = Math.Abs(r1 - r2);
        double hi = r1 + r2;
        for (int i = 0; i < 40; i++)
        {
            double mid = (lo + hi) / 2;
            if (CircleOverlapArea(r1, r2, mid) > targetArea) lo = mid;
            else hi = mid;
        }
        return (lo + hi) / 2;
    }

    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        int n = Sets.Count;
        if (n == 0) return;
        if (n < 2 || n > 3) throw new InvalidOperationException("VennChart supports 2 or 3 sets.");
        if (!(FillOpacity > 0 && FillOpacity <= 1)) throw new InvalidOperationException("Venn fillOpacity must be above 0 and at most 1.");
        Color[] colors = new Color[n];
        for (int i = 0; i < n; i++)
        {
            if (Sets[i].Size <= 0) throw new InvalidOperationException($"Venn set '{Sets[i].Id}' size must be positive.");
            colors[i] = Sets[i].Color ?? ChartCategoryColors.For(Appearance, null, i);
        }

        double PairOverlap(string idA, string idB)
        {
            double sum = 0;
            foreach (VennIntersection inter in Intersections)
            {
                if (inter.Sets.Count == 2 && inter.Sets.Contains(idA) && inter.Sets.Contains(idB)) sum = (double)inter.Size;
            }
            return Math.Max(0, sum);
        }

        double[] radii = Sets.Select(s => Math.Sqrt((double)s.Size / Math.PI)).ToArray();
        double[] xs = new double[n];
        double[] ys = new double[n];

        double d01 = SolveDistance(radii[0], radii[1], PairOverlap(Sets[0].Id, Sets[1].Id));
        xs[0] = 0; ys[0] = 0;
        xs[1] = d01; ys[1] = 0;

        if (n == 3)
        {
            double d02 = SolveDistance(radii[0], radii[2], PairOverlap(Sets[0].Id, Sets[2].Id));
            double d12 = SolveDistance(radii[1], radii[2], PairOverlap(Sets[1].Id, Sets[2].Id));
            double minSum = d01 + 1e-3;
            if (d02 + d12 < minSum)
            {
                double boost = (minSum - (d02 + d12)) / 2;
                d02 += boost;
                d12 += boost;
            }
            double x2 = d01 > 1e-6 ? (d02 * d02 - d12 * d12 + d01 * d01) / (2 * d01) : 0;
            double y2Sq = Math.Max(0, d02 * d02 - x2 * x2);
            xs[2] = x2;
            ys[2] = Math.Sqrt(y2Sq);
        }

        double minX = double.PositiveInfinity, maxX = double.NegativeInfinity, minY = double.PositiveInfinity, maxY = double.NegativeInfinity;
        for (int i = 0; i < n; i++)
        {
            minX = Math.Min(minX, xs[i] - radii[i]);
            maxX = Math.Max(maxX, xs[i] + radii[i]);
            minY = Math.Min(minY, ys[i] - radii[i]);
            maxY = Math.Max(maxY, ys[i] + radii[i]);
        }
        string font = ResolveFontFamily();
        string boldFont = string.Equals(font, StandardFonts.Helvetica, StringComparison.Ordinal) ? StandardFonts.HelveticaBold : font;
        double topEdge = (data.Title.Length > 0 ? 56 : 16) + (data.Subtitle.Length > 0 ? 18 : 0);
        double bottomEdge = Height - 16 - (Caption.Length > 0 ? 16 : 0);
        double availW = Math.Max(60, Width - 48);
        double availH = Math.Max(60, bottomEdge - topEdge - 12);
        double spanW = Math.Max(1e-6, maxX - minX);
        double spanH = Math.Max(1e-6, maxY - minY);
        double scale = Math.Min(availW / spanW, availH / spanH);
        double midX = (minX + maxX) / 2;
        double midY = (minY + maxY) / 2;
        double cx0 = Width / 2.0;
        double cy0 = topEdge + (bottomEdge - topEdge) / 2;

        double[] sx = xs.Select(x => cx0 + (x - midX) * scale).ToArray();
        double[] sy = ys.Select(y => cy0 + (y - midY) * scale).ToArray();
        double[] sr = radii.Select(r => r * scale).ToArray();

        for (int i = 0; i < n; i++)
        {
            List<ChartPathCommand> cmds = ChartArc.CircleCommands(sx[i], sy[i], sr[i]);
            ChartVector.Fill(canvas, new ChartPathPlan(cmds, colors[i].WithAlpha(FillOpacity), null));
            if (StrokeWidth > 0)
                ChartVector.Stroke(canvas, new ChartStrokePlan(cmds, colors[i], StrokeWidth));
        }

        double avgX = sx.Sum() / n;
        double avgY = sy.Sum() / n;
        for (int i = 0; i < n; i++)
        {
            double dx = sx[i] - avgX;
            double dy = sy[i] - avgY;
            double len = Math.Sqrt(dx * dx + dy * dy);
            double push = sr[i] * 0.36;
            double lx = len > 1e-4 ? sx[i] + dx / len * push : sx[i];
            double ly = len > 1e-4 ? sy[i] + dy / len * push : sy[i];
            canvas.DrawText((float)lx, (float)(ShowValues ? ly - 10 : ly - 5), Sets[i].Label, Appearance.LegendTextColor, 11, boldFont, TextAlignment.Center);
            if (ShowValues)
                canvas.DrawText((float)lx, (float)(ly + 3), ChartNumberFormat.Format(Sets[i].Size, ValueFormat), Appearance.AxisColor, 10, font, TextAlignment.Center);
        }

        if (ShowValues)
        {
            foreach (VennIntersection inter in Intersections)
            {
                if (inter.Sets.Count == 2)
                {
                    int i0 = -1, i1 = -1;
                    for (int k = 0; k < n; k++)
                    {
                        if (string.Equals(Sets[k].Id, inter.Sets[0], StringComparison.Ordinal)) i0 = k;
                        if (string.Equals(Sets[k].Id, inter.Sets[1], StringComparison.Ordinal)) i1 = k;
                    }
                    if (i0 < 0 || i1 < 0 || inter.Size <= 0) continue;
                    double mx = (sx[i0] + sx[i1]) / 2;
                    double my = (sy[i0] + sy[i1]) / 2;
                    string text = inter.Label ?? ChartNumberFormat.Format(inter.Size, ValueFormat);
                    canvas.DrawText((float)mx, (float)(my - 5), text, Appearance.LegendTextColor, 10, boldFont, TextAlignment.Center);
                }
            }
            VennIntersection? tri = Intersections.FirstOrDefault(inter => inter.Sets.Count == 3);
            if (tri is not null && n == 3 && tri.Size > 0)
            {
                string text = tri.Label ?? ChartNumberFormat.Format(tri.Size, ValueFormat);
                canvas.DrawText((float)avgX, (float)(avgY - 5), text, Appearance.LegendTextColor, 10, boldFont, TextAlignment.Center);
            }
        }
    }
}
