// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BonniePDF.Rendering.Svg.Parsers;

namespace BonniePDF.Rendering.Svg.Ir;

/// <summary>An axis-aligned box in user space.</summary>
/// <param name="X">Left edge.</param>
/// <param name="Y">Top edge.</param>
/// <param name="Width">Width; may be zero for a horizontal line.</param>
/// <param name="Height">Height; may be zero for a vertical line.</param>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct Box(double X, double Y, double Width, double Height);

/// <summary>Computes the tight bounding box of a path.</summary>
internal static class BoundingBox
{
    /// <summary>
    /// The tight bounding box of a path, in its own user space, or null when the
    /// path draws nothing.
    /// </summary>
    /// <remarks>
    /// <c>gradientUnits="objectBoundingBox"</c> - the default - places a gradient
    /// in fractions of this box, so a loose box puts the gradient in the wrong
    /// place. The control-point hull is the easy answer and is wrong: a curve is
    /// contained by its hull but rarely touches it. This solves each cubic's
    /// derivative for its extrema instead, which is what browsers do. Per the
    /// spec the box covers geometry only - stroke width does not extend it.
    /// </remarks>
    public static Box? Of(IReadOnlyList<PathSegment> segments)
    {
        double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;
        bool any = false;

        void Include(double x, double y)
        {
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }

        double currentX = 0, currentY = 0, startX = 0, startY = 0;

        foreach (PathSegment segment in segments)
        {
            switch (segment.Kind)
            {
                case PathSegmentKind.Move:
                    currentX = startX = segment.X;
                    currentY = startY = segment.Y;
                    Include(currentX, currentY);
                    any = true;
                    break;

                case PathSegmentKind.Line:
                    currentX = segment.X;
                    currentY = segment.Y;
                    Include(currentX, currentY);
                    any = true;
                    break;

                case PathSegmentKind.Cubic:
                {
                    Include(segment.X, segment.Y);
                    double fromX = currentX, fromY = currentY;
                    void Visit(double t) => Include(
                        CubicAt(fromX, segment.C1X, segment.C2X, segment.X, t),
                        CubicAt(fromY, segment.C1Y, segment.C2Y, segment.Y, t));

                    ForEachExtremum(fromX, segment.C1X, segment.C2X, segment.X, Visit);
                    ForEachExtremum(fromY, segment.C1Y, segment.C2Y, segment.Y, Visit);
                    currentX = segment.X;
                    currentY = segment.Y;
                    any = true;
                    break;
                }

                case PathSegmentKind.Close:
                    currentX = startX;
                    currentY = startY;
                    break;
            }
        }

        return any ? new Box(minX, minY, maxX - minX, maxY - minY) : null;
    }

    private static double CubicAt(double p0, double c1, double c2, double p1, double t)
    {
        double u = 1 - t;
        return u * u * u * p0 + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * p1;
    }

    /// <summary>
    /// Feeds each interior extremum of one coordinate of a cubic to <paramref name="visit"/>.
    /// </summary>
    /// <remarks>
    /// The derivative of a cubic is a quadratic; its roots in (0,1) are where that
    /// coordinate turns. Degenerate cases are handled rather than divided through,
    /// since a straight segment expressed as a cubic is common in real path data.
    /// </remarks>
    private static void ForEachExtremum(double p0, double c1, double c2, double p1, Action<double> visit)
    {
        double a = 3 * (-p0 + 3 * c1 - 3 * c2 + p1);
        double b = 6 * (p0 - 2 * c1 + c2);
        double c = 3 * (c1 - p0);

        void Consider(double t)
        {
            if (t > 0 && t < 1) visit(t);
        }

        if (Math.Abs(a) < 1e-12)
        {
            if (Math.Abs(b) > 1e-12) Consider(-c / b);
            return;
        }

        double discriminant = b * b - 4 * a * c;
        if (discriminant < 0) return;
        double root = Math.Sqrt(discriminant);
        Consider((-b + root) / (2 * a));
        Consider((-b - root) / (2 * a));
    }
}
