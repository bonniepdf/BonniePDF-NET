// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>A node of a <see cref="SankeyChart"/>.</summary>
public sealed class SankeyNode
{
    /// <summary>Unique id.</summary>
    public required string Id { get; set; }
    /// <summary>The text beside the node.</summary>
    public required string Label { get; set; }
    /// <summary>The node's colour; the theme palette in input order otherwise.</summary>
    public Color? Color { get; set; }
}

/// <summary>A flow between two nodes of a <see cref="SankeyChart"/>.</summary>
public sealed class SankeyLink
{
    /// <summary>The id of the node the flow leaves.</summary>
    public required string Source { get; set; }
    /// <summary>The id of the node the flow enters.</summary>
    public required string Target { get; set; }
    /// <summary>The size of the flow, above zero.</summary>
    public double Value { get; set; }
}

/// <summary>A Sankey diagram: nodes in columns joined by ribbons whose thickness is the size of the flow.</summary>
public class SankeyChart : ChartBase
{
    /// <summary>The number of relaxation iterations; fixed so both implementations agree.</summary>
    private const int Relaxations = 32;

    /// <summary>The nodes. Nodes without links are not drawn.</summary>
    public IList<SankeyNode> Nodes { get; set; } = [];
    /// <summary>The flows. Cycles throw.</summary>
    public IList<SankeyLink> Links { get; set; } = [];
    /// <summary><c>left</c> (default) puts a node in the column after its deepest source; <c>justify</c> moves nodes that nothing leaves to the last column.</summary>
    public string NodeAlign { get; set; } = "left";
    /// <summary>Space in points between nodes of a column.</summary>
    public float NodePadding { get; set; } = 10;
    /// <summary>Width of the node bars in points.</summary>
    public float NodeWidth { get; set; } = 14;
    /// <summary>Opacity of the ribbons, above 0 and up to 1.</summary>
    public float LinkOpacity { get; set; } = 0.4f;
    /// <summary>The text beside each node; <c>{label}</c> and <c>{value}</c> are replaced.</summary>
    public string LabelTemplate { get; set; } = "{label} {value}";
    /// <summary>Number format of <c>{value}</c>.</summary>
    public string ValueFormat { get; set; } = "G";

    internal override bool UsesStyledGeometry => true;

    /// <summary>Creates an empty Sankey diagram.</summary>
    public SankeyChart() { AutoLayout = true; }

    /// <summary>Creates a Sankey diagram for <paramref name="data"/>.</summary>
    public SankeyChart(ChartData data) : base(data) { AutoLayout = true; }

    private sealed class Laid(SankeyNode node, int index, Color color)
    {
        internal SankeyNode Node { get; } = node;
        internal int Index { get; } = index;
        internal int Column { get; set; }
        internal double Value { get; set; }
        internal double Y { get; set; }
        internal double Height { get; set; }
        internal Color Color { get; } = color;
        internal List<SankeyLink> Incoming { get; } = [];
        internal List<SankeyLink> Outgoing { get; } = [];
        internal double Centre => Y + Height / 2;
    }

    /// <inheritdoc />
    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        List<Laid> laid = Validate();
        if (Links.Count == 0) return;
        List<Laid> used = laid.Where(n => n.Incoming.Count + n.Outgoing.Count > 0).ToList();
        int columns = used.Max(n => n.Column) + 1;
        string font = ResolveFontFamily();
        string template = LabelTemplate.Replace("{value}", "{value:" + ValueFormat + "}", StringComparison.Ordinal);
        string LabelOf(Laid n) => ChartTemplate.Apply(template, new Dictionary<string, object> { ["label"] = n.Node.Label, ["value"] = n.Value });

        double topEdge = (data.Title.Length > 0 ? 56 : 18) + (data.Subtitle.Length > 0 ? 18 : 0);
        double bottom = Height - 12 - (Caption.Length > 0 ? 16 : 0);
        double Widest(int column) => used.Where(n => n.Column == column).Select(n => (double)canvas.MeasureText(LabelOf(n), 10, font).Width).DefaultIfEmpty(0).Max();
        double cap = Width * 0.3;
        double left = 12 + (columns > 1 ? Math.Min(cap, Widest(0)) + 6 : 0);
        double right = 12 + Math.Min(cap, Widest(columns - 1)) + 6;
        double x0 = left;
        double x1 = Math.Max(x0 + NodeWidth, Width - right);
        double span = columns > 1 ? (x1 - x0 - NodeWidth) / (columns - 1) : 0;
        double ColumnX(int c) => x0 + c * span;

