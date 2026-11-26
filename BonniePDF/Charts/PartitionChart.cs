// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>An icicle / flame partition hierarchy chart: each depth level occupies a band and children span their parent proportionally.</summary>
public class PartitionChart : ChartBase
{
    public IList<ChartNode> Nodes { get; set; } = [];
    /// <summary><c>Horizontal</c> (default) places depth levels left to right; <c>Vertical</c> stacks depth levels top to bottom (icicle).</summary>
    public ChartOrientation Orientation { get; set; } = ChartOrientation.Horizontal;
    public int MaxDepth { get; set; }
    public float Padding { get; set; } = 1.5f;
    public string LabelTemplate { get; set; } = "{label}  {value}";
    public string ValueFormat { get; set; } = "G";

    internal override bool UsesStyledGeometry => true;

    public PartitionChart() { AutoLayout = true; }
    public PartitionChart(ChartData data) : base(data) { AutoLayout = true; }

    private static int HierarchyHeight(IReadOnlyList<TreeNode> nodes)
    {
        int best = 0;
        foreach (TreeNode n in nodes)
            best = Math.Max(best, 1 + HierarchyHeight(n.Children));
        return best;
    }

    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        if (Nodes.Count == 0) return;
        if (MaxDepth < 0) throw new InvalidOperationException("Partition maxDepth must be a nonnegative integer.");
        if (!(Padding >= 0)) throw new InvalidOperationException("Partition padding must be nonnegative.");
        List<TreeNode> top = ChartHierarchy.Build(Nodes, Appearance);
        double total = top.Sum(n => n.Total);
        if (!(total > 0)) return;
        int h = HierarchyHeight(top);
        int levels = MaxDepth == 0 ? h : Math.Min(MaxDepth, h);
        string font = ResolveFontFamily();
        string boldFont = string.Equals(font, StandardFonts.Helvetica, StringComparison.Ordinal) ? StandardFonts.HelveticaBold : font;
        double topEdge = (data.Title.Length > 0 ? 56 : 16) + (data.Subtitle.Length > 0 ? 18 : 0);
        double bottomEdge = Height - 14 - (Caption.Length > 0 ? 16 : 0);
        const double x0 = 14;
        double plotW = Math.Max(40, Width - 28);
        double plotH = Math.Max(40, bottomEdge - topEdge);
        string template = LabelTemplate.Replace("{value}", "{value:" + ValueFormat + "}", StringComparison.Ordinal);
        double pad = Padding;
        bool horiz = Orientation == ChartOrientation.Horizontal;
        double bandSize = horiz ? plotW / levels : plotH / levels;

        void DrawNodes(IReadOnlyList<TreeNode> list, double offset, double span, double parentTotal)
        {
            double cur = offset;
            foreach (TreeNode node in list)
            {
                if (!(node.Total > 0)) continue;
                double size = span * node.Total / parentTotal;
                if (MaxDepth == 0 || node.Depth < MaxDepth)
                {
                    double rx = horiz ? x0 + node.Depth * bandSize + pad / 2 : cur + pad / 2;
                    double ry = horiz ? cur + pad / 2 : topEdge + node.Depth * bandSize + pad / 2;
                    double rw = (horiz ? bandSize : size) - pad;
                    double rh = (horiz ? size : bandSize) - pad;
                    if (rw > 0.5 && rh > 0.5)
                    {
                        canvas.FillRect((float)rx, (float)ry, (float)rw, (float)rh, node.Color);
                        string label = ChartTemplate.Apply(template, new Dictionary<string, object> { ["label"] = node.Node.Label, ["value"] = node.Total });
                        string labelFont = node.Depth == 0 ? boldFont : font;
                        Color textColor = ChartValueScale.ContrastTextColor(node.Color);
                        if (rh >= 14 && canvas.MeasureText(label, 10, labelFont).Width + 8 <= rw)
                            canvas.DrawText((float)(rx + 6), (float)(ry + rh / 2 - 5), label, textColor, 10, labelFont, TextAlignment.Left);
                        else if (rh >= 14 && canvas.MeasureText(node.Node.Label, 9, labelFont).Width + 8 <= rw)
                            canvas.DrawText((float)(rx + 6), (float)(ry + rh / 2 - 4.5), node.Node.Label, textColor, 9, labelFont, TextAlignment.Left);
                    }
                    if (node.Children.Count > 0) DrawNodes(node.Children, cur, size, node.Total);
                }
                cur += size;
            }
        }

        DrawNodes(top, horiz ? topEdge : x0, horiz ? plotH : plotW, total);
    }
}
