// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    // Per immutable layout table. Populate only at the original lookup access point.
    internal sealed class LookupHeaderCache
    {
        private readonly BigEndianData data;
        private readonly GlyphFilter filter;
        private readonly int list, count;
        private readonly bool positioning;
        private readonly Header[] entries;

        internal LookupHeaderCache(BigEndianData data, int list, GlyphFilter filter, bool positioning)
        {
            this.data = data; this.list = list; this.filter = filter; this.positioning = positioning;
            count = data.U16(list);
            entries = new Header[Math.Min(count, 256)];
        }

        internal int Count { get { return count; } }

        internal Header Get(int index)
        {
            if (index >= count || (positioning && index < 0))
                throw new InvalidDataException(positioning ? "Invalid GPOS lookup index." : "Invalid GSUB lookup index.");
            if ((uint)index < (uint)entries.Length && entries[index].Valid) return entries[index];
            int offset = positioning ? OpenTypeLayout.Offset16(data, list, list + 2 + index * 2) : data.Relative16(list, list + 2 + index * 2);
            int type = data.U16(offset), flags = data.U16(offset + 2), subtables = data.U16(offset + 4);
            data.Check(offset + 6, subtables * 2);
            int markSet = (flags & 16) != 0 ? data.U16(offset + 6 + subtables * 2) : -1;
            filter.Validate(flags, markSet);
            var header = new Header(offset, type, flags, subtables, markSet);
            if ((uint)index < (uint)entries.Length) entries[index] = header;
            return header;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal struct Header
        {
            internal readonly int Offset, Type, Flags, Count, MarkSet;
            internal readonly bool Valid;
            internal Header(int offset, int type, int flags, int count, int markSet)
            {
                Offset = offset; Type = type; Flags = flags; Count = count; MarkSet = markSet; Valid = true;
            }
        }
    }
}
