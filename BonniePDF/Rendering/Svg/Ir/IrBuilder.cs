// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using BonniePDF.Rendering.Svg.Fonts;
using BonniePDF.Rendering.Svg.Geometry;
using BonniePDF.Rendering.Svg.Parsers;
using BonniePDF.Rendering.Svg.Sink;

namespace BonniePDF.Rendering.Svg.Ir;

/// <summary>Options for building the IR.</summary>
/// <param name="Width">Target box width the viewBox maps onto; defaults to the document's own.</param>
/// <param name="Height">Target box height; defaults to the document's own.</param>
/// <param name="Fonts">
/// Fonts for <c>text</c>, which is imported as outlines. Without one, text is reported
/// as unsupported rather than drawn in a font nobody chose.
/// </param>
/// <param name="RasterScale">
/// When set, hand what the profile cannot draw to a rasterizer, as
/// <see cref="IrRaster"/> nodes, instead of skipping it - at this many raster pixels
/// per unit of the target box. Set by <c>SvgImporter.Parse</c> when the caller
/// supplies a rasterizer and is not strict.
/// </param>
internal readonly record struct BuildOptions(
    double? Width = null, double? Height = null, IFontProvider? Fonts = null, double? RasterScale = null);

/// <summary>Parses an SVG document into the intermediate representation.</summary>
internal static partial class IrBuilder
{
    private static readonly HashSet<string> ShapeElements =
        new(StringComparer.Ordinal) { "rect", "circle", "ellipse", "line", "polyline", "polygon", "path" };

    /// <summary>Containers that are traversed and drawn.</summary>
    private static readonly HashSet<string> Groups =
        new(StringComparer.Ordinal) { "g", "svg", "a", "switch" };

    /// <summary>
    /// Elements parsed but never drawn where they sit. Their content appears only
    /// through a reference - <c>use</c>, a paint, a clip, an effect - and it is the
    /// reference that is checked against the profile, not the definition.
    /// </summary>
    private static readonly HashSet<string> NonRendering = new(StringComparer.Ordinal)
    {
        "defs", "symbol", "linearGradient", "radialGradient", "clipPath", "mask", "pattern", "marker", "filter"
    };

    /// <summary>
    /// Elements outside the supported profile, and what they need. Named
    /// explicitly so a report says which feature was hit rather than "unknown
    /// element".
    /// </summary>
    private static readonly Dictionary<string, string> OutOfProfile = new(StringComparer.Ordinal)
    {
        ["foreignObject"] = "embedded foreign content is not in the supported profile",
        ["script"] = "scripting is refused, not deferred",
        ["text"] = "text is outlined in supplied fonts, and none were supplied",
        ["tspan"] = "text is outlined in supplied fonts, and none were supplied",
        ["image"] = "raster images are not in the supported profile",
        ["animate"] = "animation is never supported in a static medium",
        ["animateTransform"] = "animation is never supported in a static medium",
    };

    private static readonly HashSet<string> PresentationAttributes = new(StringComparer.Ordinal)
    {
        "fill", "stroke", "stroke-width", "fill-rule", "fill-opacity", "stroke-opacity", "opacity",
        "stroke-linecap", "stroke-linejoin", "stroke-miterlimit", "stroke-dasharray", "stroke-dashoffset",
        "color", "display", "visibility",
        "font-family", "font-size", "font-weight", "font-style", "text-anchor", "letter-spacing", "word-spacing",
        "dominant-baseline", "alignment-baseline", "baseline-shift", "text-decoration", "writing-mode",
        "direction", "unicode-bidi", "font-variant", "font-stretch"
    };

    /// <summary>
    /// Attributes that apply an effect the profile cannot honour.
    /// </summary>
    /// <remarks>
    /// These are reported at the point of <em>reference</em>, not of definition.
    /// Reporting <c>&lt;filter&gt;</c> where it is declared would be both noisy and
    /// wrong: a filter sitting unreferenced in <c>defs</c> affects nothing, while an
    /// element carrying <c>filter="url(#a)"</c> renders grossly wrong without it - a
    /// drop-shadow layer drawn unfiltered becomes an opaque black shape. Slice 1
    /// reported by element name and got away with it only because <c>defs</c> was
    /// itself out of profile then.
    /// </remarks>
    private static readonly string[] EffectAttributes =
        ["filter", "mask", "marker-start", "marker-mid", "marker-end", "marker"];

    /// <summary>The box a document without a viewBox is fitted by: its own declared size.</summary>
    /// <remarks>
    /// A document drawn into a box is drawn as an image is, and an image scales to the
    /// box it is given. Without this, a badge declaring only width="88" height="20"
    /// drew at 88x20 in the corner of a 512-wide box - which resvg, and a browser's
    /// &lt;img&gt;, both scale to fill. Without a declared size there is nothing to fit.
    /// </remarks>
    /// <summary>
    /// The root's own <c>width</c> or <c>height</c> as an intrinsic size, or null when
    /// it is absent, a percentage - of a container a standalone document does not
    /// have - or not a positive length.
    /// </summary>
    private static double? IntrinsicLength(string? value)
    {
        if (value is null || value.Trim().EndsWith('%')) return null;
        return LengthParser.Parse(value) is { } length && length > 0 ? length : null;
    }

