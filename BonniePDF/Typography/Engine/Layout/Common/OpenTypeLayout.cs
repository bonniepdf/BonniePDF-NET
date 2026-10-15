// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System.IO;
using System.Collections.Generic;

namespace BonniePDF.Typography.Engine
{
    // Coverage and class reads through maps built once per table (CoverageMap, ClassMap), so
    // retained memory is bounded by the font's tables, not by the text. A table no map can
    // answer exactly is read directly each time. Reads charge nothing; callers charge every
    // lookup step.
    internal sealed class LayoutReadCache
    {
        private readonly Dictionary<int, CoverageMap> coverage = new Dictionary<int, CoverageMap>();
        private readonly Dictionary<int, ClassMap> classes = new Dictionary<int, ClassMap>();

        // The coverage map for the table at offset, or null when the table must be read directly.
        internal CoverageMap CoverageMap(BigEndianData data, int offset)
        {
            CoverageMap map;
            if (!coverage.TryGetValue(offset, out map))
            {
                map = Engine.CoverageMap.TryBuild(data, offset);
                coverage.Add(offset, map);
            }
            return map;
        }

        internal int Coverage(BigEndianData data, int offset, int glyph)
        {
            var map = CoverageMap(data, offset);
            return map != null ? map.Index(glyph) : OpenTypeLayout.Coverage(data, offset, glyph);
        }

        internal ClassMap ClassMap(BigEndianData data, int offset)
        {
            ClassMap map;
            if (!classes.TryGetValue(offset, out map))
            {
                map = Engine.ClassMap.TryBuild(data, offset);
                classes.Add(offset, map);
            }
            return map;
        }

        internal int Class(BigEndianData data, int offset, int glyph)
        {
            var map = ClassMap(data, offset);
            return map != null ? map.Class(glyph) : OpenTypeLayout.Class(data, offset, glyph);
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
