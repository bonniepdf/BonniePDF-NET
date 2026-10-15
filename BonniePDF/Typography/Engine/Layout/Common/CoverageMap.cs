// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;

namespace BonniePDF.Typography.Engine
{
    // A coverage table read once into a bitset with a running count per 64 glyphs, so Index
    // is a bit test and a population count instead of a binary search. Built only from tables
    // for which OpenTypeLayout.Coverage cannot throw and returns the glyph's rank among the
    // covered glyphs: glyph arrays strictly increasing, ranges ordered and disjoint with
    // consecutive start indices. Index then equals OpenTypeLayout.Coverage for every glyph.
    internal sealed class CoverageMap
    {
        private readonly ulong[] bits;
        private readonly int[] before;

        private CoverageMap(ulong[] bits, int[] before)
        {
            this.bits = bits; this.before = before;
        }

        // The number of covered glyphs, one more than the largest index.
        internal int Count { get { return bits.Length == 0 ? 0 : before[bits.Length - 1] + PopCount(bits[bits.Length - 1]); } }

        internal int Index(int glyph)
        {
            int word = glyph >> 6;
            if (glyph < 0 || word >= bits.Length) return -1;
            ulong value = bits[word], bit = 1UL << (glyph & 63);
            if ((value & bit) == 0) return -1;
            return before[word] + PopCount(value & (bit - 1));
        }

        internal bool Covers(int glyph)
        {
            int word = glyph >> 6;
            return glyph >= 0 && word < bits.Length && (bits[word] & (1UL << (glyph & 63))) != 0;
        }

        // The map for the coverage table at offset, or null when the table is not one Index can
        // answer exactly (the caller then reads it with OpenTypeLayout.Coverage).
        internal static CoverageMap TryBuild(BigEndianData data, int offset)
        {
            try
            {
                int format = data.U16(offset), count = data.U16(offset + 2);
                if (format != 1 && format != 2) return null;
                int stride = format == 1 ? 2 : 6;
                data.Check(offset + 4, count * stride);
                int last = -1, rank = 0;
                for (int i = 0; i < count; i++)
                {
                    int record = offset + 4 + i * stride;
                    int start = data.U16(record), end = format == 1 ? start : data.U16(record + 2);
                    if (end < start || start <= last) return null;
                    if (format == 2 && data.U16(record + 4) != rank) return null;
                    rank += end - start + 1;
                    last = end;
                }
                var bits = new ulong[(last >> 6) + 1];
                for (int i = 0; i < count; i++)
                {
                    int record = offset + 4 + i * stride;
                    int start = data.U16(record), end = format == 1 ? start : data.U16(record + 2);
                    for (int glyph = start; glyph <= end; glyph++) bits[glyph >> 6] |= 1UL << (glyph & 63);
                }
                var before = new int[bits.Length];
                for (int word = 1; word < bits.Length; word++) before[word] = before[word - 1] + PopCount(bits[word - 1]);
                return new CoverageMap(bits, before);
            }
            catch (Exception) { return null; }
        }

        internal static int PopCount(ulong value)
        {
            value -= (value >> 1) & 0x5555555555555555UL;
            value = (value & 0x3333333333333333UL) + ((value >> 2) & 0x3333333333333333UL);
            value = (value + (value >> 4)) & 0x0f0f0f0f0f0f0f0fUL;
            return (int)((value * 0x0101010101010101UL) >> 56);
        }
    }

    // A class definition read once into an array of classes, so Class is an index instead of
    // a binary search. Built only from tables for which OpenTypeLayout.Class cannot throw and
    // ranges are ordered and disjoint, and only when the glyphs it spans are few enough to
    // hold; Class then equals OpenTypeLayout.Class for every glyph.
    internal sealed class ClassMap
    {
        private const int MaxSpan = 16384;
        private readonly int first;
        private readonly ushort[] classes;

        private ClassMap(int first, ushort[] classes)
        {
            this.first = first; this.classes = classes;
        }

        internal int Class(int glyph)
        {
            int index = glyph - first;
            return index >= 0 && index < classes.Length ? classes[index] : 0;
        }

        internal static ClassMap TryBuild(BigEndianData data, int offset)
        {
            try
            {
                int format = data.U16(offset);
                if (format == 1)
                {
                    int start = data.U16(offset + 2), count = data.U16(offset + 4);
                    data.Check(offset + 6, count * 2);
                    var values = new ushort[count];
                    for (int i = 0; i < count; i++) values[i] = (ushort)data.U16(offset + 6 + i * 2);
                    return new ClassMap(start, values);
                }
                if (format != 2) return null;
                int ranges = data.U16(offset + 2);
                data.Check(offset + 4, ranges * 6);
                if (ranges == 0) return new ClassMap(0, Array.Empty<ushort>());
                int last = -1, low = data.U16(offset + 4);
                for (int i = 0; i < ranges; i++)
                {
                    int record = offset + 4 + i * 6, start = data.U16(record), end = data.U16(record + 2);
                    if (end < start || start <= last) return null;
                    last = end;
                }
                if (last - low + 1 > MaxSpan) return null;
                var classes = new ushort[last - low + 1];
                for (int i = 0; i < ranges; i++)
                {
                    int record = offset + 4 + i * 6, start = data.U16(record), end = data.U16(record + 2);
                    ushort value = (ushort)data.U16(record + 4);
                    for (int glyph = start; glyph <= end; glyph++) classes[glyph - low] = value;
                }
                return new ClassMap(low, classes);
            }
            catch (Exception) { return null; }
        }
    }
}
