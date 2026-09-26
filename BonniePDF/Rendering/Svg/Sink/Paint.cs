// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System.Runtime.InteropServices;

namespace BonniePDF.Rendering.Svg.Sink;

/// <summary>One stop in a gradient ramp.</summary>
/// <param name="Offset">Position along the ramp, 0..1.</param>
/// <param name="Color">The colour at that position.</param>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct GradientStop(float Offset, SinkColor Color);

/// <summary>
/// An affine transform describing a paint's own coordinate space.
/// </summary>
/// <remarks>
/// Copied from BonniePDF's <c>PaintTransform</c>, in the component order
/// <c>ICanvas.Transform</c>, PDF's <c>cm</c> and SVG's <c>matrix()</c> all
/// share. Only the six components are carried here: inverting one and mapping
/// a box through it are the consuming canvas's concerns, not the IR's.
/// </remarks>
/// <param name="A">Horizontal scale / rotation component.</param>
/// <param name="B">Vertical shear / rotation component.</param>
/// <param name="C">Horizontal shear / rotation component.</param>
/// <param name="D">Vertical scale / rotation component.</param>
/// <param name="E">Horizontal translation.</param>
/// <param name="F">Vertical translation.</param>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct PaintTransform(float A, float B, float C, float D, float E, float F);

/// <summary>
/// A paint source a sink can fill or stroke with.
/// </summary>
/// <remarks>
/// Mirrors the shape of BonniePDF's shipped <c>Paint</c> hierarchy
/// (<c>BonniePDF/Drawing/Paint.cs</c>) so the adapter is a field-for-field
/// map. Keep the three cases and their members in step with that file; see
/// AGENTS.md for why drift here is expensive.
/// </remarks>
internal abstract class Paint;

/// <summary>Paints an area with a single colour.</summary>
internal sealed class SolidPaint(SinkColor color) : Paint
{
    /// <summary>Gets the fill colour.</summary>
    public SinkColor Color { get; } = color;
}

/// <summary>Paints an area with colours interpolated along a straight line.</summary>
/// <remarks>
/// <see cref="Start"/> and <see cref="End"/> are in the paint's own space;
/// <see cref="Transform"/> maps that space onto the sink's. Mirrors BonniePDF's
/// shipped <c>LinearGradientPaint</c>, which gained the same member so that a
/// sheared ramp is expressible at all.
/// </remarks>
internal sealed class LinearGradientPaint(SinkPoint start, SinkPoint end, params GradientStop[] stops) : Paint
{
    /// <summary>Gets the ramp's start point.</summary>
    public SinkPoint Start { get; } = start;

    /// <summary>Gets the ramp's end point.</summary>
    public SinkPoint End { get; } = end;

    /// <summary>Gets the ramp's stops, in ascending offset order.</summary>
    public GradientStop[] Stops { get; } = stops;

    /// <summary>Gets the map from the ramp's own space onto the sink's.</summary>
    public PaintTransform? Transform { get; init; }
}

/// <summary>Paints an area with colours interpolated outward from a centre point.</summary>
/// <remarks>
/// A non-uniform <see cref="Transform"/> is what makes an elliptical gradient
/// expressible - the paint itself carries a single radius.
/// </remarks>
internal sealed class RadialGradientPaint(SinkPoint center, float radius, GradientStop[] stops, SinkPoint? focal = null) : Paint
{
    /// <summary>Gets the ramp's centre.</summary>
    public SinkPoint Center { get; } = center;

    /// <summary>Gets the ramp's outer radius.</summary>
    public float Radius { get; } = radius;

    /// <summary>Gets the ramp's stops, in ascending offset order.</summary>
    public GradientStop[] Stops { get; } = stops;

    /// <summary>Gets the focal point, when it differs from <see cref="Center"/>.</summary>
    public SinkPoint? Focal { get; } = focal;

    /// <summary>Gets the map from the ramp's own space onto the sink's.</summary>
    public PaintTransform? Transform { get; init; }
}