    private static ViewBox? IntrinsicBox(double? width, double? height) =>
        width is { } w && height is { } h && w > 0 && h > 0 ? new ViewBox(0, 0, w, h) : null;

    /// <summary>
    /// Stylesheets are reported for the whole document, not where the walk meets them.
    /// </summary>
    /// <remarks>
    /// A <c>&lt;style&gt;</c> element applies to everything in the document wherever
    /// it sits, and Illustrator's CSS export puts it inside <c>defs</c> - which the
    /// walk does not enter. Detecting it during the walk therefore missed exactly
    /// the common case, and the document was drawn with its class rules ignored: a
    /// rule as ordinary as <c>.cls-1 { fill: none }</c> left an element painted in
    /// the default black over the artwork, and nothing was reported. An empty
    /// stylesheet affects nothing and is not reported.
    /// </remarks>
    private const string StylesheetReason = "CSS stylesheets are not in the supported profile (inline style is)";

    private static bool HasStylesheet(SvgElement element) =>
        (string.Equals(element.Name, "style", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(element.Text))
        || element.Children.Exists(HasStylesheet);

    /// <summary>Blend modes are reported for the whole document too.</summary>
    /// <remarks>
    /// Found when the rasterizer fallback first drew the filtered crypto icons: 10 of
    /// them carry a <c>mix-blend-mode: soft-light</c> highlight that had never been
    /// reported, and was drawn as an ordinary translucent gradient - about half the
    /// icon wrong. It went unseen only because their filters had kept them out of the
    /// gate. Detected document-wide, like a stylesheet, because a blend cannot be
    /// rasterized in isolation: it needs the backdrop beneath the element. A blend
    /// mode on an element nobody draws is over-reported, which is the safe direction.
    /// </remarks>
    private const string BlendReason = "mix-blend-mode blends with the backdrop, which the supported profile cannot reproduce";

    private static bool HasBlendMode(SvgElement element) =>
        (EffectValue(element, "mix-blend-mode") is { } value
            && !string.Equals(value.Trim(), "normal", StringComparison.OrdinalIgnoreCase))
        || element.Children.Exists(HasBlendMode);

    /// <summary>A use chain deeper than this is treated as runaway rather than legitimate.</summary>
    private const int MaxUseDepth = 16;

    /// <summary>Where rasterized elements go, and what they were rasterized for.</summary>
    private sealed record RasterState(RasterRegion Region, List<UnsupportedFeature> Rasterized);

    /// <summary>The state inherited down the walk.</summary>
    /// <param name="Style">The computed style so far.</param>
    /// <param name="Transform">The matrix from here to root user space.</param>
    /// <param name="ActiveUses">Ids currently being expanded by <c>use</c>, to catch cycles.</param>
    /// <param name="Fonts">Fonts for text, if any.</param>
    /// <param name="Lineage">
    /// The elements whose inherited properties reach this point, outermost first -
    /// the document path, or through a <c>use</c> the use and its own ancestors. A
    /// rasterized element's snippet carries what these pass down.
    /// </param>
    /// <param name="Raster">Raster mode, when the caller supplied a rasterizer.</param>
    private readonly record struct Context(
        ComputedStyle Style,
        Matrix Transform,
        IReadOnlyList<string> ActiveUses,
        IFontProvider? Fonts,
        IReadOnlyList<SvgElement> Lineage,
        RasterState? Raster);

    /// <summary>Parses an SVG document.</summary>
    public static IrDocument Build(string source, BuildOptions options = default)
    {
        SvgDom dom = DomParser.Parse(source);
        var unsupported = new List<UnsupportedFeature>();
        var children = new List<IrNode>();

        if (dom.Root is not { } root)
        {
            return new IrDocument(Matrix.Identity, children, null, null, unsupported, [], null, null,
                new DocumentFit(null, PreserveAspectRatio.Default, null, null));
        }

        ViewBox? viewBox = root.Attributes.TryGetValue("viewBox", out string? viewBoxAttribute)
            ? ViewBoxParser.ParseViewBox(viewBoxAttribute)
            : null;
        PreserveAspectRatio preserve = root.Attributes.TryGetValue("preserveAspectRatio", out string? preserveAttribute)
            ? ViewBoxParser.ParsePreserveAspectRatio(preserveAttribute)
            : PreserveAspectRatio.Default;

        double? declaredWidth = ReadLength(root.Attributes, "width", viewBox?.Width);
        double? declaredHeight = ReadLength(root.Attributes, "height", viewBox?.Height);
        double? targetWidth = options.Width ?? declaredWidth ?? viewBox?.Width;
        double? targetHeight = options.Height ?? declaredHeight ?? viewBox?.Height;
        ViewBox? userBox = viewBox ?? IntrinsicBox(declaredWidth, declaredHeight);
        Matrix rootTransform = targetWidth is { } w && targetHeight is { } h
            ? ViewBoxParser.Transform(userBox, w, h, preserve)
            : Matrix.Identity;
        var fit = new DocumentFit(
            userBox,
            preserve,
            IntrinsicLength(root.Attributes.GetValueOrDefault("width")) ?? viewBox?.Width,
            IntrinsicLength(root.Attributes.GetValueOrDefault("height")) ?? viewBox?.Height);

        RasterRegion? region = options.RasterScale is { } scale
            ? RasterRegionFor(userBox, targetWidth, targetHeight, rootTransform, scale)
            : null;
        RasterState? raster = region is { } r ? new RasterState(r, []) : null;
        IrDocument Document(IReadOnlyList<IrNode> nodes) =>
            new(rootTransform, nodes, targetWidth, targetHeight, unsupported,
                raster?.Rasterized ?? [], FirstText(root, "title"), FirstText(root, "desc"), fit);

        // Problems that cannot be confined to one element. A stylesheet applies
        // everywhere; a blend mode mixes an element with whatever is painted beneath
        // it, which a snippet holding only that element does not have. Either way
        // nothing drawn without it can be trusted, so with a rasterizer the whole
        // document goes to it as one image.
        var documentWide = new List<UnsupportedFeature>();
        if (HasStylesheet(root)) documentWide.Add(new UnsupportedFeature("style", StylesheetReason));
        if (HasBlendMode(root)) documentWide.Add(new UnsupportedFeature("mix-blend-mode", BlendReason));
        if (documentWide.Count > 0)
        {
            if (raster is not null)
            {
                raster.Rasterized.AddRange(documentWide);
                RasterRegion whole = raster.Region;
                return Document([new IrRaster(Raster.DocumentSnippet(root, whole), whole.X, whole.Y, whole.Width, whole.Height, "svg")]);
            }
            unsupported.AddRange(documentWide);
        }

        ComputedStyle rootStyle = Cascade(ComputedStyle.Initial, root.Attributes);
        var context = new Context(rootStyle, Matrix.Identity, [], options.Fonts, [root], raster);

        foreach (SvgElement child in root.Children) Visit(child, context, dom, children, unsupported);

        return Document(rootStyle.Opacity < 1.0 ? [new IrGroup(children, rootStyle.Opacity)] : children);
    }

    /// <summary>
    /// The region a raster covers - the document's viewport in root user space - and
    /// its size in pixels at <paramref name="scale"/> pixels per target unit. Null
    /// when the document has no size at all, which leaves nothing to rasterize against.
    /// </summary>
    private static RasterRegion? RasterRegionFor(
        ViewBox? viewBox, double? targetWidth, double? targetHeight, Matrix rootTransform, double scale)
    {
        ViewBox? area = viewBox ?? (targetWidth is { } w && targetHeight is { } h ? new ViewBox(0, 0, w, h) : null);
        if (area is not { } a || !(a.Width > 0) || !(a.Height > 0)) return null;
        // Sqrt rather than a hypot: IEEE rounds sqrt exactly, so TypeScript
        // computes the same bits.
        double sx = Math.Sqrt(rootTransform.A * rootTransform.A + rootTransform.B * rootTransform.B);
        double sy = Math.Sqrt(rootTransform.C * rootTransform.C + rootTransform.D * rootTransform.D);
        return new RasterRegion(
            a.MinX, a.MinY, a.Width, a.Height,
            Math.Max(1, (int)Math.Ceiling(a.Width * sx * scale - 1e-9)),
            Math.Max(1, (int)Math.Ceiling(a.Height * sy * scale - 1e-9)));
    }

    /// <summary>Visits one element, and rasterizes it if its drawing reported anything.</summary>
    /// <remarks>
    /// The unit of rasterization is the smallest element whose own drawing the
    /// profile could not reproduce: a descendant that rasterized itself takes its
    /// report with it, so only what this element alone caused is left. Rasterizing
    /// the smallest unit keeps everything around it as vectors and keeps the image in
    /// its place in the paint order.
    /// </remarks>
    private static void Visit(
        SvgElement element, Context context, SvgDom dom, List<IrNode> output, List<UnsupportedFeature> unsupported)
    {
        if (context.Raster is not { } raster)
        {
            VisitElement(element, context, dom, output, unsupported);
            return;
        }

        int reported = unsupported.Count;
        int drawn = output.Count;
        VisitElement(element, context, dom, output, unsupported);
        if (unsupported.Count == reported) return;

        output.RemoveRange(drawn, output.Count - drawn);
        raster.Rasterized.AddRange(unsupported.GetRange(reported, unsupported.Count - reported));
        unsupported.RemoveRange(reported, unsupported.Count - reported);
        RasterRegion region = raster.Region;
        output.Add(new IrRaster(
            Raster.ElementSnippet(element, context.Lineage, context.Transform, region, dom),
            region.X, region.Y, region.Width, region.Height, element.Name));
    }

    private static void VisitElement(
        SvgElement element, Context context, SvgDom dom, List<IrNode> output, List<UnsupportedFeature> unsupported)
    {
        string name = element.Name;
        // A stylesheet is detected for the whole document in Build, not here.
        if (name is "title" or "desc" or "metadata" or "style") return;

        if (!(string.Equals(name, "text", StringComparison.Ordinal) && context.Fonts is not null) && OutOfProfile.TryGetValue(name, out string? reason))
        {
            unsupported.Add(new UnsupportedFeature(name, reason));
            return;
        }

        // Parsed but not drawn here; reachable only through <use>.
        if (NonRendering.Contains(name)) return;

        ComputedStyle style = Cascade(context.Style, element.Attributes);
        if (!style.Display) return;

        foreach (string attribute in EffectAttributes)
        {
            if (EffectValue(element, attribute) is { } value)
            {
                unsupported.Add(new UnsupportedFeature(attribute,
                    $"{attribute}=\"{value}\" applies an effect the supported profile cannot reproduce"));
            }
        }

        Matrix transform = element.Attributes.TryGetValue("transform", out string? transformAttribute)
            ? Matrix.Multiply(context.Transform, TransformParser.Parse(transformAttribute))
            : context.Transform;
        IReadOnlyList<SvgElement> lineage = [.. context.Lineage, element];

        if (string.Equals(name, "text", StringComparison.Ordinal))
        {
            VisitText(element, style, transform, context.Fonts!, dom, output, unsupported);
            return;
        }

        if (string.Equals(name, "use", StringComparison.Ordinal))
        {
            VisitUse(element, context with { Style = style, Transform = transform, Lineage = lineage }, dom, output, unsupported);
            return;
        }

        if (ShapeElements.Contains(name))
        {
            if (!style.Visibility) return;
            List<PathSegment> segments = ShapeSegments(name, element.Attributes);
            if (segments.Count == 0) return;

            Box? bounds = BoundingBox.Of(segments);
            Paint? fillPaint = ResolvePaint(style.Fill, style.FillOpacity, bounds, style, dom, unsupported);
            Paint? strokePaint = ResolvePaint(style.Stroke, style.StrokeOpacity, bounds, style, dom, unsupported);
            if (fillPaint is null && strokePaint is null) return;

            var path = new IrPath(segments, transform, style, fillPaint, strokePaint, name);
            IrNode composited = style.Opacity < 1.0 ? new IrGroup([path], style.Opacity) : path;
            // The clip is resolved here rather than before the paints because
            // clipPathUnits="objectBoundingBox" needs this path's own bounding box.
            output.Add(Clipped(composited, element, transform, bounds, dom, unsupported));
            return;
        }

        if (Groups.Contains(name))
        {
            var nested = new List<IrNode>();
            foreach (SvgElement child in element.Children)
            {
                Visit(child, context with { Style = style, Transform = transform, Lineage = lineage }, dom, nested, unsupported);
            }

            if (nested.Count == 0) return;

            if (element.Attributes.TryGetValue("clip-path", out string? clipValue)
                && !string.Equals(clipValue.Trim(), "none", StringComparison.OrdinalIgnoreCase))
            {
                IrNode compositedGroup = new IrGroup(nested, style.Opacity < 1.0 ? style.Opacity : 1.0);
                output.Add(Clipped(compositedGroup, element, transform, null, dom, unsupported));
                return;
            }

            // A group that composites nothing is flattened away, so the IR does
            // not accumulate a wrapper - and the sink a saveState/restoreState
            // pair - per nesting level of the source.
            if (style.Opacity < 1.0) output.Add(new IrGroup(nested, style.Opacity));
            else output.AddRange(nested);
            return;
        }

        // Unknown element: traverse it but draw nothing itself. Real exports carry
        // vendor elements whose children are ordinary shapes.
        foreach (SvgElement child in element.Children)
        {
            Visit(child, context with { Style = style, Transform = transform, Lineage = lineage }, dom, output, unsupported);
        }
    }

    /// <summary>
    /// Expands a <c>use</c>.
    /// </summary>
    /// <remarks>
    /// <c>x</c>/<c>y</c> become a translate, and the element's own style is the
    /// <em>inherited</em> context for the referenced content - the referenced
    /// element's own attributes still win, which is what makes one defs shape
    /// reusable in several colours.
    /// </remarks>
    private static void VisitUse(
        SvgElement element, Context context, SvgDom dom, List<IrNode> output, List<UnsupportedFeature> unsupported)
    {
        // Both spellings appear in the wild: SVG 1.1's xlink:href and SVG 2's
        // href. The XML reader strips prefixes, so both arrive as `href`.
        if (!element.Attributes.TryGetValue("href", out string? href)) return;

        if (!href.StartsWith('#'))
        {
            unsupported.Add(new UnsupportedFeature("use",
                "an external or non-fragment href is refused; only same-document references are resolved"));
            return;
        }

        string id = href[1..];
        if (context.ActiveUses.Contains(id))
        {
            // A cycle is a real thing in hand-edited files, and expanding it would
            // not terminate.
            unsupported.Add(new UnsupportedFeature("use", $"reference cycle through #{id}"));
            return;
        }

        if (context.ActiveUses.Count >= MaxUseDepth)
        {
            unsupported.Add(new UnsupportedFeature("use", $"nesting deeper than {MaxUseDepth} is treated as runaway"));
            return;
        }

        // A dead reference draws nothing, per the spec, and is not a profile failure.
        if (!dom.ById.TryGetValue(id, out SvgElement? target)) return;

        double x = LengthParser.Parse(element.Attributes.GetValueOrDefault("x") ?? "0") ?? 0;
        double y = LengthParser.Parse(element.Attributes.GetValueOrDefault("y") ?? "0") ?? 0;
        Matrix placed = x != 0 || y != 0
            ? Matrix.Multiply(context.Transform, Matrix.Translation(x, y))
            : context.Transform;

        // width/height only establish a viewport for an <svg> or <symbol> target
        // that declares a viewBox; ignoring them silently there would scale wrongly.
        if (target.Name is "symbol" or "svg" && target.Attributes.ContainsKey("viewBox")
            && (element.Attributes.ContainsKey("width") || element.Attributes.ContainsKey("height")))
        {
            unsupported.Add(new UnsupportedFeature("use",
                "width/height on a use referencing a viewBox-bearing symbol or svg establishes a viewport, which is not supported"));
        }

        var activeUses = new List<string>(context.ActiveUses) { id };
        var nested = new List<IrNode>();

        if (target.Name is "symbol" or "svg")
        {
            // A referenced symbol renders as a group: its non-rendering nature
            // applies only where it is defined.
            ComputedStyle symbolStyle = Cascade(context.Style, target.Attributes);
            foreach (SvgElement child in target.Children)
            {
                Visit(child, context with
                {
                    Style = symbolStyle, Transform = placed, ActiveUses = activeUses, Lineage = [.. context.Lineage, target],
                }, dom, nested, unsupported);
            }
        }
        else
        {
            Visit(target, context with { Transform = placed, ActiveUses = activeUses }, dom, nested, unsupported);
        }

        if (nested.Count == 0) return;
        output.Add(nested.Count == 1 ? nested[0] : new IrGroup(nested, 1.0));
    }

    /// <summary>
    /// Turns a resolved paint reference into a sink paint, folding in the relevant
    /// opacity. Returns null when nothing should be painted, including a fully
    /// transparent result - emitting it costs output for no visible effect.
    /// </summary>
    private static Paint? ResolvePaint(
        PaintRef reference, double opacity, Box? bounds, ComputedStyle style, SvgDom dom,
        List<UnsupportedFeature> unsupported)
    {
        if (reference.Kind == PaintRefKind.None) return null;

        if (reference.Kind == PaintRefKind.Reference)
        {
            GradientResult result = Gradients.Resolve(reference.ReferenceId!, dom,
                new GradientContext(bounds, style.CurrentColor, opacity));
            unsupported.AddRange(result.Unsupported);
            return result.Paint;
        }

        if (reference.Kind != PaintRefKind.Color) return null;
        double alpha = reference.Color.A * opacity;
        if (alpha <= 0.0) return null;
        return new SolidPaint(new SinkColor(reference.Color.R, reference.Color.G, reference.Color.B, (float)alpha));
    }

    /// <summary>A <c>text</c> element, as glyph outlines.</summary>
    /// <remarks>
    /// The layout returns one run per stretch of text sharing a style; each becomes a
    /// path exactly as a shape would, so paints, stroking, clipping and opacity need
    /// nothing text-specific downstream. Paints in objectBoundingBox units use the whole
    /// text element's box, as the spec defines it for text.
    /// </remarks>
    private static void VisitText(
        SvgElement element, ComputedStyle style, Matrix transform, IFontProvider fonts, SvgDom dom,
        List<IrNode> output, List<UnsupportedFeature> unsupported)
    {
        List<TextRun> runs = TextLayout.Layout(element, style, fonts, Cascade, unsupported);
        if (runs.Count == 0) return;

        Box? bounds = BoundingBox.Of(runs.SelectMany(run => run.Segments).ToList());
        var nodes = new List<IrNode>();
        foreach (TextRun run in runs)
        {
            if (!run.Style.Visibility) continue;
            Paint? fillPaint = ResolvePaint(run.Style.Fill, run.Style.FillOpacity, bounds, run.Style, dom, unsupported);
            Paint? strokePaint = ResolvePaint(run.Style.Stroke, run.Style.StrokeOpacity, bounds, run.Style, dom, unsupported);
            if (fillPaint is null && strokePaint is null) continue;

            // Glyph outlines are designed for nonzero winding. fill-rule applies to text
            // in principle, but evenodd on a glyph whose contours overlap punches holes
            // the type designer never drew, and browsers do not do it either.
            var path = new IrPath(run.Segments, transform, run.Style with { FillRule = FillRule.NonZero },
                fillPaint, strokePaint, run.Source);
            // A tspan's own opacity composites its run; the text element's is applied to
            // the whole below, and must not be applied twice. Identity, not equality:
            // ComputedStyle is a record.
            nodes.Add(!ReferenceEquals(run.Style, style) && run.Style.Opacity < 1.0
                ? new IrGroup([path], run.Style.Opacity)
                : path);
        }
        if (nodes.Count == 0) return;

        IrNode node = style.Opacity < 1.0
            ? new IrGroup(nodes, style.Opacity)
            : nodes.Count == 1 ? nodes[0] : new IrGroup(nodes, 1.0);
        output.Add(Clipped(node, element, transform, bounds, dom, unsupported));
    }

    /// <summary>
    /// Wraps a node in its element's clip regions, if it has any.
    /// </summary>
    /// <remarks>
    /// <paramref name="bounds"/> is the node's own bounding box, needed only for
    /// <c>clipPathUnits="objectBoundingBox"</c>. A group has no single box of its
    /// own, so that combination is reported rather than guessed at.
    /// </remarks>
    private static IrNode Clipped(
        IrNode node, SvgElement element, Matrix transform, Box? bounds, SvgDom dom,
        List<UnsupportedFeature> unsupported)
    {
        if (EffectValue(element, "clip-path") is not { } value) return node;

        List<IrClip> clips = Clipping.Resolve(value, dom, bounds, transform, unsupported);
        return clips.Count == 0 ? node : new IrClipGroup(clips, [node]);
    }

    /// <summary>
    /// An effect attribute's value, from either the attribute or the inline style,
    /// or null when absent or explicitly <c>none</c>.
    /// </summary>
    private static string? EffectValue(SvgElement element, string name)
    {
        if (element.Attributes.TryGetValue(name, out string? direct)
            && !string.Equals(direct.Trim(), "none", StringComparison.OrdinalIgnoreCase))
        {
            return direct;
        }

        if (element.Attributes.TryGetValue("style", out string? style))
        {
            foreach (string declaration in style.Split(';'))
            {
                int colon = declaration.IndexOf(':', StringComparison.Ordinal);
                if (colon == -1) continue;
                if (!string.Equals(declaration[..colon].Trim(), name, StringComparison.OrdinalIgnoreCase)) continue;
                string value = declaration[(colon + 1)..].Trim();
                if (value.Length > 0 && !string.Equals(value, "none", StringComparison.OrdinalIgnoreCase)) return value;
            }
        }

        return null;
    }

    private static string? FirstText(SvgElement root, string name)
    {
        foreach (SvgElement child in root.Children)
        {
            if (!string.Equals(child.Name, name, StringComparison.Ordinal)) continue;
            string text = child.Text.Trim();
            return text.Length == 0 ? null : text;
        }

        return null;
    }

    private static double? ReadLength(IReadOnlyDictionary<string, string> attributes, string key, double? basis)
    {
        if (!attributes.TryGetValue(key, out string? value)) return null;
        return LengthParser.Parse(value, new LengthContext(basis ?? 0.0, 16.0));
    }

    private static List<PathSegment> ShapeSegments(string name, IReadOnlyDictionary<string, string> attributes)
    {
        double Number(string key, double fallback = 0.0)
        {
            if (!attributes.TryGetValue(key, out string? raw)) return fallback;
            return LengthParser.Parse(raw) ?? fallback;
        }

        double? Optional(string key) =>
            attributes.TryGetValue(key, out string? raw) ? LengthParser.Parse(raw) : null;

        switch (name)
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
                return Shapes.Poly(attributes.GetValueOrDefault("points") ?? string.Empty, false);
            case "polygon":
                return Shapes.Poly(attributes.GetValueOrDefault("points") ?? string.Empty, true);
            case "path":
                return attributes.TryGetValue("d", out string? d) ? PathDataParser.Parse(d) : [];
            default:
                return [];
        }
    }

