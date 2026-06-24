using System;

namespace BonniePDF.Drawing;

/// <summary>Factory methods that draw common shapes onto a canvas path.</summary>
public static class Shapes
{
    /// <summary>Adds a circle path centered at (<paramref name="cx"/>, <paramref name="cy"/>).</summary>
    public static void Circle(ICanvas canvas, float cx, float cy, float radius)
    {
        const float k = 0.552284749831f;
        float kr = k * radius;
        canvas.MoveTo(cx + radius, cy);
        canvas.CubicBezierTo(cx + radius, cy + kr, cx + kr, cy + radius, cx, cy + radius);
        canvas.CubicBezierTo(cx - kr, cy + radius, cx - radius, cy + kr, cx - radius, cy);
        canvas.CubicBezierTo(cx - radius, cy - kr, cx - kr, cy - radius, cx, cy - radius);
        canvas.CubicBezierTo(cx + kr, cy - radius, cx + radius, cy - kr, cx + radius, cy);
        canvas.ClosePath();
    }

    /// <summary>Adds a diamond (rotated square) path.</summary>
    public static void Diamond(ICanvas canvas, float cx, float cy, float size)
    {
        float half = size / 2f;
        canvas.MoveTo(cx, cy - half);
        canvas.LineTo(cx + half, cy);
        canvas.LineTo(cx, cy + half);
        canvas.LineTo(cx - half, cy);
        canvas.ClosePath();
    }

    /// <summary>Adds a regular polygon path.</summary>
    public static void RegularPolygon(ICanvas canvas, float cx, float cy, float radius, int sides)
    {
        if (sides < 3) sides = 3;
        float angleStep = 2f * MathF.PI / sides;
        float startAngle = -MathF.PI / 2f; // top
        for (int i = 0; i <= sides; i++)
        {
            float angle = startAngle + i * angleStep;
            float px = cx + radius * MathF.Cos(angle);
            float py = cy + radius * MathF.Sin(angle);
            if (i == 0) canvas.MoveTo(px, py);
            else canvas.LineTo(px, py);
        }
        canvas.ClosePath();
    }

    /// <summary>Adds a star path with the given number of points.</summary>
    public static void Star(ICanvas canvas, float cx, float cy, float outerRadius, float innerRadius, int points)
    {
        if (points < 3) points = 3;
        int totalVertices = points * 2;
        float angleStep = MathF.PI / points;
        float startAngle = -MathF.PI / 2f;
        for (int i = 0; i <= totalVertices; i++)
        {
            float r = (i % 2 == 0) ? outerRadius : innerRadius;
            float angle = startAngle + i * angleStep;
            float px = cx + r * MathF.Cos(angle);
            float py = cy + r * MathF.Sin(angle);
            if (i == 0) canvas.MoveTo(px, py);
            else canvas.LineTo(px, py);
        }
        canvas.ClosePath();
    }

    /// <summary>Adds a rounded rectangle path.</summary>
    public static void RoundedRect(ICanvas canvas, float x, float y, float width, float height, float cornerRadius)
    {
        const float k = 0.552284749831f;
        float r = MathF.Min(cornerRadius, MathF.Min(width / 2f, height / 2f));
        float kr = k * r;
        canvas.MoveTo(x + r, y);
        canvas.LineTo(x + width - r, y);
        canvas.CubicBezierTo(x + width - r + kr, y, x + width, y + r - kr, x + width, y + r);
        canvas.LineTo(x + width, y + height - r);
        canvas.CubicBezierTo(x + width, y + height - r + kr, x + width - r + kr, y + height, x + width - r, y + height);
        canvas.LineTo(x + r, y + height);
        canvas.CubicBezierTo(x + r - kr, y + height, x, y + height - r + kr, x, y + height - r);
        canvas.LineTo(x, y + r);
        canvas.CubicBezierTo(x, y + r - kr, x + r - kr, y, x + r, y);
        canvas.ClosePath();
    }
}
