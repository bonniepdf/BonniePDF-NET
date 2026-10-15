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
        private int Walk(List<ShapingGlyph> glyphs, int position, int direction, int flags, int markSet)
        {
            do
            {
                Tick();
                position += direction;
            }
            while (position >= 0 && position < glyphs.Count && filter.Ignored(glyphs[position].Id, flags, markSet));
            return position;
        }

        // mode: glyph IDs (0), glyph classes (1), or coverage offsets (2).
        private bool Match(List<ShapingGlyph> glyphs, int start, int direction, int count, int record,
            int mode, int classDefinition, int sub, int flags, int markSet, out List<ShapingGlyph> matched, bool requireFeature = false)
        {
            data.Check(record, count * 2);
            matched = count == 0 ? new List<ShapingGlyph>() : null;
            if (count > glyphs.Count)
                return false;
            int position = start;
            for (int j = 0; j < count; j++)
            {
                Tick();
                if (position < 0 || position >= glyphs.Count)
                    return false;
                if (requireFeature && (activeRule != null && RuleValue(glyphs[position].FeatureCluster) == 0)) return false;
                int id = glyphs[position].Id, expected = data.U16(record + j * 2);
                bool same = mode == 2 ? Coverage(checked(sub + expected), id) >= 0 :
                    (mode == 1 ? Class(classDefinition, id) : id) == expected;
                if (!same)
                    return false;
                if (matched == null) matched = new List<ShapingGlyph>();
                matched.Add(glyphs[position]);
                if (j + 1 < count)
                    position = Walk(glyphs, position, direction, flags, markSet);
            }
            return true;
        }

        private int Context(int type, int format, int sub, int coverage, List<ShapingGlyph> glyphs, GlyphAdjustment[] adjustments, int at, int depth,
            int flags, int markSet, Func<int, int> advanceWidth)
        {
            int contextFirst = at, contextLast = at;
            bool chained = type == 8;
            List<ShapingGlyph> ignored, matched;
            if (format == 3)
            {
                int p = sub + 2;
                bool match = true;
                if (chained)
                {
                    int back = data.U16(p);
                    p += 2;
                    match = Match(glyphs, Walk(glyphs, at, -1, flags, markSet), -1, back, p, 2, 0, sub, flags, markSet, out ignored);
                    if (match && ignored.Count > 0) contextFirst = glyphs.IndexOf(ignored[ignored.Count - 1]);
                    p += back * 2;
                }
                int input = data.U16(p);
                p += 2;
                int records = chained ? -1 : data.U16(p);
                if (!chained)
                    p += 2;
                if (input < 1)
                    throw new InvalidDataException("Empty GPOS context.");
                match &= Match(glyphs, at, 1, input, p, 2, 0, sub, flags, markSet, out matched, true);
                p += input * 2;
                if (chained)
                {
                    int ahead = data.U16(p);
                    p += 2;
                    data.Check(p, ahead * 2);
                    if (match)
                    {
                        int last = glyphs.IndexOf(matched[matched.Count - 1]);
                        match = Match(glyphs, Walk(glyphs, last, 1, flags, markSet), 1, ahead, p, 2, 0, sub, flags, markSet, out ignored);
                        if (match && ignored.Count > 0) contextLast = glyphs.IndexOf(ignored[ignored.Count - 1]);
                    }
                    p += ahead * 2;
                    records = data.U16(p);
                    p += 2;
                }
                if (match) UnsafeRange.Mark(glyphs, contextFirst, Math.Max(contextLast, glyphs.IndexOf(matched[matched.Count - 1])), charge);
                return match ? ApplyRecords(p, records, matched, glyphs, adjustments, at, depth, advanceWidth) : 0;
            }
            if (format != 1 && format != 2)
                throw new NotSupportedException("Unsupported GPOS context format.");
            int backClass = 0, inputClass = 0, aheadClass = 0, header = sub + 4;
            if (format == 2)
            {
                if (chained)
                {
                    backClass = Offset(sub, header);
                    header += 2;
                }
                inputClass = Offset(sub, header);
                header += 2;
                if (chained)
                {
                    aheadClass = Offset(sub, header);
                    header += 2;
                }
            }
            int setCount = data.U16(header), setIndex = format == 1 ? coverage : Class(inputClass, glyphs[at].Id);
            data.Check(header + 2, setCount * 2);
            if (setIndex >= setCount)
                return 0;
            int relativeSet = data.U16(header + 2 + setIndex * 2);
            if (relativeSet == 0)
                return 0;
            int set = checked(sub + relativeSet), rules = data.U16(set);
            data.Check(set + 2, rules * 2);
            for (int ruleIndex = 0; ruleIndex < rules; ruleIndex++)
            {
                Tick();
                int p = Offset(set, set + 2 + ruleIndex * 2), mode = format == 1 ? 0 : 1;
                bool match = true;
                if (chained)
                {
                    int back = data.U16(p);
                    p += 2;
                    match = Match(glyphs, Walk(glyphs, at, -1, flags, markSet), -1, back, p, mode, backClass, sub, flags, markSet, out ignored);
                    if (match && ignored.Count > 0) contextFirst = glyphs.IndexOf(ignored[ignored.Count - 1]);
                    p += back * 2;
                }
                int input = data.U16(p);
                p += 2;
                if (input < 1)
                    throw new InvalidDataException("Empty GPOS context.");
                int records = chained ? -1 : data.U16(p);
                if (!chained)
                    p += 2;
                match &= Match(glyphs, Walk(glyphs, at, 1, flags, markSet), 1, input - 1, p, mode, inputClass, sub, flags, markSet, out matched, true);
                if (match) matched.Insert(0, glyphs[at]);
                p += (input - 1) * 2;
                if (chained)
                {
                    int ahead = data.U16(p);
                    p += 2;
                    data.Check(p, ahead * 2);
                    if (match)
                    {
                        int last = glyphs.IndexOf(matched[matched.Count - 1]);
                        match = Match(glyphs, Walk(glyphs, last, 1, flags, markSet), 1, ahead, p, mode, aheadClass, sub, flags, markSet, out ignored);
                        if (match && ignored.Count > 0) contextLast = glyphs.IndexOf(ignored[ignored.Count - 1]);
                    }
                    p += ahead * 2;
                    records = data.U16(p);
                    p += 2;
                }
                if (match)
                {
                    UnsafeRange.Mark(glyphs, contextFirst, Math.Max(contextLast, glyphs.IndexOf(matched[matched.Count - 1])), charge);
                    return ApplyRecords(p, records, matched, glyphs, adjustments, at, depth, advanceWidth);
                }
            }
            return 0;
        }

        private int ApplyRecords(int p, int records, List<ShapingGlyph> matched, List<ShapingGlyph> glyphs,
            GlyphAdjustment[] adjustments, int at, int depth, Func<int, int> advanceWidth)
        {
            data.Check(p, records * 4);
            for (int i = 0; i < records; i++)
            {
                Tick();
                int index = data.U16(p + i * 4), lookup = data.U16(p + i * 4 + 2);
                if (index >= matched.Count)
                    throw new InvalidDataException("Invalid GPOS contextual sequence index.");
                ApplyAt(lookup, glyphs, adjustments, glyphs.IndexOf(matched[index]), depth + 1, advanceWidth);
            }
            return glyphs.IndexOf(matched[matched.Count - 1]) + 1 - at;
        }

        private int Coverage(int offset, int glyph) { return reads.Coverage(data, offset, glyph); }
        private int Class(int offset, int glyph) { return reads.Class(data, offset, glyph); }
    }
}
