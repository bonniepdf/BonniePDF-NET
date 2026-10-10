// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    // Shared GSUB/GPOS lookup filtering and post-substitution mark classification.
    internal sealed class GlyphFilter
    {
        private readonly BigEndianData data;
        private readonly LayoutReadCache reads = new LayoutReadCache();

        internal GlyphFilter(byte[] bytes)
        {
            if (bytes != null)
                data = new BigEndianData(bytes);
        }

        internal int Class(int glyph)
        {
            if (data == null || data.U16(4) == 0)
                return 0;
            int kind = reads.Class(data, data.U16(4), glyph);
            if (kind > 4)
                throw new InvalidDataException("Invalid GDEF glyph class.");
            return kind;
        }

        internal bool IsMark(ShapingGlyph glyph)
        {
            int kind = Class(glyph.Id);
            if (kind != 0)
                return kind == 3;
            if (glyph.Ligature)
                return false;
            return UnicodeData.IsNonSpacingMark(glyph.Scalar);
        }

        internal void Validate(int flags, int markSet)
        {
            if ((flags & 0xe0) != 0)
                throw new NotSupportedException("Reserved OpenType lookup flags are unsupported.");
            if ((flags & 0xff1e) == 0)
                return;
            if (data == null || data.U16(0) != 1 || data.U16(4) == 0)
                throw new NotSupportedException("OpenType filtering requires GDEF glyph classes.");
            if ((flags & 16) != 0)
            {
                if (data.U16(2) < 2 || data.U16(12) == 0)
                    throw new NotSupportedException("OpenType mark filtering requires GDEF mark sets.");
                int sets = data.U16(12);
                if (data.U16(sets) != 1 || markSet < 0 || markSet >= data.U16(sets + 2))
                    throw new InvalidDataException("Invalid GDEF mark filtering set.");
                data.Check(sets + 4, data.U16(sets + 2) * 4);
            }
            else if ((flags & 0xff00) != 0 && (flags & 8) == 0 && data.U16(10) == 0)
                throw new NotSupportedException("OpenType mark attachment filtering requires GDEF attachment classes.");
        }

        internal bool Ignored(int glyph, int flags, int markSet)
        {
            if ((flags & 0xff1e) == 0)
                return false;
            int kind = Class(glyph);
            if ((kind == 1 && (flags & 2) != 0) || (kind == 2 && (flags & 4) != 0) || (kind == 3 && (flags & 8) != 0))
                return true;
            if (kind != 3)
                return false;
            if (markSet >= 0)
            {
                int sets = data.U16(12), delta = data.Offset32(sets + 4 + markSet * 4);
                if (delta == 0)
                    throw new InvalidDataException("Missing GDEF mark set coverage.");
                return reads.Coverage(data, checked(sets + delta), glyph) < 0;
            }
            return (flags & 0xff00) != 0 && reads.Class(data, data.U16(10), glyph) != flags >> 8;
        }
    }
}
