// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;
namespace BonniePDF;

internal static class ChartVector
{
    private static void Replay(ICanvas sink, IReadOnlyList<ChartPathCommand> commands)
    {
        sink.BeginPath();
        foreach (var c in commands)
        {
            var v = c.Values;
            switch (c.Op)
            {
                case "M": sink.MoveTo((float)v[0], (float)v[1]); break;
                case "L": sink.LineTo((float)v[0], (float)v[1]); break;
                case "C": sink.CubicBezierTo((float)v[0], (float)v[1], (float)v[2], (float)v[3], (float)v[4], (float)v[5]); break;
                case "Z": sink.ClosePath(); break;
            }
        }
    }

    internal static void Fill(ICanvas sink, ChartPathPlan plan)
    {
        sink.SaveState();
        try
        {
            Replay(sink, plan.Commands);
            if (plan.Gradient is { } g)
            {
                GradientStop[] stops;
                if (g.Stops is { Count: >= 2 })
                {
                    stops = new GradientStop[g.Stops.Count];
                    for (int i = 0; i < g.Stops.Count; i++)
                        stops[i] = new GradientStop((float)g.Stops[i].Offset, g.Stops[i].Color);
                }
                else
                {
                    stops = [new GradientStop(0, g.StartColor), new GradientStop(1, g.EndColor)];
                }
                sink.FillCurrentPath(new LinearGradientPaint(new Point((float)g.StartX, (float)g.StartY), new Point((float)g.EndX, (float)g.EndY), stops));
            }
            else
            {
                sink.FillCurrentPath(plan.Color);
            }
        }
        finally { sink.RestoreState(); }
    }

    internal static void Stroke(ICanvas sink, ChartStrokePlan plan)
    {
        bool hasSegment = false;
        foreach (var c in plan.Commands) { if (c.Op is "L" or "C") { hasSegment = true; break; } }
        if (!hasSegment) return;
        sink.SaveState();
        try
        {
            Replay(sink, plan.Commands);
            float[]? dash = null;
            if (plan.DashPattern is { Count: > 0 })
            {
                dash = new float[plan.DashPattern.Count];
                for (int i = 0; i < plan.DashPattern.Count; i++) dash[i] = (float)plan.DashPattern[i];
            }
            sink.StrokeCurrentPath(new StrokeStyle
            {
                Paint = new SolidPaint(plan.Color),
                Width = (float)plan.LineWidth,
                LineCap = plan.LineCap switch { "round" => LineCap.Round, "square" => LineCap.Square, _ => LineCap.Butt },
                LineJoin = plan.LineJoin switch { "round" => LineJoin.Round, "bevel" => LineJoin.Bevel, _ => LineJoin.Miter },
                MiterLimit = (float)plan.MiterLimit,
                DashPattern = dash,
                DashOffset = (float)plan.DashOffset,
            });
        }
        finally { sink.RestoreState(); }
    }

    internal static void PushClipRect(ICanvas sink, float x, float y, float width, float height)
    {
        sink.SaveState();
        sink.BeginPath();
        sink.MoveTo(x, y);
        sink.LineTo(x + width, y);
        sink.LineTo(x + width, y + height);
        sink.LineTo(x, y + height);
        sink.ClosePath();
        sink.ClipToCurrentPath();
    }

    internal static void PopClip(ICanvas sink) => sink.RestoreState();
}
