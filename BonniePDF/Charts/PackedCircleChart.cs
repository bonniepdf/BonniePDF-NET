// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>Nested circle-packing hierarchy reusing <see cref="ChartNode"/>.</summary>
public class PackedCircleChart : ChartBase
{
    public IList<ChartNode> Nodes { get; set; } = [];
    public float Padding { get; set; } = 3;
    public bool ShowLabels { get; set; } = true;
    public string ValueFormat { get; set; } = "G";

    internal override bool UsesStyledGeometry => true;

    public PackedCircleChart() { AutoLayout = true; }
    public PackedCircleChart(ChartData data) : base(data) { AutoLayout = true; }

    private sealed class PackedItem(TreeNode node, double r, List<PackedItem> children)
    {
        public TreeNode Node { get; } = node;
        public double X { get; set; }
        public double Y { get; set; }
        public double R { get; } = r;
        public List<PackedItem> Children { get; } = children;
    }

    private static double Hypot(double a, double b) => Math.Sqrt(a * a + b * b);

    private static double PackSiblings(List<PackedItem> items, double pad)
    {
        int n = items.Count;
        if (n == 0) return 0;
        items.Sort((a, b) =>
        {
            int cmp = b.R.CompareTo(a.R);
            return cmp != 0 ? cmp : a.Node.Order.CompareTo(b.Node.Order);
        });
        List<PackedItem> placed = [];
        items[0].X = 0;
        items[0].Y = 0;
        placed.Add(items[0]);
        if (n >= 2)
        {
            items[1].X = items[0].R + items[1].R + pad;
            items[1].Y = 0;
            placed.Add(items[1]);
        }
        bool Overlaps(double x, double y, double r)
        {
            foreach (PackedItem p in placed)
            {
                double dx = x - p.X;
                double dy = y - p.Y;
                double minD = r + p.R + pad - 1e-4;
                if (dx * dx + dy * dy < minD * minD) return true;
            }
            return false;
        }
        for (int i = 2; i < n; i++)
        {
            PackedItem cur = items[i];
            double bestX = 0, bestY = 0, bestDist = double.PositiveInfinity;
            for (int a = 0; a < placed.Count; a++)
            {
                for (int b = a + 1; b < placed.Count; b++)
                {
                    PackedItem ca = placed[a];
                    PackedItem cb = placed[b];
                    double d = Hypot(cb.X - ca.X, cb.Y - ca.Y);
                    double r1 = ca.R + cur.R + pad;
                    double r2 = cb.R + cur.R + pad;
                    if (d < 1e-6 || d > r1 + r2 + 1e-6 || d < Math.Abs(r1 - r2) - 1e-6) continue;
                    double ex = (cb.X - ca.X) / d;
                    double ey = (cb.Y - ca.Y) / d;
                    double xOff = (r1 * r1 - r2 * r2 + d * d) / (2 * d);
                    double hSq = Math.Max(0, r1 * r1 - xOff * xOff);
                    double h = Math.Sqrt(hSq);
                    for (int s = 0; s < 2; s++)
                    {
                        double sign = s == 0 ? 1 : -1;
                        double px = ca.X + xOff * ex - sign * h * ey;
                        double py = ca.Y + xOff * ey + sign * h * ex;
                        if (Overlaps(px, py, cur.R)) continue;
                        double dist = px * px + py * py;
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestX = px;
                            bestY = py;
                        }
                    }
                }
            }
            if (!double.IsFinite(bestDist))
            {
                PackedItem last = placed[^1];
                bestX = last.X + last.R + cur.R + pad;
                bestY = 0;
            }
            cur.X = bestX;
            cur.Y = bestY;
            placed.Add(cur);
        }

