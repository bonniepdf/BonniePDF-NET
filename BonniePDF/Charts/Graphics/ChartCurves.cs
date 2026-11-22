// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
namespace BonniePDF;

internal static class ChartCurves
{
    internal static void ValidateDashPattern(IReadOnlyList<double>? pattern, double pathLength)
    {
        if (pattern is null || pattern.Count == 0) return;
        double min = double.PositiveInfinity;
        foreach (double d in pattern)
        {
            if (!double.IsFinite(d) || d <= 0)
                throw new ArgumentOutOfRangeException(nameof(pattern), "Dash lengths must be finite and positive and allow at most 100,000 segments.");
            if (d < min) min = d;
        }
        if (pathLength > 0 && pathLength / min > 100_000)
            throw new ArgumentOutOfRangeException(nameof(pattern), "Dash lengths must be finite and positive and allow at most 100,000 segments.");
    }

    internal static double PolylineLength(IReadOnlyList<Point> points)
    {
        double total = 0;
        for (int i = 1; i < points.Count; i++)
        {
            double dx = points[i].X - points[i - 1].X;
            double dy = points[i].Y - points[i - 1].Y;
            total += Math.Sqrt(dx * dx + dy * dy);
        }
        return total;
    }

    private static double[] MonotoneTangents(IReadOnlyList<Point> points, bool horizontal)
    {
        int n = points.Count;
        double[] u = new double[n];
        double[] v = new double[n];
        for (int i = 0; i < n; i++)
        {
            u[i] = horizontal ? points[i].Y : points[i].X;
            v[i] = horizontal ? points[i].X : points[i].Y;
        }
        double[] h = new double[n - 1];
        double[] d = new double[n - 1];
        for (int k = 0; k < n - 1; k++)
        {
            h[k] = u[k + 1] - u[k];
            d[k] = h[k] == 0 ? 0 : (v[k + 1] - v[k]) / h[k];
        }

        double[] m = new double[n];
        m[0] = d[0];
        m[n - 1] = d[n - 2];
        for (int k = 1; k < n - 1; k++)
        {
            if (d[k - 1] * d[k] <= 0 || h[k - 1] == 0 || h[k] == 0)
            {
                m[k] = 0;
            }
            else
            {
                double w1 = 2 * h[k] + h[k - 1];
                double w2 = h[k] + 2 * h[k - 1];
                m[k] = (w1 + w2) / (w1 / d[k - 1] + w2 / d[k]);
            }
        }

        for (int k = 0; k < n - 1; k++)
        {
            if (d[k] == 0)
            {
                m[k] = 0;
                m[k + 1] = 0;
            }
            else
            {
                double alpha = m[k] / d[k];
                double beta = m[k + 1] / d[k];
                double sumSq = alpha * alpha + beta * beta;
                if (sumSq > 9)
                {
                    double tau = 3.0 / Math.Sqrt(sumSq);
                    m[k] = tau * alpha * d[k];
                    m[k + 1] = tau * beta * d[k];
                }
            }
        }
        return m;
    }

    internal static List<ChartPathCommand> BuildCurveCommands(IReadOnlyList<Point> points, string curve, bool horizontal, bool moveToFirst = true)
    {
        int n = points.Count;
        List<ChartPathCommand> cmds = new();
        if (n == 0) return cmds;
        cmds.Add(new ChartPathCommand(moveToFirst ? "M" : "L", [points[0].X, points[0].Y]));
        if (n == 1) return cmds;

        if (curve is "linear" || (curve is "smooth" && n == 2))
        {
            for (int i = 1; i < n; i++) cmds.Add(new ChartPathCommand("L", [points[i].X, points[i].Y]));
            return cmds;
        }

        if (curve is "smooth")
        {
            double[] m = MonotoneTangents(points, horizontal);
            for (int k = 0; k < n - 1; k++)
            {
                Point p0 = points[k];
                Point p1 = points[k + 1];
                double du = ((horizontal ? (double)p1.Y : p1.X) - (horizontal ? (double)p0.Y : p0.X)) / 3.0;
                if (horizontal)
                    cmds.Add(new ChartPathCommand("C", [p0.X + m[k] * du, p0.Y + du, p1.X - m[k + 1] * du, p1.Y - du, p1.X, p1.Y]));
                else
                    cmds.Add(new ChartPathCommand("C", [p0.X + du, p0.Y + m[k] * du, p1.X - du, p1.Y - m[k + 1] * du, p1.X, p1.Y]));
            }
            return cmds;
        }

        for (int k = 0; k < n - 1; k++)
        {
            Point p0 = points[k];
            Point p1 = points[k + 1];
            if (curve is "stepAfter")
            {
                cmds.Add(horizontal ? new ChartPathCommand("L", [p0.X, p1.Y]) : new ChartPathCommand("L", [p1.X, p0.Y]));
                cmds.Add(new ChartPathCommand("L", [p1.X, p1.Y]));
            }
            else if (curve is "stepBefore")
            {
                cmds.Add(horizontal ? new ChartPathCommand("L", [p1.X, p0.Y]) : new ChartPathCommand("L", [p0.X, p1.Y]));
                cmds.Add(new ChartPathCommand("L", [p1.X, p1.Y]));
            }
            else
            {
                if (horizontal)
                {
                    double midY = ((double)p0.Y + p1.Y) / 2.0;
                    cmds.Add(new ChartPathCommand("L", [p0.X, midY]));
                    cmds.Add(new ChartPathCommand("L", [p1.X, midY]));
                    cmds.Add(new ChartPathCommand("L", [p1.X, p1.Y]));
                }
                else
                {
                    double midX = ((double)p0.X + p1.X) / 2.0;
                    cmds.Add(new ChartPathCommand("L", [midX, p0.Y]));
                    cmds.Add(new ChartPathCommand("L", [midX, p1.Y]));
                    cmds.Add(new ChartPathCommand("L", [p1.X, p1.Y]));
                }
            }
        }
        return cmds;
    }

    internal static List<ChartPathCommand> ReverseCurveCommands(IReadOnlyList<ChartPathCommand> forward)
    {
        List<ChartPathCommand> reversed = new();
        if (forward.Count == 0) return reversed;
        static (double X, double Y) EndPt(ChartPathCommand c) => (c.Values[^2], c.Values[^1]);
        var (lastX, lastY) = EndPt(forward[^1]);
        reversed.Add(new ChartPathCommand("L", [lastX, lastY]));
        for (int i = forward.Count - 1; i >= 1; i--)
        {
            var curr = forward[i];
            var (prevX, prevY) = EndPt(forward[i - 1]);
            if (curr.Op is "C")
                reversed.Add(new ChartPathCommand("C", [curr.Values[2], curr.Values[3], curr.Values[0], curr.Values[1], prevX, prevY]));
            else if (curr.Op is "L")
                reversed.Add(new ChartPathCommand("L", [prevX, prevY]));
        }
        return reversed;
    }
}
