// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>A treemap: a hierarchy as nested rectangles whose area is proportional to value, laid out by the squarified algorithm.</summary>
public class TreemapChart : ChartBase
{
    private const float Header = 16;

    /// <summary>The hierarchy, as a flat list. See <see cref="ChartNode"/>.</summary>
    public IList<ChartNode> Nodes { get; set; } = [];
    /// <summary>How many levels are drawn: <c>0</c> (default) all of them, <c>1</c> only the first level, and so on.</summary>
    public int Depth { get; set; }
    /// <summary>Space in points between siblings and inside a group.</summary>
    public float Padding { get; set; } = 2;
    /// <summary>The text in each cell; <c>{label}</c> and <c>{value}</c> are replaced, <c>\n</c> starts a new line. Lines that do not fit are dropped from the end.</summary>
    public string LabelTemplate { get; set; } = "{label}\n{value}";
    /// <summary>Number format of <c>{value}</c>.</summary>
    public string ValueFormat { get; set; } = "G";

    internal override bool UsesStyledGeometry => true;

    /// <summary>Creates an empty treemap.</summary>
    public TreemapChart() { AutoLayout = true; }

    /// <summary>Creates a treemap for <paramref name="data"/>.</summary>
    public TreemapChart(ChartData data) : base(data) { AutoLayout = true; }

    /// <inheritdoc />
    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        if (Nodes.Count == 0) return;
        if (Depth < 0) throw new InvalidOperationException("Treemap depth must be a nonnegative integer.");
        if (!(Padding >= 0)) throw new InvalidOperationException("Treemap padding must be nonnegative.");
        List<TreeNode> top = ChartHierarchy.Build(Nodes, Appearance);
        string font = ResolveFontFamily();
        string boldFont = string.Equals(font, StandardFonts.Helvetica, StringComparison.Ordinal) ? StandardFonts.HelveticaBold : font;
        float topEdge = (data.Title.Length > 0 ? 56 : 18) + (data.Subtitle.Length > 0 ? 18 : 0);
        float bottom = Height - 12 - (Caption.Length > 0 ? 16 : 0);
        HierarchyRect bounds = new(12, topEdge, Width - 24, bottom - topEdge);
        string template = LabelTemplate.Replace("{value}", "{value:" + ValueFormat + "}", StringComparison.Ordinal);
        double half = Padding / 2.0;

        void DrawLabel(TreeNode node, HierarchyRect cell, bool group)
        {
            string[] lines = ChartTemplate.Apply(template, new Dictionary<string, object> { ["label"] = node.Node.Label, ["value"] = node.Total }).Split('\n');
            Color color = ChartValueScale.ContrastTextColor(node.Color);
            double maxWidth = cell.Width - 8;
            int maxLines = group ? 1 : (int)Math.Floor((cell.Height - 3) / 12);
            int count = Math.Min(lines.Length, maxLines);
            while (count > 0 && Enumerable.Range(0, count).Any(k => canvas.MeasureText(lines[k], k == 0 ? 10 : 9, k == 0 ? boldFont : font).Width > maxWidth)) count--;
            for (int k = 0; k < count; k++) canvas.DrawText((float)(cell.X + 4), (float)(cell.Y + 3 + k * 12), lines[k], color, k == 0 ? 10 : 9, k == 0 ? boldFont : font, TextAlignment.Left);
        }

        void Layout(IReadOnlyList<TreeNode> list, HierarchyRect rect, int level)
        {
            foreach ((int index, HierarchyRect r) in ChartHierarchy.Squarify(list, rect))
            {
                TreeNode node = list[index];
                HierarchyRect cell = new(r.X + half, r.Y + half, r.Width - Padding, r.Height - Padding);
                if (!(cell.Width > 0 && cell.Height > 0)) continue;
                canvas.FillRect((float)cell.X, (float)cell.Y, (float)cell.Width, (float)cell.Height, node.Color);
                bool group = node.Children.Count > 0 && (Depth == 0 || level + 1 < Depth);
                if (group)
                {
                    DrawLabel(node, new HierarchyRect(cell.X, cell.Y, cell.Width, Header), true);
                    Layout(node.Children, new HierarchyRect(cell.X + Padding, cell.Y + Header, cell.Width - 2 * Padding, cell.Height - Header - Padding), level + 1);
                }
                else DrawLabel(node, cell, false);
            }
        }
        Layout(top, bounds, 0);
    }
}