    /// <summary>
    /// Computes a node's style from its parent's plus its own attributes.
    /// </summary>
    /// <remarks>
    /// Order follows CSS: presentation attributes, then the inline <c>style</c>
    /// attribute, which wins. <c>opacity</c> does not inherit, because it
    /// composites a subtree rather than describing one.
    /// </remarks>
    private static ComputedStyle Cascade(ComputedStyle parent, IReadOnlyDictionary<string, string> attributes)
    {
        var declarations = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach ((string key, string value) in attributes)
        {
            if (PresentationAttributes.Contains(key)) declarations[key] = value;
        }

        if (attributes.TryGetValue("style", out string? inline))
        {
            foreach (string declaration in inline.Split(';'))
            {
                int colon = declaration.IndexOf(':', StringComparison.Ordinal);
                if (colon == -1) continue;
                string property = declaration[..colon].Trim().ToLowerInvariant();
                string value = declaration[(colon + 1)..].Trim();
                if (value.Length > 0 && PresentationAttributes.Contains(property)) declarations[property] = value;
            }
        }

        string? Get(string key) => declarations.GetValueOrDefault(key);

        PaintRef currentColor = ResolvePaintRef(Get("color"), parent.CurrentColor, parent.CurrentColor) ?? parent.CurrentColor;

        return new ComputedStyle
        {
            Opacity = Number(Get("opacity"), 1.0, clampToUnit: true),
            Fill = ResolvePaintRef(Get("fill"), parent.Fill, currentColor) ?? parent.Fill,
            Stroke = ResolvePaintRef(Get("stroke"), parent.Stroke, currentColor) ?? parent.Stroke,
            StrokeWidth = Number(Get("stroke-width"), parent.StrokeWidth, clampToUnit: false),
            FillRule = Get("fill-rule")?.Trim().ToLowerInvariant() switch
            {
                "evenodd" => FillRule.EvenOdd,
                "nonzero" => FillRule.NonZero,
                _ => parent.FillRule,
            },
            FillOpacity = Number(Get("fill-opacity"), parent.FillOpacity, clampToUnit: true),
            StrokeOpacity = Number(Get("stroke-opacity"), parent.StrokeOpacity, clampToUnit: true),
            LineCap = Get("stroke-linecap")?.Trim().ToLowerInvariant() switch
            {
                "round" => LineCap.Round,
                "square" => LineCap.Square,
                "butt" => LineCap.Butt,
                _ => parent.LineCap,
            },
            LineJoin = Get("stroke-linejoin")?.Trim().ToLowerInvariant() switch
            {
                "round" => LineJoin.Round,
                "bevel" => LineJoin.Bevel,
                "miter" => LineJoin.Miter,
                _ => parent.LineJoin,
            },
            MiterLimit = OptionalNumber(Get("stroke-miterlimit")) ?? parent.MiterLimit,
            DashPattern = DashPattern(Get("stroke-dasharray")) ?? parent.DashPattern,
            DashOffset = OptionalNumber(Get("stroke-dashoffset")) ?? parent.DashOffset,
            CurrentColor = currentColor,
            Display = string.Equals(Get("display"), "none", StringComparison.Ordinal) ? false : parent.Display,
            Visibility = Visibility(Get("visibility"), parent.Visibility),
        }.WithTextProperties(parent, Get, attributes.GetValueOrDefault("space"));
    }