        double minX = double.PositiveInfinity, maxX = double.NegativeInfinity, minY = double.PositiveInfinity, maxY = double.NegativeInfinity;
        foreach (PackedItem p in placed)
        {
            minX = Math.Min(minX, p.X - p.R);
            maxX = Math.Max(maxX, p.X + p.R);
            minY = Math.Min(minY, p.Y - p.R);
            maxY = Math.Max(maxY, p.Y + p.R);
        }
        double cx = (minX + maxX) / 2;
        double cy = (minY + maxY) / 2;
        double maxR = 0;
        foreach (PackedItem p in placed)
        {
            p.X -= cx;
            p.Y -= cy;
            maxR = Math.Max(maxR, Hypot(p.X, p.Y) + p.R);
        }
        return maxR;
    }

    private static PackedItem PackTree(TreeNode node, double pad)
    {
        if (node.Children.Count == 0)
            return new PackedItem(node, Math.Sqrt(Math.Max(1e-6, node.Total)), []);
        List<PackedItem> kids = node.Children.Where(c => c.Total > 0).Select(c => PackTree(c, pad)).ToList();
        double encR = PackSiblings(kids, pad);
        return new PackedItem(node, encR + pad * 2, kids);
    }

    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        if (Nodes.Count == 0) return;
        if (!(Padding >= 0)) throw new InvalidOperationException("PackedCircle padding must be nonnegative.");
        List<TreeNode> top = ChartHierarchy.Build(Nodes, Appearance).Where(n => n.Total > 0).ToList();
        if (top.Count == 0) return;
        string font = ResolveFontFamily();
        string boldFont = string.Equals(font, StandardFonts.Helvetica, StringComparison.Ordinal) ? StandardFonts.HelveticaBold : font;
        Color bg = EffectiveBackgroundColor ?? Color.White;
        const double unitPad = 0.25;
        List<PackedItem> packedTop = top.Select(n => PackTree(n, unitPad)).ToList();
        double rootR = PackSiblings(packedTop, unitPad * 1.5);
        if (!(rootR > 0)) return;

        double topEdge = (data.Title.Length > 0 ? 56 : 16) + (data.Subtitle.Length > 0 ? 18 : 0);
        double bottomEdge = Height - 14 - (Caption.Length > 0 ? 16 : 0);
        double cx = Width / 2.0;
        double cy = topEdge + (bottomEdge - topEdge) / 2;
        double targetR = Math.Max(20, Math.Min(Width / 2.0 - 14, (bottomEdge - topEdge) / 2 - 6));
        double scale = targetR / rootR;

        void Render(PackedItem item, double ox, double oy)
        {
            double ix = ox + item.X * scale;
            double iy = oy + item.Y * scale;
            double ir = Math.Max(1, item.R * scale - (item.Children.Count == 0 ? Padding * 0.25 : 0));
            bool isGroup = item.Children.Count > 0;
            List<ChartPathCommand> cmds = ChartArc.CircleCommands(ix, iy, ir);
            if (isGroup)
            {
                ChartVector.Fill(canvas, new ChartPathPlan(cmds, ChartTint.Dim(item.Node.Color, bg, 0.14), null));
                ChartVector.Stroke(canvas, new ChartStrokePlan(cmds, ChartTint.Dim(item.Node.Color, bg, 0.55), 1.25));
                foreach (PackedItem child in item.Children) Render(child, ix, iy);
                if (ShowLabels && ir >= 24)
                    canvas.DrawText((float)ix, (float)(iy - ir + 5), item.Node.Node.Label, item.Node.Color, 9, boldFont, TextAlignment.Center);
            }
            else
            {
                ChartVector.Fill(canvas, new ChartPathPlan(cmds, item.Node.Color, null));
                if (ShowLabels)
                {
                    Color textColor = ChartValueScale.ContrastTextColor(item.Node.Color);
                    string name = item.Node.Node.Label;
                    if (canvas.MeasureText(name, 10, boldFont).Width + 6 <= ir * 1.85)
                    {
                        string val = ChartNumberFormat.Format(ChartTemplate.ToDecimal(item.Node.Total), ValueFormat);
                        bool showVal = ir >= 22 && canvas.MeasureText(val, 9, font).Width + 6 <= ir * 1.7;
                        canvas.DrawText((float)ix, (float)(showVal ? iy - 10 : iy - 5), name, textColor, 10, boldFont, TextAlignment.Center);
                        if (showVal) canvas.DrawText((float)ix, (float)(iy + 2), val, textColor, 9, font, TextAlignment.Center);
                    }
                }
            }
        }

        foreach (PackedItem item in packedTop) Render(item, cx, cy);
    }
}
