// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
namespace BonniePDF;

/// <summary>A visual effect: a drop shadow, a glow or a gloss. Validated when the chart is drawn.</summary>
public abstract class ChartEffect
{
    private protected ChartEffect() { }
    internal abstract ChartResolvedEffect Resolve();

    private protected static double Within(string name, double value, double min, double max)
    {
        if (!double.IsFinite(value) || value < min || value > max) throw new InvalidOperationException($"{name} must be a finite number from {min} to {max}.");
        return value;
    }

    internal const double MaxExtent = 48;

    internal static IReadOnlyList<ChartResolvedEffect>? ResolveAll(IList<ChartEffect>? effects) =>
        effects is null || effects.Count == 0 ? null : effects.Select(e => e.Resolve()).ToList();
}

/// <summary>A soft drop shadow beneath a shape.</summary>
public sealed class ChartShadowEffect : ChartEffect
{
    /// <summary>Shadow colour (<c>#0f172a</c>).</summary>
    public Color Color { get; set; } = Color.FromHex("#0f172a");
    /// <summary>Peak opacity, <c>0..1</c> (<c>0.18</c>).</summary>
    public double Opacity { get; set; } = 0.18;
    /// <summary>Blur extent in points, <c>0..48</c> (<c>8</c>).</summary>
    public double Blur { get; set; } = 8;
    /// <summary>Horizontal offset in points, magnitude at most 48 (<c>0</c>).</summary>
    public double OffsetX { get; set; }
    /// <summary>Vertical offset in points, magnitude at most 48 (<c>3</c>).</summary>
    public double OffsetY { get; set; } = 3;

    internal override ChartResolvedEffect Resolve() => new("shadow", Color, Within("Shadow opacity", Opacity, 0, 1), Within("Shadow blur", Blur, 0, MaxExtent),
        Within("Shadow offsetX", OffsetX, -MaxExtent, MaxExtent), Within("Shadow offsetY", OffsetY, -MaxExtent, MaxExtent));
}

/// <summary>A soft halo around a shape or line.</summary>
public sealed class ChartGlowEffect : ChartEffect
{
    /// <summary>Glow colour; unset uses the shape's own colour.</summary>
    public Color? Color { get; set; }
    /// <summary>Peak opacity, <c>0..1</c> (<c>0.35</c>).</summary>
    public double Opacity { get; set; } = 0.35;
    /// <summary>Radius in points, <c>0..48</c> (<c>6</c>).</summary>
    public double Radius { get; set; } = 6;

    internal override ChartResolvedEffect Resolve() => new("glow", Color, Within("Glow opacity", Opacity, 0, 1), Within("Glow radius", Radius, 0, MaxExtent));
}

/// <summary>A white highlight fading from the top of a shape, for a glassy look.</summary>
public sealed class ChartGlossEffect : ChartEffect
{
    /// <summary>Opacity of the highlight at the top edge, <c>0..1</c> (<c>0.35</c>).</summary>
    public double Opacity { get; set; } = 0.35;
    /// <summary>Fraction of the shape's height the highlight covers, <c>(0, 1]</c> (<c>0.45</c>).</summary>
    public double Extent { get; set; } = 0.45;

    internal override ChartResolvedEffect Resolve()
    {
        double extent = Within("Gloss extent", Extent, 0, 1);
        if (extent == 0) throw new InvalidOperationException("Gloss extent must be greater than 0.");
        return new("gloss", null, Within("Gloss opacity", Opacity, 0, 1), Extent: extent);
    }
}

/// <summary>Expands plans that carry effects into the plans a sink draws.</summary>
internal static class ChartEffects
{
    /// <summary>Number of translucent layers that stand in for a blur.</summary>
    internal const int Layers = 8;

    private static bool Is(string? value, string expected) => string.Equals(value, expected, StringComparison.Ordinal);

    private sealed record Bounds(double MinX, double MinY, double MaxX, double MaxY);

    private static Bounds? CommandBounds(IReadOnlyList<ChartPathCommand> commands)
    {
        Bounds? b = null;
        foreach (ChartPathCommand c in commands)
            for (int i = 0; i + 1 < c.Values.Length; i += 2)
            {
                double x = c.Values[i], y = c.Values[i + 1];
                b = b is { } o ? new Bounds(Math.Min(o.MinX, x), Math.Min(o.MinY, y), Math.Max(o.MaxX, x), Math.Max(o.MaxY, y)) : new Bounds(x, y, x, y);
            }
        return b;
    }

    /// <summary>Alpha of layer <paramref name="i"/> (0 is the outermost): cumulative opacity rises linearly from the rim to <paramref name="opacity"/>.</summary>
    internal static double LayerAlpha(double opacity, int i)
    {
        double target = opacity * (i + 1) / Layers, before = opacity * i / Layers;
        return i == 0 ? target : 1 - (1 - target) / (1 - before);
    }

