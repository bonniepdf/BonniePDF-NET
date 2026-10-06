using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BonniePDF.Drawing;

/// <summary>A gradient color stop at a normalized offset (0-1).</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct GradientStop(float Offset, Color Color);

/// <summary>Base type for paint values (solid color, linear/radial gradient).</summary>
public abstract class Paint
{
    /// <summary>Implicitly converts a Color to a SolidPaint.</summary>
    public static implicit operator Paint(Color color) => new SolidPaint(color);
}

/// <summary>A single solid color.</summary>
public sealed class SolidPaint(Color color) : Paint
{
    /// <summary>Gets the solid color.</summary>
    public Color Color { get; } = color;
}

/// <summary>A linear gradient between two points.</summary>
/// <remarks>
/// <see cref="Start"/> and <see cref="End"/> are in the paint's own coordinate
/// space; <see cref="Transform"/>, when set, maps that space onto the canvas
/// space where the paint is used. Leaving it null is identity and produces
/// byte-identical output to a paint without one.
/// </remarks>
public sealed class LinearGradientPaint(Point start, Point end, params GradientStop[] stops) : Paint
{
    /// <summary>
    /// Gets or initializes the transform mapping this paint's own coordinate
    /// space onto the canvas space where it is used. Null means identity.
    /// </summary>
    public PaintTransform? Transform { get; init; }

    /// <summary>Gets the start point of the gradient.</summary>
    public Point Start { get; } = start;

    /// <summary>Gets the end point of the gradient.</summary>
    public Point End { get; } = end;

    /// <summary>Gets the gradient color stops.</summary>
    public IReadOnlyList<GradientStop> Stops { get; } = stops;
}

/// <summary>A radial gradient from a center point.</summary>
/// <remarks>
/// <see cref="Center"/>, <see cref="Radius"/> and <see cref="Focal"/> are in the
/// paint's own coordinate space; <see cref="Transform"/>, when set, maps that
/// space onto the canvas space where the paint is used. A non-uniform transform
/// is what makes an elliptical gradient expressible - the paint itself carries a
/// single radius.
/// </remarks>
public sealed class RadialGradientPaint(Point center, float radius, GradientStop[] stops, Point? focal = null) : Paint
{
    /// <summary>
    /// Gets or initializes the transform mapping this paint's own coordinate
    /// space onto the canvas space where it is used. Null means identity.
    /// </summary>
    public PaintTransform? Transform { get; init; }

    /// <summary>Gets the center point of the gradient.</summary>
    public Point Center { get; } = center;

    /// <summary>Gets the radius of the gradient.</summary>
    public float Radius { get; } = radius;

    /// <summary>Gets the gradient color stops.</summary>
    public IReadOnlyList<GradientStop> Stops { get; } = stops;

    /// <summary>Gets the optional focal point of the gradient.</summary>
    public Point? Focal { get; } = focal;
}
