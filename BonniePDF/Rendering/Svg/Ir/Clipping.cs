// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BonniePDF.Rendering.Svg.Geometry;
using BonniePDF.Rendering.Svg.Parsers;
using BonniePDF.Rendering.Svg.Sink;

namespace BonniePDF.Rendering.Svg.Ir;

/// <summary>
/// One clip region: a path, a rule, and the space it is expressed in.
/// </summary>
/// <remarks>
/// The transform is kept rather than folded into the segments because the
/// document's root transform is only known at emit time, and applying it twice
/// would double-scale the clip.
/// </remarks>
/// <param name="Segments">The clip geometry.</param>
/// <param name="Rule">The fill rule that decides what is inside.</param>
/// <param name="Transform">The matrix mapping the segments into the emitting space.</param>
internal readonly record struct IrClip(IReadOnlyList<PathSegment> Segments, FillRule Rule, Matrix Transform);

/// <summary>Resolves <c>clip-path</c> references into clip regions.</summary>
internal static partial class Clipping
{
    /// <summary>Nested clip-path chains deeper than this are treated as runaway.</summary>
    private const int MaxClipDepth = 8;

    private static readonly HashSet<string> ClipShapes =
        new(StringComparer.Ordinal) { "rect", "circle", "ellipse", "line", "polyline", "polygon", "path" };

