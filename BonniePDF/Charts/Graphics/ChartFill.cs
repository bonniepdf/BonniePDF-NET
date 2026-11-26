// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
namespace BonniePDF;

/// <summary>One colour stop of a gradient fill.</summary>
public sealed class ChartFillStop
{
    /// <summary>Position along the gradient, <c>0..1</c>.</summary>
    public double Offset { get; set; }
    /// <summary>Stop colour. Translucent colours are allowed.</summary>
    public Color Color { get; set; }
}

/// <summary>An axis-aligned rectangle in chart points.</summary>
internal sealed record ChartBox(double X, double Y, double Width, double Height);

/// <summary>The boxes a fill's geometry can be relative to, supplied by the chart.</summary>
internal sealed record ChartFillBoxes(ChartBox Mark, ChartBox Plot, ChartBox Chart);

/// <summary>What a sink receives for a fill: a fallback colour and at most one resolved gradient.</summary>
internal sealed record ChartResolvedFill(Color Color, ChartResolvedGradient? Gradient = null, ChartResolvedRadialGradient? Radial = null);

/// <summary>
/// A fill for a chart surface or mark: a solid colour, or a linear or radial gradient with up to sixteen stops.
/// Gradient geometry is relative to <see cref="ChartLinearFill.Space"/> and resolved to absolute coordinates by the chart.
/// Gradient fills are validated when the chart is drawn.
/// </summary>
public abstract class ChartFill
{
    private protected ChartFill() { }

    /// <summary>A solid colour fill.</summary>
    public static ChartFill Solid(Color color) => new ChartSolidFill(color);

    /// <summary>A colour is a solid fill.</summary>
    public static implicit operator ChartFill(Color color) => Solid(color);

    /// <summary>The colour used where a single colour is needed (legend swatches, contrast, sinks without radial support).</summary>
    internal abstract Color FallbackColor { get; }

    internal abstract bool IsSolid { get; }

    internal abstract ChartResolvedFill Resolve(ChartFillBoxes boxes);

    internal static IReadOnlyList<ChartGradientStop> CheckStops(IList<ChartFillStop>? stops)
    {
        if (stops is null || stops.Count < 2 || stops.Count > 16) throw new InvalidOperationException("A gradient fill needs 2 to 16 stops.");
        double previous = 0;
        List<ChartGradientStop> result = new(stops.Count);
        foreach (ChartFillStop s in stops)
        {
            if (!double.IsFinite(s.Offset) || s.Offset < 0 || s.Offset > 1) throw new InvalidOperationException("Gradient stop offsets must be finite and within 0..1.");
            if (s.Offset < previous) throw new InvalidOperationException("Gradient stop offsets must not decrease.");
            previous = s.Offset;
            result.Add(new ChartGradientStop(s.Offset, s.Color));
        }
        return result;
    }

    internal static ChartBox SpaceBox(string space, ChartFillBoxes boxes) => space switch
    {
        "mark" => boxes.Mark,
        "plot" => boxes.Plot,
        "chart" => boxes.Chart,
        _ => throw new InvalidOperationException("Fill space must be 'mark', 'plot' or 'chart'."),
    };

    /// <summary>Interpolates the stops at <paramref name="t"/>, per channel with halves rounded away from zero.</summary>
    internal static Color ColorAtOffset(IReadOnlyList<ChartGradientStop> stops, double t)
    {
        if (t <= stops[0].Offset) return stops[0].Color;
        for (int i = 1; i < stops.Count; i++)
        {
            ChartGradientStop a = stops[i - 1], b = stops[i];
            if (t <= b.Offset)
            {
                double span = b.Offset - a.Offset;
                double f = span == 0 ? 1 : (t - a.Offset) / span;
                static double Byte(double v) => Math.Round(v * 255, MidpointRounding.AwayFromZero);
                int Mix(double x, double y) => (int)Math.Round(Byte(x) + (Byte(y) - Byte(x)) * f, MidpointRounding.AwayFromZero);
                return new Color(Mix(a.Color.ScR, b.Color.ScR), Mix(a.Color.ScG, b.Color.ScG), Mix(a.Color.ScB, b.Color.ScB), Mix(a.Color.ScA, b.Color.ScA));
            }
        }
        return stops[^1].Color;
    }
}

