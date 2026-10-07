using System.Collections.Generic;
using System.Text;

namespace BonniePDF.Rendering.Pdf;

/// <summary>
/// Helper methods for drawing common PDF primitives (boxes, borders, backgrounds).
/// Handles coordinate conversion and stroke alignment.
/// </summary>
internal static class PdfDrawing
{
    /// <summary>
    /// Draws a filled rectangle (background).
    /// </summary>
    /// <param name="stream">The PDF content stream to write to</param>
    /// <param name="x">X position in PDF coordinates</param>
    /// <param name="y">Y position in PDF coordinates</param>
    /// <param name="width">Width of the rectangle</param>
    /// <param name="height">Height of the rectangle</param>
    /// <param name="color">Fill color</param>
    public static void FillRect(StringBuilder stream, float x, float y, float width, float height, Color color)
    {
        PdfOperators.SaveState(stream);
        PdfOperators.SetFillColor(stream, color);
        PdfOperators.RectangleFill(stream, x, y, width, height);
        PdfOperators.RestoreState(stream);
    }

    /// <summary>
    /// Draws a stroked rectangle (border).
    /// Automatically insets the path by half the stroke width so the outer edge
    /// aligns with the specified bounds (PDF strokes are centered on the path).
    /// </summary>
    /// <param name="stream">The PDF content stream to write to</param>
    /// <param name="x">X position in PDF coordinates</param>
    /// <param name="y">Y position in PDF coordinates</param>
    /// <param name="width">Width of the rectangle</param>
    /// <param name="height">Height of the rectangle</param>
    /// <param name="color">Stroke color</param>
    /// <param name="strokeWidth">Width of the stroke</param>
    public static void StrokeRect(StringBuilder stream, float x, float y, float width, float height, Color color,
        float strokeWidth)
    {
        if (strokeWidth <= 0) return;

        // Inset the path by half the stroke width so the outer edge aligns with bounds
        float halfStroke = strokeWidth / 2;
        float insetX = x + halfStroke;
        float insetY = y + halfStroke;
        float insetWidth = width - strokeWidth;
        float insetHeight = height - strokeWidth;

        PdfOperators.SaveState(stream);
        PdfOperators.SetStrokeColor(stream, color);
        PdfOperators.SetLineWidth(stream, strokeWidth);
        PdfOperators.SetLineJoin(stream, 0);  // miter (sharp corners)
        PdfOperators.SetMiterLimit(stream, 10);
        PdfOperators.RectangleStroke(stream, insetX, insetY, insetWidth, insetHeight);
        PdfOperators.RestoreState(stream);
    }

    /// <summary>
    /// Draws a box with optional background fill and border.
    /// </summary>
    /// <param name="stream">The PDF content stream to write to</param>
    /// <param name="x">X position in PDF coordinates</param>
    /// <param name="y">Y position in PDF coordinates</param>
    /// <param name="width">Width of the box</param>
    /// <param name="height">Height of the box</param>
    /// <param name="backgroundColor">Optional fill color</param>
    /// <param name="borderColor">Optional border color</param>
    /// <param name="borderWidth">Border stroke width (ignored if borderColor is null)</param>
    public static void DrawBox(StringBuilder stream, float x, float y, float width, float height,
        Color? backgroundColor = null, Color? borderColor = null, float borderWidth = 0)
    {
        if (!backgroundColor.HasValue && (!borderColor.HasValue || borderWidth <= 0))
            return;

        if (backgroundColor.HasValue) FillRect(stream, x, y, width, height, backgroundColor.Value);

        if (borderColor.HasValue && borderWidth > 0)
            StrokeRect(stream, x, y, width, height, borderColor.Value, borderWidth);
    }

