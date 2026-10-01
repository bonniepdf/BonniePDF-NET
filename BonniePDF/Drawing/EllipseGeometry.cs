using System.Runtime.InteropServices;

namespace BonniePDF.Drawing;

/// <summary>
/// Shared geometry for the elliptical path primitives.
///
/// PDF has no arc operator, so an ellipse is four cubic Beziers. The control-point
/// offset is the standard circular approximation constant, kappa = 4/3*(sqrt(2) - 1);
/// the maximum radial error is under 0.02% of the radius, well below what any
/// rasteriser resolves.
///
/// Every canvas builds its ellipses from this one type so <see cref="PdfCanvas"/>
/// and <see cref="SvgCanvas"/> emit the same curve for the same input, and so the
/// TypeScript implementation has a single set of numbers to mirror.
/// </summary>
internal static class EllipseGeometry
{
    /// <summary>4/3*(sqrt(2) - 1) - the circular Bezier approximation constant.</summary>
    public const float Kappa = 0.5522847498f;

    /// <summary>One cubic segment: two control points and an end point.</summary>
    [StructLayout(LayoutKind.Auto)]
    internal readonly record struct EllipseSegment(
        float C1X, float C1Y, float C2X, float C2Y, float X, float Y);

    /// <summary>
    /// Decomposes an axis-aligned ellipse into a start point and four cubic
    /// segments, walking from the rightmost point in the direction of increasing y.
    /// </summary>
    public static (float StartX, float StartY, EllipseSegment[] Segments) ToBeziers(
        float cx, float cy, float rx, float ry)
    {
        float kx = rx * Kappa;
        float ky = ry * Kappa;
        return (cx + rx, cy,
        [
            new(cx + rx, cy + ky, cx + kx, cy + ry, cx, cy + ry),
            new(cx - kx, cy + ry, cx - rx, cy + ky, cx - rx, cy),
            new(cx - rx, cy - ky, cx - kx, cy - ry, cx, cy - ry),
            new(cx + kx, cy - ry, cx + rx, cy - ky, cx + rx, cy),
        ]);
    }
}