/// <summary>A solid colour fill.</summary>
public sealed class ChartSolidFill : ChartFill
{
    /// <summary>Creates a solid fill.</summary>
    public ChartSolidFill(Color color) => Color = color;
    /// <summary>The fill colour.</summary>
    public Color Color { get; }
    internal override Color FallbackColor => Color;
    internal override bool IsSolid => true;
    internal override ChartResolvedFill Resolve(ChartFillBoxes boxes) => new(Color);
}

/// <summary>A linear gradient fill.</summary>
public sealed class ChartLinearFill : ChartFill
{
    /// <summary>Degrees clockwise from the positive x axis, so <c>90</c> (the default) runs top to bottom.</summary>
    public double Angle { get; set; } = 90;
    /// <summary>Two to sixteen stops with non-decreasing offsets.</summary>
    public IList<ChartFillStop> Stops { get; set; } = new List<ChartFillStop>();
    /// <summary>The box the gradient spans: <c>mark</c> (default), <c>plot</c> or <c>chart</c>.</summary>
    public string Space { get; set; } = "mark";

    internal override bool IsSolid => false;
    internal override Color FallbackColor => ColorAtOffset(CheckStops(Stops), 0.5);

    internal override ChartResolvedFill Resolve(ChartFillBoxes boxes)
    {
        IReadOnlyList<ChartGradientStop> stops = CheckStops(Stops);
        if (!double.IsFinite(Angle)) throw new InvalidOperationException("A linear fill angle must be finite.");
        ChartBox box = SpaceBox(Space, boxes);
        double rad = Angle * Math.PI / 180;
        double dx = Math.Cos(rad), dy = Math.Sin(rad);
        double half = (Math.Abs(box.Width * dx) + Math.Abs(box.Height * dy)) / 2;
        double cx = box.X + box.Width / 2, cy = box.Y + box.Height / 2;
        return new(ColorAtOffset(stops, 0.5),
            new ChartResolvedGradient(cx - dx * half, cy - dy * half, cx + dx * half, cy + dy * half, stops[0].Color, stops[^1].Color, stops));
    }
}

/// <summary>A radial gradient fill.</summary>
public sealed class ChartRadialFill : ChartFill
{
    /// <summary>Centre as two fractions of the box (<c>[0.5, 0.5]</c>).</summary>
    public IList<double> Center { get; set; } = new List<double> { 0.5, 0.5 };
    /// <summary>Radius as a fraction of half the box diagonal (<c>1</c>).</summary>
    public double Radius { get; set; } = 1;
    /// <summary>Focal point as two fractions of the box; unset uses the centre.</summary>
    public IList<double>? Focus { get; set; }
    /// <summary>Two to sixteen stops with non-decreasing offsets.</summary>
    public IList<ChartFillStop> Stops { get; set; } = new List<ChartFillStop>();
    /// <summary>The box the gradient is relative to: <c>mark</c> (default), <c>plot</c> or <c>chart</c>.</summary>
    public string Space { get; set; } = "mark";

    internal override bool IsSolid => false;
    internal override Color FallbackColor => ColorAtOffset(CheckStops(Stops), 0.5);

    private static (double, double) Pair(string name, IList<double>? value)
    {
        if (value is null || value.Count != 2 || !double.IsFinite(value[0]) || !double.IsFinite(value[1])) throw new InvalidOperationException($"{name} must be two finite fractions.");
        return (value[0], value[1]);
    }

    internal override ChartResolvedFill Resolve(ChartFillBoxes boxes)
    {
        IReadOnlyList<ChartGradientStop> stops = CheckStops(Stops);
        if (!double.IsFinite(Radius) || Radius <= 0) throw new InvalidOperationException("A radial fill radius must be positive and finite.");
        (double cxf, double cyf) = Pair(nameof(Center), Center);
        (double fxf, double fyf) = Focus is null ? (cxf, cyf) : Pair(nameof(Focus), Focus);
        ChartBox box = SpaceBox(Space, boxes);
        return new(ColorAtOffset(stops, 0.5), null,
            new ChartResolvedRadialGradient(box.X + cxf * box.Width, box.Y + cyf * box.Height, Radius * Math.Sqrt(box.Width * box.Width + box.Height * box.Height) / 2,
                box.X + fxf * box.Width, box.Y + fyf * box.Height, stops));
    }
}