    /// <summary>
    /// Text properties the layout does not honour, with the values that need nothing
    /// from it. Anything else is carried as an issue and reported where text is drawn.
    /// </summary>
    private static readonly (string Property, string[] Fine)[] TextIssueProperties =
    [
        ("dominant-baseline", ["auto", "alphabetic"]),
        ("alignment-baseline", ["auto", "baseline", "alphabetic"]),
        ("baseline-shift", ["baseline", "0", "0px"]),
        ("text-decoration", ["none"]),
        ("writing-mode", ["lr", "lr-tb", "horizontal-tb"]),
        ("direction", ["ltr"]),
        ("unicode-bidi", ["normal"]),
        ("font-variant", ["normal"]),
        ("font-stretch", ["normal", "100%"]),
    ];

    private static readonly Dictionary<string, double> FontSizeKeywords = new(StringComparer.Ordinal)
    {
        ["xx-small"] = 9,
        ["x-small"] = 10,
        ["small"] = 13,
        ["medium"] = 16,
        ["large"] = 18,
        ["x-large"] = 24,
        ["xx-large"] = 32,
        ["xxx-large"] = 48,
    };

    /// <summary>The font and text properties of the cascade, resolved against the parent's.</summary>
    private static ComputedStyle WithTextProperties(
        this ComputedStyle style, ComputedStyle parent, Func<string, string?> get, string? xmlSpace)
    {
        double fontSize = FontSizeValue(get("font-size"), parent.FontSize);
        string? family = get("font-family");
        string? fontStyle = get("font-style")?.Trim().ToLowerInvariant();

        var issues = new List<string>(parent.TextIssues);
        foreach ((string property, string[] fine) in TextIssueProperties)
        {
            string? value = get(property)?.Trim().ToLowerInvariant();
            if (value is null || string.Equals(value, "inherit", StringComparison.Ordinal)) continue;
            int at = issues.IndexOf(property);
            if (fine.Contains(value))
            {
                if (at != -1) issues.RemoveAt(at);
            }
            else if (at == -1)
            {
                issues.Add(property);
            }
        }

        return style with
        {
            FontFamily = family is null || family.Trim().Length == 0 || string.Equals(family.Trim(), "inherit", StringComparison.Ordinal) ? parent.FontFamily : family,
            FontSize = fontSize,
            FontWeight = FontWeightValue(get("font-weight"), parent.FontWeight),
            FontItalic = fontStyle is null || string.Equals(fontStyle, "inherit", StringComparison.Ordinal)
                ? parent.FontItalic
                : fontStyle.StartsWith("italic", StringComparison.Ordinal) || fontStyle.StartsWith("oblique", StringComparison.Ordinal),
            TextAnchor = get("text-anchor")?.Trim().ToLowerInvariant() switch
            {
                "start" => TextAnchor.Start,
                "middle" => TextAnchor.Middle,
                "end" => TextAnchor.End,
                _ => parent.TextAnchor,
            },
            LetterSpacing = SpacingValue(get("letter-spacing"), parent.LetterSpacing, fontSize),
            WordSpacing = SpacingValue(get("word-spacing"), parent.WordSpacing, fontSize),
            PreserveSpace = xmlSpace is null ? parent.PreserveSpace : string.Equals(xmlSpace.Trim(), "preserve", StringComparison.Ordinal),
            TextIssues = issues,
        };
    }

