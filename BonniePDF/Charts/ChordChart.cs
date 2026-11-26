// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>A circular chord diagram connecting entities around a ring with proportional flow ribbons.</summary>
public class ChordChart : ChartBase
{
    public IList<SankeyNode> Nodes { get; set; } = [];
    public IList<SankeyLink> Links { get; set; } = [];
    /// <summary>Angular gap between node arcs in degrees.</summary>
    public float PadAngle { get; set; } = 3;
    public float ArcThickness { get; set; } = 14;
    public float RibbonOpacity { get; set; } = 0.45f;
    public bool ShowLabels { get; set; } = true;

    internal override bool UsesStyledGeometry => true;

    public ChordChart() { AutoLayout = true; }
    public ChordChart(ChartData data) : base(data) { AutoLayout = true; }

    private static ChartPathCommand QuadToCubic(double x0, double y0, double qx, double qy, double x1, double y1) =>
        new("C", [x0 + 2.0 / 3.0 * (qx - x0), y0 + 2.0 / 3.0 * (qy - y0), x1 + 2.0 / 3.0 * (qx - x1), y1 + 2.0 / 3.0 * (qy - y1), x1, y1]);

    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        if (Nodes.Count == 0 || Links.Count == 0) return;
        if (!(RibbonOpacity > 0 && RibbonOpacity <= 1)) throw new InvalidOperationException("Chord ribbonOpacity must be above 0 and at most 1.");
        Dictionary<string, int> indexById = new(StringComparer.Ordinal);
        double[] totals = new double[Nodes.Count];
        Color[] colors = new Color[Nodes.Count];
        for (int i = 0; i < Nodes.Count; i++)
        {
            SankeyNode node = Nodes[i];
            if (indexById.ContainsKey(node.Id)) throw new InvalidOperationException($"Duplicate node id '{node.Id}'.");
            indexById[node.Id] = i;
            colors[i] = node.Color ?? ChartCategoryColors.For(Appearance, null, i);
        }
        foreach (SankeyLink l in Links)
        {
            if (!indexById.TryGetValue(l.Source, out int si)) throw new InvalidOperationException($"Chord link names unknown source '{l.Source}'.");
            if (!indexById.TryGetValue(l.Target, out int ti)) throw new InvalidOperationException($"Chord link names unknown target '{l.Target}'.");
            if (!(l.Value > 0) || !double.IsFinite(l.Value)) throw new InvalidOperationException("Chord link value must be positive.");
            totals[si] += l.Value;
            totals[ti] += l.Value;
        }
        List<int> active = Enumerable.Range(0, Nodes.Count).Where(i => totals[i] > 0).ToList();
        if (active.Count == 0) return;

        string font = ResolveFontFamily();
        double topEdge = (data.Title.Length > 0 ? 56 : 16) + (data.Subtitle.Length > 0 ? 18 : 0);
        double bottomEdge = Height - 16 - (Caption.Length > 0 ? 16 : 0);
        double labelPad = ShowLabels ? 56 : 12;
        double outerR = Math.Max(30, Math.Min(Width / 2.0 - labelPad, (bottomEdge - topEdge) / 2 - (ShowLabels ? 24 : 10)));
        double innerR = Math.Max(12, outerR - Math.Max(4, (double)ArcThickness));
        double cx = Width / 2.0;
        double cy = topEdge + (bottomEdge - topEdge) / 2;

        double padRad = Math.Max(0, (double)PadAngle) * Math.PI / 180.0;
        double totalFlow = active.Sum(i => totals[i]);
        double availableAngle = Math.Max(0.1, 2 * Math.PI - padRad * active.Count);
        double[] startA = new double[Nodes.Count];
        double[] endA = new double[Nodes.Count];
        double[] cursor = new double[Nodes.Count];
        double angle = -Math.PI / 2;
        foreach (int i in active)
        {
            double span = totals[i] / totalFlow * availableAngle;
            startA[i] = angle;
            endA[i] = angle + span;
            cursor[i] = angle;
            angle += span + padRad;
        }

        foreach (SankeyLink l in Links)
        {
            int si = indexById[l.Source];
            int ti = indexById[l.Target];
            double sSpan = l.Value / totalFlow * availableAngle;
            double tSpan = l.Value / totalFlow * availableAngle;
            double sa0 = cursor[si];
            double sa1 = sa0 + sSpan;
            cursor[si] = sa1;
            double ta0 = cursor[ti];
            double ta1 = ta0 + tSpan;
            cursor[ti] = ta1;

            double sx0 = cx + innerR * Math.Cos(sa0);
            double sy0 = cy + innerR * Math.Sin(sa0);
            double sx1 = cx + innerR * Math.Cos(sa1);
            double sy1 = cy + innerR * Math.Sin(sa1);
            double tx0 = cx + innerR * Math.Cos(ta0);
            double ty0 = cy + innerR * Math.Sin(ta0);
            double tx1 = cx + innerR * Math.Cos(ta1);
            double ty1 = cy + innerR * Math.Sin(ta1);

            List<ChartPathCommand> cmds = [new("M", [sx0, sy0])];
            cmds.AddRange(ChartArc.ArcSegments(cx, cy, innerR, sa0, sa1));
            cmds.Add(QuadToCubic(sx1, sy1, cx, cy, tx0, ty0));
            cmds.AddRange(ChartArc.ArcSegments(cx, cy, innerR, ta0, ta1));
            cmds.Add(QuadToCubic(tx1, ty1, cx, cy, sx0, sy0));
            cmds.Add(new("Z", []));
            ChartVector.Fill(canvas, new ChartPathPlan(cmds, colors[si].WithAlpha(RibbonOpacity), null));
        }

        foreach (int i in active)
        {
            ChartVector.Fill(canvas, new ChartPathPlan(ChartArc.RingSectorCommands(cx, cy, innerR, outerR, startA[i], endA[i]), colors[i], null));
            if (ShowLabels)
            {
                double mid = (startA[i] + endA[i]) / 2;
                double lx = cx + (outerR + 10) * Math.Cos(mid);
                double ly = cy + (outerR + 10) * Math.Sin(mid) - 5;
                double cos = Math.Cos(mid);
                TextAlignment align = cos > 0.2 ? TextAlignment.Left : cos < -0.2 ? TextAlignment.Right : TextAlignment.Center;
                canvas.DrawText((float)lx, (float)ly, Nodes[i].Label, Appearance.LegendTextColor, 10, font, align);
            }
        }
    }
}
