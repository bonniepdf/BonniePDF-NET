// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System.Collections.Generic;
using BonniePDF.Rendering.Svg.Geometry;
using BonniePDF.Rendering.Svg.Parsers;
using BonniePDF.Rendering.Svg.Sink;

namespace BonniePDF.Rendering.Svg.Ir;

/// <summary>
/// The resolved style of one node.
/// </summary>
/// <remarks>
/// Numeric members are <c>double</c>, not <c>float</c>, even though
/// <see cref="Sink.IVectorSink"/> takes <c>float</c>. The IR computes in double
/// and narrows only at the sink boundary, so that every arithmetic step and
/// every discrete decision matches the TypeScript implementation exactly.
/// Carrying float here cost byte parity: accumulated float drift through
/// relative path commands flipped an arc's segment count, which is a different
/// number of PDF operators for the same input rather than a rounding
/// difference. See AGENTS.md.
/// </remarks>
/// <remarks>
/// Every property is already resolved - inherited, defaulted, lengths in user
/// units. Nothing downstream re-reads attributes, which keeps the emitter free
/// of SVG concepts.
/// </remarks>
internal sealed record ComputedStyle
{
    /// <summary>The initial values, before any document styling.</summary>
    public static readonly ComputedStyle Initial = new()
    {
        Fill = PaintRef.FromColor(new SinkColor(0, 0, 0, 1)),
        Stroke = PaintRef.None,
        CurrentColor = PaintRef.FromColor(new SinkColor(0, 0, 0, 1)),
    };

    /// <summary>Gets the fill paint.</summary>
    public PaintRef Fill { get; init; }

    /// <summary>Gets the stroke paint.</summary>
    public PaintRef Stroke { get; init; }

    /// <summary>Gets the stroke width in user units.</summary>
    public double StrokeWidth { get; init; } = 1.0;

    /// <summary>Gets the fill rule.</summary>
    public FillRule FillRule { get; init; } = FillRule.NonZero;

    /// <summary>Gets the fill opacity.</summary>
    public double FillOpacity { get; init; } = 1.0;

    /// <summary>Gets the stroke opacity.</summary>
    public double StrokeOpacity { get; init; } = 1.0;

    /// <summary>Gets the group alpha. Not inherited - it composites a subtree as a unit.</summary>
    public double Opacity { get; init; } = 1.0;

    /// <summary>Gets the line cap.</summary>
    public LineCap LineCap { get; init; } = LineCap.Butt;

    /// <summary>Gets the line join.</summary>
    public LineJoin LineJoin { get; init; } = LineJoin.Miter;

    /// <summary>
    /// Gets the miter limit, or null when the document did not specify one.
    /// </summary>
    /// <remarks>
    /// Deliberately not defaulted. SVG's default is 4 and PDF's is 10, the two
    /// BonniePDF implementations disagree about which to apply, and FOP emits 4
    /// for an SVG that specifies none. Carrying "unspecified" lets the sink decide.
    /// </remarks>
    public double? MiterLimit { get; init; }

    /// <summary>Gets the dash pattern, or null for a solid stroke.</summary>
    public double[]? DashPattern { get; init; }

    /// <summary>Gets the dash phase offset, or null when unspecified.</summary>
    public double? DashOffset { get; init; }

    /// <summary>Gets the resolved <c>currentColor</c> target.</summary>
    public PaintRef CurrentColor { get; init; }

    /// <summary>Gets whether this subtree renders at all.</summary>
    public bool Display { get; init; } = true;

    /// <summary>Gets whether this element is visible.</summary>
    public bool Visibility { get; init; } = true;

    // CSS initial values: the font is UA-defined, and browsers default to a serif.

    /// <summary>Gets the <c>font-family</c> list as written; the text layout walks it in order.</summary>
    public string FontFamily { get; init; } = "serif";

    /// <summary>Gets the font size in user units, with em, % and keywords resolved against the parent.</summary>
    public double FontSize { get; init; } = 16.0;

    /// <summary>Gets the font weight, 1-1000, with bold, bolder and lighter resolved.</summary>
    public double FontWeight { get; init; } = 400.0;

    /// <summary>Gets whether the style is italic or oblique, which the font seam does not distinguish.</summary>
    public bool FontItalic { get; init; }

    /// <summary>Gets the text anchor.</summary>
    public TextAnchor TextAnchor { get; init; } = TextAnchor.Start;

    /// <summary>Gets the letter spacing in user units; normal is 0.</summary>
    public double LetterSpacing { get; init; }

    /// <summary>Gets the word spacing in user units; normal is 0.</summary>
    public double WordSpacing { get; init; }

    /// <summary>Gets whether <c>xml:space="preserve"</c> is in force.</summary>
    public bool PreserveSpace { get; init; }

    /// <summary>
    /// Gets text properties set to something the layout cannot honour - a baseline
    /// shift, a decoration, a vertical writing mode.
    /// </summary>
    /// <remarks>
    /// Carried rather than reported at once, so they are reported only where text is
    /// actually drawn under them: a <c>&lt;g dominant-baseline="middle"&gt;</c>
    /// holding no text affects nothing.
    /// </remarks>
    public IReadOnlyList<string> TextIssues { get; init; } = [];
}

/// <summary>The <c>text-anchor</c> property.</summary>
internal enum TextAnchor
{
    /// <summary>The chunk starts at its position.</summary>
    Start,

    /// <summary>The chunk is centred on its position.</summary>
    Middle,

    /// <summary>The chunk ends at its position.</summary>
    End,
}