        // Heights: the tightest column sets the scale.
        List<Laid>[] byColumn = Enumerable.Range(0, columns).Select(_ => new List<Laid>()).ToArray();
        foreach (Laid n in used) byColumn[n.Column].Add(n);
        double plotHeight = bottom - topEdge;
        double scale = double.PositiveInfinity;
        foreach (List<Laid> column in byColumn)
        {
            double sum = column.Sum(n => n.Value);
            if (sum > 0) scale = Math.Min(scale, (plotHeight - Math.Max(0, column.Count - 1) * (double)NodePadding) / sum);
        }
        if (!(scale > 0)) throw new InvalidOperationException("The Sankey diagram does not fit: reduce nodePadding or the number of nodes.");
        foreach (List<Laid> column in byColumn)
        {
            double y = topEdge;
            foreach (Laid n in column)
            {
                n.Height = n.Value * scale;
                n.Y = y;
                y += n.Height + NodePadding;
            }
        }

        // Relaxation: alternating passes, each followed by collision resolution.
        void Resolve(List<Laid> column)
        {
            List<Laid> sorted = column.OrderBy(n => n.Y).ToList();
            double y = topEdge;
            foreach (Laid n in sorted)
            {
                if (n.Y < y) n.Y = y;
                y = n.Y + n.Height + NodePadding;
            }
            if (sorted.Count > 0 && sorted[^1].Y + sorted[^1].Height > bottom)
            {
                sorted[^1].Y = bottom - sorted[^1].Height;
                for (int i = sorted.Count - 2; i >= 0; i--)
                {
                    Laid next = sorted[i + 1];
                    if (sorted[i].Y + sorted[i].Height + NodePadding > next.Y) sorted[i].Y = next.Y - NodePadding - sorted[i].Height;
                }
            }
        }
        Dictionary<string, Laid> laidById = used.ToDictionary(n => n.Node.Id, StringComparer.Ordinal);
        void Pull(Laid n, List<SankeyLink> links, Func<SankeyLink, string> other, double alpha)
        {
            if (links.Count == 0) return;
            double weight = 0, sum = 0;
            foreach (SankeyLink l in links)
            {
                weight += l.Value;
                sum += l.Value * laidById[other(l)].Centre;
            }
            n.Y += (sum / weight - n.Centre) * alpha;
        }
        for (int i = 0; i < Relaxations; i++)
        {
            double alpha = Math.Pow(0.99, i);
            for (int c = 1; c < columns; c++)
            {
                foreach (Laid n in byColumn[c]) Pull(n, n.Incoming, l => l.Source, alpha);
                Resolve(byColumn[c]);
            }
            for (int c = columns - 2; c >= 0; c--)
            {
                foreach (Laid n in byColumn[c]) Pull(n, n.Outgoing, l => l.Target, alpha);
                Resolve(byColumn[c]);
            }
        }

        // Ribbons: each node's flows stack along its edge in the order of the node at the other end.
        Dictionary<SankeyLink, double> outOffset = new(ReferenceEqualityComparer.Instance);
        Dictionary<SankeyLink, double> inOffset = new(ReferenceEqualityComparer.Instance);
        foreach (Laid n in used)
        {
            double o = 0;
            foreach (SankeyLink l in n.Outgoing.OrderBy(l => laidById[l.Target].Centre))
            {
                outOffset[l] = o;
                o += l.Value * scale;
            }
            double k = 0;
            foreach (SankeyLink l in n.Incoming.OrderBy(l => laidById[l.Source].Centre))
            {
                inOffset[l] = k;
                k += l.Value * scale;
            }
        }
        foreach (SankeyLink l in Links)
        {
            Laid s = laidById[l.Source];
            Laid t = laidById[l.Target];
            double thickness = l.Value * scale;
            double sx = ColumnX(s.Column) + NodeWidth;
            double tx = ColumnX(t.Column);
            double sy = s.Y + outOffset[l];
            double ty = t.Y + inOffset[l];
            double mx = (sx + tx) / 2;
            List<ChartPathCommand> commands =
            [
                new("M", [sx, sy]),
                new("C", [mx, sy, mx, ty, tx, ty]),
                new("L", [tx, ty + thickness]),
                new("C", [mx, ty + thickness, mx, sy + thickness, sx, sy + thickness]),
                new("Z", []),
            ];
            ChartVector.Fill(canvas, new ChartPathPlan(commands, s.Color.WithAlpha(LinkOpacity), null));
        }

