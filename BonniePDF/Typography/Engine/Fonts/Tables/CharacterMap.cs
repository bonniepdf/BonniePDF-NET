// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    internal sealed class CharacterMap
    {
        private readonly BigEndianData data;
        private readonly int glyphCount, selected, variation;
        private int work;

        private void Charge(int count = 1)
        {
            if (count < 0 || work > 1000000 - count)
                throw new InvalidDataException("cmap validation operation limit exceeded.");
            work += count;
        }

        private static int Priority(int platform, int encoding)
        {
            if (platform == 3 && encoding == 10) return 90;
            if (platform == 0 && encoding == 6) return 80;
            if (platform == 0 && encoding == 4) return 70;
            if (platform == 3 && encoding == 1) return 60;
            if (platform == 0 && encoding >= 0 && encoding <= 3) return 40 + encoding;
            return -1;
        }

        internal CharacterMap(byte[] bytes, int glyphCount)
        {
            data = new BigEndianData(bytes);
            this.glyphCount = glyphCount;
            if (data.U16(0) != 0)
                throw new NotSupportedException("Unsupported cmap version.");
            int count = data.U16(2), best = -1;
            selected = -1;
            variation = -1;
            data.Check(4, count * 8);
            var validated = new HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                Charge();
                int record = 4 + i * 8, platform = data.U16(record), encoding = data.U16(record + 2);
                int score = Priority(platform, encoding);
                if (score < 0 && !(platform == 0 && encoding == 5))
                    continue;
                int p = data.Offset32(record + 4);
                if (p < 4 + count * 8)
                    throw new InvalidDataException("Invalid cmap offset.");
                int format = data.U16(p);
                if (format != 0 && format != 4 && format != 6 && format != 12 && format != 13 && format != 14)
                    continue;
                if (validated.Add(p))
                    Validate(p, format);
                if (format == 14)
                {
                    if (platform == 0 && encoding == 5) variation = p;
                    continue;
                }
                if (score > best)
                {
                    best = score;
                    selected = p;
                }
            }
            if (selected < 0)
                throw new NotSupportedException("No supported Unicode cmap (formats 0, 4, 6, 12 or 13).");
        }

        private void Glyph(long id)
        {
            if (id < 0 || id >= glyphCount)
                throw new InvalidDataException("cmap references an invalid glyph.");
        }

        private static void Within(int p, int length, int end)
        {
            if (length < 0 || p < 0 || p > end - length)
                throw new InvalidDataException("Truncated cmap subtable.");
        }

        private void Validate(int p, int format)
        {
            int length = format == 14 ? data.Offset32(p + 2) :
                format >= 12 ? data.Offset32(p + 4) : data.U16(p + 2);
            data.Check(p, length);
            int end = p + length;
            Within(p, format == 14 ? 10 : format >= 12 ? 16 : 6, end);
            if (format == 0)
            {
                Within(p, 262, end);
                Charge(256);
                for (int i = 0; i < 256; i++) Glyph(data.U8(p + 6 + i));
            }
            else if (format == 6)
            {
                Within(p, 10, end);
                int first = data.U16(p + 6), count = data.U16(p + 8);
                if (first + count > 65536) throw new InvalidDataException("Invalid cmap format 6 range.");
                Within(p + 10, count * 2, end);
                Charge(count);
                for (int i = 0; i < count; i++) Glyph(data.U16(p + 10 + i * 2));
            }
            else if (format == 4)
            {
                Within(p, 16, end);
                int n = data.U16(p + 6) / 2;
                if (n == 0 || (data.U16(p + 6) & 1) != 0) throw new InvalidDataException("Invalid cmap format 4 segments.");
                Within(p, 16 + n * 8, end);
                int ends = p + 14, starts = ends + n * 2 + 2, deltas = starts + n * 2, ranges = deltas + n * 2;
                if (data.U16(ends + n * 2) != 0 || data.U16(ends + (n - 1) * 2) != 65535)
                    throw new InvalidDataException("Invalid cmap format 4 sentinel/padding.");
                int previous = -1;
                for (int i = 0; i < n; i++)
                {
                    int start = data.U16(starts + i * 2), last = data.U16(ends + i * 2), range = data.U16(ranges + i * 2);
                    if (start > last || start <= previous || (range & 1) != 0)
                        throw new InvalidDataException("Unsorted or invalid cmap format 4 segments.");
                    previous = last;
                    if (range != 0)
                    {
                        int address = ranges + i * 2 + range;
                        if (address < ranges + n * 2) throw new InvalidDataException("Invalid cmap glyph address.");
                        Within(address, (last - start + 1) * 2, end);
                    }
                    Charge(last - start + 1);
                    for (int cp = start; cp <= last; cp++)
                    {
                        int id = range == 0 ? (cp + data.I16(deltas + i * 2)) & 65535 : data.U16(ranges + i * 2 + range + (cp - start) * 2);
                        if (range != 0 && id != 0) id = (id + data.I16(deltas + i * 2)) & 65535;
                        Glyph(id);
                    }
                }
            }
            else if (format == 12 || format == 13)
            {
                if (data.U16(p + 2) != 0) throw new InvalidDataException("Invalid cmap reserved field.");
                int count = data.Offset32(p + 12);
                if (count > (length - 16) / 12) throw new InvalidDataException("Invalid cmap groups.");
                Charge(count);
                long previous = -1;
                for (int i = 0; i < count; i++)
                {
                    int g = p + 16 + i * 12;
                    uint start = data.U32(g), last = data.U32(g + 4), id = data.U32(g + 8);
                    if (start > last || last > 0x10ffff || start <= previous) throw new InvalidDataException("Unsorted or invalid cmap groups.");
                    previous = last;
                    Glyph(id);
                    Glyph(format == 12 ? id + (long)last - start : id);
                }
            }
            else
                ValidateVariation(p, end);
        }

        private int U24(int p) { return data.U8(p) * 65536 + data.U16(p + 1); }

        private void ValidateVariation(int p, int end)
        {
            int count = data.Offset32(p + 6);
            if (count > (end - p - 10) / 11) throw new InvalidDataException("Truncated variation selector records.");
            Charge(count);
            int prior = -1;
            for (int i = 0; i < count; i++)
            {
                int r = p + 10 + i * 11, selector = U24(r);
                if (selector <= prior || !((selector >= 0xfe00 && selector <= 0xfe0f) || (selector >= 0xe0100 && selector <= 0xe01ef)))
                    throw new InvalidDataException("Invalid variation selector.");
                prior = selector;
                for (int kind = 0; kind < 2; kind++)
                {
                    int relative = data.Offset32(r + 3 + kind * 4);
                    if (relative == 0) continue;
                    if (relative < 10 + count * 11 || relative > end - p - 4) throw new InvalidDataException("Invalid variation mapping offset.");
                    int mapping = p + relative, entries = data.Offset32(mapping), stride = kind == 0 ? 4 : 5;
                    if (entries > (end - mapping - 4) / stride) throw new InvalidDataException("Truncated variation mappings.");
                    Charge(entries);
                    int previous = -1;
                    for (int j = 0; j < entries; j++)
                    {
                        int m = mapping + 4 + j * stride, first = U24(m), last = kind == 0 ? first + data.U8(m + 3) : first;
                        if (first <= previous || last > 0x10ffff) throw new InvalidDataException("Invalid variation ranges.");
                        previous = last;
                        if (kind == 1) Glyph(data.U16(m + 3));
                    }
                }
            }
        }

        internal int Map(int scalar)
        {
            if (scalar < 0 || scalar > 0x10ffff || (scalar >= 0xd800 && scalar <= 0xdfff)) throw new ArgumentOutOfRangeException("codePoint");
            int p = selected, format = data.U16(p);
            if (format == 0) return scalar < 256 ? data.U8(p + 6 + scalar) : 0;
            if (format == 6)
            {
                int index = scalar - data.U16(p + 6);
                return index >= 0 && index < data.U16(p + 8) ? data.U16(p + 10 + index * 2) : 0;
            }
            if (format == 12 || format == 13)
            {
                int low = 0, high = data.Offset32(p + 12) - 1;
                while (low <= high)
                {
                    int middle = low + (high - low) / 2, g = p + 16 + middle * 12;
                    uint start = data.U32(g), end = data.U32(g + 4);
                    if (scalar < start) high = middle - 1;
                    else if (scalar > end) low = middle + 1;
                    else return (int)(data.U32(g + 8) + (format == 12 ? scalar - start : 0));
                }
                return 0;
            }
            if (scalar > 65535) return 0;
            int n = data.U16(p + 6) / 2, ends = p + 14, starts = ends + n * 2 + 2, deltas = starts + n * 2, ranges = deltas + n * 2;
            int left = 0, right = n - 1;
            while (left <= right)
            {
                int middle = left + (right - left) / 2;
                if (scalar > data.U16(ends + middle * 2)) left = middle + 1;
                else if (scalar < data.U16(starts + middle * 2)) right = middle - 1;
                else
                {
                    int delta = data.I16(deltas + middle * 2), range = data.U16(ranges + middle * 2);
                    if (range == 0) return (scalar + delta) & 65535;
                    int id = data.U16(ranges + middle * 2 + range + (scalar - data.U16(starts + middle * 2)) * 2);
                    return id == 0 ? 0 : (id + delta) & 65535;
                }
            }
            return 0;
        }

        internal int MapVariation(int scalar, int selector, out bool consumed)
        {
            int original = Map(scalar);
            consumed = false;
            if (variation < 0) return original;
            int p = variation, count = data.Offset32(p + 6);
            for (int i = 0; i < count; i++)
            {
                int r = p + 10 + i * 11;
                if (U24(r) != selector) continue;
                for (int kind = 1; kind >= 0; kind--)
                {
                    int relative = data.Offset32(r + 3 + kind * 4);
                    if (relative == 0) continue;
                    int mapping = p + relative, entries = data.Offset32(mapping), stride = kind == 0 ? 4 : 5;
                    int low = 0, high = entries - 1;
                    while (low <= high)
                    {
                        int middle = low + (high - low) / 2, m = mapping + 4 + middle * stride, first = U24(m);
                        int last = kind == 0 ? first + data.U8(m + 3) : first;
                        if (scalar < first) high = middle - 1;
                        else if (scalar > last) low = middle + 1;
                        else { consumed = true; return kind == 0 ? original : data.U16(m + 3); }
                    }
                }
                break;
            }
            return original;
        }
    }
}
