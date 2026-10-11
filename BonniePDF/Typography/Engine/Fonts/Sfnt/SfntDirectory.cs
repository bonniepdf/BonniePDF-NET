// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    internal sealed class SfntDirectory
    {
        internal readonly Dictionary<string, FontTable> Tables = new Dictionary<string, FontTable>(StringComparer.Ordinal);
        internal readonly int FaceCount;

        internal static int Count(byte[] bytes)
        {
            int count;
            Header(new BigEndianData(bytes), 0, out count, true);
            return count;
        }

        private static int Header(BigEndianData data, int faceIndex, out int count, bool countOnly = false)
        {
            count = 1;
            int offset = 0;
            if (data.Tag(0) == "ttcf")
            {
                uint version = data.U32(4);
                if (version != 0x10000 && version != 0x20000)
                    throw new NotSupportedException("Unsupported font collection version.");
                count = data.Offset32(8);
                if (count < 1 || count > 65535)
                    throw new InvalidDataException("Invalid font collection face count.");
                int end = 12 + count * 4;
                data.Check(12, count * 4 + (version == 0x20000 ? 12 : 0));
                for (int i = 0; i < count; i++)
                {
                    int directory = data.Offset32(12 + i * 4);
                    if (directory < end + (version == 0x20000 ? 12 : 0))
                        throw new InvalidDataException("Invalid font collection directory offset.");
                    data.Check(directory, 12);
                }
                if (version == 0x20000)
                {
                    uint tag = data.U32(end);
                    int length = data.Offset32(end + 4), signature = data.Offset32(end + 8);
                    if (tag != 0 && tag != 0x44534947)
                        throw new InvalidDataException("Invalid collection signature tag.");
                    if (tag == 0 && (length != 0 || signature != 0))
                        throw new InvalidDataException("Invalid empty collection signature.");
                    if (tag != 0)
                        data.Check(signature, length);
                }
                if (faceIndex >= 0 && faceIndex < count)
                    offset = data.Offset32(12 + faceIndex * 4);
            }
            if (faceIndex < 0 || faceIndex >= count)
                throw new ArgumentOutOfRangeException("faceIndex");
            if (countOnly && data.Tag(0) == "ttcf") return offset;
            uint sfnt = data.U32(offset);
            if (sfnt != 0x10000 && sfnt != 0x74727565 && sfnt != 0x4f54544f)
                throw new NotSupportedException("Use static TrueType or CFF 1 OpenType fonts, or CBDT bitmaps. WOFF is unsupported.");
            if (data.Tag(0) == "ttcf" && sfnt == 0x4f54544f) throw new NotSupportedException("CFF collections are unsupported; supply an individual OTF.");
            return offset;
        }

        internal SfntDirectory(byte[] bytes, int faceIndex)
        {
            var data = new BigEndianData(bytes);
            int count;
            int origin = Header(data, faceIndex, out count);
            FaceCount = count;
            int tables = data.U16(origin + 4);
            data.Check(origin + 12, tables * 16);
            for (int i = 0; i < tables; i++)
            {
                int p = origin + 12 + i * 16;
                string tag = data.Tag(p);
                int offset = data.Offset32(p + 8), length = data.Offset32(p + 12);
                data.Check(offset, length);
                if (Tables.ContainsKey(tag))
                    throw new InvalidDataException("Duplicate SFNT table tag.");
                Tables.Add(tag, new FontTable { Offset = offset, Length = length });
            }
        }
    }
}
