// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    internal sealed class FontMetrics
    {
        internal readonly int UnitsPerEm, Ascender, Descender, LineGap, XMin, YMin, XMax, YMax;
        internal readonly int? TypographicAscender, TypographicDescender, TypographicLineGap, WindowsAscent, WindowsDescent;
        internal readonly int? CapHeight, XHeight, WeightClass, WidthClass, EmbeddingFlags, SelectionFlags;
        internal readonly int? UnderlinePosition, UnderlineThickness, ItalicAngleFixed;
        internal readonly bool? IsFixedPitch;
        internal readonly int? Os2Version;
        internal bool UseTypographicMetrics { get { return Os2Version >= 4 && SelectionFlags.HasValue && (SelectionFlags.Value & 128) != 0; } }
        internal int LayoutAscender { get { return UseTypographicMetrics && TypographicAscender.HasValue ? TypographicAscender.Value : Ascender; } }
        internal int LayoutDescender { get { return UseTypographicMetrics && TypographicDescender.HasValue ? TypographicDescender.Value : Descender; } }
        internal int LayoutLineGap { get { return UseTypographicMetrics && TypographicLineGap.HasValue ? TypographicLineGap.Value : LineGap; } }
        internal double? ItalicAngle { get { return ItalicAngleFixed.HasValue ? ItalicAngleFixed.Value / 65536.0 : (double?)null; } }

        internal FontMetrics(byte[] headBytes, byte[] hheaBytes, byte[] os2Bytes, byte[] postBytes)
        {
            var head = new BigEndianData(headBytes);
            var hhea = new BigEndianData(hheaBytes);
            head.Check(0, 54); hhea.Check(0, 36);
            UnitsPerEm = head.U16(18); Ascender = hhea.I16(4); Descender = hhea.I16(6); LineGap = hhea.I16(8);
            XMin = head.I16(36); YMin = head.I16(38); XMax = head.I16(40); YMax = head.I16(42);
            if (os2Bytes != null)
            {
                var os2 = new BigEndianData(os2Bytes);
                int version = os2.U16(0);
                Os2Version = version;
                if (version > 5) throw new NotSupportedException("Unsupported OS/2 table version.");
                os2.Check(0, version == 0 ? 68 : version == 1 ? 86 : version == 5 ? 100 : 96);
                WeightClass = os2.U16(4); WidthClass = os2.U16(6); EmbeddingFlags = os2.U16(8); SelectionFlags = os2.U16(62);
                // Legacy version 0 may legally stop after usLastCharIndex.
                if (version != 0 || os2.Length >= 78)
                {
                    TypographicAscender = os2.I16(68); TypographicDescender = os2.I16(70); TypographicLineGap = os2.I16(72);
                    WindowsAscent = os2.U16(74); WindowsDescent = os2.U16(76);
                }
                else if (os2.Length != 68) throw new InvalidDataException("Truncated legacy OS/2 metrics.");
                if (version >= 2) { XHeight = os2.I16(86); CapHeight = os2.I16(88); }
            }
            if (postBytes != null)
            {
                var post = new BigEndianData(postBytes);
                post.Check(0, 32);
                uint version = post.U32(0);
                if (version != 0x10000 && version != 0x20000 && version != 0x25000 && version != 0x30000)
                    throw new NotSupportedException("Unsupported post table version.");
                ItalicAngleFixed = unchecked((int)post.U32(4));
                UnderlinePosition = post.I16(8); UnderlineThickness = post.I16(10); IsFixedPitch = post.U32(12) != 0;
            }
        }
    }
}
