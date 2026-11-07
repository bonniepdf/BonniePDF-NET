using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BonniePDF.Drawing;

/// <summary>Captures only paint geometry that can establish a single opaque text backdrop.</summary>
internal sealed class CanvasContrast(RenderContext ctx, float width, float height)
{
    [StructLayout(LayoutKind.Auto)]
    private readonly record struct PaintRect(float Left, float Top, float Right, float Bottom, Color? Color);
    private readonly List<PaintRect> _paint = [];
    private bool _uncertainState;
    private readonly float _originX = ctx.CurrentX;
    private readonly float _originY = ctx.CurrentY;

    public void Rect(float x, float y, float w, float h, Color color)
    {
        if (!_uncertainState && w > 0 && h > 0) _paint.Add(new PaintRect(x, y, x + w, y + h, color));
    }

    public void UnknownPaint() => _paint.Clear();

    public void UnknownState() => _uncertainState = true;

    public void Text(float x, float y, float textWidth, float size, Color color, string font, TextAlignment align, bool complex = false)
    {
        float left = x - (align == TextAlignment.Center ? textWidth / 2 : align == TextAlignment.Right ? textWidth : 0);
        float right = left + textWidth;
        float bottom = y + size * 1.2f;
        string? backdrop = null;
        if (!complex && !_uncertainState && left >= 0 && right <= width && y >= 0 && bottom <= height)
        {
            for (int i = _paint.Count - 1; i >= 0; i--)
            {
                PaintRect p = _paint[i];
                if (p.Right <= left || p.Left >= right || p.Bottom <= y || p.Top >= bottom) continue;
                if (p.Color is { ScA: 1 } bg && p.Left <= left && p.Right >= right && p.Top <= y && p.Bottom >= bottom)
                    backdrop = Hex(bg);
                break;
            }
            _paint.Add(new PaintRect(left, y, right, bottom, null));
        }
        else UnknownPaint();

        if (ctx.LayoutRecorder is not { } recorder) return;
        object node = new();
        float top = _originY + y;
        recorder.OpenNode(node, "CanvasText", null, ctx.CurrentPageIndex, _originX + left, top, textWidth);
        recorder.AnnotatePaint(node, new NodePaint
        {
            Text = Hex(color),
            TextAlpha = color.ScA < 1 ? color.ScA : null,
            TextBackdrop = backdrop,
            FontSize = size,
            Bold = font.Contains("Bold", StringComparison.OrdinalIgnoreCase) ? true : null
        });
        recorder.CloseNode(node, ctx.CurrentPageIndex, top + size * 1.2f, complete: true);
    }

    private static string Hex(Color color) => $"#{color.R:x2}{color.G:x2}{color.B:x2}";
}