    private static List<ChartPathCommand> Grow(IReadOnlyList<ChartPathCommand> commands, Bounds b, double grow, double dx, double dy)
    {
        double w = b.MaxX - b.MinX, h = b.MaxY - b.MinY, cx = (b.MinX + b.MaxX) / 2, cy = (b.MinY + b.MaxY) / 2;
        double sx = (w + 2 * grow) / w, sy = (h + 2 * grow) / h;
        List<ChartPathCommand> result = new(commands.Count);
        foreach (ChartPathCommand c in commands)
        {
            double[] v = (double[])c.Values.Clone();
            for (int i = 0; i + 1 < v.Length; i += 2) { v[i] = cx + (v[i] - cx) * sx + dx; v[i + 1] = cy + (v[i + 1] - cy) * sy + dy; }
            result.Add(new ChartPathCommand(c.Op, v));
        }
        return result;
    }

    private static ChartPathPlan GlossPlan(ChartPathPlan plan, ChartResolvedEffect gloss, Bounds b)
    {
        Color top = new(255, 255, 255, (int)Math.Round(gloss.Opacity * 255, MidpointRounding.AwayFromZero));
        Color clear = new(255, 255, 255, 0);
        return new ChartPathPlan(plan.Commands.Select(c => new ChartPathCommand(c.Op, (double[])c.Values.Clone())).ToList(), Color.White.WithAlpha((float)(gloss.Opacity / 2)),
            new ChartResolvedGradient((b.MinX + b.MaxX) / 2, b.MinY, (b.MinX + b.MaxX) / 2, b.MaxY, top, clear,
                [new ChartGradientStop(0, top), new ChartGradientStop(gloss.Extent, clear), new ChartGradientStop(1, clear)]));
    }

    /// <summary>
    /// <c>native</c> leaves shadow and glow on the plan for the sink; <c>geometry</c> (null) replaces them with translucent layers
    /// beneath it; <c>none</c> drops them. A gloss is always a gradient overlay above the shape.
    /// </summary>
    internal static List<ChartPathPlan> Expand(ChartPathPlan plan, string? mode)
    {
        if (plan.Effects is not { Count: > 0 } effects) return [plan];
        Bounds? bounds = CommandBounds(plan.Commands);
        List<ChartPathPlan> layers = [], overlays = [];
        ChartResolvedEffect[] kept = effects.Where(e => !e.IsGloss).ToArray();
        if (bounds is { } b && b.MaxX > b.MinX && b.MaxY > b.MinY)
        {
            if (mode is not ("native" or "none"))
                foreach (ChartResolvedEffect e in effects)
                {
                    if (e.IsGloss) continue;
                    double spread = e.IsShadow ? e.Blur / 2 : e.Blur;
                    Color tint = e.Color ?? plan.Color;
                    double dx = e.IsShadow ? e.OffsetX : 0, dy = e.IsShadow ? e.OffsetY : 0;
                    for (int i = 0; i < Layers; i++)
                        layers.Add(new ChartPathPlan(Grow(plan.Commands, b, spread * (Layers - i) / Layers, dx, dy), tint.WithAlpha((float)LayerAlpha(e.Opacity, i)), null));
                }
            foreach (ChartResolvedEffect e in effects) if (e.IsGloss) overlays.Add(GlossPlan(plan, e, b));
        }
        ChartPathPlan baseplan = new(plan.Commands, plan.Color, plan.Gradient, plan.RadialGradient, Is(mode, "native") && kept.Length > 0 ? kept : null);
        return [.. layers, baseplan, .. overlays];
    }

    /// <summary>Expands the glow and shadow of a stroke into wider, translucent strokes beneath it. Strokes have no native form.</summary>
    internal static List<ChartStrokePlan> ExpandStroke(ChartStrokePlan plan, string? mode)
    {
        if (plan.Effects is not { Count: > 0 } effects) return [plan];
        ChartStrokePlan baseplan = plan with { Effects = null };
        if (Is(mode, "none")) return [baseplan];
        List<ChartStrokePlan> layers = [];
        foreach (ChartResolvedEffect e in effects)
        {
            if (e.IsGloss) continue;
            double spread = e.IsShadow ? e.Blur / 2 : e.Blur;
            Color tint = e.Color ?? plan.Color;
            double dx = e.IsShadow ? e.OffsetX : 0, dy = e.IsShadow ? e.OffsetY : 0;
            for (int i = 0; i < Layers; i++)
            {
                IReadOnlyList<ChartPathCommand> commands = dx == 0 && dy == 0 ? baseplan.Commands
                    : baseplan.Commands.Select(c => new ChartPathCommand(c.Op, c.Values.Select((v, n) => v + (n % 2 == 0 ? dx : dy)).ToArray())).ToList();
                layers.Add(baseplan with { Commands = commands, Color = tint.WithAlpha((float)LayerAlpha(e.Opacity, i)), LineWidth = plan.LineWidth + 2 * spread * (Layers - i) / Layers });
            }
        }
        return [.. layers, baseplan];
    }
}