    /// <summary>
    /// Draws a filled circle.
    /// </summary>
    /// <param name="stream">The PDF content stream to write to</param>
    /// <param name="centerX">X position of circle center in PDF coordinates</param>
    /// <param name="centerY">Y position of circle center in PDF coordinates</param>
    /// <param name="radius">Radius of the circle</param>
    /// <param name="color">Fill color</param>
    public static void FillCircle(StringBuilder stream, float centerX, float centerY, float radius, Color color)
    {
        // PDF doesn't have a circle primitive, so we approximate with Bézier curves
        // The magic number 0.552284749831 is the control point distance for a good circle approximation
        const float k = 0.552284749831f;
        float kr = k * radius;

        PdfOperators.SaveState(stream);
        PdfOperators.SetFillColor(stream, color);

        // Move to rightmost point
        PdfOperators.MoveTo(stream, centerX + radius, centerY);

        // Draw four Bézier curves for the circle (clockwise from right)
        PdfOperators.CurveTo(stream, centerX + radius, centerY + kr, centerX + kr, centerY + radius, centerX, centerY + radius);
        PdfOperators.CurveTo(stream, centerX - kr, centerY + radius, centerX - radius, centerY + kr, centerX - radius, centerY);
        PdfOperators.CurveTo(stream, centerX - radius, centerY - kr, centerX - kr, centerY - radius, centerX, centerY - radius);
        PdfOperators.CurveTo(stream, centerX + kr, centerY - radius, centerX + radius, centerY - kr, centerX + radius, centerY);

        PdfOperators.Fill(stream);
        PdfOperators.RestoreState(stream);
    }

    public static void FillEllipse(StringBuilder stream, float centerX, float centerY, float rx, float ry, Color color)
    {
        const float k = 0.552284749831f;
        float kx = k * rx;
        float ky = k * ry;

        PdfOperators.SaveState(stream);
        PdfOperators.SetFillColor(stream, color);

        PdfOperators.MoveTo(stream, centerX + rx, centerY);
        PdfOperators.CurveTo(stream, centerX + rx, centerY + ky, centerX + kx, centerY + ry, centerX, centerY + ry);
        PdfOperators.CurveTo(stream, centerX - kx, centerY + ry, centerX - rx, centerY + ky, centerX - rx, centerY);
        PdfOperators.CurveTo(stream, centerX - rx, centerY - ky, centerX - kx, centerY - ry, centerX, centerY - ry);
        PdfOperators.CurveTo(stream, centerX + kx, centerY - ry, centerX + rx, centerY - ky, centerX + rx, centerY);

        PdfOperators.Fill(stream);
        PdfOperators.RestoreState(stream);
    }

    /// <summary>
    /// Draws a filled rounded rectangle using Bézier curves for the corners.
    /// </summary>
    public static void FillRoundedRect(StringBuilder stream, float x, float y, float width, float height,
        float radius, Color color)
    {
        if (radius <= 0)
        {
            FillRect(stream, x, y, width, height, color);
            return;
        }

        // Clamp radius to half the smallest dimension
        float maxRadius = System.Math.Min(width, height) / 2f;
        if (radius > maxRadius) radius = maxRadius;

        PdfOperators.SaveState(stream);
        PdfOperators.SetFillColor(stream, color);
        AppendRoundedRectPath(stream, x, y, width, height, radius);
        PdfOperators.Fill(stream);
        PdfOperators.RestoreState(stream);
    }

    /// <summary>
    /// Draws a stroked rounded rectangle using Bézier curves for the corners.
    /// Automatically insets the path by half the stroke width.
    /// </summary>
    public static void StrokeRoundedRect(StringBuilder stream, float x, float y, float width, float height,
        float radius, Color color, float strokeWidth)
    {
        if (strokeWidth <= 0) return;

        if (radius <= 0)
        {
            StrokeRect(stream, x, y, width, height, color, strokeWidth);
            return;
        }

        // Inset the path by half the stroke width so the outer edge aligns with bounds
        float halfStroke = strokeWidth / 2;
        float insetX = x + halfStroke;
        float insetY = y + halfStroke;
        float insetWidth = width - strokeWidth;
        float insetHeight = height - strokeWidth;
        float insetRadius = System.Math.Max(0, radius - halfStroke);

        // Clamp radius to half the smallest inset dimension
        float maxRadius = System.Math.Min(insetWidth, insetHeight) / 2f;
        if (insetRadius > maxRadius) insetRadius = maxRadius;

        PdfOperators.SaveState(stream);
        PdfOperators.SetStrokeColor(stream, color);
        PdfOperators.SetLineWidth(stream, strokeWidth);
        PdfOperators.SetLineJoin(stream, 1); // round join for rounded rect
        AppendRoundedRectPath(stream, insetX, insetY, insetWidth, insetHeight, insetRadius);
        PdfOperators.ClosePathStroke(stream);
        PdfOperators.RestoreState(stream);
    }

