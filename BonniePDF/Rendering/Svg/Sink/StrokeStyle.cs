// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
namespace BonniePDF.Rendering.Svg.Sink;

/// <summary>Path fill rule.</summary>
/// <remarks>
/// A real enum here, a string-literal union on the TypeScript side — same
/// values, idiomatic per language, matching how BonniePDF itself spells them.
/// That divergence is intentional.
/// </remarks>
internal enum FillRule
{
    /// <summary>Non-zero winding number rule.</summary>
    NonZero,

    /// <summary>Even-odd rule.</summary>
    EvenOdd
}

/// <summary>Line cap style for stroke endpoints.</summary>
internal enum LineCap
{
    /// <summary>Butt cap (square end at the endpoint).</summary>
    Butt,

    /// <summary>Round cap (semicircle at the endpoint).</summary>
    Round,

    /// <summary>Square cap (extends half the line width beyond the endpoint).</summary>
    Square
}

/// <summary>Line join style for stroke corners.</summary>
internal enum LineJoin
{
    /// <summary>Miter join (sharp corner, clipped at the miter limit).</summary>
    Miter,

    /// <summary>Round join (circular arc at the corner).</summary>
    Round,

    /// <summary>Bevel join (straight line across the corner).</summary>
    Bevel
}

/// <summary>
/// Stroke parameters. The paint travels inside the style, matching the shipped
/// <c>ICanvas.StrokeCurrentPath(StrokeStyle)</c> signature.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MiterLimit"/> is nullable with no default applied here. The two
/// BonniePDF implementations do not currently agree on the default (see
/// AGENTS.md, "Known upstream discrepancy"), so this package declines to pick
/// one and passes through only what the SVG actually specified. An SVG that
/// omits <c>stroke-miterlimit</c> leaves this null — not 4 and not 10.
/// </para>
/// </remarks>
internal sealed class StrokeStyle
{
    /// <summary>Gets or initializes the paint used for the stroke.</summary>
    public required Paint Paint { get; init; }

    /// <summary>Gets or initializes the stroke width in sink units.</summary>
    public required float Width { get; init; }

    /// <summary>Gets or initializes the dash pattern (alternating on/off lengths).</summary>
    public float[]? DashPattern { get; init; }

    /// <summary>Gets or initializes the dash phase offset.</summary>
    public float? DashOffset { get; init; }

    /// <summary>Gets or initializes the line cap style.</summary>
    public LineCap? LineCap { get; init; }

    /// <summary>Gets or initializes the line join style.</summary>
    public LineJoin? LineJoin { get; init; }

    /// <summary>Gets or initializes the miter limit. See the type remarks.</summary>
    public float? MiterLimit { get; init; }
}
