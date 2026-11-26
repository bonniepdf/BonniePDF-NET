// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>A linear arc diagram: nodes ordered along a baseline linked by weighted semicircular arcs.</summary>
public class ArcDiagramChart : ChartBase
{
    public IList<SankeyNode> Nodes { get; set; } = [];
    public IList<SankeyLink> Links { get; set; } = [];
    public float NodeRadius { get; set; } = 7;
    public float LinkOpacity { get; set; } = 0.55f;
    public float MaxStrokeWidth { get; set; } = 10;
    public bool ShowLabels { get; set; } = true;

    internal override bool UsesStyledGeometry => true;

    public ArcDiagramChart() { AutoLayout = true; }
    public ArcDiagramChart(ChartData data) : base(data) { AutoLayout = true; }

    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        int n = Nodes.Count;
        if (n == 0) return;
        if (!(LinkOpacity > 0 && LinkOpacity <= 1)) throw new InvalidOperationException("ArcDiagram linkOpacity must be above 0 and at most 1.");
        Dictionary<string, int> indexById = new(StringComparer.Ordinal);
        Color[] colors = new Color[n];
        for (int i = 0; i < n; i++)
        {
            SankeyNode node = Nodes[i];
            if (indexById.ContainsKey(node.Id)) throw new InvalidOperationException($"Duplicate node id '{node.Id}'.");
            indexById[node.Id] = i;
            colors[i] = node.Color ?? ChartCategoryColors.For(Appearance, null, i);
        }
        foreach (SankeyLink l in Links)
        {
            if (!indexById.ContainsKey(l.Source)) throw new InvalidOperationException($"ArcDiagram link names unknown source '{l.Source}'.");
            if (!indexById.ContainsKey(l.Target)) throw new InvalidOperationException($"ArcDiagram link names unknown target '{l.Target}'.");
            if (!(l.Value > 0) || !double.IsFinite(l.Value)) throw new InvalidOperationException("ArcDiagram link value must be positive.");
        }

        string font = ResolveFontFamily();
        double topEdge = (data.Title.Length > 0 ? 56 : 18) + (data.Subtitle.Length > 0 ? 18 : 0);
        double labelBand = ShowLabels ? 28 : 10;
        double baselineY = Height - labelBand - (Caption.Length > 0 ? 16 : 0) - NodeRadius;
        double maxArcH = Math.Max(24, baselineY - topEdge - 8);
        double padX = Math.Max(36, (double)NodeRadius + 24);
        double spanX = Math.Max(40, Width - padX * 2);
        double NodeX(int i) => n == 1 ? Width / 2.0 : padX + spanX * i / (n - 1);
        double maxVal = Links.Count > 0 ? Links.Max(l => l.Value) : 1;

        const double K = 0.5522847498;
        foreach (SankeyLink l in Links)
        {
            int si = indexById[l.Source];
            int ti = indexById[l.Target];
            if (si == ti) continue;
            double x0 = Math.Min(NodeX(si), NodeX(ti));
            double x1 = Math.Max(NodeX(si), NodeX(ti));
            double rx = (x1 - x0) / 2;
            double ry = Math.Min(rx, maxArcH);
            double midX = (x0 + x1) / 2;
            double topY = baselineY - ry;
            List<ChartPathCommand> cmds =
            [
                new("M", [x0, baselineY]),
                new("C", [x0, baselineY - K * ry, midX - K * rx, topY, midX, topY]),
                new("C", [midX + K * rx, topY, x1, baselineY - K * ry, x1, baselineY]),
            ];
            double sw = Math.Max(1.25, l.Value / maxVal * MaxStrokeWidth);
            ChartVector.Stroke(canvas, new ChartStrokePlan(cmds, colors[si].WithAlpha(LinkOpacity), sw, "round"));
        }

        for (int i = 0; i < n; i++)
        {
            double x = NodeX(i);
            ChartVector.Fill(canvas, new ChartPathPlan(ChartArc.CircleCommands(x, baselineY, NodeRadius), colors[i], null));
            if (ShowLabels)
                canvas.DrawText((float)x, (float)(baselineY + NodeRadius + 6), Nodes[i].Label, Appearance.LegendTextColor, 10, font, TextAlignment.Center);
        }
    }
}
