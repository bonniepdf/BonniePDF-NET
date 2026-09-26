// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BonniePDF.Rendering.Svg.Geometry;
using BonniePDF.Rendering.Svg.Parsers;
using BonniePDF.Rendering.Svg.Sink;

namespace BonniePDF.Rendering.Svg.Ir;

/// <summary>What a gradient needs to know about the element referencing it.</summary>
/// <param name="Bounds">The referencing element's tight bounding box, for objectBoundingBox units.</param>
/// <param name="CurrentColor">The colour <c>currentColor</c> resolves to at the point of use.</param>
/// <param name="Opacity">Multiplied into every stop's alpha - fill-opacity or stroke-opacity.</param>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct GradientContext(Box? Bounds, PaintRef CurrentColor, double Opacity);

/// <summary>The outcome of resolving a paint reference.</summary>
/// <param name="Paint">The paint, or null when nothing should be painted.</param>
/// <param name="Unsupported">Anything about the gradient the sink cannot express.</param>
internal readonly record struct GradientResult(Paint? Paint, IReadOnlyList<UnsupportedFeature> Unsupported);

/// <summary>Resolves SVG gradients to sink paints.</summary>
internal static class Gradients
{
    /// <summary>How deep an href-inheriting chain may go before we call it a cycle.</summary>
    private const int MaxHrefDepth = 16;

    /// <summary>
    /// Resolves a <c>url(#id)</c> paint reference to a sink paint.
    /// </summary>
    /// <remarks>
    /// A missing reference is not an error: per the spec the element simply is not
    /// painted, and real documents do carry dead references.
    /// </remarks>
    public static GradientResult Resolve(string id, SvgDom dom, GradientContext context)
    {
        var unsupported = new List<UnsupportedFeature>();

        if (!dom.ById.TryGetValue(id, out SvgElement? element))
        {
            // Not reported: this is valid-but-dead markup, and flagging it would
            // make strict mode reject documents browsers render.
            return new GradientResult(null, unsupported);
        }

        if (element.Name is not ("linearGradient" or "radialGradient"))
        {
            unsupported.Add(new UnsupportedFeature(element.Name,
                $"paint server <{element.Name}> is not in the supported profile (gradients are)"));
            return new GradientResult(null, unsupported);
        }

        List<SvgElement> chain = CollectChain(element, dom);

        string? Attribute(string name)
        {
            foreach (SvgElement link in chain)
            {
                if (link.Attributes.TryGetValue(name, out string? value)) return value;
            }

            return null;
        }

        string? spread = Attribute("spreadMethod");
        if (spread is not null && !string.Equals(spread, "pad", StringComparison.Ordinal))
        {
            // The sink's gradient paints have no spread mode - ICanvas exposes an
            // axial or radial ramp and nothing else - so reflect and repeat cannot
            // be expressed. Approximating them by duplicating stops would be a
            // different gradient, not the same one drawn differently.
            unsupported.Add(new UnsupportedFeature($"spreadMethod=\"{spread}\"",
                "the sink supports only pad; reflect and repeat cannot be expressed"));
        }

        List<GradientStop> stops = CollectStops(chain, context);
        // Per the spec: no stops means the element is not painted at all.
        if (stops.Count == 0) return new GradientResult(null, unsupported);
        // Per the spec: a single stop is a solid fill of that stop's colour.
        if (stops.Count == 1) return new GradientResult(new SolidPaint(stops[0].Color), unsupported);

        bool userSpace = string.Equals(Attribute("gradientUnits"), "userSpaceOnUse", StringComparison.Ordinal);
        // Without a bounding box there is nothing for objectBoundingBox to be
        // relative to, which happens for an empty path. Nothing is drawn anyway.
        if (!userSpace && context.Bounds is null) return new GradientResult(null, unsupported);

        string? gradientTransform = Attribute("gradientTransform");
        Matrix transform = gradientTransform is not null ? TransformParser.Parse(gradientTransform) : Matrix.Identity;

        Matrix mapping = userSpace
            ? transform
            : Matrix.Multiply(ObjectBoundingBoxMatrix(context.Bounds!.Value), transform);

        if (string.Equals(element.Name, "linearGradient", StringComparison.Ordinal))
        {
            // Defaults per the spec: a horizontal ramp across the whole box.
            double x1 = Coordinate(Attribute("x1"), 0, userSpace, context.Bounds, horizontal: true);
            double y1 = Coordinate(Attribute("y1"), 0, userSpace, context.Bounds, horizontal: false);
            double x2 = Coordinate(Attribute("x2"), 1, userSpace, context.Bounds, horizontal: true);
            double y2 = Coordinate(Attribute("y2"), 0, userSpace, context.Bounds, horizontal: false);
            // The sink carries the mapping now, so the endpoints stay in the
            // gradient's own space. Pre-multiplying them as well would apply the
            // matrix twice - and would lose the shear, which is the whole reason
            // the sink grew a transform.
            return new GradientResult(
                new LinearGradientPaint(Point(x1, y1), Point(x2, y2), stops.ToArray())
                {
                    Transform = ToPaintTransform(mapping),
                },
                unsupported);
        }

        double cx = Coordinate(Attribute("cx"), 0.5, userSpace, context.Bounds, horizontal: true);
        double cy = Coordinate(Attribute("cy"), 0.5, userSpace, context.Bounds, horizontal: false);
        double r = Radius(Attribute("r"), 0.5, userSpace, context.Bounds);
        // fx/fy default to cx/cy, giving a centred highlight.
        double fx = Attribute("fx") is { } rawFx ? Coordinate(rawFx, cx, userSpace, context.Bounds, horizontal: true) : cx;
        double fy = Attribute("fy") is { } rawFy ? Coordinate(rawFy, cy, userSpace, context.Bounds, horizontal: false) : cy;

        // Likewise: the centre, focal point and radius stay in gradient space and
        // the sink's transform turns the circle into an ellipse where the map is
        // non-conformal. That case used to be reported as inexpressible.
        return new GradientResult(
            new RadialGradientPaint(Point(cx, cy), (float)r, stops.ToArray(), Point(fx, fy))
            {
                Transform = ToPaintTransform(mapping),
            },
            unsupported);
    }

