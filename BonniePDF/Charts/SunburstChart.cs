// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>A sunburst: a hierarchy as concentric rings, each node an arc whose angle is proportional to its value.</summary>
public class SunburstChart : ChartBase
{
    /// <summary>The hierarchy, as a flat list. See <see cref="ChartNode"/>.</summary>
    public IList<ChartNode> Nodes { get; set; } = [];
    /// <summary>How many rings are drawn: <c>0</c> (default) all of them.</summary>
    public int MaxDepth { get; set; }
    /// <summary>The hole in the middle as a fraction of the radius (<c>0.25</c>).</summary>
    public float HoleRatio { get; set; } = 0.25f;
    /// <summary>Write the total in the hole.</summary>
    public bool ShowTotal { get; set; } = true;
    /// <summary>Number format of the total.</summary>
    public string ValueFormat { get; set; } = "G";

    internal override bool UsesStyledGeometry => true;

    /// <summary>Creates an empty sunburst.</summary>
    public SunburstChart() { AutoLayout = true; }

    /// <summary>Creates a sunburst for <paramref name="data"/>.</summary>
    public SunburstChart(ChartData data) : base(data) { AutoLayout = true; }

    /// <summary>Cubic segments of an arc of radius <paramref name="r"/> from <paramref name="a0"/> to <paramref name="a1"/> (radians, clockwise on screen), each at most a quarter turn.</summary>
    private static List<ChartPathCommand> ArcSegments(double cx, double cy, double r, double a0, double a1)
    {
        int n = Math.Max(1, (int)Math.Ceiling(Math.Abs(a1 - a0) / (Math.PI / 2) - 1e-9));
        List<ChartPathCommand> output = [];
        for (int i = 0; i < n; i++)
        {
            double t0 = a0 + (a1 - a0) * i / n;
            double t1 = a0 + (a1 - a0) * (i + 1) / n;
            double k = 4.0 / 3 * Math.Tan((t1 - t0) / 4);
            output.Add(new("C", [
                cx + r * Math.Cos(t0) - k * r * Math.Sin(t0), cy + r * Math.Sin(t0) + k * r * Math.Cos(t0),
                cx + r * Math.Cos(t1) + k * r * Math.Sin(t1), cy + r * Math.Sin(t1) - k * r * Math.Cos(t1),
                cx + r * Math.Cos(t1), cy + r * Math.Sin(t1)]));
        }
        return output;
    }

    private static int HierarchyHeight(IReadOnlyList<TreeNode> nodes) => nodes.Count == 0 ? 0 : 1 + nodes.Max(n => HierarchyHeight(n.Children));

    /// <inheritdoc />
    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        if (Nodes.Count == 0) return;
        if (MaxDepth < 0) throw new InvalidOperationException("Sunburst maxDepth must be a nonnegative integer.");
        if (!(HoleRatio >= 0 && HoleRatio <= 0.9)) throw new InvalidOperationException("Sunburst holeRatio must be from 0 to 0.9.");
        List<TreeNode> top = ChartHierarchy.Build(Nodes, Appearance);
        double total = top.Sum(n => n.Total);
        if (!(total > 0)) return;
        int levels = MaxDepth == 0 ? HierarchyHeight(top) : Math.Min(MaxDepth, HierarchyHeight(top));
        string font = ResolveFontFamily();
        string boldFont = string.Equals(font, StandardFonts.Helvetica, StringComparison.Ordinal) ? StandardFonts.HelveticaBold : font;
        double topEdge = (data.Title.Length > 0 ? 56 : 18) + (data.Subtitle.Length > 0 ? 18 : 0);
        double bottom = base.Height - 12 - (Caption.Length > 0 ? 16 : 0);
        double cx = Width / 2.0;
        double cy = (topEdge + bottom) / 2;
        double radius = Math.Max(20, Math.Min(Width / 2.0 - 12, (bottom - topEdge) / 2 - 4));
        double hole = HoleRatio * radius;
        double thickness = (radius - hole) / levels;
        Color background = EffectiveBackgroundColor ?? Color.White;

        void DrawArc(TreeNode node, double a0, double a1)
        {
            double inner = hole + node.Depth * thickness;
            double outer = inner + thickness;
            List<ChartPathCommand> commands = [new("M", [cx + outer * Math.Cos(a0), cy + outer * Math.Sin(a0)])];
            commands.AddRange(ArcSegments(cx, cy, outer, a0, a1));
            commands.Add(new("L", [cx + inner * Math.Cos(a1), cy + inner * Math.Sin(a1)]));
            commands.AddRange(ArcSegments(cx, cy, inner, a1, a0));
            commands.Add(new("Z", []));
            ChartVector.Fill(canvas, new ChartPathPlan(commands, node.Color, null));
            ChartVector.Stroke(canvas, new ChartStrokePlan(commands, background, 1, "butt", "round", 10));
            string label = node.Node.Label;
            const double size = 10;
            double w = canvas.MeasureText(label, (float)size, font).Width;
            double h = size * 1.2;
            double mid = (a0 + a1) / 2;
            double rm = (inner + outer) / 2;
            double arcLength = rm * (a1 - a0);
            double? phi = null;
            if (arcLength >= w + 6 && thickness >= h + 2) phi = Upright(mid * 180 / Math.PI + 90);
            else if (thickness >= w + 6 && arcLength >= h + 2) phi = Upright(mid * 180 / Math.PI);
            if (phi is not { } angle) return;
            angle %= 360;
            double t = angle * Math.PI / 180;
            double mx = cx + rm * Math.Cos(mid), my = cy + rm * Math.Sin(mid);
            double px = mx - (w / 2 * Math.Cos(t) - h / 2 * Math.Sin(t));
            double py = my - (w / 2 * Math.Sin(t) + h / 2 * Math.Cos(t));
            canvas.DrawTextRotated((float)px, (float)py, label, ChartValueScale.ContrastTextColor(node.Color), (float)size, font, (float)angle);
        }

        void Lay(IReadOnlyList<TreeNode> list, double start, double span, double parentTotal)
        {
            double a = start;
            foreach (TreeNode node in list)
            {
                if (!(node.Total > 0)) continue;
                double next = a + span * node.Total / parentTotal;
                if (MaxDepth == 0 || node.Depth < MaxDepth)
                {
                    DrawArc(node, a, next);
                    Lay(node.Children, a, next - a, node.Total);
                }
                a = next;
            }
        }
        Lay(top, -Math.PI / 2, 2 * Math.PI, total);

        if (ShowTotal && hole > 0)
        {
            canvas.DrawText((float)cx, (float)(cy - 11), ChartNumberFormat.Format(ChartTemplate.ToDecimal(total), ValueFormat), Appearance.LegendTextColor, 14, boldFont, TextAlignment.Center);
            canvas.DrawText((float)cx, (float)(cy + 7), "Total", Appearance.AxisColor, 9, font, TextAlignment.Center);
        }
    }

    /// <summary>Normalises an angle to [0, 360) and turns it half a turn when text at that angle would read upside down.</summary>
    private static double Upright(double degrees)
    {
        double n = (degrees % 360 + 360) % 360;
        return n > 90 && n < 270 ? n + 180 : n;
    }
}
