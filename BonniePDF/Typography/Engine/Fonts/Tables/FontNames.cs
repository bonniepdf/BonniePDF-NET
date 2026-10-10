// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace BonniePDF.Typography.Engine
{
    internal sealed class FontNames
    {
        internal readonly ReadOnlyCollection<FontNameRecord> Records;
        internal string Family { get { return Get(1); } }
        internal string Subfamily { get { return Get(2); } }
        internal string FullName { get { return Get(4); } }
        internal string PostScriptName { get { return Get(6); } }
        internal string TypographicFamily { get { return Get(16); } }
        internal string TypographicSubfamily { get { return Get(17); } }

        internal FontNames(byte[] bytes)
        {
            var records = new List<FontNameRecord>();
            Records = records.AsReadOnly();
            if (bytes == null) return;
            var data = new BigEndianData(bytes);
            int format = data.U16(0), count = data.U16(2), strings = data.U16(4);
            if (format > 1) throw new NotSupportedException("Unsupported name table format.");
            data.Check(6, count * 12);
            int headerEnd = 6 + count * 12;
            string[] tags = System.Array.Empty<string>();
            int decodedBytes = 0;
            if (format == 1)
            {
                int tagCount = data.U16(headerEnd);
                data.Check(headerEnd + 2, tagCount * 4);
                tags = new string[tagCount];
                for (int i = 0; i < tagCount; i++)
                {
                    int length = data.U16(headerEnd + 2 + i * 4);
                    Charge(ref decodedBytes, length);
                    tags[i] = Decode(data, strings + data.U16(headerEnd + 4 + i * 4), length);
                }
                headerEnd += 2 + tagCount * 4;
            }
            if (strings < headerEnd || strings > data.Length) throw new InvalidDataException("Invalid name string storage offset.");
            for (int i = 0; i < count; i++)
            {
                int p = 6 + i * 12, platform = data.U16(p), encoding = data.U16(p + 2), language = data.U16(p + 4);
                int length = data.U16(p + 8), offset = strings + data.U16(p + 10);
                Charge(ref decodedBytes, length);
                data.Check(offset, length);
                string tag = null;
                if (format == 1 && language >= 0x8000)
                {
                    if (language - 0x8000 >= tags.Length) throw new InvalidDataException("Invalid name language tag index.");
                    tag = tags[language - 0x8000];
                }
                string value = null;
                if (platform == 0 || (platform == 3 && (encoding == 0 || encoding == 1 || encoding == 10)))
                    value = Decode(data, offset, length);
                else if (platform == 1 && encoding == 0)
                {
                    // ASCII is a shared subset of Mac Roman. Non-ASCII legacy
                    // names stay explicitly undecoded rather than using an OS
                    // code page; Unicode records provide the modern names.
                    bool ascii = true;
                    for (int j = 0; j < length; j++) ascii &= data.U8(offset + j) < 128;
                    if (ascii) value = Encoding.ASCII.GetString(bytes, offset, length);
                }
                records.Add(new FontNameRecord(platform, encoding, language, data.U16(p + 6), tag, value));
            }
        }

        private static void Charge(ref int total, int length)
        {
            if (total > 1000000 - length) throw new InvalidDataException("name string byte limit exceeded.");
            total += length;
        }

        private static string Decode(BigEndianData data, int offset, int length)
        {
            data.Check(offset, length);
            if ((length & 1) != 0) throw new InvalidDataException("Odd UTF-16 name string length.");
            try { return new UnicodeEncoding(true, false, true).GetString(data.Bytes, offset, length); }
            catch (DecoderFallbackException error) { throw new InvalidDataException("Invalid UTF-16 name string.", error); }
        }

        internal string Get(int nameId)
        {
            int best = -1;
            string value = null;
            foreach (var record in Records)
            {
                if (record.NameId != nameId || string.IsNullOrEmpty(record.Value)) continue;
                int score = record.PlatformId == 3 ? 30 : record.PlatformId == 0 ? 20 : 10;
                if (record.LanguageId == 0x409 || (record.PlatformId != 3 && record.LanguageId == 0) ||
                    string.Equals(record.LanguageTag, "en", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(record.LanguageTag, "en-US", StringComparison.OrdinalIgnoreCase)) score += 100;
                if (score > best) { best = score; value = record.Value; }
            }
            return value;
        }
    }
}
