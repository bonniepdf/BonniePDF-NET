// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    internal sealed class GlyphSubstitution
    {
        private readonly BigEndianData data;
        private readonly LayoutReadCache reads = new LayoutReadCache();
        private readonly GlyphFilter filter;
        private readonly int lookupList, glyphCount;
        private readonly LookupHeaderCache headers;
        private readonly Dictionary<int, FirstGlyphSet> firstGlyphs = new Dictionary<int, FirstGlyphSet>();
        private readonly int[] lookups;
        private readonly LookupRule[] rules;
        private LookupRule activeRule;
        private readonly bool arabic;
        private int operations;
        private readonly Action charge;
        private readonly ShapingLimits limits;
        internal GlyphSubstitution(byte[] bytes, int glyphCount, byte[] gdef, ShapingLimits limits = null, ShapeOptions options = null)
        {
            arabic = options != null && options.Script == "arab";
            charge = Tick;
            this.limits = limits ?? new ShapingLimits();
            this.glyphCount = glyphCount;
            filter = new GlyphFilter(gdef);
            var plan = new ShapePlan(bytes, "GSUB", options);
            lookups = plan.Lookups; rules = plan.Rules;
            if (bytes == null) return;
            data = new BigEndianData(bytes);
            lookupList = OpenTypeLayout.Offset16(data, 0, 8);
            headers = new LookupHeaderCache(data, lookupList, filter, false);
        }
        internal void Apply(List<ShapingGlyph> glyphs)
        {
            operations = 0;
            if (glyphs.Count > limits.MaxGlyphs)
                throw new InvalidDataException("GSUB input exceeds the glyph limit.");
            for (int i = 0; i < lookups.Length; i++)
            {
                activeRule = rules[i];
                int lookup = lookups[i];
                var first = FirstGlyphs(lookup);
                int rejectedCharge = first == null ? -1 : RejectedCharge(lookup);
                if (IsReverse(lookup))
                {
                    for (int at = glyphs.Count - 1; at >= 0; at--)
                    {
                        bool rejected = Rejects(first, glyphs[at]);
                        if (rejected && rejectedCharge >= 0) ChargeSubtables(rejectedCharge);
                        else ApplyAt(lookup, glyphs, at, 0, rejected);
                    }
                }
                else
                    for (int at = 0; at < glyphs.Count;)
                    {
                        bool rejected = Rejects(first, glyphs[at]);
                        int consumed;
                        if (rejected && rejectedCharge >= 0) { ChargeSubtables(rejectedCharge); consumed = 0; }
                        else consumed = ApplyAt(lookup, glyphs, at, 0, rejected);
                        at += Math.Max(consumed, 1);
                        if (glyphs.Count > limits.MaxGlyphs)
                            throw new InvalidDataException("GSUB expansion exceeds the glyph limit.");
                    }
            }
            activeRule = null;
        }

        private bool IsReverse(int index)
        {
            int lookup = data.Relative16(lookupList, lookupList + 2 + index * 2), type = data.U16(lookup);
            if (type == 8) return true;
            if (type != 7) return false;
            int count = data.U16(lookup + 4);
            for (int i = 0; i < count; i++)
            {
                Tick();
                int sub = data.Relative16(lookup, lookup + 6 + i * 2);
                if (data.U16(sub + 2) == 8) return true;
            }
            return false;
        }

        private bool Ignored(ShapingGlyph glyph, int flags, int set) { return (arabic && glyph.Scalar != 0x200c && glyph.Scalar != 0x200d && UnicodeData.IsIgnorable(glyph.Scalar)) || filter.Ignored(glyph.Id, flags, set); }
        private bool Enabled(ShapingGlyph glyph) { return activeRule == null || activeRule.Value(glyph.FeatureCluster, charge, glyph.Mirrored, glyph.JoiningForm) != 0; }

        private static bool Rejects(FirstGlyphSet first, ShapingGlyph glyph) { return first != null && !first.Contains(glyph.Id); }

        // A lookup's first-glyph set, or null when any subtable reads before its coverage
        // test (contextual format 3) or fails the checks that precede it.
        private FirstGlyphSet FirstGlyphs(int index)
        {
            FirstGlyphSet first;
            if (firstGlyphs.TryGetValue(index, out first)) return first;
            first = BuildFirstGlyphs(index);
            firstGlyphs.Add(index, first);
            return first;
        }

        private FirstGlyphSet BuildFirstGlyphs(int index)
        {
            try
            {
                var header = headers.Get(index);
                var first = new FirstGlyphSet();
                for (int i = 0; i < header.Count; i++)
                {
                    int sub = data.Relative16(header.Offset, header.Offset + 6 + i * 2), type = header.Type;
                    if (type == 7)
                    {
                        if (data.U16(sub) != 1 || data.U16(sub + 2) == 7) return null;
                        type = data.U16(sub + 2);
                        sub = checked(sub + data.Offset32(sub + 4));
                    }
                    if ((type == 5 || type == 6) && data.U16(sub) == 3) return null;
                    if (!first.AddCoverage(data, data.Relative16(sub, sub + 2))) return null;
                }
                return first;
            }
            catch (Exception) { return null; }
        }

        // What ApplyAt charges for every rejected glyph of this lookup, when that cannot
        // depend on the glyph: a constant feature rule and no glyph filtering. Otherwise -1.
        private int RejectedCharge(int index)
        {
            var header = headers.Get(index);
            if (arabic || (header.Flags & 0xff1e) != 0 || limits.MaxNesting < 0) return -1;
            int value = 1, ticks = 0;
            if (activeRule != null && !activeRule.Constant(out value, out ticks)) return -1;
            return 1 + ticks + (value != 0 ? header.Count : 0);
        }

        // Charges what the subtable loop would charge for a glyph no subtable covers.
        private void ChargeSubtables(int count)
        {
            if (count > 0 && (operations += count) > limits.MaxOperations)
                throw new InvalidDataException("GSUB operation limit exceeded.");
        }

        private int ApplyAt(int index, List<ShapingGlyph> glyphs, int at, int depth, bool rejected = false)
        {
            Tick();
            if (depth > limits.MaxNesting)
                throw new InvalidDataException("GSUB recursion/operation limit exceeded.");
            if (at < 0 || at >= glyphs.Count)
                return 0;
            if (depth == 0 && !Enabled(glyphs[at])) return 0;
            var header = headers.Get(index);
            int lookup = header.Offset, type = header.Type, flags = header.Flags, count = header.Count, markSet = header.MarkSet;
            if (Ignored(glyphs[at], flags, markSet))
                return 0;
            if (rejected) { ChargeSubtables(count); return 0; }
            for (int i = 0; i < count; i++)
            {
                Tick();
                int sub = data.Relative16(lookup, lookup + 6 + i * 2);
                int result = Subtable(type, sub, glyphs, at, depth, flags, markSet);
                if (result != 0)
                    return result;
            }
            return 0;
        }
        private void SetGlyph(ShapingGlyph glyph, int id)
        {
            if (id < 0 || id >= glyphCount)
                throw new InvalidDataException("GSUB references an invalid glyph.");
            glyph.Id = id;
        }
        private int Subtable(int type, int sub, List<ShapingGlyph> glyphs, int at, int depth, int flags, int markSet)
        {
            int format = data.U16(sub);
            if (type == 7)
            {
                if (format != 1 || depth > limits.MaxNesting)
                    throw new InvalidDataException("Invalid GSUB extension.");
                int extendedType = data.U16(sub + 2);
                if (extendedType == 7)
                    throw new InvalidDataException("Recursive GSUB extension.");
                return Subtable(extendedType, checked(sub + data.Offset32(sub + 4)), glyphs, at, depth + 1, flags, markSet);
            }
            if ((type == 5 || type == 6) && format == 3)
                return Context(type, format, sub, 0, glyphs, at, depth, flags, markSet);
            int coverage = Coverage(data.Relative16(sub, sub + 2), glyphs[at].Id);
            if (coverage < 0)
                return 0;
            if (type == 1)
            {
                if (format == 1)
                    SetGlyph(glyphs[at], (glyphs[at].Id + data.I16(sub + 4)) & 0xffff);
                else if (format == 2 && coverage < data.U16(sub + 4))
                    SetGlyph(glyphs[at], data.U16(sub + 6 + coverage * 2));
                else
                    throw new InvalidDataException("Invalid single substitution.");
                return 1;
            }
            if (type == 2)
            {
                if (format != 1 || coverage >= data.U16(sub + 4))
                    throw new InvalidDataException("Invalid multiple substitution.");
                int sequence = data.Relative16(sub, sub + 6 + coverage * 2), count = data.U16(sequence);
                if (count > limits.MaxGlyphs)
                    throw new InvalidDataException("Excessive GSUB expansion.");
                var original = glyphs[at];
                if (glyphs.Count - 1 + count > limits.MaxGlyphs)
                    throw new InvalidDataException("GSUB expansion exceeds the glyph limit.");
                glyphs.RemoveAt(at);
                for (int i = 0; i < count; i++)
                {
                    Tick();
                    var next = new ShapingGlyph(0, original.Scalar, original.Cluster)
                    {
                        MarkLigature = original.MarkLigature, MarkComponent = original.MarkComponent,
                        FeatureCluster = original.FeatureCluster, UnsafeBefore = original.UnsafeBefore,
                        AdvanceOverride = original.AdvanceOverride, Mirrored = original.Mirrored, JoiningForm = original.JoiningForm,
                        Ligature = original.Ligature, ComponentCount = original.ComponentCount
                    };
                    if (i == 0) next = original;
                    SetGlyph(next, data.U16(sequence + 2 + i * 2));
                    glyphs.Insert(at + i, next);
                }
                foreach (var glyph in glyphs)
                {
                    Tick();
                    if (glyph.MarkLigature == original)
                        glyph.MarkLigature = count == 0 ? null : glyphs[at];
                }
                return Math.Max(count, 1);
            }
            if (type == 3)
            {
                if (format != 1 || coverage >= data.U16(sub + 4))
                    throw new InvalidDataException("Invalid alternate substitution.");
                int set = data.Relative16(sub, sub + 6 + coverage * 2), count = data.U16(set);
                data.Check(set + 2, count * 2);
                int value = activeRule == null ? 1 : activeRule.Value(glyphs[at].FeatureCluster, charge, glyphs[at].Mirrored, glyphs[at].JoiningForm);
                if (value < 1 || value > count) return 0;
                SetGlyph(glyphs[at], data.U16(set + value * 2));
                return 1;
            }
            if (type == 8)
            {
                if (format != 1) throw new InvalidDataException("Invalid reverse chaining substitution.");
                int p = sub + 4, back = data.U16(p);
                p += 2; data.Check(p, back * 2);
                int position = at;
                for (int i = 0; i < back; i++)
                {
                    position = Walk(glyphs, position, -1, flags, markSet);
                    if (position < 0 || Coverage(data.Relative16(sub, p + i * 2), glyphs[position].Id) < 0) return 0;
                }
                int first = position;
                p += back * 2;
                int ahead = data.U16(p);
                p += 2; data.Check(p, ahead * 2);
                position = at;
                for (int i = 0; i < ahead; i++)
                {
                    position = Walk(glyphs, position, 1, flags, markSet);
                    if (position >= glyphs.Count || Coverage(data.Relative16(sub, p + i * 2), glyphs[position].Id) < 0) return 0;
                }
                p += ahead * 2;
                int count = data.U16(p);
                data.Check(p + 2, count * 2);
                if (coverage >= count) throw new InvalidDataException("Invalid reverse chaining coverage index.");
                UnsafeRange.Mark(glyphs, first, position, charge);
                SetGlyph(glyphs[at], data.U16(p + 2 + coverage * 2));
                return 1;
            }
            if (type == 4)
            {
                if (format != 1 || coverage >= data.U16(sub + 4))
                    throw new InvalidDataException("Invalid ligature substitution.");
                int set = data.Relative16(sub, sub + 6 + coverage * 2), count = data.U16(set);
                for (int i = 0; i < count; i++)
                {
                    Tick();
                    int ligature = data.Relative16(set, set + 2 + i * 2), components = data.U16(ligature + 2);
                    if (components < 1)
                        throw new InvalidDataException("Empty GSUB ligature.");
                    data.Check(ligature + 4, (components - 1) * 2);
                    var matched = new List<int> { at };
                    bool matches = true;
                    int position = at;
                    for (int j = 1; matches && j < components; j++)
                    {
                        position = Walk(glyphs, position, 1, flags, markSet);
                        matches = position < glyphs.Count && Enabled(glyphs[position]) && glyphs[position].Id == data.U16(ligature + 2 + j * 2);
                        if (matches)
                            matched.Add(position);
                    }
                    if (!matches)
                        continue;
                    AssociateComponents(glyphs, matched);
                    // A ligature spanning skipped marks merges every source
                    // cluster through its final component, including marks
                    // already belonging to that component's cluster.
                    int final = matched[matched.Count - 1], finalCluster = glyphs[final].Cluster;
                    while (final + 1 < glyphs.Count && glyphs[final + 1].Cluster == finalCluster) final++;
                    int firstCluster = glyphs[at].Cluster;
                    for (int j = at; j <= final; j++) { Tick(); firstCluster = Math.Min(firstCluster, glyphs[j].Cluster); }
                    bool inherited = glyphs[at].Cluster == firstCluster && glyphs[at].UnsafeBefore;
                    for (int j = at; j <= final; j++) { Tick(); glyphs[j].Cluster = firstCluster; if (arabic) glyphs[j].UnsafeBefore = inherited; }
                    SetGlyph(glyphs[at], data.U16(ligature));
                    glyphs[at].Ligature = true;
                    for (int j = matched.Count - 1; j > 0; j--)
                        glyphs.RemoveAt(matched[j]);
                    return 1;
                }
                return 0;
            }
            if (type == 5 || type == 6)
                return Context(type, format, sub, coverage, glyphs, at, depth, flags, markSet);
            throw new NotSupportedException("GSUB lookup type " + type + " is unsupported.");
        }
        private void AssociateComponents(List<ShapingGlyph> glyphs, List<int> matched)
        {
            var offsets = new Dictionary<ShapingGlyph, int>();
            int total = 0;
            foreach (int index in matched)
            {
                offsets.Add(glyphs[index], total);
                total = checked(total + glyphs[index].ComponentCount);
                if (total > limits.MaxGlyphs)
                    throw new InvalidDataException("GSUB ligature component limit exceeded.");
            }
            int component = 0;
            for (int i = matched[0] + 1; i < glyphs.Count; i++)
            {
                Tick();
                if (component + 1 < matched.Count && i == matched[component + 1])
                {
                    component++;
                    continue;
                }
                var mark = glyphs[i];
                int oldOffset;
                if (mark.MarkLigature != null && offsets.TryGetValue(mark.MarkLigature, out oldOffset))
                    mark.MarkComponent = checked(oldOffset + mark.MarkComponent);
                else if (filter.IsMark(mark))
                    mark.MarkComponent = offsets[glyphs[matched[component]]] + glyphs[matched[component]].ComponentCount - 1;
                else if (i > matched[matched.Count - 1])
                    break;
                else
                    continue;
                mark.MarkLigature = glyphs[matched[0]];
            }
            glyphs[matched[0]].ComponentCount = total;
        }

        private int Coverage(int offset, int glyph)
        {
            return reads.Coverage(data, offset, glyph);
        }
        private int Class(int offset, int glyph)
        {
            return reads.Class(data, offset, glyph);
        }

        private void Tick()
        {
            if (++operations > limits.MaxOperations)
                throw new InvalidDataException("GSUB operation limit exceeded.");
        }


        private int Walk(List<ShapingGlyph> glyphs, int position, int direction, int flags, int markSet)
        {
            do
            {
                Tick();
                position += direction;
            }
            while (position >= 0 && position < glyphs.Count && Ignored(glyphs[position], flags, markSet));
            return position;
        }

        // mode: glyph IDs (0), glyph classes (1), or coverage offsets (2).
        private bool Match(List<ShapingGlyph> glyphs, int start, int direction, int count, int record,
            int mode, int classDefinition, int sub, int flags, int markSet, out List<ShapingGlyph> matched, bool requireFeature = false)
        {
            data.Check(record, count * 2);
            // A successful empty match owns no glyphs; materialize only when input is added.
            matched = null;
            if (count > glyphs.Count)
                return false;
            int position = start;
            for (int j = 0; j < count; j++)
            {
                Tick();
                if (position < 0 || position >= glyphs.Count)
                    return false;
                if (requireFeature && !Enabled(glyphs[position])) return false;
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

        private int Context(int type, int format, int sub, int coverage, List<ShapingGlyph> glyphs, int at, int depth,
            int flags, int markSet)
        {
            int contextFirst = at, contextLast = at;
            bool chained = type == 6;
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
                    if (match && ignored != null) contextFirst = glyphs.IndexOf(ignored[ignored.Count - 1]);
                    p += back * 2;
                }
                int input = data.U16(p);
                p += 2;
                int records = chained ? -1 : data.U16(p);
                if (!chained)
                    p += 2;
                if (input < 1)
                    throw new InvalidDataException("Empty GSUB context.");
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
                        if (match && ignored != null) contextLast = glyphs.IndexOf(ignored[ignored.Count - 1]);
                    }
                    p += ahead * 2;
                    records = data.U16(p);
                    p += 2;
                }
                if (match) UnsafeRange.Mark(glyphs, contextFirst, Math.Max(contextLast, glyphs.IndexOf(matched[matched.Count - 1])), charge);
                return match ? ApplyRecords(p, records, matched, glyphs, at, depth) : 0;
            }
            if (format != 1 && format != 2)
                throw new NotSupportedException("Unsupported GSUB context format.");
            int backClass = 0, inputClass = 0, aheadClass = 0, header = sub + 4;
            if (format == 2)
            {
                if (chained)
                {
                    backClass = data.Relative16(sub, header);
                    header += 2;
                }
                inputClass = data.Relative16(sub, header);
                header += 2;
                if (chained)
                {
                    aheadClass = data.Relative16(sub, header);
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
                int p = data.Relative16(set, set + 2 + ruleIndex * 2), mode = format == 1 ? 0 : 1;
                bool match = true;
                if (chained)
                {
                    int back = data.U16(p);
                    p += 2;
                    match = Match(glyphs, Walk(glyphs, at, -1, flags, markSet), -1, back, p, mode, backClass, sub, flags, markSet, out ignored);
                    if (match && ignored != null) contextFirst = glyphs.IndexOf(ignored[ignored.Count - 1]);
                    p += back * 2;
                }
                int input = data.U16(p);
                p += 2;
                if (input < 1)
                    throw new InvalidDataException("Empty GSUB context.");
                int records = chained ? -1 : data.U16(p);
                if (!chained)
                    p += 2;
                match &= Match(glyphs, Walk(glyphs, at, 1, flags, markSet), 1, input - 1, p, mode, inputClass, sub, flags, markSet, out matched, true);
                if (match)
                {
                    if (matched == null) matched = new List<ShapingGlyph>();
                    matched.Insert(0, glyphs[at]);
                }
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
                        if (match && ignored != null) contextLast = glyphs.IndexOf(ignored[ignored.Count - 1]);
                    }
                    p += ahead * 2;
                    records = data.U16(p);
                    p += 2;
                }
                if (match)
                {
                    UnsafeRange.Mark(glyphs, contextFirst, Math.Max(contextLast, glyphs.IndexOf(matched[matched.Count - 1])), charge);
                    return ApplyRecords(p, records, matched, glyphs, at, depth);
                }
            }
            return 0;
        }

        private int ApplyRecords(int p, int records, List<ShapingGlyph> matched, List<ShapingGlyph> glyphs, int at, int depth)
        {
            // Records index filtered input glyphs, retaining identity through nested changes.
            data.Check(p, records * 4);
            int last = glyphs.IndexOf(matched[matched.Count - 1]);
            ShapingGlyph next = last + 1 < glyphs.Count ? glyphs[last + 1] : null;
            for (int i = 0; i < records; i++)
            {
                Tick();
                int index = data.U16(p + i * 4), lookup = data.U16(p + i * 4 + 2);
                if (index >= matched.Count)
                    throw new InvalidDataException("Invalid contextual sequence index.");
                int location = glyphs.IndexOf(matched[index]);
                if (location >= 0)
                {
                    int count = glyphs.Count;
                    ApplyAt(lookup, glyphs, location, depth + 1);
                    if (arabic && glyphs.Count > count) matched.InsertRange(index + 1, glyphs.GetRange(location + 1, glyphs.Count - count));
                }
            }
            int end = next == null ? glyphs.Count : glyphs.IndexOf(next);
            return Math.Max(1, end < 0 ? 1 : end - at);
        }
    }
}
