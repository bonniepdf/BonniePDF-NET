// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BonniePDF.Rendering.Svg.Parsers;

/// <summary>The kind of a <see cref="PathSegment"/>.</summary>
internal enum PathSegmentKind
{
    /// <summary>Start a new subpath.</summary>
    Move,

    /// <summary>A straight segment.</summary>
    Line,

    /// <summary>A cubic Bézier segment.</summary>
    Cubic,

    /// <summary>Close the current subpath.</summary>
    Close
}

/// <summary>
/// A path reduced to the four primitives every sink can draw.
/// </summary>
/// <remarks>
/// Quadratics and arcs are converted during parsing rather than passed through,
/// so no sink needs to know they existed. That de-sugaring is what keeps sinks
/// small.
/// </remarks>
/// <param name="Kind">Which primitive this is.</param>
/// <param name="X">Endpoint x, unused for <see cref="PathSegmentKind.Close"/>.</param>
/// <param name="Y">Endpoint y, unused for <see cref="PathSegmentKind.Close"/>.</param>
/// <param name="C1X">First control point x, cubics only.</param>
/// <param name="C1Y">First control point y, cubics only.</param>
/// <param name="C2X">Second control point x, cubics only.</param>
/// <param name="C2Y">Second control point y, cubics only.</param>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct PathSegment(
    PathSegmentKind Kind,
    double X = 0,
    double Y = 0,
    double C1X = 0,
    double C1Y = 0,
    double C2X = 0,
    double C2Y = 0)
{
    /// <summary>A moveto.</summary>
    public static PathSegment Move(double x, double y) => new(PathSegmentKind.Move, x, y);

    /// <summary>A lineto.</summary>
    public static PathSegment Line(double x, double y) => new(PathSegmentKind.Line, x, y);

    /// <summary>A cubic curveto.</summary>
    public static PathSegment Cubic(double c1x, double c1y, double c2x, double c2y, double x, double y) =>
        new(PathSegmentKind.Cubic, x, y, c1x, c1y, c2x, c2y);

    /// <summary>A closepath.</summary>
    public static PathSegment Close() => new(PathSegmentKind.Close);
}

/// <summary>Parses SVG path data and converts arcs and quadratics to cubics.</summary>
internal static class PathDataParser
{
    /// <summary>
    /// Converts one elliptical arc to cubic Béziers, per the SVG implementation
    /// notes (F.6.5 endpoint-to-centre conversion, F.6.6 radius correction).
    /// </summary>
    /// <remarks>
    /// Splitting into segments of at most 90 degrees keeps the approximation
    /// within tolerance; a single Bézier cannot represent a large arc accurately.
    /// Measured deviation from the true ellipse is about 2.7e-4 radii - see the
    /// note in characterization/README.md about why this does not match FOP.
    /// </remarks>
    public static List<PathSegment> ArcToCubics(
        double x1, double y1, double rxIn, double ryIn, double xAxisRotationDegrees,
        bool largeArc, bool sweep, double x2, double y2)
    {
        var segments = new List<PathSegment>();

        // F.6.2: a zero radius degenerates to a line; negative radii take their
        // absolute value.
        double rx = Math.Abs(rxIn);
        double ry = Math.Abs(ryIn);
        if (rx == 0 || ry == 0)
        {
            segments.Add(PathSegment.Line(x2, y2));
            return segments;
        }

        if (x1 == x2 && y1 == y2) return segments;

        double phi = xAxisRotationDegrees * Math.PI / 180.0;
        double cosPhi = Math.Cos(phi);
        double sinPhi = Math.Sin(phi);

        double dx2 = (x1 - x2) / 2.0;
        double dy2 = (y1 - y2) / 2.0;
        double x1p = cosPhi * dx2 + sinPhi * dy2;
        double y1p = -sinPhi * dx2 + cosPhi * dy2;

        // F.6.6: scale radii up if they cannot span the endpoints.
        double lambda = x1p * x1p / (rx * rx) + y1p * y1p / (ry * ry);
        if (lambda > 1)
        {
            double scale = Math.Sqrt(lambda);
            rx *= scale;
            ry *= scale;
        }

        double rxSq = rx * rx;
        double rySq = ry * ry;
        double numerator = rxSq * rySq - rxSq * y1p * y1p - rySq * x1p * x1p;
        double denominator = rxSq * y1p * y1p + rySq * x1p * x1p;
        // Clamp at zero: doubleing-point error can make this slightly negative
        // for a nearly degenerate arc, and Sqrt would return NaN.
        double coefficient = (largeArc == sweep ? -1 : 1) * Math.Sqrt(Math.Max(0, numerator / denominator));
        double cxp = coefficient * rx * y1p / ry;
        double cyp = -coefficient * ry * x1p / rx;
        double cx = cosPhi * cxp - sinPhi * cyp + (x1 + x2) / 2.0;
        double cy = sinPhi * cxp + cosPhi * cyp + (y1 + y2) / 2.0;

        double theta = Angle(1, 0, (x1p - cxp) / rx, (y1p - cyp) / ry);
        double delta = Angle((x1p - cxp) / rx, (y1p - cyp) / ry, (-x1p - cxp) / rx, (-y1p - cyp) / ry);
        if (!sweep && delta > 0) delta -= 2 * Math.PI;
        else if (sweep && delta < 0) delta += 2 * Math.PI;

        // Snap the count before rounding up. Ceiling on a value that lands exactly
        // on an integer is decided by the last bit of doubleing-point error, and a
        // half-circle arc - extremely common in real icons - puts |delta| at
        // exactly two quarter turns. Without the tolerance the two
        // implementations disagree about whether that is 2 segments or 3:
        // identical rendering, different sink calls, and therefore different PDF
        // content streams for the same input. Found by the examples parity check
        // on heroicons' paper-clip.
        double quarterTurns = Math.Abs(delta) / (Math.PI / 2);
        int count = Math.Max(1, (int)Math.Ceiling(quarterTurns - 1e-9));
        double step = delta / count;
        // Control-point distance for a circular arc of `step` radians; exact at
        // the endpoints and tangents, which keeps the joins smooth.
        double alpha = 4.0 / 3.0 * Math.Tan(step / 4.0);

        double angleStart = theta;
        for (int i = 0; i < count; i++)
        {
            double angleEnd = angleStart + step;

            (double sx, double sy) = OnEllipse(cx, cy, rx, ry, cosPhi, sinPhi, Math.Cos(angleStart), Math.Sin(angleStart));
            (double ex, double ey) = OnEllipse(cx, cy, rx, ry, cosPhi, sinPhi, Math.Cos(angleEnd), Math.Sin(angleEnd));
            (double dsx, double dsy) = Tangent(rx, ry, cosPhi, sinPhi, Math.Cos(angleStart), Math.Sin(angleStart));
            (double dex, double dey) = Tangent(rx, ry, cosPhi, sinPhi, Math.Cos(angleEnd), Math.Sin(angleEnd));

            segments.Add(PathSegment.Cubic(
                (double)(sx + alpha * dsx), (double)(sy + alpha * dsy),
                (double)(ex - alpha * dex), (double)(ey - alpha * dey),
                (double)ex, (double)ey));

            angleStart = angleEnd;
        }

        // Land exactly on the commanded endpoint: the trigonometry lands within
        // doubleing-point distance, and that drift shows as a gap on closed shapes.
        PathSegment last = segments[^1];
        segments[^1] = PathSegment.Cubic(last.C1X, last.C1Y, last.C2X, last.C2Y, x2, y2);
        return segments;
    }

