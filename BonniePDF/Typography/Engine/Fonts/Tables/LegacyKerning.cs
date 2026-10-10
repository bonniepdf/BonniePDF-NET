// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    internal sealed class LegacyKerning
    {
        private readonly List<LegacyKerningSubtable> subtables = new List<LegacyKerningSubtable>();
        internal bool HasHorizontalKerning { get { return subtables.Count != 0; } }
        internal int WorkPerPair { get { return subtables.Count; } }

        internal LegacyKerning(byte[] bytes, int glyphCount)
        {
            if (bytes == null) return;
            var data = new BigEndianData(bytes);
            if (data.U16(0) != 0) throw new NotSupportedException("Apple kern extensions are unsupported.");
            int count = data.U16(2), p = 4, work = 0;
            for (int i = 0; i < count; i++)
            {
                int version = data.U16(p), length = data.U16(p + 2), coverage = data.U16(p + 4);
                if (version != 0 || length < 6) throw new InvalidDataException("Invalid kern subtable header.");
                data.Check(p, length);
                if ((coverage & 0xff) == 1 || (coverage & 0xff) == 9)
                    subtables.Add(new LegacyKerningSubtable(data.Copy(p, length), glyphCount, ref work));
                else if ((coverage & 1) != 0)
                    throw new NotSupportedException("Minimum, cross-stream or reserved kern flags are unsupported.");
                p += length;
            }
        }

        internal int Get(int left, int right)
        {
            int total = 0;
            foreach (var subtable in subtables)
            {
                int value;
                if (subtable.TryGet(left, right, out value))
                    total = subtable.Override ? value : checked(total + value);
            }
            return total;
        }

        internal void Split(int left, int right, out int first, out int second)
        {
            first = second = 0;
            foreach (var subtable in subtables)
            {
                int value;
                if (!subtable.TryGet(left, right, out value)) continue;
                int half = value / 2;
                if (value < 0 && (value & 1) != 0) half--;
                if (subtable.Override) { first = half; second = value - half; }
                else { first = checked(first + half); second = checked(second + value - half); }
            }
        }
    }
}
