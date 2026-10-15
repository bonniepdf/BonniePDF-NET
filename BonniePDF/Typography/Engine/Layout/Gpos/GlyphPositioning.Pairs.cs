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
        // A single or pair adjustment subtable (GPOS 1, 2) read and checked once: its coverage
        // and class maps, value formats, and the bounds every read below relies on. PairAt
        // then does what Subtable does for it, with the same charges, results and exceptions,
        // reading values at precomputed positions. Built only when every check Subtable makes
        // before a value is read holds for every glyph; any other subtable is left to Subtable.
        private sealed class PairSubtable
        {
            internal int Type, Format, Sub;
            internal CoverageMap Coverage;
            internal int FirstFormat, FirstSize, SecondFormat, SecondSize;
            // Format 1 pairs: decoded pair sets, by coverage index (null until first used).
            internal PairSet[] Sets;
            // Format 2 pairs: class definitions and the class matrix.
            internal int Class1, Class2, Rows, Columns;
        }

        private sealed class PairSet
        {
            // A set whose reading throws.
            internal static readonly PairSet Unsupported = new PairSet(0, Array.Empty<ushort>());
            internal readonly int Origin, Records;
            internal readonly ushort[] Seconds;
            internal PairSet(int origin, ushort[] seconds) { Origin = origin; Records = origin + 2; Seconds = seconds; }
        }

        private readonly Dictionary<int, PairSubtable[]> pairs = new Dictionary<int, PairSubtable[]>();

        // Each subtable's plan (null where Subtable must read it), or null when the lookup is
        // not single or pair adjustment.
        private PairSubtable[] Pairs(int index)
        {
            PairSubtable[] plans;
            if (pairs.TryGetValue(index, out plans)) return plans;
            plans = BuildPairs(index);
            pairs.Add(index, plans);
            return plans;
        }

        private PairSubtable[] BuildPairs(int index)
        {
            LookupHeaderCache.Header header;
            try { header = headers.Get(index); } catch (Exception) { return null; }
            if (header.Type != 1 && header.Type != 2 && header.Type != 9) return null;
            PairSubtable[] plans = null;
            for (int i = 0; i < header.Count; i++)
            {
                try
                {
                    var plan = BuildPair(Offset(header.Offset, header.Offset + 6 + i * 2), header.Type);
                    if (plan == null) continue;
                    if (plans == null) plans = new PairSubtable[header.Count];
                    plans[i] = plan;
                }
                catch (Exception) { }
            }
            return plans;
        }

        private PairSubtable BuildPair(int sub, int type)
        {
            int format = data.U16(sub);
            if (type == 9)
            {
                int extended = data.U16(sub + 2), delta = data.Offset32(sub + 4);
                if (format != 1 || extended < 1 || extended > 8 || delta == 0) return null;
                type = extended; sub = checked(sub + delta); format = data.U16(sub);
            }
            if ((type != 1 && type != 2) || (format != 1 && format != 2)) return null;
            var coverage = reads.CoverageMap(data, Offset(sub, sub + 2));
            if (coverage == null) return null;
            int covered = coverage.Count;
            var plan = new PairSubtable { Type = type, Format = format, Sub = sub, Coverage = coverage };
            plan.FirstFormat = data.U16(sub + 4);
            if ((plan.FirstFormat & ~0xff) != 0) return null;
            plan.FirstSize = ValueSize(plan.FirstFormat);
            if (type == 1)
            {
                if (format == 1) data.Check(sub + 6, plan.FirstSize);
                else
                {
                    int count = data.U16(sub + 6);
                    if (covered > count) return null;
                    data.Check(sub + 8, count * plan.FirstSize);
                }
                return plan;
            }
            plan.SecondFormat = data.U16(sub + 6);
            if ((plan.SecondFormat & ~0xff) != 0) return null;
            plan.SecondSize = ValueSize(plan.SecondFormat);
            if (format == 1)
            {
                int sets = data.U16(sub + 8);
                if (covered > sets) return null;
                data.Check(sub + 10, sets * 2);
                plan.Sets = new PairSet[sets];
                return plan;
            }
            plan.Class1 = Offset(sub, sub + 8);
            plan.Class2 = Offset(sub, sub + 10);
            plan.Rows = data.U16(sub + 12);
            plan.Columns = data.U16(sub + 14);
            data.Check(sub + 16, checked(plan.Rows * plan.Columns * (plan.FirstSize + plan.SecondSize)));
            return plan;
        }

        // The pair set at coverage index, read once; Unsupported when reading it throws, so the
        // caller leaves the glyph to Subtable, which throws the same way.
        private PairSet SetAt(PairSubtable plan, int coverage)
        {
            var set = plan.Sets[coverage];
            if (set != null) return set;
            try
            {
                int origin = Offset(plan.Sub, plan.Sub + 10 + coverage * 2), count = data.U16(origin);
                int stride = 2 + plan.FirstSize + plan.SecondSize;
                data.Check(origin + 2, count * stride);
                var seconds = new ushort[count];
                for (int i = 0; i < count; i++) seconds[i] = (ushort)data.U16(origin + 2 + i * stride);
                set = new PairSet(origin, seconds);
            }
            catch (Exception) { set = PairSet.Unsupported; }
            plan.Sets[coverage] = set;
            return set;
        }

        // Subtable for a planned single or pair adjustment subtable.
        private int PairAt(PairSubtable plan, List<ShapingGlyph> glyphs, GlyphAdjustment[] adjustments, int at, int next,
            int flags, int markSet, Func<int, int> advanceWidth, int depth)
        {
            int coverage = plan.Coverage.Index(glyphs[at].Id);
            if (coverage < 0)
                return 0;
            if (plan.Type == 1)
            {
                Value(plan.Sub, plan.Format == 1 ? plan.Sub + 6 : plan.Sub + 8 + coverage * plan.FirstSize, plan.FirstFormat, adjustments[at]);
                return 1;
            }
            int pair, valueOrigin = plan.Sub;
            if (plan.Format == 1)
            {
                var set = SetAt(plan, coverage);
                if (set == PairSet.Unsupported)
                    return Subtable(plan.Type, plan.Sub, glyphs, adjustments, at, next, flags, markSet, advanceWidth, depth);
                if (next >= glyphs.Count)
                    return 0;
                if (activeRule != null && RuleValue(glyphs[next].FeatureCluster) == 0) return 0;
                valueOrigin = set.Origin;
                var seconds = set.Seconds;
                int second = glyphs[next].Id, low = 0, high = seconds.Length - 1;
                pair = -1;
                while (low <= high)
                {
                    Tick();
                    int mid = (low + high) / 2, id = seconds[mid];
                    if (id == second)
                    {
                        pair = set.Records + mid * (2 + plan.FirstSize + plan.SecondSize) + 2;
                        break;
                    }
                    if (second < id)
                        high = mid - 1;
                    else
                        low = mid + 1;
                }
                if (pair < 0)
                    return 0;
            }
            else
            {
                if (next >= glyphs.Count)
                    return 0;
                if (activeRule != null && RuleValue(glyphs[next].FeatureCluster) == 0) return 0;
                int c1 = reads.Class(data, plan.Class1, glyphs[at].Id);
                int c2 = reads.Class(data, plan.Class2, glyphs[next].Id);
                if (c1 >= plan.Rows || c2 >= plan.Columns)
                    throw new InvalidDataException("Invalid GPOS pair class index.");
                pair = plan.Sub + 16 + (c1 * plan.Columns + c2) * (plan.FirstSize + plan.SecondSize);
            }
            int oldFirstAdvance = adjustments[at].XAdvance, oldFirstX = adjustments[at].XPlacement, oldFirstY = adjustments[at].YPlacement;
            int oldSecondAdvance = adjustments[next].XAdvance, oldSecondX = adjustments[next].XPlacement, oldSecondY = adjustments[next].YPlacement;
            Value(valueOrigin, pair, plan.FirstFormat, adjustments[at]);
            Value(valueOrigin, pair + plan.FirstSize, plan.SecondFormat, adjustments[next]);
            if (oldFirstAdvance != adjustments[at].XAdvance || oldFirstX != adjustments[at].XPlacement || oldFirstY != adjustments[at].YPlacement ||
                oldSecondAdvance != adjustments[next].XAdvance || oldSecondX != adjustments[next].XPlacement || oldSecondY != adjustments[next].YPlacement)
                UnsafeRange.Mark(glyphs, at, next, charge);
            return plan.SecondFormat == 0 ? 1 : -1;
        }
    }
}
