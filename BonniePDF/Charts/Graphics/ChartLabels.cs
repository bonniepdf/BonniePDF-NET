// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;
namespace BonniePDF;

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
internal readonly record struct ChartLabelBox(float X, float Y, float Width, float Height);
internal static class ChartLabels
{
    internal static void Draw(ICanvas sink, List<ChartLabelBox> boxes, float width, float height, float x, float y, string text, Color color, float size, string family, TextAlignment align)
    {
        if (text.Length == 0) return;
        Size measured = sink.MeasureText(text, size, family); float w = measured.Width, h = measured.Height;
        if (w > width - 8 || h > height - 8) return;
        float left = x - (align == TextAlignment.Center ? w / 2 : align == TextAlignment.Right ? w : 0);
        left = Math.Max(4, Math.Min(width - 4 - w, left)); y = Math.Max(4, Math.Min(height - 4 - h, y));
        foreach (var b in boxes) if (left < b.X + b.Width + 2 && left + w + 2 > b.X && y < b.Y + b.Height + 2 && y + h + 2 > b.Y) return;
        boxes.Add(new(left, y, w, h));
        sink.DrawText(left + (align == TextAlignment.Center ? w / 2 : align == TextAlignment.Right ? w : 0), y, text, color, size, family, align);
    }
}
