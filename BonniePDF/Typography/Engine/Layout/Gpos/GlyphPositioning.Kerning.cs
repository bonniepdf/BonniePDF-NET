// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    internal sealed partial class GlyphPositioning
    {
        // Scalar pair seam: first glyph's horizontal advance adjustment from
        // the default kern feature, without GSUB or mark advance suppression.
        // Full shaping remains authoritative for contextual text and offsets.
        internal int PairKerning(int left, int right)
        {
            operations = 0;
            int result = 0;
            foreach (int index in kerningLookups)
            {
                Tick();
                int lookup = Offset(lookupList, lookupList + 2 + index * 2), type = data.U16(lookup), flags = data.U16(lookup + 2);
                int count = data.U16(lookup + 4), markSet = (flags & 16) != 0 ? data.U16(lookup + 6 + count * 2) : -1;
                data.Check(lookup + 6, count * 2);
                filter.Validate(flags, markSet);
                if (filter.Ignored(left, flags, markSet) || filter.Ignored(right, flags, markSet)) continue;
                for (int i = 0; i < count; i++)
                {
                    Tick();
                    int sub = Offset(lookup, lookup + 6 + i * 2), actualType = type;
                    if (type == 9)
                    {
                        if (data.U16(sub) != 1 || data.U16(sub + 2) == 9 || data.Offset32(sub + 4) == 0)
                            throw new InvalidDataException("Invalid GPOS extension.");
                        actualType = data.U16(sub + 2);
                        sub = checked(sub + data.Offset32(sub + 4));
                    }
                    if (actualType != 2)
                    {
                        var glyphs = new List<ShapingGlyph> { new ShapingGlyph(left, 'A', 0), new ShapingGlyph(right, 'B', 1) };
                        return ApplySelected(glyphs, null, kerningLookups)[0].XAdvance;
                    }
                    int coverage = Coverage(Offset(sub, sub + 2), left);
                    if (coverage < 0) continue;
                    int format = data.U16(sub), firstFormat = data.U16(sub + 4), secondFormat = data.U16(sub + 6);
                    int firstSize = ValueSize(firstFormat), secondSize = ValueSize(secondFormat), value, origin = sub;
                    if (format == 1)
                    {
                        int sets = data.U16(sub + 8);
                        if (coverage >= sets) throw new InvalidDataException("Invalid GPOS pair coverage index.");
                        data.Check(sub + 10, sets * 2);
                        int set = Offset(sub, sub + 10 + coverage * 2), entries = data.U16(set), stride = 2 + firstSize + secondSize;
                        data.Check(set + 2, entries * stride);
                        int low = 0, high = entries - 1;
                        value = -1;
                        while (low <= high)
                        {
                            Tick();
                            int middle = low + (high - low) / 2, record = set + 2 + middle * stride, id = data.U16(record);
                            if (id < right) low = middle + 1;
                            else if (id > right) high = middle - 1;
                            else { value = record + 2; break; }
                        }
                        if (value < 0) continue;
                        origin = set;
                    }
                    else if (format == 2)
                    {
                        int row = Class(Offset(sub, sub + 8), left), column = Class(Offset(sub, sub + 10), right);
                        int rows = data.U16(sub + 12), columns = data.U16(sub + 14);
                        if (row >= rows || column >= columns) throw new InvalidDataException("Invalid GPOS pair class index.");
                        data.Check(sub + 16, checked(rows * columns * (firstSize + secondSize)));
                        value = checked(sub + 16 + (row * columns + column) * (firstSize + secondSize));
                    }
                    else throw new InvalidDataException("Invalid GPOS pair format.");
                    var adjustment = new GlyphAdjustment();
                    Value(origin, value, firstFormat, adjustment);
                    // Validate the second record even though this scalar seam
                    // deliberately exposes only the first advance.
                    Value(origin, value + firstSize, secondFormat, new GlyphAdjustment());
                    result = checked(result + adjustment.XAdvance);
                    break;
                }
            }
            return result;
        }
    }
}
