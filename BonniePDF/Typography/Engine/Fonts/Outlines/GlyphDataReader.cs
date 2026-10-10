// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    // Restricts every read to the current glyph's byte range.
    internal sealed class GlyphDataReader
    {
        private readonly byte[] bytes;
        private readonly int end;
        private int position;

        public GlyphDataReader(byte[] bytes, int start, int end)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            if (start < 0 || end < start || end > bytes.Length)
                throw new InvalidDataException("Invalid glyph range.");
            this.bytes = bytes;
            position = start;
            this.end = end;
        }

        private int Take(int count)
        {
            if (count < 0 || position > end - count)
                throw new InvalidDataException("Truncated glyph data.");
            int offset = position;
            position += count;
            return offset;
        }

        public int Byte()
        {
            return bytes[Take(1)];
        }
        public int SignedByte()
        {
            return (sbyte)Byte();
        }
        public int Word()
        {
            int offset = Take(2);
            return bytes[offset] * 256 + bytes[offset + 1];
        }
        public int SignedWord()
        {
            return (short)Word();
        }
        public double Fixed()
        {
            return SignedWord() / 16384.0;
        }
        public void Skip(int count)
        {
            Take(count);
        }
    }
}
