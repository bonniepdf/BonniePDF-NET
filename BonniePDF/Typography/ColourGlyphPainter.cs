using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BonniePDF.Drawing;
using BonniePDF.Typography.Engine;
using BonniePDF.Typography.TrueType;

namespace BonniePDF;

internal static class ColourGlyphPainter
{
    private sealed record Layer(Glyph Outline, PaletteColour Colour, bool Foreground);
    private sealed record Artwork(List<Layer> Layers, BitmapGlyphData? Bitmap, ImageInfo? Image, long Bytes);
    private sealed class Cache
    {
        internal readonly Dictionary<(int Glyph, int Ppem, bool Text), Artwork> Entries = new();
        internal long Bytes;
    }
    private static readonly ConditionalWeakTable<TtfFont, Cache> Caches = new();

    private static Artwork Read(TtfFont font, int glyph, int ppem, bool textPresentation)
    {
        var cache = Caches.GetOrCreateValue(font);
        lock (cache)
        {
            var key = (glyph, ppem, textPresentation);
            if (cache.Entries.TryGetValue(key, out var hit)) return hit;
            // The engine face caches glyph reads, so read it under the font's shaping lock.
            var (layers, bitmap) = font.ReadEngine(face =>
            {
                var read = new List<Layer>();
                if (!textPresentation)
                    foreach (var layer in face.Layers(glyph))
                        read.Add(new(face.ReadGlyph(layer.Glyph), layer.Colour, layer.Foreground));
                var strike = read.Count == 0 && !textPresentation ? face.BitmapGlyph(glyph, ppem) : null;
                if (read.Count == 0 && strike is null && font.HasOutlines)
                    read.Add(new(face.ReadGlyph(glyph), default, true));
                return (read, strike);
            });
            long bytes = 0;
            foreach (var layer in layers)
                foreach (var contour in layer.Outline.Contours) bytes += contour.Count * 24L + 32;
            ImageInfo? image = null;
            if (bitmap is not null)
            {
                image = ImageLoader.Load(bitmap.GetPngBytes());
                if (image.Width != bitmap.Width || image.Height != bitmap.Height)
                    throw new InvalidOperationException("Emoji bitmap dimensions disagree with its font metrics.");
                image.KeepResolution = true;
                bytes += image.Data.LongLength + image.AlphaData.LongLength + image.OriginalData.LongLength;
            }
            var result = new Artwork(layers, bitmap, image, bytes);
            // Per immutable font, bounded by both count and decoded payload. Oversized
            // artwork remains renderable but is not retained. No process-wide font root.
            if (bytes <= 16 * 1024 * 1024)
            {
                while (cache.Entries.Count >= 128 || cache.Bytes + bytes > 16 * 1024 * 1024)
                {
                    using var keys = cache.Entries.Keys.GetEnumerator();
                    keys.MoveNext(); var old = keys.Current;
                    cache.Bytes -= cache.Entries[old].Bytes; cache.Entries.Remove(old);
                }
                cache.Entries.Add(key, result); cache.Bytes += bytes;
            }
            return result;
        }
    }

    internal static void Draw(ICanvas canvas, TtfFont font, GlyphInfo glyph, string source,
        float x, float baseline, float size, Color foreground)
    {
        if (string.IsNullOrWhiteSpace(source)) return;
        var art = Read(font, glyph.Id, Math.Clamp((int)Math.Round(size * 96 / 72, MidpointRounding.AwayFromZero), 1, 65535), source.Contains('\ufe0e'));
        double scale = (double)size / font.UnitsPerEm;
        x += (float)(glyph.OffsetX * scale); baseline -= (float)(glyph.OffsetY * scale);
        if (art.Image is not null && art.Bitmap is not null)
        {
            var b = art.Bitmap;
            float sx = size / b.PpemX, sy = size / b.PpemY;
            canvas.DrawImage(art.Image, x + b.BearingX * sx, baseline - b.BearingY * sy, b.Width * sx, b.Height * sy);
            return;
        }
        if (art.Layers.Count == 0)
            throw new NotSupportedException("This emoji has no monochrome outline or supported colour artwork in the selected font.");
        foreach (var layer in art.Layers)
        {
            canvas.BeginPath();
            foreach (var contour in layer.Outline.Contours)
            {
                if (contour.Count < 2) continue;
                var points = new List<FontPoint>();
                for (int i = 0; i < contour.Count; i++)
                {
                    var a = contour[i]; var b = contour[(i + 1) % contour.Count];
                    points.Add(a);
                    if (!a.OnCurve && !b.OnCurve) points.Add(new((a.X + b.X) / 2, (a.Y + b.Y) / 2, true));
                }
                int start = points.FindIndex(p => p.OnCurve);
                if (start < 0) continue;
                FontPoint At(int i) => points[(start + i) % points.Count];
                float X(FontPoint p) => x + (float)(p.X * scale);
                float Y(FontPoint p) => baseline - (float)(p.Y * scale);
                canvas.MoveTo(X(At(0)), Y(At(0)));
                for (int i = 1; i < points.Count; i++)
                {
                    var p = At(i);
                    if (p.OnCurve) canvas.LineTo(X(p), Y(p));
                    else { var end = At(++i); canvas.QuadraticBezierTo(X(p), Y(p), X(end), Y(end)); }
                }
                canvas.ClosePath();
            }
            var c = layer.Colour;
            canvas.FillCurrentPath(layer.Foreground ? foreground : new Color(c.R, c.G, c.B, c.A).WithAlpha(c.A / 255f * foreground.ScA));
        }
    }
}