    /// <summary>
    /// The gradient element and everything it inherits from via <c>href</c>, nearest first.
    /// </summary>
    /// <remarks>
    /// Gradients commonly define stops once and derive geometry variants from
    /// them, so an attribute lookup walks this chain. Cycles are real in
    /// hand-edited files, so visited elements are tracked rather than trusted.
    /// </remarks>
    private static List<SvgElement> CollectChain(SvgElement start, SvgDom dom)
    {
        var chain = new List<SvgElement> { start };
        var seen = new HashSet<SvgElement> { start };
        SvgElement current = start;

        for (int depth = 0; depth < MaxHrefDepth; depth++)
        {
            if (!current.Attributes.TryGetValue("href", out string? href) || !href.StartsWith('#')) break;
            if (!dom.ById.TryGetValue(href[1..], out SvgElement? next)) break;
            if (!seen.Add(next)) break;
            if (next.Name is not ("linearGradient" or "radialGradient")) break;
            chain.Add(next);
            current = next;
        }

        return chain;
    }

    /// <summary>
    /// The stops of the nearest link in the chain that has any.
    /// </summary>
    /// <remarks>
    /// Stops are not merged across the chain: a gradient either declares its own
    /// or inherits the whole set, per the spec.
    /// </remarks>
    private static List<GradientStop> CollectStops(IReadOnlyList<SvgElement> chain, GradientContext context)
    {
        foreach (SvgElement link in chain)
        {
            List<SvgElement> stopElements = link.Children.Where(child => string.Equals(child.Name, "stop", StringComparison.Ordinal)).ToList();
            if (stopElements.Count == 0) continue;

            var stops = new List<GradientStop>();
            double previousOffset = 0;

            foreach (SvgElement stop in stopElements)
            {
                Dictionary<string, string> declarations = InlineStyle(stop);

                string? rawOffset = declarations.TryGetValue("offset", out string? styleOffset)
                    ? styleOffset
                    : stop.Attributes.GetValueOrDefault("offset");
                double parsed = rawOffset is null
                    ? 0
                    : LengthParser.Parse(rawOffset, new LengthContext(1, 16)) ?? 0;
                // Offsets clamp to [0,1] and must not decrease: a later stop that
                // sits before an earlier one is pulled forward, per the spec, which
                // is what makes a hard colour break expressible.
                double offset = Math.Max(previousOffset, Math.Min(1, Math.Max(0, parsed)));
                previousOffset = offset;

                string colorValue = declarations.TryGetValue("stop-color", out string? styleColor)
                    ? styleColor
                    : stop.Attributes.GetValueOrDefault("stop-color") ?? "#000000";
                string? opacityValue = declarations.TryGetValue("stop-opacity", out string? styleOpacity)
                    ? styleOpacity
                    : stop.Attributes.GetValueOrDefault("stop-opacity");
                double stopOpacity = opacityValue is null ? 1 : Clamp01(LengthParser.Parse(opacityValue) ?? 1);

                PaintRef reference = string.Equals(colorValue.Trim(), "currentColor", StringComparison.OrdinalIgnoreCase)
                    ? context.CurrentColor
                    : ColorParser.Parse(colorValue) ?? PaintRef.FromColor(new SinkColor(0, 0, 0, 1));

                SinkColor with = reference.Kind == PaintRefKind.Color ? reference.Color : new SinkColor(0, 0, 0, 1);
                // Three alphas multiply here: the stop colour's own, stop-opacity,
                // and the element's fill- or stroke-opacity. A sink has one alpha
                // per stop.
                stops.Add(new GradientStop((float)offset,
                    new SinkColor(with.R, with.G, with.B, (float)(with.A * stopOpacity * context.Opacity))));
            }

            return stops;
        }

        return [];
    }

