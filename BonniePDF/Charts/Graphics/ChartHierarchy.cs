// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
namespace BonniePDF;

/// <summary>One node of a hierarchy, in a flat list.</summary>
public sealed class ChartNode
{
    /// <summary>Unique id.</summary>
    public required string Id { get; set; }
    /// <summary>The parent's id. A node without one is a root.</summary>
    public string? ParentId { get; set; }
    /// <summary>The node's label.</summary>
    public required string Label { get; set; }
    /// <summary>The size of a leaf, <c>&gt;= 0</c>. Internal nodes must not set it: it is the sum of their children.</summary>
    public decimal? Value { get; set; }
    /// <summary>The node's colour. Descendants without one inherit it, tinted.</summary>
    public Color? Color { get; set; }
}

/// <summary>A node of the built tree.</summary>
internal sealed class TreeNode(ChartNode node, int order)
{
    internal ChartNode Node { get; } = node;
    internal int Order { get; } = order;
    internal List<TreeNode> Children { get; } = [];
    internal double Total { get; set; }
    internal int Depth { get; set; }
    internal Color Color { get; set; } = Color.FromHex("#000000");
}

/// <summary>A rectangle.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct HierarchyRect(double X, double Y, double Width, double Height);

internal static class ChartHierarchy
{
    /// <summary>Tints each channel 15 % of the way to white, in integers.</summary>
    private static Color TintStep(Color color)
    {
        string hex = color.ToHexString();
        int Channel(int offset)
        {
            int c = Convert.ToInt32(hex.Substring(offset, 2), 16);
            return c + ((255 - c) * 15 + 50) / 100;
        }
        return new Color(Channel(1), Channel(3), Channel(5));
    }

    /// <summary>
    /// Validates a flat node list and builds its tree: unique ids, known parents, no cycles, values only on leaves. Several roots share an
    /// implicit root; a single root with children is itself the container, so its children are the top level. Returns the top-level nodes.
    /// </summary>
    internal static List<TreeNode> Build(IList<ChartNode> nodes, ChartTheme theme)
    {
        Dictionary<string, TreeNode> byId = new(StringComparer.Ordinal);
        List<TreeNode> all = [];
        for (int order = 0; order < nodes.Count; order++)
        {
            ChartNode node = nodes[order];
            if (byId.ContainsKey(node.Id)) throw new InvalidOperationException($"Duplicate node id '{node.Id}'.");
            TreeNode t = new(node, order);
            byId[node.Id] = t;
            all.Add(t);
        }
        List<TreeNode> roots = [];
        foreach (TreeNode t in all)
        {
            if (t.Node.ParentId is not { } parentId) roots.Add(t);
            else
            {
                if (!byId.TryGetValue(parentId, out TreeNode? parent)) throw new InvalidOperationException($"Node '{t.Node.Id}' names an unknown parent '{parentId}'.");
                parent.Children.Add(t);
            }
        }
        foreach (TreeNode t in all)
        {
            if (t.Children.Count > 0 && t.Node.Value is not null) throw new InvalidOperationException($"Node '{t.Node.Id}' has children, so it must not set a value.");
            if (t.Children.Count == 0 && (t.Node.Value ?? 0) < 0) throw new InvalidOperationException($"Node '{t.Node.Id}' needs a finite nonnegative value.");
        }
        HashSet<string> seen = new(StringComparer.Ordinal);
        void Visit(TreeNode t) { seen.Add(t.Node.Id); foreach (TreeNode c in t.Children) Visit(c); }
        foreach (TreeNode r in roots) Visit(r);
        if (seen.Count != all.Count) throw new InvalidOperationException("The nodes contain a cycle.");
        List<TreeNode> top = roots.Count == 1 && roots[0].Children.Count > 0 ? roots[0].Children : roots;
        void Assign(TreeNode t, int depth, Color color)
        {
            t.Depth = depth;
            t.Color = t.Node.Color ?? color;
            t.Total = t.Children.Count == 0 ? (double)(t.Node.Value ?? 0) : 0;
            foreach (TreeNode c in t.Children)
            {
                Assign(c, depth + 1, TintStep(t.Color));
                t.Total += c.Total;
            }
        }
        for (int i = 0; i < top.Count; i++) Assign(top[i], 0, ChartCategoryColors.For(theme, null, i));
        return top;
    }

    /// <summary>
    /// Squarified treemap layout (Bruls, Huizing and van Wijk, 2000). Items are sorted by value descending, ties by input order, and zero
    /// values take no area. Returns one rectangle per item, in the sorted order.
    /// </summary>
    internal static List<(int Index, HierarchyRect Rect)> Squarify(IReadOnlyList<TreeNode> items, HierarchyRect bounds)
    {
        var sorted = items.Select((item, index) => (Item: item, Index: index)).Where(e => e.Item.Total > 0).OrderByDescending(e => e.Item.Total).ThenBy(e => e.Item.Order).ToList();
        double sum = sorted.Sum(e => e.Item.Total);
        List<(int, HierarchyRect)> output = [];
        if (sum <= 0 || !(bounds.Width > 0) || !(bounds.Height > 0)) return output;
        double scale = bounds.Width * bounds.Height / sum;
        double[] areas = sorted.Select(e => e.Item.Total * scale).ToArray();
        double x = bounds.X, y = bounds.Y, w = bounds.Width, h = bounds.Height;
        static double Worst(IReadOnlyList<double> row, double side)
        {
            double s = row.Sum(), max = row.Max(), min = row.Min();
            return Math.Max(side * side * max / (s * s), s * s / (side * side * min));
        }
        int start = 0;
        while (start < areas.Length)
        {
            double side = Math.Min(w, h);
            int end = start + 1;
            List<double> row = [areas[start]];
            while (end < areas.Length && Worst([.. row, areas[end]], side) <= Worst(row, side)) { row.Add(areas[end]); end++; }
            double s = row.Sum();
            if (w >= h)
            {
                double rowWidth = s / h, cy = y;
                for (int k = 0; k < row.Count; k++)
                {
                    double rh = row[k] / rowWidth;
                    output.Add((sorted[start + k].Index, new HierarchyRect(x, cy, rowWidth, rh)));
                    cy += rh;
                }
                x += rowWidth; w -= rowWidth;
            }
            else
            {
                double rowHeight = s / w, cx = x;
                for (int k = 0; k < row.Count; k++)
                {
                    double rw = row[k] / rowHeight;
                    output.Add((sorted[start + k].Index, new HierarchyRect(cx, y, rw, rowHeight)));
                    cx += rw;
                }
                y += rowHeight; h -= rowHeight;
            }
            start = end;
        }
        return output;
    }
}