        foreach (Laid n in used)
        {
            double x = ColumnX(n.Column);
            canvas.FillRect((float)x, (float)n.Y, NodeWidth, (float)n.Height, n.Color);
            if (n.Height < 1) continue;
            bool first = n.Column == 0 && columns > 1;
            double labelX = first ? x - 6 : x + NodeWidth + 6;
            canvas.DrawText((float)labelX, (float)(n.Y + n.Height / 2 - 5), LabelOf(n), Appearance.LegendTextColor, 10, font, first ? TextAlignment.Right : TextAlignment.Left);
        }
    }

    /// <summary>Checks the nodes and links and returns the nodes with their columns, values and colours.</summary>
    private List<Laid> Validate()
    {
        if (!(NodePadding >= 0)) throw new InvalidOperationException("Sankey nodePadding must be nonnegative.");
        if (!(NodeWidth > 0)) throw new InvalidOperationException("Sankey nodeWidth must be positive.");
        if (!(LinkOpacity > 0 && LinkOpacity <= 1)) throw new InvalidOperationException("Sankey linkOpacity must be above 0 and at most 1.");
        if (NodeAlign is not ("left" or "justify")) throw new InvalidOperationException($"Unknown Sankey nodeAlign '{NodeAlign}'.");
        Dictionary<string, Laid> byId = new(StringComparer.Ordinal);
        List<Laid> ordered = [];
        for (int index = 0; index < Nodes.Count; index++)
        {
            SankeyNode node = Nodes[index];
            if (byId.ContainsKey(node.Id)) throw new InvalidOperationException($"Duplicate node id '{node.Id}'.");
            Laid n = new(node, index, node.Color ?? ChartCategoryColors.For(Appearance, null, index));
            byId[node.Id] = n;
            ordered.Add(n);
        }
        foreach (SankeyLink l in Links)
        {
            if (!byId.TryGetValue(l.Source, out Laid? s)) throw new InvalidOperationException($"A link names an unknown source '{l.Source}'.");
            if (!byId.TryGetValue(l.Target, out Laid? t)) throw new InvalidOperationException($"A link names an unknown target '{l.Target}'.");
            if (!(l.Value > 0) || !double.IsFinite(l.Value)) throw new InvalidOperationException("A link value must be above zero.");
            s.Outgoing.Add(l);
            t.Incoming.Add(l);
        }
        // Longest path from any source, by repeated relaxation in topological order; leftover nodes mean a cycle.
        Dictionary<string, int> remaining = ordered.ToDictionary(n => n.Node.Id, n => n.Incoming.Count, StringComparer.Ordinal);
        Queue<Laid> queue = new(ordered.Where(n => n.Incoming.Count == 0));
        int visited = 0;
        while (queue.Count > 0)
        {
            Laid n = queue.Dequeue();
            visited++;
            foreach (SankeyLink l in n.Outgoing)
            {
                Laid t = byId[l.Target];
                t.Column = Math.Max(t.Column, n.Column + 1);
                int left = remaining[t.Node.Id] - 1;
                remaining[t.Node.Id] = left;
                if (left == 0) queue.Enqueue(t);
            }
        }
        if (visited < ordered.Count) throw new InvalidOperationException("Sankey links contain a cycle.");
        foreach (Laid n in ordered) n.Value = Math.Max(n.Incoming.Sum(l => l.Value), n.Outgoing.Sum(l => l.Value));
        if (NodeAlign == "justify")
        {
            int last = ordered.Where(n => n.Incoming.Count + n.Outgoing.Count > 0).Select(n => n.Column).DefaultIfEmpty(0).Max();
            foreach (Laid n in ordered) if (n.Outgoing.Count == 0 && n.Incoming.Count > 0) n.Column = last;
        }
        return ordered;
    }
}