    private static Dictionary<string, string> InlineStyle(SvgElement element)
    {
        var declarations = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!element.Attributes.TryGetValue("style", out string? style)) return declarations;

        foreach (string declaration in style.Split(';'))
        {
            int colon = declaration.IndexOf(':', StringComparison.Ordinal);
            if (colon == -1) continue;
            declarations[declaration[..colon].Trim().ToLowerInvariant()] = declaration[(colon + 1)..].Trim();
        }

        return declarations;
    }

    /// <summary>Maps the unit square onto the referencing element's bounding box.</summary>
    private static Matrix ObjectBoundingBoxMatrix(Box bounds) =>
        new(bounds.Width, 0, 0, bounds.Height, bounds.X, bounds.Y);

    private static SinkPoint Point(double x, double y) => new((float)x, (float)y);

    /// <summary>
    /// A resolved mapping as a sink paint transform, or null when it is the identity.
    /// </summary>
    /// <remarks>
    /// Returning null for the identity keeps the sink's output byte-identical to a
    /// paint that never had a transform, which matters because most gradients do
    /// not have one.
    /// </remarks>
    private static PaintTransform? ToPaintTransform(Matrix m)
    {
        if (m.A == 1 && m.B == 0 && m.C == 0 && m.D == 1 && m.E == 0 && m.F == 0) return null;
        return new PaintTransform((float)m.A, (float)m.B, (float)m.C, (float)m.D, (float)m.E, (float)m.F);
    }


    private static double Coordinate(string? raw, double fallback, bool userSpace, Box? bounds, bool horizontal)
    {
        if (raw is null) return fallback;
        // In objectBoundingBox units the value is a fraction, so a percentage
        // resolves against 1 - the box mapping is applied afterwards.
        double basis = userSpace
            ? horizontal ? bounds?.Width ?? 0 : bounds?.Height ?? 0
            : 1;
        return LengthParser.Parse(raw, new LengthContext(basis, 16)) ?? fallback;
    }

    private static double Radius(string? raw, double fallback, bool userSpace, Box? bounds)
    {
        if (raw is null) return fallback;
        // A percentage radius resolves against the normalised diagonal, per the spec.
        double basis = userSpace && bounds is { } box
            ? Math.Sqrt(box.Width * box.Width + box.Height * box.Height) / Math.Sqrt(2)
            : 1;
        double? parsed = LengthParser.Parse(raw, new LengthContext(basis, 16));
        return parsed is null ? fallback : Math.Max(0, parsed.Value);
    }

    private static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
}
