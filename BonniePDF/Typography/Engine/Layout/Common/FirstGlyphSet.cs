// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;

namespace BonniePDF.Typography.Engine
{
    // The glyphs that can start a match in one lookup: the union of every subtable's first
    // coverage. A glyph outside it cannot be acted on by the lookup, so the lookup is charged
    // for it without reading its subtables. Per immutable layout table, built only from
    // coverage tables that validate exactly as the lookup's own coverage reads would.
    internal sealed class FirstGlyphSet
    {
        private ulong[] bits = Array.Empty<ulong>();

        internal bool Contains(int glyph)
        {
            int word = glyph >> 6;
            return glyph >= 0 && word < bits.Length && (bits[word] & (1UL << (glyph & 63))) != 0;
        }

        // Adds every glyph of a coverage table. Returns false when the table would make
        // OpenTypeLayout.Coverage throw for some glyph, so the caller does not filter.
        internal bool AddCoverage(BigEndianData data, int offset)
        {
            int format = data.U16(offset), count = data.U16(offset + 2);
            if (format != 1 && format != 2) return false;
            int stride = format == 1 ? 2 : 6;
            data.Check(offset + 4, count * stride);
            for (int i = 0; i < count; i++)
            {
                int record = offset + 4 + i * stride;
                int start = data.U16(record), end = format == 1 ? start : data.U16(record + 2);
                if (end < start) return false;
                Grow(end);
                for (int glyph = start; glyph <= end; glyph++) bits[glyph >> 6] |= 1UL << (glyph & 63);
            }
            return true;
        }

        // Adds every glyph listed in a coverage table to masks (grown as needed) as bit `bit`.
        // A glyph a table does not list cannot be found by its binary search, so a clear bit
        // proves the table does not cover the glyph. Returns false for a table AddCoverage rejects.
        internal static bool AddCoverageBit(BigEndianData data, int offset, ref uint[] masks, int bit)
        {
            int format = data.U16(offset), count = data.U16(offset + 2);
            if (format != 1 && format != 2) return false;
            int stride = format == 1 ? 2 : 6;
            data.Check(offset + 4, count * stride);
            for (int i = 0; i < count; i++)
            {
                int record = offset + 4 + i * stride;
                int start = data.U16(record), end = format == 1 ? start : data.U16(record + 2);
                if (end < start) return false;
                if (end >= masks.Length) Array.Resize(ref masks, end + 1);
                for (int glyph = start; glyph <= end; glyph++) masks[glyph] |= 1U << bit;
            }
            return true;
        }

        private void Grow(int glyph)
        {
            int words = (glyph >> 6) + 1;
            if (words > bits.Length) Array.Resize(ref bits, words);
        }
    }
}
