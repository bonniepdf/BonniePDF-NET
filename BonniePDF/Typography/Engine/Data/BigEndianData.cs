// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.IO;
using System.Text;
namespace BonniePDF.Typography.Engine
{
    // All offsets are checked against the containing OpenType table, not the file.
    internal sealed class BigEndianData
    {
        internal readonly byte[] Bytes;
        internal BigEndianData(byte[] bytes)
        {
            Bytes = bytes ?? throw new ArgumentNullException("bytes");
        }
        internal int Length
        {
            get
            {
                return Bytes.Length;
            }
        }
        internal void Check(int offset, int count)
        {
            if (offset < 0 || count < 0 || offset > Bytes.Length - count)
                throw new InvalidDataException("Truncated OpenType table.");
        }
        internal int U8(int offset)
        {
            Check(offset, 1);
            return Bytes[offset];
        }
        internal int I8(int offset)
        {
            return (sbyte)U8(offset);
        }
        internal int U16(int offset)
        {
            Check(offset, 2);
            return Bytes[offset] * 256 + Bytes[offset + 1];
        }
        internal int I16(int offset)
        {
            return (short)U16(offset);
        }
        internal uint U32(int offset)
        {
            return ((uint)U16(offset) << 16) | (uint)U16(offset + 2);
        }
        internal int Offset32(int offset)
        {
            uint value = U32(offset);
            if (value > int.MaxValue)
                throw new InvalidDataException("OpenType offset too large.");
            return (int)value;
        }
        internal int Relative16(int origin, int offset)
        {
            return checked(origin + U16(offset));
        }
        internal string Tag(int offset)
        {
            Check(offset, 4);
            return Encoding.ASCII.GetString(Bytes, offset, 4);
        }
        internal byte[] Copy(int offset, int count)
        {
            Check(offset, count);
            var result = new byte[count];
            Buffer.BlockCopy(Bytes, offset, result, 0, count);
            return result;
        }
    }
}
