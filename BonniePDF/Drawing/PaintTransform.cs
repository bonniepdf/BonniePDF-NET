using System;
using System.Runtime.InteropServices;

namespace BonniePDF.Drawing;

/// <summary>
/// An affine transform describing a paint's own coordinate space.
/// </summary>
/// <remarks>
/// <para>
/// Component order matches <see cref="ICanvas.Transform"/>, PDF's <c>cm</c>
/// operator and SVG's <c>matrix()</c>, so the same six numbers pass through all
/// three without reordering:
/// </para>
/// <code>
/// | A  C  E |
/// | B  D  F |
/// | 0  0  1 |
/// </code>
/// <para>
/// A gradient's geometry is expressed in its own space, and this maps that space
/// onto the canvas space where the paint is used - the direction SVG's
/// <c>gradientTransform</c> and PDF's pattern <c>/Matrix</c> both use.
/// </para>
/// </remarks>
/// <param name="A">Horizontal scale / rotation component.</param>
/// <param name="B">Vertical shear / rotation component.</param>
/// <param name="C">Horizontal shear / rotation component.</param>
/// <param name="D">Vertical scale / rotation component.</param>
/// <param name="E">Horizontal translation.</param>
/// <param name="F">Vertical translation.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct PaintTransform(float A, float B, float C, float D, float E, float F)
{
    /// <summary>The transform that changes nothing.</summary>
    public static readonly PaintTransform Identity = new(1, 0, 0, 1, 0, 0);

    /// <summary>True when this transform has no effect.</summary>
    public bool IsIdentity => A == 1f && B == 0f && C == 0f && D == 1f && E == 0f && F == 0f;

    /// <summary>
    /// The determinant. Zero means the transform collapses paint space, so the
    /// paint draws nothing.
    /// </summary>
    public float Determinant => A * D - B * C;

    /// <summary>
    /// The inverse, or null when the transform is not invertible.
    /// </summary>
    /// <remarks>
    /// Needed to map a painted region back into paint space: a translucent
    /// gradient's soft-mask form has its <c>/BBox</c> in paint space once a
    /// transform exists.
    /// </remarks>
    public PaintTransform? Invert()
    {
        float determinant = Determinant;
        if (determinant == 0f || !float.IsFinite(determinant)) return null;

        return new PaintTransform(
            D / determinant,
            -B / determinant,
            -C / determinant,
            A / determinant,
            (C * F - D * E) / determinant,
            (B * E - A * F) / determinant);
    }

    /// <summary>Applies this transform to a point.</summary>
    public (float X, float Y) Apply(float x, float y) => (A * x + C * y + E, B * x + D * y + F);

    /// <summary>
    /// The axis-aligned box containing the four mapped corners of a box.
    /// </summary>
    /// <remarks>
    /// A rotated or sheared transform turns a rectangle into a parallelogram, so
    /// the containing box is what a <c>/BBox</c> needs.
    /// </remarks>
    public (float X0, float Y0, float X1, float Y1) MapBox(float x0, float y0, float x1, float y1)
    {
        (float X, float Y) a = Apply(x0, y0);
        (float X, float Y) b = Apply(x1, y0);
        (float X, float Y) c = Apply(x1, y1);
        (float X, float Y) d = Apply(x0, y1);

        float minX = Math.Min(Math.Min(a.X, b.X), Math.Min(c.X, d.X));
        float minY = Math.Min(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y));
        float maxX = Math.Max(Math.Max(a.X, b.X), Math.Max(c.X, d.X));
        float maxY = Math.Max(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y));
        return (minX, minY, maxX, maxY);
    }
}

/// <summary>Helpers for an optional paint transform.</summary>
public static class PaintTransformExtensions
{
    /// <summary>True when the transform is absent or has no effect.</summary>
    public static bool IsIdentityOrAbsent(this PaintTransform? transform) =>
        transform is not { } value || value.IsIdentity;
}
