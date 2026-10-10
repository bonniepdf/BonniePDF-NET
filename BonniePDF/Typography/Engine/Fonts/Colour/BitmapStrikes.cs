// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    // Parses encoded records only. PNG decoding and resampling are consumer work.
    internal sealed class BitmapStrikes
    {
        private readonly BigEndianData locations, pixels;
        private readonly Dictionary<long, BitmapGlyphData> cache = new Dictionary<long, BitmapGlyphData>();
        private readonly ColourReadLimits limits;
        private readonly int glyphCount;
        private long cachedBytes, operations;
        internal readonly IReadOnlyList<BitmapStrikeData> Strikes;
        private void Tick()
        {
            if (++operations > limits.MaxOperations) throw new InvalidDataException("Bitmap table operation limit exceeded.");
        }
        internal BitmapStrikes(byte[] cblc, byte[] cbdt, int glyphCount, ColourReadLimits limits)
        {
            this.glyphCount = glyphCount; this.limits = limits;
            if (cblc == null) throw new InvalidDataException("CBDT requires CBLC.");
            if (cbdt == null) throw new InvalidDataException("CBLC requires CBDT.");
            locations = new BigEndianData((byte[])cblc.Clone()); pixels = new BigEndianData((byte[])cbdt.Clone());
            locations.Check(0, 8); pixels.Check(0, 4);
            if (locations.U32(0) != 0x30000 || pixels.U32(0) != 0x30000)
                throw new NotSupportedException("Only version 3 colour bitmap tables are supported.");
            int count = locations.Offset32(4), records = 0;
            if (count > limits.MaxStrikes) throw new InvalidDataException("Bitmap strike limit exceeded.");
            locations.Check(8, checked(count * 48));
            var strikes = new BitmapStrikeData[count];
            for (int i = 0; i < count; i++)
            {
                Tick(); int p = 8 + i * 48, first = locations.U16(p + 40), last = locations.U16(p + 42);
                int x = locations.U8(p + 44), y = locations.U8(p + 45), depth = locations.U8(p + 46), flags = locations.U8(p + 47);
                if (first > last || last >= glyphCount) throw new InvalidDataException("Invalid bitmap strike glyph range.");
                if (x == 0 || y == 0) throw new InvalidDataException("Zero bitmap ppem.");
                if (depth != 32 || (flags != 1 && flags != 2 && flags != 3))
                    throw new NotSupportedException("Unsupported colour bitmap bit depth or flags.");
                int array = locations.Offset32(p), size = locations.Offset32(p + 4), subtables = locations.Offset32(p + 8);
                if (subtables > limits.MaxRecords - records) throw new InvalidDataException("Bitmap record limit exceeded.");
                records += subtables;
                if (subtables != 0 && array < 8 + count * 48) throw new InvalidDataException("Bitmap index overlaps strike headers.");
                locations.Check(array, size);
                if ((long)subtables * 8 > size) throw new InvalidDataException("Truncated bitmap index array.");
                var strike = new BitmapStrikeData(i, first, last, x, y, depth, flags, array, checked(array + size), subtables);
                int previous = -1;
                for (int j = 0; j < subtables; j++)
                {
                    Tick(); int r = array + j * 8, a = locations.U16(r), b = locations.U16(r + 2);
                    if (a <= previous || a > b || a < first || b > last) throw new InvalidDataException("Invalid bitmap index glyph order or range.");
                    previous = b;
                    int table = checked(array + locations.Offset32(r + 4));
                    if (table < array + subtables * 8) throw new InvalidDataException("Bitmap subtable overlaps index array.");
                    ValidateIndex(strike, table, a, b);
                }
                strikes[i] = strike;
            }
            Strikes = Array.AsReadOnly(strikes);
        }
        private static void CheckIndex(BitmapStrikeData strike, int offset, int count)
        {
            if (offset < strike.ArrayOffset || count < 0 || offset > strike.ArrayEnd - count)
                throw new InvalidDataException("Truncated bitmap index subtable.");
        }
        private void ValidateIndex(BitmapStrikeData strike, int table, int first, int last)
        {
            CheckIndex(strike, table, 8);
            int format = locations.U16(table), imageFormat = locations.U16(table + 2), origin = locations.Offset32(table + 4);
            if (imageFormat < 17 || imageFormat > 19 || (imageFormat == 19 && format != 2 && format != 5))
                throw new NotSupportedException("Only PNG bitmap formats 17, 18 and 19 with defined metrics are supported.");
            if (origin < 4 || origin > pixels.Length) throw new InvalidDataException("Invalid bitmap data origin.");
            if (format == 1 || format == 3)
            {
                int width = format == 1 ? 4 : 2, count = last - first + 2, previous = -1;
                CheckIndex(strike, table + 8, count * width);
                for (int i = 0; i < count; i++)
                {
                    Tick(); int value = width == 4 ? locations.Offset32(table + 8 + i * width) : locations.U16(table + 8 + i * width);
                    if (value < previous || value > pixels.Length - origin) throw new InvalidDataException("Reversed or outside bitmap offsets.");
                    previous = value;
                }
            }
            else if (format == 2 || format == 5)
            {
                CheckIndex(strike, table, format == 2 ? 20 : 24);
                int size = locations.Offset32(table + 8), count = format == 2 ? last - first + 1 : locations.Offset32(table + 20);
                if (count > limits.MaxRecords) throw new InvalidDataException("Bitmap sparse record limit exceeded.");
                if (size == 0 || (long)size * count > pixels.Length - origin) throw new InvalidDataException("Invalid fixed bitmap size.");
                if (format == 5)
                {
                    CheckIndex(strike, table + 24, count * 2); int previous = -1;
                    for (int i = 0; i < count; i++)
                    {
                        Tick(); int id = locations.U16(table + 24 + i * 2);
                        if (id <= previous || id < first || id > last) throw new InvalidDataException("Invalid sparse bitmap glyph order or range.");
                        previous = id;
                    }
                }
            }
            else if (format == 4)
            {
                CheckIndex(strike, table, 12); int count = locations.Offset32(table + 8);
                if (count > limits.MaxRecords) throw new InvalidDataException("Bitmap sparse record limit exceeded.");
                CheckIndex(strike, table + 12, checked((count + 1) * 4));
                int previous = -1, previousOffset = -1;
                for (int i = 0; i <= count; i++)
                {
                    Tick(); int p = table + 12 + i * 4, id = locations.U16(p), offset = locations.U16(p + 2);
                    if (i < count && (id <= previous || id < first || id > last)) throw new InvalidDataException("Invalid sparse bitmap glyph order or range.");
                    if (offset < previousOffset || offset > pixels.Length - origin) throw new InvalidDataException("Reversed or outside bitmap offsets.");
                    previous = id; previousOffset = offset;
                }
            }
            else throw new NotSupportedException("Unsupported CBLC index format.");
        }
        private bool Locate(BitmapStrikeData strike, int glyph, out int image, out int length, out int metrics, out int imageFormat)
        {
            image = length = imageFormat = 0; metrics = -1;
            int low = 0, high = strike.SubtableCount - 1;
            while (low <= high)
            {
                Tick(); int mid = low + (high - low) / 2, r = strike.ArrayOffset + mid * 8;
                int first = locations.U16(r), last = locations.U16(r + 2);
                if (glyph < first) { high = mid - 1; continue; }
                if (glyph > last) { low = mid + 1; continue; }
                int table = checked(strike.ArrayOffset + locations.Offset32(r + 4)), format = locations.U16(table), index = glyph - first;
                imageFormat = locations.U16(table + 2); int origin = locations.Offset32(table + 4), start, end;
                if (format == 4 || format == 5)
                {
                    int count = locations.Offset32(table + (format == 4 ? 8 : 20)), step = format == 4 ? 4 : 2;
                    int list = table + (format == 4 ? 12 : 24); index = -1;
                    int a = 0, b = count - 1;
                    while (a <= b)
                    {
                        Tick(); int m = a + (b - a) / 2, id = locations.U16(list + m * step);
                        if (id < glyph) a = m + 1;
                        else if (id > glyph) b = m - 1;
                        else { index = m; break; }
                    }
                    if (index < 0) return false;
                }
                if (format == 1 || format == 3)
                {
                    int width = format == 1 ? 4 : 2, p = table + 8 + index * width;
                    start = width == 4 ? locations.Offset32(p) : locations.U16(p);
                    end = width == 4 ? locations.Offset32(p + width) : locations.U16(p + width);
                }
                else if (format == 4)
                {
                    start = locations.U16(table + 14 + index * 4); end = locations.U16(table + 18 + index * 4);
                }
                else
                {
                    int size = locations.Offset32(table + 8); start = checked(index * size); end = checked(start + size); metrics = table + 12;
                }
                image = checked(origin + start); length = end - start;
                return length != 0;
            }
            return false;
        }
        internal BitmapGlyphData Read(int glyph, double size)
        {
            if (glyph < 0 || glyph >= glyphCount) throw new ArgumentOutOfRangeException("glyph");
            if (double.IsNaN(size) || double.IsInfinity(size) || size <= 0 || size > 65535) throw new ArgumentOutOfRangeException("size");
            operations = 0; BitmapStrikeData chosen = null;
            double distance = double.MaxValue; int image = 0, length = 0, metrics = -1, format = 0;
            foreach (var strike in Strikes)
            {
                Tick(); if (glyph < strike.FirstGlyph || glyph > strike.LastGlyph) continue;
                int at, count, m, f;
                if (!Locate(strike, glyph, out at, out count, out m, out f)) continue;
                double difference = Math.Abs(strike.PpemY - size);
                if (difference < distance || (difference == distance && (chosen == null || strike.PpemY > chosen.PpemY)))
                {
                    chosen = strike; distance = difference; image = at; length = count; metrics = m; format = f;
                }
            }
            if (chosen == null) return null;
            long key = ((long)chosen.Index << 16) | (uint)glyph;
            BitmapGlyphData result;
            if (cache.TryGetValue(key, out result)) return result;
            int header = format == 17 ? 5 : format == 18 ? 8 : 0;
            if (length < header + 4) throw new InvalidDataException("Truncated bitmap image record.");
            int p = format == 19 ? metrics : image;
            BigEndianData source = format == 19 ? locations : pixels;
            int height = source.U8(p), width = source.U8(p + 1), x = source.I8(p + 2), y = source.I8(p + 3), advance = source.U8(p + 4);
            bool vertical = format != 17;
            int vx = vertical ? source.I8(p + 5) : 0, vy = vertical ? source.I8(p + 6) : 0, va = vertical ? source.U8(p + 7) : 0;
            int pngLength = pixels.Offset32(image + header), payload = image + header + 4;
            if (pngLength > limits.MaxEncodedBitmapBytes) throw new InvalidDataException("Encoded bitmap size limit exceeded.");
            if (pngLength < 8 || pngLength > length - header - 4) throw new InvalidDataException("PNG exceeds its bitmap record or has no signature.");
            const string signature = "\u0089PNG\r\n\u001a\n";
            for (int i = 0; i < signature.Length; i++)
                if (pixels.U8(payload + i) != signature[i]) throw new InvalidDataException("Invalid PNG bitmap signature.");
            if (width == 0 || height == 0) throw new InvalidDataException("Zero bitmap dimensions.");
            result = new BitmapGlyphData(pixels.Copy(payload, pngLength), width, height, x, y, advance, vx, vy, va, vertical, chosen, format);
            if (cache.Count >= 64 || cachedBytes + pngLength > limits.MaxEncodedBitmapBytes) { cache.Clear(); cachedBytes = 0; }
            cache.Add(key, result); cachedBytes += pngLength;
            return result;
        }
    }
}