    /// <summary>
    /// Appends the path operators for a rounded rectangle (no fill or stroke).
    /// Uses four arcs (quarter-circle Bézier curves) connected by straight lines.
    /// PDF coordinates: bottom-left origin.
    /// </summary>
    private static void AppendRoundedRectPath(StringBuilder stream, float x, float y, float width, float height,
        float radius)
    {
        // Bézier control point distance for a quarter-circle arc
        const float k = 0.552284749831f;
        float kr = k * radius;
        float r = radius;

        // Start at bottom-left, just after the corner arc
        PdfOperators.MoveTo(stream, x + r, y);

        // Bottom edge → bottom-right corner arc
        PdfOperators.LineTo(stream, x + width - r, y);
        PdfOperators.CurveTo(stream,
            x + width - r + kr, y,
            x + width, y + r - kr,
            x + width, y + r);

        // Right edge → top-right corner arc
        PdfOperators.LineTo(stream, x + width, y + height - r);
        PdfOperators.CurveTo(stream,
            x + width, y + height - r + kr,
            x + width - r + kr, y + height,
            x + width - r, y + height);

        // Top edge → top-left corner arc
        PdfOperators.LineTo(stream, x + r, y + height);
        PdfOperators.CurveTo(stream,
            x + r - kr, y + height,
            x, y + height - r + kr,
            x, y + height - r);

        // Left edge → bottom-left corner arc
        PdfOperators.LineTo(stream, x, y + r);
        PdfOperators.CurveTo(stream,
            x, y + r - kr,
            x + r - kr, y,
            x + r, y);

        PdfOperators.ClosePath(stream);
    }

    /// <summary>
    /// Intersects the current clipping path with a rounded-rectangle region.
    /// Appends the rounded-rect path (reusing the same Bézier construction as the
    /// fill/stroke variants) followed by <c>W n</c>, so subsequent drawing is
    /// masked to the rounded rect. The caller owns the surrounding <c>q</c>/<c>Q</c>
    /// so the clip can be undone. A radius of zero clips to a plain rectangle.
    /// </summary>
    public static void ClipRoundedRect(StringBuilder stream, float x, float y, float width, float height,
        float radius)
    {
        if (radius <= 0)
        {
            PdfOperators.Rectangle(stream, x, y, width, height);
            PdfOperators.Clip(stream);
            return;
        }

        // Clamp radius to half the smallest dimension, matching FillRoundedRect.
        float maxRadius = System.Math.Min(width, height) / 2f;
        if (radius > maxRadius) radius = maxRadius;

        AppendRoundedRectPath(stream, x, y, width, height, radius);
        PdfOperators.Clip(stream);
    }

    /// <summary>
    /// Fills a closed path defined by a series of points.
    /// </summary>
    /// <param name="stream">The PDF content stream to write to</param>
    /// <param name="points">The points that define the path in PDF coordinates</param>
    /// <param name="color">Fill color</param>
    public static void FillPath(StringBuilder stream, List<Point>? points, Color color)
    {
        if (points == null || points.Count < 3) return;

        PdfOperators.SaveState(stream);
        PdfOperators.SetFillColor(stream, color);

        var first = points[0];
        PdfOperators.MoveTo(stream, first.X, first.Y);

        for (int i = 1; i < points.Count; i++)
        {
            Point p = points[i];
            PdfOperators.LineTo(stream, p.X, p.Y);
        }

        PdfOperators.ClosePath(stream);
        PdfOperators.Fill(stream);
        PdfOperators.RestoreState(stream);
    }
}