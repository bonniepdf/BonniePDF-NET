// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System;
using System.Collections.Generic;
using BonniePDF.Rendering.Svg.Parsers;

namespace BonniePDF.Rendering.Svg.Ir;

/// <summary>
/// Turns each basic shape into path segments.
/// </summary>
/// <remarks>
/// This is the de-sugaring the IR is built on: afterwards nothing downstream
/// knows a circle from a path. Sinks with a native circle primitive may still
/// recognise one, but are not required to.
/// </remarks>
internal static class Shapes
{
    /// <summary>The control-point distance that makes a cubic approximate a quarter circle.</summary>
    private const double Kappa = 0.5522847498307933;

    /// <summary>Converts a rectangle, with optional corner radii.</summary>
    public static List<PathSegment> Rect(double x, double y, double width, double height, double? rxInput, double? ryInput)
    {
        var segments = new List<PathSegment>();
        // A zero or negative extent disables rendering, per the spec.
        if (width <= 0 || height <= 0) return segments;

        // One radius implies the other, and each clamps to half its side.
        double rx = rxInput ?? ryInput ?? 0;
        double ry = ryInput ?? rxInput ?? 0;
        rx = Math.Min(Math.Max(rx, 0), width / 2);
        ry = Math.Min(Math.Max(ry, 0), height / 2);

        if (rx == 0 || ry == 0)
        {
            segments.Add(PathSegment.Move(x, y));
            segments.Add(PathSegment.Line(x + width, y));
            segments.Add(PathSegment.Line(x + width, y + height));
            segments.Add(PathSegment.Line(x, y + height));
            segments.Add(PathSegment.Close());
            return segments;
        }

        double cx = rx * Kappa;
        double cy = ry * Kappa;
        double right = x + width;
        double bottom = y + height;

        segments.Add(PathSegment.Move(x + rx, y));
        segments.Add(PathSegment.Line(right - rx, y));
        segments.Add(PathSegment.Cubic(right - rx + cx, y, right, y + ry - cy, right, y + ry));
        segments.Add(PathSegment.Line(right, bottom - ry));
        segments.Add(PathSegment.Cubic(right, bottom - ry + cy, right - rx + cx, bottom, right - rx, bottom));
        segments.Add(PathSegment.Line(x + rx, bottom));
        segments.Add(PathSegment.Cubic(x + rx - cx, bottom, x, bottom - ry + cy, x, bottom - ry));
        segments.Add(PathSegment.Line(x, y + ry));
        segments.Add(PathSegment.Cubic(x, y + ry - cy, x + rx - cx, y, x + rx, y));
        segments.Add(PathSegment.Close());
        return segments;
    }

    /// <summary>Converts an ellipse, and by extension a circle.</summary>
    public static List<PathSegment> Ellipse(double cx, double cy, double rx, double ry)
    {
        var segments = new List<PathSegment>();
        if (rx <= 0 || ry <= 0) return segments;

        double ax = rx * Kappa;
        double ay = ry * Kappa;

        segments.Add(PathSegment.Move(cx + rx, cy));
        segments.Add(PathSegment.Cubic(cx + rx, cy + ay, cx + ax, cy + ry, cx, cy + ry));
        segments.Add(PathSegment.Cubic(cx - ax, cy + ry, cx - rx, cy + ay, cx - rx, cy));
        segments.Add(PathSegment.Cubic(cx - rx, cy - ay, cx - ax, cy - ry, cx, cy - ry));
        segments.Add(PathSegment.Cubic(cx + ax, cy - ry, cx + rx, cy - ay, cx + rx, cy));
        segments.Add(PathSegment.Close());
        return segments;
    }

    /// <summary>Converts a line.</summary>
    public static List<PathSegment> Line(double x1, double y1, double x2, double y2) =>
        [PathSegment.Move(x1, y1), PathSegment.Line(x2, y2)];

    /// <summary>
    /// Converts a polyline or polygon, which differ only in the closing segment.
    /// </summary>
    /// <remarks>
    /// An odd trailing coordinate is dropped rather than throwing: the spec says
    /// to render the valid prefix, and real exports do truncate.
    /// </remarks>
    public static List<PathSegment> Poly(string points, bool close)
    {
        List<double> values = NumberScanner.ParseList(points);
        int count = values.Count / 2;
        var segments = new List<PathSegment>();
        if (count == 0) return segments;

        segments.Add(PathSegment.Move(values[0], values[1]));
        for (int i = 1; i < count; i++) segments.Add(PathSegment.Line(values[i * 2], values[i * 2 + 1]));
        if (close) segments.Add(PathSegment.Close());
        return segments;
    }
}
