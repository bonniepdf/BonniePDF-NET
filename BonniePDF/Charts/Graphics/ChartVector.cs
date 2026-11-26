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

    // PDF canvases do not expose whether transparency is allowed (PDF/A-1 forbids it), so shadow and glow are omitted here and
    // only the opaque-safe parts of an effect (the gloss overlay) are drawn.
    /// <summary>PDF canvases keep no groups: the marks are drawn as they are.</summary>
    internal static void WithGroup(ICanvas sink, Func<ChartGroupInfo> info, Action draw) => draw();

    internal static void Fill(ICanvas sink, ChartPathPlan plan)
    {
        if (plan.Effects is { Count: > 0 })
        {
            foreach (ChartPathPlan expanded in ChartEffects.Expand(plan, "none")) FillOne(sink, expanded);
            return;
        }
        FillOne(sink, plan);
    }

    private static void FillOne(ICanvas sink, ChartPathPlan plan)
    {
        sink.SaveState();
        try
        {
            Replay(sink, plan.Commands);
            if (plan.RadialGradient is { } rg)
            {
                GradientStop[] radialStops = new GradientStop[rg.Stops.Count];
                for (int i = 0; i < rg.Stops.Count; i++) radialStops[i] = new GradientStop((float)rg.Stops[i].Offset, rg.Stops[i].Color);
                Point? focal = rg.Fx != rg.Cx || rg.Fy != rg.Cy ? new Point((float)rg.Fx, (float)rg.Fy) : null;
                sink.FillCurrentPath(new RadialGradientPaint(new Point((float)rg.Cx, (float)rg.Cy), (float)rg.R, radialStops, focal));
            }
            else if (plan.Gradient is { } g)
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
        if (plan.Effects is { Count: > 0 })
        {
            foreach (ChartStrokePlan expanded in ChartEffects.ExpandStroke(plan, "none")) StrokeOne(sink, expanded);
            return;
        }
        StrokeOne(sink, plan);
    }

    private static void StrokeOne(ICanvas sink, ChartStrokePlan plan)
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