/// <summary>A node of the intermediate representation.</summary>
internal abstract record IrNode;

/// <summary>A drawable path with its resolved style and place in the world.</summary>
/// <remarks>
/// <see cref="FillPaint"/> and <see cref="StrokePaint"/> are fully resolved, with
/// every opacity already folded into their alpha, and are null when there is
/// nothing to paint. They are resolved by the builder rather than the emitter
/// because a gradient in <c>objectBoundingBox</c> units needs this path's
/// bounding box, which only the builder has - and because it keeps every SVG
/// concept out of the emitter.
/// </remarks>
/// <param name="Segments">The path, already de-sugared to primitives.</param>
/// <param name="Transform">The matrix mapping this path into its parent's space.</param>
/// <param name="Style">The resolved style.</param>
/// <param name="Source">The source element name, for diagnostics only.</param>
/// <param name="FillPaint">The resolved fill, with every opacity folded in, or null.</param>
/// <param name="StrokePaint">The resolved stroke paint, with every opacity folded in, or null.</param>
internal sealed record IrPath(
    IReadOnlyList<PathSegment> Segments,
    Matrix Transform,
    ComputedStyle Style,
    Paint? FillPaint,
    Paint? StrokePaint,
    string Source) : IrNode;

/// <summary>
/// A subtree composited as a unit.
/// </summary>
/// <remarks>
/// Group opacity is not the same as applying that opacity to each child:
/// overlapping children inside a half-transparent group must not show through
/// each other.
/// </remarks>
/// <param name="Children">The subtree.</param>
/// <param name="Opacity">The alpha to composite it with.</param>
internal sealed record IrGroup(IReadOnlyList<IrNode> Children, double Opacity) : IrNode;

/// <summary>
/// A subtree drawn through one or more clip regions.
/// </summary>
/// <remarks>
/// Separate from <see cref="IrGroup"/> because clipping and group compositing are
/// different operations that can both apply to the same element, and a sink applies
/// them differently: a clip narrows what subsequent painting affects, while group
/// alpha composites a finished subtree.
/// </remarks>
/// <param name="Clips">Outermost first, each intersected with the next.</param>
/// <param name="Children">The subtree drawn through them.</param>
internal sealed record IrClipGroup(IReadOnlyList<IrClip> Clips, IReadOnlyList<IrNode> Children) : IrNode;

/// <summary>An element the profile cannot draw, handed to the caller's rasterizer.</summary>
/// <remarks>
/// <see cref="Svg"/> is a self-contained document drawing just that element (see
/// <see cref="Raster"/>); the image covers the rectangle given, in root user space -
/// the document's viewport - and is drawn under the root transform only, because
/// the element's own placement is inside the snippet.
/// </remarks>
/// <param name="Svg">The snippet to rasterize.</param>
/// <param name="X">Left edge of the covered region.</param>
/// <param name="Y">Top edge of the covered region.</param>
/// <param name="Width">Width of the covered region.</param>
/// <param name="Height">Height of the covered region.</param>
/// <param name="Source">The source element name, for diagnostics only.</param>
internal sealed record IrRaster(string Svg, double X, double Y, double Width, double Height, string Source) : IrNode;

/// <summary>A feature encountered that the supported profile does not cover.</summary>
/// <param name="Feature">The element or attribute name.</param>
/// <param name="Detail">What it would need.</param>
public readonly record struct UnsupportedFeature(string Feature, string Detail);

/// <summary>A parsed SVG document, ready to emit.</summary>
/// <param name="RootTransform">The matrix mapping user space onto the target box.</param>
/// <param name="Children">The drawable content.</param>
/// <param name="Width">Intrinsic width in user units, when declared.</param>
/// <param name="Height">Intrinsic height in user units, when declared.</param>
/// <param name="Unsupported">Features outside the profile that were not rasterized.</param>
/// <param name="Rasterized">Features handed to the rasterizer instead: drawn, but as an image.</param>
/// <param name="Title">The <c>title</c> text, for accessibility fallback.</param>
/// <param name="Description">The <c>desc</c> text, for accessibility fallback.</param>
/// <param name="Fit">How user space maps onto a box, kept so <c>SvgImporter.Fit</c> can re-fit without re-parsing.</param>
internal sealed record IrDocument(
    Matrix RootTransform,
    IReadOnlyList<IrNode> Children,
    double? Width,
    double? Height,
    IReadOnlyList<UnsupportedFeature> Unsupported,
    IReadOnlyList<UnsupportedFeature> Rasterized,
    string? Title,
    string? Description,
    DocumentFit Fit);

/// <summary>What fitting a document onto a box depends on, and its intrinsic size.</summary>
/// <remarks>
/// Everything else in the IR is in user space, so a document parsed for one box
/// is re-fitted to another by replacing only the root transform. That is what
/// lets a host rasterize before it knows the final size.
/// </remarks>
/// <param name="UserBox">The user-space box fitted onto the target: the viewBox, else the declared size.</param>
/// <param name="Preserve">The root's <c>preserveAspectRatio</c>.</param>
/// <param name="IntrinsicWidth">
/// Intrinsic width in user units (CSS px): the root's <c>width</c> as a length,
/// unitless meaning px, with the viewBox standing in for one that is absent, a
/// percentage, or not positive. Null when neither gives one.
/// </param>
/// <param name="IntrinsicHeight">Intrinsic height, by the same rule.</param>
internal sealed record DocumentFit(ViewBox? UserBox, PreserveAspectRatio Preserve, double? IntrinsicWidth, double? IntrinsicHeight);
