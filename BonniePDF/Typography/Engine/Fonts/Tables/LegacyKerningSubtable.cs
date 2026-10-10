// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    internal sealed class LegacyKerningSubtable
    {
        private readonly Dictionary<uint, int> pairs;
        private readonly BigEndianData data;
        private readonly int[] leftClasses, rightClasses;
        private readonly int leftFirst, rightFirst, array;
        internal readonly bool Override;

        private static void Charge(ref int work, int count)
        {
            if (count < 0 || work > 1000000 - count) throw new InvalidDataException("kern validation operation limit exceeded.");
            work += count;
        }

        internal LegacyKerningSubtable(byte[] bytes, int glyphCount, ref int work)
        {
            data = new BigEndianData(bytes);
            int coverage = data.U16(4), format = coverage >> 8;
            Override = (coverage & 8) != 0;
            if (format == 0)
            {
                if (bytes.Length < 14) throw new InvalidDataException("Truncated kern format 0 header.");
                int count = data.U16(6);
                if (count > (bytes.Length - 14) / 6) throw new InvalidDataException("Truncated kern pairs.");
                Charge(ref work, count);
                pairs = new Dictionary<uint, int>();
                uint previous = 0;
                for (int i = 0; i < count; i++)
                {
                    int p = 14 + i * 6, left = data.U16(p), right = data.U16(p + 2);
                    uint key = ((uint)left << 16) | (uint)right;
                    if (left >= glyphCount || right >= glyphCount || (i > 0 && key <= previous))
                        throw new InvalidDataException("Unsorted or invalid kern pair.");
                    previous = key;
                    pairs.Add(key, data.I16(p + 4));
                }
            }
            else if (format == 2)
            {
                data.Check(0, 14);
                int width = data.U16(6), left = data.U16(8), right = data.U16(10);
                array = data.U16(12);
                if (width < 2 || (width & 1) != 0 || left < 14 || right < 14 || array < 14)
                    throw new InvalidDataException("Invalid kern format 2 offsets or row width.");
                leftFirst = data.U16(left); rightFirst = data.U16(right);
                leftClasses = ReadClasses(left, leftFirst, glyphCount, width, true, ref work);
                rightClasses = ReadClasses(right, rightFirst, glyphCount, width, false, ref work);
                int maximum = 0;
                foreach (int value in leftClasses) maximum = Math.Max(maximum, value);
                data.Check(array, maximum + width);
                Charge(ref work, (maximum + width) / 2);
                for (int column = 0; column < width; column += 2)
                    if (data.I16(array + column) != 0) throw new InvalidDataException("Nonzero kern class zero row.");
                for (int row = 0; row <= maximum; row += width)
                    if (data.I16(array + row) != 0) throw new InvalidDataException("Nonzero kern class zero column.");
            }
            else throw new NotSupportedException("Only horizontal kern formats 0 and 2 are supported.");
        }

        private int[] ReadClasses(int p, int first, int glyphCount, int width, bool left, ref int work)
        {
            int count = data.U16(p + 2);
            if (first + count > glyphCount) throw new InvalidDataException("Invalid kern class glyph range.");
            data.Check(p + 4, count * 2); Charge(ref work, count);
            var values = new int[count];
            for (int i = 0; i < count; i++)
            {
                int value = data.U16(p + 4 + i * 2);
                if ((left && value % width != 0) || (!left && ((value & 1) != 0 || value >= width)))
                    throw new InvalidDataException("Invalid kern class offset.");
                values[i] = value;
            }
            return values;
        }

        internal bool TryGet(int left, int right, out int value)
        {
            if (pairs != null) return pairs.TryGetValue(((uint)left << 16) | (uint)right, out value);
            int row = left - leftFirst, column = right - rightFirst;
            row = row >= 0 && row < leftClasses.Length ? leftClasses[row] : 0;
            column = column >= 0 && column < rightClasses.Length ? rightClasses[column] : 0;
            value = data.I16(array + row + column);
            return true;
        }
    }
}
