using System;
using BonniePDF.Core;
using BonniePDF.Drawing;

namespace BonniePDF.Elements.Text;

/// <summary>
/// Line style for text decorations (underline/strikethrough/overline).
/// All variants are painted with straight <see cref="ICanvas.DrawLine"/> segments
/// so they render identically on the PDF and SVG canvases.
/// </summary>
public enum DecorationStyle
{
    /// <summary>A single continuous line (default).</summary>
    Solid = 0,

    /// <summary>A sine wave, e.g. a spell-check squiggle.</summary>
    Wavy = 1,

    /// <summary>Evenly spaced dashes.</summary>
    Dashed = 2,

    /// <summary>Evenly spaced dots.</summary>
    Dotted = 3,
}

/// <summary>
/// Paints horizontal decoration lines in a <see cref="DecorationStyle"/>. Mirrors
/// the TypeScript <c>paintDecorationLine</c> so both implementations produce the
/// same segments.
/// </summary>
public static class DecorationPainter
{
    /// <summary>
    /// Paints a horizontal decoration line from <paramref name="x"/> to
    /// <c>x + width</c> at baseline <paramref name="y"/> in the requested style.
    /// Wavy/dashed/dotted are approximated with short straight segments so the
    /// output is canvas-agnostic and PDF/UA-safe (paths only, no fonts).
    /// <see cref="DecorationStyle.Solid"/> emits a single segment, byte-identical
    /// to a plain single-segment line.
    /// </summary>
    public static void Paint(ICanvas canvas, float x, float y, float width, Color color, float thickness, DecorationStyle style)
    {
        if (width <= 0) return;

        switch (style)
        {
            case DecorationStyle.Solid:
                canvas.DrawLine(x, y, x + width, y, color, thickness);
                return;

            case DecorationStyle.Wavy:
            {
                float amplitude = Math.Max(0.75f, thickness * 1.2f);
                float wavelength = Math.Max(3f, amplitude * 4f);
                float step = wavelength / 6f; // 6 segments per wave = smooth enough
                float px = x;
                float py = y;
                for (float cx = x + step; cx <= x + width + 1e-6f; cx += step)
                {
                    float clamped = Math.Min(cx, x + width);
                    float cy = y + amplitude * (float)Math.Sin((clamped - x) / wavelength * 2 * Math.PI);
                    canvas.DrawLine(px, py, clamped, cy, color, thickness);
                    px = clamped;
                    py = cy;
                }
                return;
            }

            case DecorationStyle.Dashed:
            {
                float dash = Math.Max(2f, thickness * 3f);
                float gap = dash * 0.6f;
                for (float sx = x; sx < x + width; sx += dash + gap)
                {
                    float ex = Math.Min(sx + dash, x + width);
                    canvas.DrawLine(sx, y, ex, y, color, thickness);
                }
                return;
            }

            case DecorationStyle.Dotted:
            {
                float dot = Math.Max(thickness, 0.75f);
                float gap = dot * 1.8f;
                for (float sx = x; sx < x + width; sx += dot + gap)
                {
                    float ex = Math.Min(sx + dot, x + width);
                    canvas.DrawLine(sx, y, ex, y, color, thickness);
                }
                return;
            }
        }
    }
}