    private static (double, double) OnEllipse(double cx, double cy, double rx, double ry,
        double cosPhi, double sinPhi, double cosAngle, double sinAngle)
    {
        double x = rx * cosAngle;
        double y = ry * sinAngle;
        return (cx + cosPhi * x - sinPhi * y, cy + sinPhi * x + cosPhi * y);
    }

    private static (double, double) Tangent(double rx, double ry, double cosPhi, double sinPhi,
        double cosAngle, double sinAngle)
    {
        double dx = -rx * sinAngle;
        double dy = ry * cosAngle;
        return (cosPhi * dx - sinPhi * dy, sinPhi * dx + cosPhi * dy);
    }

    private static double Angle(double ux, double uy, double vx, double vy)
    {
        double dot = ux * vx + uy * vy;
        double length = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
        // Clamp before Acos: dot/length can exceed 1 by an epsilon.
        double value = Math.Acos(Math.Min(1, Math.Max(-1, dot / length)));
        return ux * vy - uy * vx < 0 ? -value : value;
    }

    /// <summary>
    /// Parses a <c>d</c> attribute into primitive segments. Supports every
    /// command in both cases, implicit repeats, and the smooth-variant
    /// control-point reflection.
    /// </summary>
    public static List<PathSegment> Parse(string d)
    {
        var scanner = new NumberScanner(d);
        var segments = new List<PathSegment>();

        double currentX = 0, currentY = 0;
        double startX = 0, startY = 0;
        // Previous control points, for the reflection S and T perform. Null when
        // the previous command was not the matching curve type: the spec then
        // degenerates the reflection to the current point.
        (double X, double Y)? lastCubicControl = null;
        (double X, double Y)? lastQuadControl = null;
        char command = '\0';

        while (!scanner.AtEnd)
        {
            char next = scanner.Peek()!.Value;
            if (char.IsAsciiLetter(next))
            {
                command = scanner.Take()!.Value;
            }
            else if (command == '\0')
            {
                throw new SvgParseException("path data must begin with a command", scanner.Position, d);
            }
            else if (command == 'M')
            {
                // An implicit repeat of moveto is lineto, not another moveto.
                command = 'L';
            }
            else if (command == 'm')
            {
                command = 'l';
            }
            else if (command is 'Z' or 'z')
            {
                throw new SvgParseException("closepath takes no arguments", scanner.Position, d);
            }

            bool relative = char.IsLower(command);
            double originX = relative ? currentX : 0;
            double originY = relative ? currentY : 0;

            switch (char.ToUpperInvariant(command))
            {
                case 'M':
                    currentX = originX + scanner.Number();
                    currentY = originY + scanner.Number();
                    startX = currentX;
                    startY = currentY;
                    segments.Add(PathSegment.Move(currentX, currentY));
                    lastCubicControl = lastQuadControl = null;
                    break;

                case 'L':
                    currentX = originX + scanner.Number();
                    currentY = originY + scanner.Number();
                    segments.Add(PathSegment.Line(currentX, currentY));
                    lastCubicControl = lastQuadControl = null;
                    break;

                case 'H':
                    currentX = originX + scanner.Number();
                    segments.Add(PathSegment.Line(currentX, currentY));
                    lastCubicControl = lastQuadControl = null;
                    break;

                case 'V':
                    currentY = originY + scanner.Number();
                    segments.Add(PathSegment.Line(currentX, currentY));
                    lastCubicControl = lastQuadControl = null;
                    break;

                case 'C':
                {
                    double c1x = originX + scanner.Number();
                    double c1y = originY + scanner.Number();
                    double c2x = originX + scanner.Number();
                    double c2y = originY + scanner.Number();
                    currentX = originX + scanner.Number();
                    currentY = originY + scanner.Number();
                    segments.Add(PathSegment.Cubic(c1x, c1y, c2x, c2y, currentX, currentY));
                    lastCubicControl = (c2x, c2y);
                    lastQuadControl = null;
                    break;
                }

                case 'S':
                {
                    // Reflect the previous cubic's second control point through
                    // the current point; degenerate to the current point when the
                    // previous command was not a cubic.
                    (double X, double Y) reflected = lastCubicControl is { } previousCubic
                        ? (2 * currentX - previousCubic.X, 2 * currentY - previousCubic.Y)
                        : (currentX, currentY);
                    double c2x = originX + scanner.Number();
                    double c2y = originY + scanner.Number();
                    currentX = originX + scanner.Number();
                    currentY = originY + scanner.Number();
                    segments.Add(PathSegment.Cubic(reflected.X, reflected.Y, c2x, c2y, currentX, currentY));
                    lastCubicControl = (c2x, c2y);
                    lastQuadControl = null;
                    break;
                }

                case 'Q':
                {
                    double qx = originX + scanner.Number();
                    double qy = originY + scanner.Number();
                    double endX = originX + scanner.Number();
                    double endY = originY + scanner.Number();
                    segments.Add(QuadToCubic(currentX, currentY, qx, qy, endX, endY));
                    currentX = endX;
                    currentY = endY;
                    lastQuadControl = (qx, qy);
                    lastCubicControl = null;
                    break;
                }

                case 'T':
                {
                    // Same reflection rule as S, for the quadratic control point.
                    (double X, double Y) mirrored = lastQuadControl is { } previousQuad
                        ? (2 * currentX - previousQuad.X, 2 * currentY - previousQuad.Y)
                        : (currentX, currentY);
                    double qx = mirrored.X;
                    double qy = mirrored.Y;
                    double endX = originX + scanner.Number();
                    double endY = originY + scanner.Number();
                    segments.Add(QuadToCubic(currentX, currentY, qx, qy, endX, endY));
                    currentX = endX;
                    currentY = endY;
                    lastQuadControl = (qx, qy);
                    lastCubicControl = null;
                    break;
                }

                case 'A':
                {
                    double rx = scanner.Number();
                    double ry = scanner.Number();
                    double rotation = scanner.Number();
                    bool largeArc = scanner.Flag();
                    bool sweep = scanner.Flag();
                    double endX = originX + scanner.Number();
                    double endY = originY + scanner.Number();
                    segments.AddRange(ArcToCubics(currentX, currentY, rx, ry, rotation, largeArc, sweep, endX, endY));
                    currentX = endX;
                    currentY = endY;
                    lastCubicControl = lastQuadControl = null;
                    break;
                }

                case 'Z':
                    segments.Add(PathSegment.Close());
                    currentX = startX;
                    currentY = startY;
                    lastCubicControl = lastQuadControl = null;
                    break;

                default:
                    throw new SvgParseException($"unknown path command \"{command}\"", scanner.Position, d);
            }
        }

        return segments;
    }

    /// <summary>
    /// Raises a quadratic to a cubic. Exact, not an approximation: every
    /// quadratic is a cubic whose control points sit two thirds along.
    /// </summary>
    private static PathSegment QuadToCubic(double x0, double y0, double qx, double qy, double x1, double y1) =>
        PathSegment.Cubic(
            x0 + 2.0 / 3.0 * (qx - x0), y0 + 2.0 / 3.0 * (qy - y0),
            x1 + 2.0 / 3.0 * (qx - x1), y1 + 2.0 / 3.0 * (qy - y1),
            x1, y1);
}