    private static double FontSizeValue(string? value, double parentSize)
    {
        if (value is null) return parentSize;
        string text = value.Trim().ToLowerInvariant();
        if (text.Length == 0 || string.Equals(text, "inherit", StringComparison.Ordinal)) return parentSize;
        if (FontSizeKeywords.TryGetValue(text, out double keyword)) return keyword;
        // CSS's ratio between adjacent keyword sizes, applied to the parent.
        if (string.Equals(text, "larger", StringComparison.Ordinal)) return parentSize * 1.2;
        if (string.Equals(text, "smaller", StringComparison.Ordinal)) return parentSize / 1.2;
        double? parsed = LengthParser.Parse(text, new LengthContext(parentSize, parentSize));
        return parsed is not { } size || size < 0 ? parentSize : size;
    }

    /// <summary><c>bolder</c> and <c>lighter</c> follow the CSS Fonts Level 4 table rather than stepping by 100.</summary>
    private static double FontWeightValue(string? value, double parentWeight)
    {
        if (value is null) return parentWeight;
        string text = value.Trim().ToLowerInvariant();
        switch (text)
        {
            case "normal": return 400;
            case "bold": return 700;
            case "bolder": return parentWeight < 350 ? 400 : parentWeight < 550 ? 700 : parentWeight < 900 ? 900 : parentWeight;
            case "lighter": return parentWeight < 100 ? parentWeight : parentWeight < 550 ? 100 : parentWeight < 750 ? 400 : 700;
        }
        // A plain number only, matching the TypeScript engine's pattern exactly.
        if (!PlainNumber().IsMatch(text)) return parentWeight;
        double parsed = double.Parse(text, CultureInfo.InvariantCulture);
        return parsed is >= 1 and <= 1000 ? parsed : parentWeight;
    }