    [GeneratedRegex(@"^url\([ \t\n\r\f]*['""]?#(?<id>[^'"") \t\n\r\f]+)['""]?[ \t\n\r\f]*\)$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ReferencePattern();

    /// <summary>
    /// Resolves a <c>clip-path</c> reference into the clip regions to apply,
    /// outermost first.
    /// </summary>
    /// <remarks>
    /// Returns more than one when the clip path itself carries a <c>clip-path</c>,
    /// which the spec defines as an intersection. Apache FOP ignores that nesting
    /// and emits a single clip; this follows the spec, as resvg and browsers do.
    /// </remarks>
    public static List<IrClip> Resolve(
        string value, SvgDom dom, Box? bounds, Matrix elementTransform, List<UnsupportedFeature> unsupported)
    {
        var clips = new List<IrClip>();
        string? reference = ReferenceId(value);

        if (reference is null)
        {
            // `clip-path: none` is handled by the caller; anything else
            // non-referential is a CSS basic shape, which is a different feature.
            unsupported.Add(new UnsupportedFeature("clip-path",
                $"only url(#id) references are supported, not \"{value.Trim()}\""));
            return clips;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (int depth = 0; depth < MaxClipDepth && reference is not null; depth++)
        {
            if (!seen.Add(reference))
            {
                unsupported.Add(new UnsupportedFeature("clip-path", $"reference cycle through #{reference}"));
                break;
            }

            if (!dom.ById.TryGetValue(reference, out SvgElement? element))
            {
                // A dead reference is not the same as no clip: per the spec an
                // invalid reference makes the element not render at all, which is
                // expressed as an empty clip.
                clips.Add(new IrClip([], FillRule.NonZero, Matrix.Identity));
                break;
            }

            if (!string.Equals(element.Name, "clipPath", StringComparison.Ordinal))
            {
                unsupported.Add(new UnsupportedFeature("clip-path",
                    $"#{reference} is <{element.Name}>, not a clipPath"));
                break;
            }

            IrClip? clip = BuildClip(element, dom, bounds, elementTransform, unsupported);
            if (clip is { } resolved) clips.Insert(0, resolved);

            // A clip-path on the clipPath itself intersects with it.
            reference = null;
            if (element.Attributes.TryGetValue("clip-path", out string? nested)
                && !string.Equals(nested.Trim(), "none", StringComparison.OrdinalIgnoreCase))
            {
                reference = ReferenceId(nested);
                if (reference is null)
                {
                    unsupported.Add(new UnsupportedFeature("clip-path",
                        $"only url(#id) references are supported, not \"{nested.Trim()}\""));
                }
            }
        }

        return clips;
    }

    private static string? ReferenceId(string value)
    {
        Match match = ReferencePattern().Match(value.Trim());
        return match.Success ? match.Groups["id"].Value : null;
    }

    /// <summary>
    /// Builds one clip region from a <c>clipPath</c> element's children.
    /// </summary>
    /// <remarks>
    /// The children's silhouettes are logically unioned per the spec. This
    /// concatenates their subpaths into one path, which is the union exactly when
    /// the children do not overlap with opposing winding - so a clip path with more
    /// than one child is reported as an approximation. In the harvested corpus all
    /// 12 clip paths have a single child, so that report costs nothing in practice.
    /// </remarks>
    private static IrClip? BuildClip(
        SvgElement element, SvgDom dom, Box? bounds, Matrix elementTransform, List<UnsupportedFeature> unsupported)
    {
        bool objectBoundingBox = string.Equals(element.Attributes.GetValueOrDefault("clipPathUnits"), "objectBoundingBox", StringComparison.Ordinal);
        if (objectBoundingBox && bounds is null) return null;

        var segments = new List<PathSegment>();
        var rules = new HashSet<FillRule>();
        int children = 0;

        foreach (SvgElement child in element.Children)
        {
            if (ClipChild(child, dom, unsupported) is not { } contribution) continue;
            children++;
            rules.Add(contribution.Rule);
            segments.AddRange(contribution.Segments);
        }

        if (children > 1)
        {
            unsupported.Add(new UnsupportedFeature("clip-path",
                "a clipPath with more than one child is unioned by concatenating subpaths, which differs "
                + "from a true union where they overlap with opposing winding"));
        }

        if (rules.Count > 1)
        {
            unsupported.Add(new UnsupportedFeature("clip-rule",
                "children disagreeing on clip-rule cannot be expressed as one clip path"));
        }

        // In objectBoundingBox units the children's coordinates are fractions of
        // the clipped element's box, so the box mapping comes first.
        Matrix unitTransform = objectBoundingBox
            ? new Matrix(bounds!.Value.Width, 0, 0, bounds.Value.Height, bounds.Value.X, bounds.Value.Y)
            : Matrix.Identity;

        return new IrClip(
            segments,
            rules.Count == 1 ? rules.First() : FillRule.NonZero,
            Matrix.Multiply(elementTransform, unitTransform));
    }

    private readonly record struct ClipContribution(IReadOnlyList<PathSegment> Segments, FillRule Rule);

    /// <summary>
    /// One child's contribution.
    /// </summary>
    /// <remarks>
    /// Only raw geometry counts - fill, stroke and opacity on a clip path's child
    /// are ignored per the spec. A child's own <c>transform</c> is folded into its
    /// coordinates here, because the whole clip is emitted under a single matrix.
    /// </remarks>
    private static ClipContribution? ClipChild(SvgElement child, SvgDom dom, List<UnsupportedFeature> unsupported)
    {
        if (string.Equals(child.Name, "use", StringComparison.Ordinal))
        {
            if (!child.Attributes.TryGetValue("href", out string? href) || !href.StartsWith('#')) return null;
            if (!dom.ById.TryGetValue(href[1..], out SvgElement? target)) return null;
            if (ClipChild(target, dom, unsupported) is not { } inner) return null;

            double x = LengthParser.Parse(child.Attributes.GetValueOrDefault("x") ?? "0") ?? 0;
            double y = LengthParser.Parse(child.Attributes.GetValueOrDefault("y") ?? "0") ?? 0;
            return new ClipContribution(TransformSegments(inner.Segments, Matrix.Translation(x, y)), inner.Rule);
        }

        if (!ClipShapes.Contains(child.Name))
        {
            // <text> in a clip path is legal and needs glyph outlines, which v1
            // does not have. Anything else is not clip geometry at all.
            if (child.Name is "text" or "tspan")
            {
                unsupported.Add(new UnsupportedFeature("clip-path",
                    "text in a clipPath needs glyph outlines, which are not implemented"));
            }

            return null;
        }

        List<PathSegment> raw = ShapeSegments(child);
        if (raw.Count == 0) return null;

        IReadOnlyList<PathSegment> segments = child.Attributes.TryGetValue("transform", out string? local)
            ? TransformSegments(raw, TransformParser.Parse(local))
            : raw;

        FillRule rule = string.Equals(child.Attributes.GetValueOrDefault("clip-rule")?.Trim(), "evenodd",
            StringComparison.OrdinalIgnoreCase)
            ? FillRule.EvenOdd
            : FillRule.NonZero;

        return new ClipContribution(segments, rule);
    }

    /// <summary>Applies a matrix to every coordinate of a path.</summary>
    public static List<PathSegment> TransformSegments(IReadOnlyList<PathSegment> segments, Matrix m)
    {
        double X(double px, double py) => m.A * px + m.C * py + m.E;
        double Y(double px, double py) => m.B * px + m.D * py + m.F;

        var mapped = new List<PathSegment>(segments.Count);
        foreach (PathSegment segment in segments)
        {
            mapped.Add(segment.Kind switch
            {
                PathSegmentKind.Move => PathSegment.Move(X(segment.X, segment.Y), Y(segment.X, segment.Y)),
                PathSegmentKind.Line => PathSegment.Line(X(segment.X, segment.Y), Y(segment.X, segment.Y)),
                PathSegmentKind.Cubic => PathSegment.Cubic(
                    X(segment.C1X, segment.C1Y), Y(segment.C1X, segment.C1Y),
                    X(segment.C2X, segment.C2Y), Y(segment.C2X, segment.C2Y),
                    X(segment.X, segment.Y), Y(segment.X, segment.Y)),
                _ => PathSegment.Close(),
            });
        }

        return mapped;
    }

    private static List<PathSegment> ShapeSegments(SvgElement element)
    {
        double Number(string key, double fallback = 0.0)
        {
            if (!element.Attributes.TryGetValue(key, out string? raw)) return fallback;
            return LengthParser.Parse(raw) ?? fallback;
        }

        double? Optional(string key) =>
            element.Attributes.TryGetValue(key, out string? raw) ? LengthParser.Parse(raw) : null;

        switch (element.Name)
        {
            case "rect":
                return Shapes.Rect(Number("x"), Number("y"), Number("width"), Number("height"), Optional("rx"), Optional("ry"));
            case "circle":
            {
                double r = Number("r");
                return Shapes.Ellipse(Number("cx"), Number("cy"), r, r);
            }
            case "ellipse":
                return Shapes.Ellipse(Number("cx"), Number("cy"), Number("rx"), Number("ry"));
            case "line":
                return Shapes.Line(Number("x1"), Number("y1"), Number("x2"), Number("y2"));
            case "polyline":
                return Shapes.Poly(element.Attributes.GetValueOrDefault("points") ?? string.Empty, false);
            case "polygon":
                return Shapes.Poly(element.Attributes.GetValueOrDefault("points") ?? string.Empty, true);
            case "path":
                return element.Attributes.TryGetValue("d", out string? d) ? PathDataParser.Parse(d) : [];
            default:
                return [];
        }
    }
}
