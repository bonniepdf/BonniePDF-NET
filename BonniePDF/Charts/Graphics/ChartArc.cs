// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;

namespace BonniePDF;

internal static class ChartArc
{
    /// <summary>Cubic Bezier segments of an arc of radius <paramref name="r"/> from <paramref name="a0"/> to <paramref name="a1"/> (radians), each at most 90 degrees.</summary>
    internal static List<ChartPathCommand> ArcSegments(double cx, double cy, double r, double a0, double a1)
    {
        double span = a1 - a0;
        if (Math.Abs(span) < 1e-9 || !(r > 0)) return [];
        int n = Math.Max(1, (int)Math.Ceiling(Math.Abs(span) / (Math.PI / 2) - 1e-9));
        List<ChartPathCommand> output = [];
        for (int i = 0; i < n; i++)
        {
            double t0 = a0 + span * i / n;
            double t1 = a0 + span * (i + 1) / n;
            double k = 4.0 / 3 * Math.Tan((t1 - t0) / 4);
            output.Add(new("C", [
                cx + r * Math.Cos(t0) - k * r * Math.Sin(t0), cy + r * Math.Sin(t0) + k * r * Math.Cos(t0),
                cx + r * Math.Cos(t1) + k * r * Math.Sin(t1), cy + r * Math.Sin(t1) - k * r * Math.Cos(t1),
                cx + r * Math.Cos(t1), cy + r * Math.Sin(t1)]));
        }
        return output;
    }

    internal static List<ChartPathCommand> ArcStrokeCommands(double cx, double cy, double r, double a0, double a1)
    {
        if (!(r > 0) || Math.Abs(a1 - a0) < 1e-9) return [];
        List<ChartPathCommand> cmds = [new("M", [cx + r * Math.Cos(a0), cy + r * Math.Sin(a0)])];
        cmds.AddRange(ArcSegments(cx, cy, r, a0, a1));
        return cmds;
    }

    internal static List<ChartPathCommand> CircleCommands(double cx, double cy, double r)
    {
        double k = 4.0 / 3 * Math.Tan(Math.PI / 8) * r;
        return
        [
            new("M", [cx + r, cy]),
            new("C", [cx + r, cy + k, cx + k, cy + r, cx, cy + r]),
            new("C", [cx - k, cy + r, cx - r, cy + k, cx - r, cy]),
            new("C", [cx - r, cy - k, cx - k, cy - r, cx, cy - r]),
            new("C", [cx + k, cy - r, cx + r, cy - k, cx + r, cy]),
            new("Z", []),
        ];
    }

    internal static List<ChartPathCommand> RingSectorCommands(double cx, double cy, double inner, double outer, double a0, double a1)
    {
        List<ChartPathCommand> cmds = [new("M", [cx + outer * Math.Cos(a0), cy + outer * Math.Sin(a0)])];
        cmds.AddRange(ArcSegments(cx, cy, outer, a0, a1));
        if (inner > 1e-6)
        {
            cmds.Add(new("L", [cx + inner * Math.Cos(a1), cy + inner * Math.Sin(a1)]));
            cmds.AddRange(ArcSegments(cx, cy, inner, a1, a0));
        }
        else
        {
            cmds.Add(new("L", [cx, cy]));
        }
        cmds.Add(new("Z", []));
        return cmds;
    }

    internal static List<ChartPathCommand> RoundedRectCommands(double x, double y, double w, double h, double radius)
    {
        double r = Math.Max(0, Math.Min(radius, Math.Min(w / 2, h / 2)));
        if (r <= 1e-6)
        {
            return
            [
                new("M", [x, y]),
                new("L", [x + w, y]),
                new("L", [x + w, y + h]),
                new("L", [x, y + h]),
                new("Z", []),
            ];
        }
        double k = r * 0.5522847498;
        return
        [
            new("M", [x + r, y]),
            new("L", [x + w - r, y]),
            new("C", [x + w - r + k, y, x + w, y + r - k, x + w, y + r]),
            new("L", [x + w, y + h - r]),
            new("C", [x + w, y + h - r + k, x + w - r + k, y + h, x + w - r, y + h]),
            new("L", [x + r, y + h]),
            new("C", [x + r - k, y + h, x, y + h - r + k, x, y + h - r]),
            new("L", [x, y + r]),
            new("C", [x, y + r - k, x + r - k, y, x + r, y]),
            new("Z", []),
        ];
    }
}
