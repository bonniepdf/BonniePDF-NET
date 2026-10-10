// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System.IO;
using System.Collections.Generic;

namespace BonniePDF.Typography.Engine
{
    // Per immutable layout table, bounded independently of input text diversity.
    // Only successful reads are retained; callers still charge every lookup step.
    internal sealed class LayoutReadCache
    {
        private const int Capacity = 2048;
        private readonly Dictionary<long, int> coverage = new Dictionary<long, int>();
        private readonly Dictionary<long, int> classes = new Dictionary<long, int>();
        internal int Coverage(BigEndianData data, int offset, int glyph)
        {
            long key = ((long)offset << 32) | (uint)glyph;
            int value;
            if (coverage.TryGetValue(key, out value)) return value;
            value = OpenTypeLayout.Coverage(data, offset, glyph);
            if (coverage.Count < Capacity) coverage.Add(key, value);
            return value;
        }
        internal int Class(BigEndianData data, int offset, int glyph)
        {
            long key = ((long)offset << 32) | (uint)glyph;
            int value;
            if (classes.TryGetValue(key, out value)) return value;
            value = OpenTypeLayout.Class(data, offset, glyph);
            if (classes.Count < Capacity) classes.Add(key, value);
            return value;
        }
    }

    internal static class OpenTypeLayout
    {
        internal static int Offset16(BigEndianData data, int origin, int field)
        {
            int delta = data.U16(field);
            if (delta == 0)
                throw new InvalidDataException("Missing required OpenType offset.");
            int result = checked(origin + delta);
            data.Check(result, 2);
            return result;
        }

        internal static int Coverage(BigEndianData data, int offset, int glyph)
        {
            int format = data.U16(offset), count = data.U16(offset + 2);
            if (format != 1 && format != 2)
                throw new InvalidDataException("Invalid OpenType coverage format.");
            int stride = format == 1 ? 2 : 6;
            data.Check(offset + 4, count * stride);
            int low = 0, high = count - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2, record = offset + 4 + mid * stride;
                int start = data.U16(record), end = format == 1 ? start : data.U16(record + 2);
                if (end < start)
                    throw new InvalidDataException("Invalid OpenType coverage range.");
                if (glyph < start)
                    high = mid - 1;
                else if (glyph > end)
                    low = mid + 1;
                else
                    return format == 1 ? mid : data.U16(record + 4) + glyph - start;
            }
            return -1;
        }

        internal static int Class(BigEndianData data, int offset, int glyph)
        {
            int format = data.U16(offset);
            if (format == 1)
            {
                int start = data.U16(offset + 2), count = data.U16(offset + 4);
                data.Check(offset + 6, count * 2);
                return glyph >= start && glyph < start + count ? data.U16(offset + 6 + (glyph - start) * 2) : 0;
            }
            if (format != 2)
                throw new InvalidDataException("Invalid OpenType class definition.");
            int ranges = data.U16(offset + 2);
            data.Check(offset + 4, ranges * 6);
            int low = 0, high = ranges - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2, record = offset + 4 + mid * 6;
                int start = data.U16(record), end = data.U16(record + 2);
                if (end < start)
                    throw new InvalidDataException("Invalid OpenType class range.");
                if (glyph < start)
                    high = mid - 1;
                else if (glyph > end)
                    low = mid + 1;
                else
                    return data.U16(record + 4);
            }
            return 0;
        }
    }
}