    private static double SpacingValue(string? value, double parentValue, double fontSize)
    {
        if (value is null) return parentValue;
        string text = value.Trim().ToLowerInvariant();
        if (string.Equals(text, "inherit", StringComparison.Ordinal)) return parentValue;
        if (string.Equals(text, "normal", StringComparison.Ordinal)) return 0;
        return LengthParser.Parse(text, new LengthContext(fontSize, fontSize)) ?? parentValue;
    }

    [GeneratedRegex(@"^[0-9]+(\.[0-9]+)?$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PlainNumber();

    private static PaintRef? ResolvePaintRef(string? value, PaintRef inherited, PaintRef currentColor)
    {
        if (value is null) return null;
        if (string.Equals(value.Trim(), "inherit", StringComparison.OrdinalIgnoreCase)) return inherited;

        PaintRef? parsed = ColorParser.Parse(value);
        if (parsed is not { } paint) return null;
        return paint.Kind == PaintRefKind.CurrentColor ? currentColor : paint;
    }

    private static double Number(string? value, double fallback, bool clampToUnit)
    {
        if (value is null) return fallback;
        if (string.Equals(value.Trim(), "inherit", StringComparison.OrdinalIgnoreCase)) return fallback;
        if (LengthParser.Parse(value) is not { } parsed) return fallback;
        if (!clampToUnit) return parsed;
        return parsed < 0.0 ? 0.0 : parsed > 1.0 ? 1.0 : parsed;
    }

    private static double? OptionalNumber(string? value) => value is null ? null : LengthParser.Parse(value);

    private static bool Visibility(string? value, bool fallback) => value?.Trim().ToLowerInvariant() switch
    {
        "hidden" or "collapse" => false,
        "visible" => true,
        _ => fallback,
    };

    /// <summary><c>none</c> and an all-zero list both mean a solid stroke.</summary>
    private static double[]? DashPattern(string? value)
    {
        if (value is null) return null;
        string lower = value.Trim().ToLowerInvariant();
        if (lower is "none" or "") return null;

        List<double> values = NumberScanner.ParseList(value).Select(entry => Math.Max(0.0, entry)).ToList();
        if (values.Count == 0 || values.TrueForAll(entry => entry == 0.0)) return null;

        // An odd-length list repeats to become even, per the spec.
        if (values.Count % 2 == 1) values.AddRange(values.ToArray());
        return values.ToArray();
    }
}
