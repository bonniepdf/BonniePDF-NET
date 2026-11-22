// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;
namespace BonniePDF;

internal static class ChartMarkers
{
    private const double K = 0.5522847498307933;

    private static List<ChartPathCommand> CircleCommands(double cx, double cy, double r)
    {
        double kr = K * r;
        return
        [
            new("M", [cx, cy - r]),
            new("C", [cx + kr, cy - r, cx + r, cy - kr, cx + r, cy]),
            new("C", [cx + r, cy + kr, cx + kr, cy + r, cx, cy + r]),
            new("C", [cx - kr, cy + r, cx - r, cy + kr, cx - r, cy]),
            new("C", [cx - r, cy - kr, cx - kr, cy - r, cx, cy - r]),
            new("Z", []),
        ];
    }

    private static List<ChartPathCommand> MarkerCommands(double cx, double cy, double r, string shape)
    {
        if (shape is "circle") return CircleCommands(cx, cy, r);
        if (shape is "square")
        {
            double s = r * 0.886;
            return
            [
                new("M", [cx - s, cy - s]),
                new("L", [cx + s, cy - s]),
                new("L", [cx + s, cy + s]),
                new("L", [cx - s, cy + s]),
                new("Z", []),
            ];
        }
        if (shape is "diamond")
        {
            double d = r * 1.2;
            return
            [
                new("M", [cx, cy - d]),
                new("L", [cx + d, cy]),
                new("L", [cx, cy + d]),
                new("L", [cx - d, cy]),
                new("Z", []),
            ];
        }
        double rt = r * 1.25;
        const double sin60 = 0.8660254037844386;
        return
        [
            new("M", [cx, cy - rt]),
            new("L", [cx + rt * sin60, cy + rt * 0.5]),
            new("L", [cx - rt * sin60, cy + rt * 0.5]),
            new("Z", []),
        ];
    }

    internal static void Draw(
        ICanvas sink,
        float cx,
        float cy,
        float radius,
        Color color,
        string shape = "circle",
        string fill = "solid",
        float haloWidth = 0,
        Color? backgroundColor = null,
        float strokeWidth = 1.5f)
    {
        if (radius <= 0) return;
        Color bg = backgroundColor ?? Color.White;
        if (shape is "circle" && fill is "solid" && haloWidth <= 0)
        {
            sink.FillCircle(cx, cy, radius, color);
            return;
        }

        if (haloWidth > 0)
        {
            if (shape is "circle") sink.FillCircle(cx, cy, radius + haloWidth, bg);
            else ChartVector.Fill(sink, new ChartPathPlan(MarkerCommands(cx, cy, radius + haloWidth, shape), bg, null));
        }

        var cmds = MarkerCommands(cx, cy, radius, shape);
        if (fill is "hollow")
        {
            ChartVector.Fill(sink, new ChartPathPlan(cmds, bg, null));
            ChartVector.Stroke(sink, new ChartStrokePlan(cmds, color, Math.Max(1.5, strokeWidth), "butt", "round"));
        }
        else if (shape is "circle")
        {
            sink.FillCircle(cx, cy, radius, color);
        }
        else
        {
            ChartVector.Fill(sink, new ChartPathPlan(cmds, color, null));
        }
    }
}
