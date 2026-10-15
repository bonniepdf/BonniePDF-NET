// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    // Static horizontal Latin/default-language positioning and mark attachments.
    internal sealed partial class GlyphPositioning
    {
        private readonly BigEndianData data;
        private readonly LayoutReadCache reads = new LayoutReadCache();
        private readonly GlyphFilter filter;
        private readonly int lookupList;
        private readonly LookupHeaderCache headers;
        private readonly Dictionary<int, FirstGlyphSet> firstGlyphs = new Dictionary<int, FirstGlyphSet>();
        private readonly int[] lookups;
        private readonly LookupRule[] rules;
        private readonly ShapeOptions options;
        private LookupRule activeRule;
        private readonly int[] kerningLookups;
        internal bool HasKerning { get { return kerningLookups.Length != 0; } }
        private int operations;
        private readonly Action charge;
        private readonly ShapingLimits limits;
        internal ShapingLimits Limits { get { return limits; } }

        internal GlyphPositioning(byte[] bytes, byte[] gdef, ShapingLimits limits = null, ShapeOptions options = null)
        {
            charge = Tick;
            this.limits = limits ?? new ShapingLimits();
            filter = new GlyphFilter(gdef);
            this.options = options ?? new ShapeOptions();
            var plan = new ShapePlan(bytes, "GPOS", this.options);
            lookups = plan.Lookups; rules = plan.Rules; kerningLookups = plan.KerningLookups;
            if (bytes == null) return;
            data = new BigEndianData(bytes);
            lookupList = OpenTypeLayout.Offset16(data, 0, 8);
            headers = new LookupHeaderCache(data, lookupList, filter, true);
        }


        private int Offset(int origin, int field)
        {
            return OpenTypeLayout.Offset16(data, origin, field);
        }

        private void Tick()
        {
            if (++operations > limits.MaxOperations)
                throw new InvalidDataException("GPOS operation limit exceeded.");
        }


        internal GlyphAdjustment[] Apply(List<ShapingGlyph> glyphs, Func<int, int> advanceWidth = null, LegacyKerning legacyKerning = null)
        {
            return ApplySelected(glyphs, advanceWidth, lookups, HasKerning ? null : legacyKerning);
        }

        private GlyphAdjustment[] ApplySelected(List<ShapingGlyph> glyphs, Func<int, int> advanceWidth, int[] selection, LegacyKerning legacyKerning = null)
        {
            if (glyphs.Count > limits.MaxGlyphs)
                throw new InvalidDataException("GPOS glyph limit exceeded.");
            var adjustments = new GlyphAdjustment[glyphs.Count];
            for (int i = 0; i < glyphs.Count; i++)
            {
                adjustments[i] = new GlyphAdjustment();
                if (advanceWidth != null && glyphs[i].AdvanceOverride.HasValue)
                    adjustments[i].XAdvance = glyphs[i].AdvanceOverride.Value - advanceWidth(glyphs[i].Id);
                if (advanceWidth != null && filter.IsMark(glyphs[i]) && !(options.Script == "arab" && UnicodeData.IsIgnorable(glyphs[i].Scalar)))
                {
                    adjustments[i].XAdvance = -advanceWidth(glyphs[i].Id);
                    adjustments[i].ZeroMarkAdvance = true;
                }
            }
            operations = 0;
            if (legacyKerning != null && legacyKerning.HasHorizontalKerning)
            {
                int previous = -1;
                for (int i = 0; i < glyphs.Count; i++)
                {
                    Tick();
                    if (filter.IsMark(glyphs[i])) continue;
                    if (previous >= 0)
                    {
                        if (options.FeatureValue("kern", glyphs[previous].FeatureCluster, 1, charge) == 0 ||
                            options.FeatureValue("kern", glyphs[i].FeatureCluster, 1, charge) == 0) { previous = i; continue; }
                        int first, second;
                        for (int step = 0; step < legacyKerning.WorkPerPair; step++) Tick();
                        legacyKerning.Split(glyphs[previous].Id, glyphs[i].Id, out first, out second);
                        if (first != 0 || second != 0) UnsafeRange.Mark(glyphs, previous, i, charge);
                        adjustments[previous].XAdvance = checked(adjustments[previous].XAdvance + first);
                        adjustments[i].XAdvance = checked(adjustments[i].XAdvance + second);
                        adjustments[i].XPlacement = checked(adjustments[i].XPlacement + second);
                    }
                    previous = i;
                }
            }
            foreach (int index in selection)
            {
                activeRule = null;
                foreach (var rule in rules) { Tick(); if (rule.Index == index) { activeRule = rule; break; } }
                var first = FirstGlyphs(index);
                int rejectedCharge = first == null ? -1 : RejectedCharge(index);
                for (int at = 0; at < glyphs.Count;)
                {
                    bool rejected = first != null && !first.Contains(glyphs[at].Id);
                    if (rejected && rejectedCharge >= 0) { ChargeSubtables(rejectedCharge); at++; }
                    else at += Math.Max(1, ApplyAt(index, glyphs, adjustments, at, 0, advanceWidth, rejected));
                }
            }
            activeRule = null;
            Resolve(glyphs, adjustments, advanceWidth);
            return adjustments;
        }

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
                    int sub = Offset(header.Offset, header.Offset + 6 + i * 2), type = header.Type, format = data.U16(sub);
                    if (type == 9)
                    {
                        int extended = data.U16(sub + 2), delta = data.Offset32(sub + 4);
                        if (format != 1 || extended < 1 || extended > 8 || delta == 0) return null;
                        type = extended; sub = checked(sub + delta); format = data.U16(sub);
                    }
                    if ((type == 7 || type == 8) && format == 3) return null;
                    if (type == 3)
                    {
                        if (format != 1) return null;
                        data.Check(sub + 6, data.U16(sub + 4) * 4);
                    }
                    else if (type >= 4 && type <= 6) { if (format != 1) return null; }
                    else if (type == 1 || type == 2) { if (format != 1 && format != 2) return null; }
                    else if (type != 7 && type != 8) return null;
                    if (!first.AddCoverage(data, Offset(sub, sub + 2))) return null;
                }
                return first;
            }
            catch (Exception) { return null; }
        }

        // What ApplyAt charges for every rejected glyph of this lookup, when that cannot
        // depend on the glyph: a constant feature rule, no glyph filtering (so the walk to
        // the next glyph is one step) and no Arabic ignorables. Otherwise -1.
        private int RejectedCharge(int index)
        {
            var header = headers.Get(index);
            if (options.Script == "arab" || (header.Flags & 0xff1e) != 0 || limits.MaxNesting < 0) return -1;
            int value = 1, ticks = 0;
            if (activeRule != null && !activeRule.Constant(out value, out ticks)) return -1;
            return 1 + ticks + (value != 0 ? 1 + header.Count : 0);
        }

        // Charges what the subtable loop would charge for a glyph no subtable covers.
        private void ChargeSubtables(int count)
        {
            if (count > 0 && (operations += count) > limits.MaxOperations)
                throw new InvalidDataException("GPOS operation limit exceeded.");
        }

        private int ApplyAt(int index, List<ShapingGlyph> glyphs, GlyphAdjustment[] adjustments,
            int at, int depth, Func<int, int> advanceWidth, bool rejected = false)
        {
            Tick();
            if (options.Script == "arab" && UnicodeData.IsIgnorable(glyphs[at].Scalar)) return 0;
            if (depth > limits.MaxNesting)
                throw new InvalidDataException("GPOS recursion limit exceeded.");
            if (index < 0 || index >= headers.Count)
                throw new InvalidDataException("Invalid GPOS lookup index.");
            if (at < 0 || at >= glyphs.Count)
                return 0;
            if (depth == 0 && activeRule != null && activeRule.Value(glyphs[at].FeatureCluster, charge) == 0) return 0;
            var header = headers.Get(index);
            int lookup = header.Offset, type = header.Type, flags = header.Flags, count = header.Count, markSet = header.MarkSet;
            if (filter.Ignored(glyphs[at].Id, flags, markSet))
                return 0;
            int next = Walk(glyphs, at, 1, flags, markSet);
            if (rejected) { ChargeSubtables(count); return 0; }
            for (int i = 0; i < count; i++)
            {
                Tick();
                int consumed = Subtable(type, Offset(lookup, lookup + 6 + i * 2), glyphs, adjustments,
                    at, next, flags, markSet, advanceWidth, depth);
                if (consumed != 0)
                    return consumed == -1 ? next + 1 - at : consumed;
            }
            return 0;
        }

        private int Subtable(int type, int sub, List<ShapingGlyph> glyphs, GlyphAdjustment[] adjustments, int at, int next,
            int flags, int markSet, Func<int, int> advanceWidth, int depth)
        {
            int format = data.U16(sub);
            if (type == 9)
            {
                int extended = data.U16(sub + 2), delta = data.Offset32(sub + 4);
                if (format != 1 || extended < 1 || extended > 8 || delta == 0)
                    throw new InvalidDataException("Invalid GPOS extension.");
                return Subtable(extended, checked(sub + delta), glyphs, adjustments, at, next, flags, markSet, advanceWidth, depth);
            }
            if (type == 7 || type == 8)
            {
                int covered = format == 3 ? 0 : Coverage(Offset(sub, sub + 2), glyphs[at].Id);
                return covered < 0 ? 0 : Context(type, format, sub, covered, glyphs, adjustments, at, depth, flags, markSet, advanceWidth);
            }
            if (type == 3)
                return Cursive(sub, glyphs, adjustments, at, next, flags, advanceWidth);
            if (type == 4 || type == 5 || type == 6)
                return Attach(type, sub, glyphs, adjustments, at, flags, markSet, advanceWidth);
            if (type != 1 && type != 2)
                throw new NotSupportedException("Unsupported GPOS lookup type " + type + ".");
            if (format != 1 && format != 2)
                throw new InvalidDataException("Invalid GPOS adjustment format.");
            int coverage = reads.Coverage(data, Offset(sub, sub + 2), glyphs[at].Id);
            if (coverage < 0)
                return 0;
            int firstFormat = data.U16(sub + 4), firstSize = ValueSize(firstFormat);
            if (type == 1)
            {
                int record = sub + 6;
                if (format == 2)
                {
                    int count = data.U16(sub + 6);
                    if (coverage >= count)
                        throw new InvalidDataException("Invalid GPOS single coverage index.");
                    data.Check(sub + 8, count * firstSize);
                    record = sub + 8 + coverage * firstSize;
                }
                Value(sub, record, firstFormat, adjustments[at]);
                return 1;
            }
            int secondFormat = data.U16(sub + 6), secondSize = ValueSize(secondFormat);
            if (next >= glyphs.Count)
                return 0;
            if (activeRule != null && activeRule.Value(glyphs[next].FeatureCluster, charge) == 0) return 0;
            int pair, valueOrigin = sub;
            if (format == 1)
            {
                int sets = data.U16(sub + 8);
                if (coverage >= sets)
                    throw new InvalidDataException("Invalid GPOS pair coverage index.");
                data.Check(sub + 10, sets * 2);
                int set = Offset(sub, sub + 10 + coverage * 2), count = data.U16(set);
                valueOrigin = set;
                int stride = 2 + firstSize + secondSize;
                data.Check(set + 2, count * stride);
                int low = 0, high = count - 1;
                pair = -1;
                while (low <= high)
                {
                    Tick();
                    int mid = (low + high) / 2, record = set + 2 + mid * stride;
                    int id = data.U16(record);
                    if (id == glyphs[next].Id)
                    {
                        pair = record + 2;
                        break;
                    }
                    if (glyphs[next].Id < id)
                        high = mid - 1;
                    else
                        low = mid + 1;
                }
                if (pair < 0)
                    return 0;
            }
            else
            {
                int c1 = reads.Class(data, Offset(sub, sub + 8), glyphs[at].Id);
                int c2 = reads.Class(data, Offset(sub, sub + 10), glyphs[next].Id);
                int rows = data.U16(sub + 12), columns = data.U16(sub + 14);
                if (c1 >= rows || c2 >= columns)
                    throw new InvalidDataException("Invalid GPOS pair class index.");
                int length = checked(rows * columns * (firstSize + secondSize));
                data.Check(sub + 16, length);
                pair = checked(sub + 16 + (c1 * columns + c2) * (firstSize + secondSize));
            }
            int oldFirstAdvance = adjustments[at].XAdvance, oldFirstX = adjustments[at].XPlacement, oldFirstY = adjustments[at].YPlacement;
            int oldSecondAdvance = adjustments[next].XAdvance, oldSecondX = adjustments[next].XPlacement, oldSecondY = adjustments[next].YPlacement;
            Value(valueOrigin, pair, firstFormat, adjustments[at]);
            Value(valueOrigin, pair + firstSize, secondFormat, adjustments[next]);
            if (oldFirstAdvance != adjustments[at].XAdvance || oldFirstX != adjustments[at].XPlacement || oldFirstY != adjustments[at].YPlacement ||
                oldSecondAdvance != adjustments[next].XAdvance || oldSecondX != adjustments[next].XPlacement || oldSecondY != adjustments[next].YPlacement)
                UnsafeRange.Mark(glyphs, at, next, charge);
            // A null second ValueRecord permits overlapping pairs (AV, VA in AVA).
            return secondFormat == 0 ? 1 : -1;
        }

        private int Attach(int type, int sub, List<ShapingGlyph> glyphs, GlyphAdjustment[] adjustments,
            int at, int flags, int markSet, Func<int, int> advanceWidth)
        {
            if (data.U16(sub) != 1)
                throw new InvalidDataException("Invalid GPOS mark attachment format.");
            int markIndex = reads.Coverage(data, Offset(sub, sub + 2), glyphs[at].Id);
            if (markIndex < 0)
                return 0;
            int parent = Previous(glyphs, at, type != 6, flags, markSet);
            if (parent < 0 || (type == 6 && !filter.IsMark(glyphs[parent])))
                return 0;
            if (type == 6 && glyphs[at].MarkLigature != null && glyphs[parent].MarkLigature == glyphs[at].MarkLigature &&
                glyphs[parent].MarkComponent != glyphs[at].MarkComponent)
                return 0;
            int baseIndex = reads.Coverage(data, Offset(sub, sub + 4), glyphs[parent].Id);
            if (baseIndex < 0)
                return 0;
            int classes = data.U16(sub + 6), marks = Offset(sub, sub + 8), bases = Offset(sub, sub + 10);
            int markCount = data.U16(marks), baseCount = data.U16(bases);
            if (classes == 0 || markIndex >= markCount || baseIndex >= baseCount)
                throw new InvalidDataException("Invalid GPOS mark attachment array index.");
            data.Check(marks + 2, markCount * 4);
            data.Check(bases + 2, checked(baseCount * (type == 5 ? 1 : classes) * 2));
            int record = marks + 2 + markIndex * 4, markClass = data.U16(record);
            if (markClass >= classes)
                throw new InvalidDataException("Invalid GPOS mark class.");
            int anchorOrigin = bases;
            int baseField;
            if (type == 5)
            {
                anchorOrigin = Offset(bases, bases + 2 + baseIndex * 2);
                int components = data.U16(anchorOrigin);
                if (components == 0)
                    throw new InvalidDataException("Empty GPOS ligature attachment.");
                data.Check(anchorOrigin + 2, checked(components * classes * 2));
                // Unassociated marks (including directly encoded ligatures) use the last component.
                int component = glyphs[at].MarkLigature == glyphs[parent] ? glyphs[at].MarkComponent : components - 1;
                component = Math.Min(component, components - 1);
                if (component < 0)
                    throw new InvalidDataException("Invalid ligature component association.");
                baseField = checked(anchorOrigin + 2 + (component * classes + markClass) * 2);
            }
            else
                baseField = checked(bases + 2 + (baseIndex * classes + markClass) * 2);
            if (data.U16(baseField) == 0)
                return 0; // This base/mark class has no attachment point.
            FontAnchor markAnchor = Anchor(Offset(marks, record + 2));
            FontAnchor baseAnchor = Anchor(Offset(anchorOrigin, baseField));
            var adjustment = adjustments[at];
            UnsafeRange.Mark(glyphs, parent, at, charge);
            adjustment.Parent = parent;
            adjustment.CursiveParent = -1;
            adjustment.XPlacement = checked(baseAnchor.X - markAnchor.X);
            adjustment.YPlacement = checked(baseAnchor.Y - markAnchor.Y);
            if (advanceWidth != null && !adjustment.ZeroMarkAdvance)
            {
                adjustment.XAdvance = checked(adjustment.XAdvance - advanceWidth(glyphs[at].Id));
                adjustment.ZeroMarkAdvance = true;
            }
            return 1;
        }

        private int Previous(List<ShapingGlyph> glyphs, int at, bool skipMarks, int flags, int markSet)
        {
            for (int parent = at - 1; parent >= 0; parent--)
            {
                Tick();
                if (options.Script == "arab" && UnicodeData.IsIgnorable(glyphs[parent].Scalar)) continue;
                if (!filter.Ignored(glyphs[parent].Id, flags, markSet) && (!skipMarks || !filter.IsMark(glyphs[parent])))
                    return parent;
            }
            return -1;
        }

        private FontAnchor Anchor(int offset)
        {
            int format = data.U16(offset), x = data.I16(offset + 2), y = data.I16(offset + 4);
            if (format == 2)
                data.U16(offset + 6); // Contour-point hinting is deliberately not executed.
            else if (format == 3)
            {
                int xDevice = data.U16(offset + 6), yDevice = data.U16(offset + 8);
                if (xDevice != 0)
                    Device(checked(offset + xDevice));
                if (yDevice != 0)
                    Device(checked(offset + yDevice));
            }
            else if (format != 1)
                throw new InvalidDataException("Invalid GPOS anchor format.");
            return new FontAnchor(x, y);
        }

        private void Device(int offset)
        {
            // Validate pixel corrections, but leave unhinted design coordinates unchanged.
            int start = data.U16(offset), end = data.U16(offset + 2), format = data.U16(offset + 4);
            if (format == 0x8000)
                throw new NotSupportedException("GPOS variation indices are unsupported.");
            if (format < 1 || format > 3 || end < start)
                throw new InvalidDataException("Invalid GPOS device adjustment.");
            int bits = 1 << format;
            data.Check(offset + 6, ((end - start + 1) * bits + 15) / 16 * 2);
        }

        private void Resolve(List<ShapingGlyph> glyphs, GlyphAdjustment[] adjustments, Func<int, int> advanceWidth)
        {
            bool attached = false;
            foreach (var adjustment in adjustments)
                if (adjustment.Parent >= 0 || adjustment.CursiveParent >= 0) { attached = true; break; }
            if (!attached)
            {
                // Independent placements already contain their final offsets.
                // Retain the pen overflow checks and exactly the same operation
                // charges as Placements/ResolveAxis, without their scratch arrays.
                int x = 0, y = 0;
                for (int i = 0; i < glyphs.Count; i++)
                {
                    Tick();
                    x = checked(x + (advanceWidth == null ? 0 : advanceWidth(glyphs[i].Id)) + adjustments[i].XAdvance);
                    y = checked(y + adjustments[i].YAdvance);
                }
                for (int axis = 0; axis < 2; axis++)
                    for (int i = 0; i < glyphs.Count; i++) { Tick(); Tick(); }
                return;
            }
            int[] penX, penY, placedX, placedY;
            Placements(glyphs, adjustments, advanceWidth, out penX, out penY, out placedX, out placedY);
            for (int i = 0; i < adjustments.Length; i++)
            {
                adjustments[i].XPlacement = placedX[i];
                adjustments[i].YPlacement = placedY[i];
            }
        }

        private void Placements(List<ShapingGlyph> glyphs, GlyphAdjustment[] adjustments, Func<int, int> advanceWidth,
            out int[] penX, out int[] penY, out int[] placedX, out int[] placedY)
        {
            penX = new int[glyphs.Count]; penY = new int[glyphs.Count];
            int x = 0, y = 0;
            for (int i = 0; i < glyphs.Count; i++)
            {
                Tick();
                penX[i] = x; penY[i] = y;
                x = checked(x + (advanceWidth == null ? 0 : advanceWidth(glyphs[i].Id)) + adjustments[i].XAdvance);
                y = checked(y + adjustments[i].YAdvance);
                if ((options.Script == "hebr" || options.Script == "arab")) { penX[i] = -x; penY[i] = -y; }
            }
            placedX = ResolveAxis(adjustments, penX, false);
            placedY = ResolveAxis(adjustments, penY, true);
        }

        private int[] ResolveAxis(GlyphAdjustment[] adjustments, int[] pen, bool vertical)
        {
            var result = new int[adjustments.Length];
            var state = new byte[adjustments.Length];
            var path = new List<int>();
            for (int i = 0; i < adjustments.Length; i++)
            {
                path.Clear();
                int at = i;
                while (at >= 0 && state[at] == 0)
                {
                    Tick();
                    state[at] = 1;
                    path.Add(at);
                    var a = adjustments[at];
                    at = vertical && a.CursiveParent >= 0 ? a.CursiveParent : a.Parent;
                    if (at >= adjustments.Length)
                        throw new InvalidDataException("Invalid GPOS attachment parent.");
                }
                if (at >= 0 && state[at] == 1)
                    throw new InvalidDataException("Cyclic GPOS attachment relationship.");
                for (int j = path.Count - 1; j >= 0; j--)
                {
                    Tick();
                    at = path[j];
                    var a = adjustments[at];
                    int parent = vertical && a.CursiveParent >= 0 ? a.CursiveParent : a.Parent;
                    int value = vertical ? a.YPlacement : a.XPlacement;
                    result[at] = parent < 0 ? value : checked(value + result[parent] + pen[parent] - pen[at]);
                    state[at] = 2;
                }
            }
            return result;
        }

        private int Cursive(int sub, List<ShapingGlyph> glyphs, GlyphAdjustment[] adjustments,
            int at, int next, int flags, Func<int, int> advanceWidth)
        {
            if (data.U16(sub) != 1)
                throw new InvalidDataException("Invalid GPOS cursive format.");
            int coverage = Offset(sub, sub + 2), count = data.U16(sub + 4);
            data.Check(sub + 6, count * 4);
            int first = Coverage(coverage, glyphs[at].Id);
            if (first < 0 || next >= glyphs.Count)
                return 0;
            int second = Coverage(coverage, glyphs[next].Id);
            if (second < 0)
                return 0;
            if (first >= count || second >= count)
                throw new InvalidDataException("Invalid GPOS cursive coverage index.");
            int exitField = sub + 8 + first * 4, entryField = sub + 6 + second * 4;
            if (data.U16(exitField) == 0 || data.U16(entryField) == 0)
                return 0;
            FontAnchor exit = Anchor(Offset(sub, exitField)), entry = Anchor(Offset(sub, entryField));
            int[] penX, penY, placedX, placedY;
            Placements(glyphs, adjustments, advanceWidth, out penX, out penY, out placedX, out placedY);
            // RTL removes the exit from the first logical glyph; LTR keeps it
            // on its advance. The complementary entry adjustment follows the next
            // glyph. Canonical LTR positioning keeps the exit on the first
            // glyph's advance, and removes the entry from both the next
            // glyph's placement and advance. Equal final ink positions alone
            // are insufficient: consumers also need the correct run width.
            if (options.Script == "arab") {
                int exitAdjustment = checked(placedX[at] + exit.X);
                adjustments[at].XAdvance = checked(adjustments[at].XAdvance - exitAdjustment);
                adjustments[at].XPlacement = checked(adjustments[at].XPlacement - exitAdjustment);
                adjustments[next].XAdvance = checked(placedX[next] + entry.X - (advanceWidth == null ? 0 : advanceWidth(glyphs[next].Id)));
            } else {
                adjustments[at].XAdvance = checked(placedX[at] + exit.X -
                    (advanceWidth == null ? 0 : advanceWidth(glyphs[at].Id)));
                int entryAdjustment = checked(placedX[next] + entry.X);
                adjustments[next].XAdvance = checked(adjustments[next].XAdvance - entryAdjustment);
                adjustments[next].XPlacement = checked(adjustments[next].XPlacement - entryAdjustment);
            }
            UnsafeRange.Mark(glyphs, at, next, charge);
            int child = (flags & 1) == 0 ? next : at, parent = (flags & 1) == 0 ? at : next;
            // Reversing an existing cursive connection must not create a cycle.
            // Keep the old parent's current baseline when breaking that connection.
            int ancestor = parent;
            for (int steps = 0; ancestor >= 0 && steps < adjustments.Length; steps++)
            {
                Tick();
                int previous = adjustments[ancestor].CursiveParent;
                if (previous == child)
                {
                    adjustments[ancestor].CursiveParent = -1;
                    adjustments[ancestor].YPlacement = placedY[ancestor];
                    break;
                }
                ancestor = previous;
            }
            adjustments[child].CursiveParent = parent;
            adjustments[child].YPlacement = (flags & 1) == 0 ? checked(exit.Y - entry.Y) : checked(entry.Y - exit.Y);
            return 1; // The next glyph may start another connection.
        }

        private static int ValueSize(int format)
        {
            if ((format & ~0xff) != 0)
                throw new InvalidDataException("Reserved GPOS value format bits.");
            int size = 0;
            for (int bit = 1; bit <= 128; bit <<= 1)
                if ((format & bit) != 0)
                    size += 2;
            return size;
        }

        private void Value(int sub, int record, int format, GlyphAdjustment result)
        {
            data.Check(record, ValueSize(format));
            for (int bit = 1; bit <= 128; bit <<= 1)
            {
                if ((format & bit) == 0)
                    continue;
                int value = data.I16(record);
                if (bit == 1)
                    result.XPlacement = checked(result.XPlacement + value);
                else if (bit == 2)
                    result.YPlacement = checked(result.YPlacement + value);
                else if (bit == 4)
                    result.XAdvance = checked(result.XAdvance + value);
                else if (bit == 8)
                {
                    // yAdvance is read but only applies to vertical layout.
                    // This positioning pipeline is horizontal.
                }
                else if (data.U16(record) != 0)
                {
                    Device(checked(sub + data.U16(record)));
                }
                record += 2;
            }
        }
    }
}
