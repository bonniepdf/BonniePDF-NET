// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Runtime.InteropServices;

namespace BonniePDF.Rendering.Svg.Geometry;

/// <summary>A 2D affine matrix, in the order PDF's <c>cm</c> and SVG's <c>matrix()</c> use.</summary>
/// <param name="A">Horizontal scale / rotation component.</param>
/// <param name="B">Vertical shear / rotation component.</param>
/// <param name="C">Horizontal shear / rotation component.</param>
/// <param name="D">Vertical scale / rotation component.</param>
/// <param name="E">Horizontal translation.</param>
/// <param name="F">Vertical translation.</param>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct Matrix(double A, double B, double C, double D, double E, double F)
{
    /// <summary>The identity matrix.</summary>
    public static readonly Matrix Identity = new(1, 0, 0, 1, 0, 0);

    /// <summary>True when this matrix has no effect.</summary>
    public bool IsIdentity => A == 1 && B == 0 && C == 0 && D == 1 && E == 0 && F == 0;

    /// <summary><paramref name="first"/> applied before <paramref name="second"/> - SVG's left-to-right order.</summary>
    public static Matrix Multiply(Matrix first, Matrix second) => new(
        first.A * second.A + first.C * second.B,
        first.B * second.A + first.D * second.B,
        first.A * second.C + first.C * second.D,
        first.B * second.C + first.D * second.D,
        first.A * second.E + first.C * second.F + first.E,
        first.B * second.E + first.D * second.F + first.F);

    /// <summary>A translation.</summary>
    public static Matrix Translation(double tx, double ty) => new(1, 0, 0, 1, tx, ty);

    /// <summary>A scale.</summary>
    public static Matrix Scaling(double sx, double sy) => new(sx, 0, 0, sy, 0, 0);

    /// <summary>A rotation, in degrees.</summary>
    public static Matrix Rotation(double degrees)
    {
        double radians = degrees * Math.PI / 180.0;
        return new(Math.Cos(radians), Math.Sin(radians), -Math.Sin(radians), Math.Cos(radians), 0, 0);
    }

    /// <summary>A skew along the x axis, in degrees.</summary>
    public static Matrix SkewX(double degrees) => new(1, 0, Math.Tan(degrees * Math.PI / 180.0), 1, 0, 0);

    /// <summary>A skew along the y axis, in degrees.</summary>
    public static Matrix SkewY(double degrees) => new(1, Math.Tan(degrees * Math.PI / 180.0), 0, 1, 0, 0);
}
