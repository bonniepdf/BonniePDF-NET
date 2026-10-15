// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;

namespace BonniePDF.Typography.Engine
{
    // The glyphs of a shaped run, packed into one integer array: the same values as GlyphInfo,
    // without an object per glyph. A run whose glyphs have no vertical advance or offset, one
    // component each and component index 0 (most text) keeps five integers per glyph; any other
    // run keeps nine. Immutable once built.
    internal sealed class GlyphRun
    {
        // Narrow and wide: [Id | IsMark << 16 | IsLigature << 17 | UnsafeToBreak << 18], Cluster,
        // AdvanceX, OffsetX, SourceLength. Wide only: AdvanceY, OffsetY, ComponentCount, ComponentIndex.
        private const int Narrow = 5, Wide = 9;
        private const int MarkBit = 1 << 16, LigatureBit = 1 << 17, UnsafeBit = 1 << 18;
        private readonly int[] data;
        private readonly int stride;
        internal readonly int Count;

        // An empty run of count glyphs, filled by Set before anything else sees it.
        internal GlyphRun(int count, bool wide)
        {
            Count = count;
            stride = wide ? Wide : Narrow;
            data = new int[count * stride];
        }

        internal int Id(int i) { return data[i * stride] & 0xffff; }
        internal int Cluster(int i) { return data[i * stride + 1]; }
        internal int AdvanceX(int i) { return data[i * stride + 2]; }
        internal int OffsetX(int i) { return data[i * stride + 3]; }
        internal int SourceLength(int i) { return data[i * stride + 4]; }
        internal int AdvanceY(int i) { return stride == Wide ? data[i * stride + 5] : 0; }
        internal int OffsetY(int i) { return stride == Wide ? data[i * stride + 6] : 0; }
        internal int ComponentCount(int i) { return stride == Wide ? data[i * stride + 7] : 1; }
        internal int ComponentIndex(int i) { return stride == Wide ? data[i * stride + 8] : 0; }
        internal bool IsMark(int i) { return (data[i * stride] & MarkBit) != 0; }
        internal bool IsLigature(int i) { return (data[i * stride] & LigatureBit) != 0; }
        internal bool UnsafeToBreak(int i) { return (data[i * stride] & UnsafeBit) != 0; }

        // Whether glyph i needs the wide layout.
        internal static bool NeedsWide(int advanceY, int offsetY, int componentCount, int componentIndex)
        {
            return advanceY != 0 || offsetY != 0 || componentCount != 1 || componentIndex != 0;
        }

        internal void Set(int i, int id, int cluster, int advanceX, int advanceY, int offsetX, int offsetY,
            int componentCount, bool isMark, bool isLigature, int sourceLength, bool unsafeToBreak, int componentIndex)
        {
            if (id < 0 || id > 0xffff) throw new ArgumentOutOfRangeException("id");
            int at = i * stride;
            data[at] = id | (isMark ? MarkBit : 0) | (isLigature ? LigatureBit : 0) | (unsafeToBreak ? UnsafeBit : 0);
            data[at + 1] = cluster; data[at + 2] = advanceX; data[at + 3] = offsetX; data[at + 4] = sourceLength;
            if (stride == Wide) { data[at + 5] = advanceY; data[at + 6] = offsetY; data[at + 7] = componentCount; data[at + 8] = componentIndex; }
        }

        // Glyph i as a GlyphInfo record.
        internal GlyphInfo Info(int i)
        {
            return new GlyphInfo(Id(i), Cluster(i), AdvanceX(i), AdvanceY(i), OffsetX(i), OffsetY(i), ComponentCount(i),
                IsMark(i), IsLigature(i), SourceLength(i), UnsafeToBreak(i), ComponentIndex(i));
        }

        // The sum of the horizontal advances, in design units.
        internal long TotalAdvanceX()
        {
            long total = 0;
            for (int i = 0, at = 2; i < Count; i++, at += stride) total += data[at];
            return total;
        }

        internal static GlyphRun From(IReadOnlyList<GlyphInfo> glyphs)
        {
            bool wide = false;
            foreach (var g in glyphs) if (NeedsWide(g.AdvanceY, g.OffsetY, g.ComponentCount, g.ComponentIndex)) { wide = true; break; }
            var run = new GlyphRun(glyphs.Count, wide);
            for (int i = 0; i < glyphs.Count; i++)
            {
                var g = glyphs[i];
                run.Set(i, g.Id, g.Cluster, g.AdvanceX, g.AdvanceY, g.OffsetX, g.OffsetY, g.ComponentCount, g.IsMark, g.IsLigature,
                    g.SourceLength, g.UnsafeToBreak, g.ComponentIndex);
            }
            return run;
        }

        // Glyphs first to end - 1, with clusterShift added to every cluster.
        internal GlyphRun Slice(int first, int end, int clusterShift)
        {
            if (first < 0 || end > Count || first > end) throw new ArgumentOutOfRangeException("first");
            var run = new GlyphRun(end - first, stride == Wide);
            Array.Copy(data, first * stride, run.data, 0, run.data.Length);
            if (clusterShift != 0) for (int at = 1; at < run.data.Length; at += stride) run.data[at] += clusterShift;
            return run;
        }

        // Builds a run glyph by glyph. Glyphs are kept wide until Build, which narrows them when it can.
        internal sealed class Builder
        {
            private int[] values;
            private int count;
            private bool wide;

            internal Builder(int capacity = 16) { values = new int[Math.Max(capacity, 1) * Wide]; }

            internal int Count { get { return count; } }

            internal void Add(int id, int cluster, int advanceX, int advanceY, int offsetX, int offsetY,
                int componentCount, bool isMark, bool isLigature, int sourceLength, bool unsafeToBreak, int componentIndex)
            {
                if (id < 0 || id > 0xffff) throw new ArgumentOutOfRangeException("id");
                if ((count + 1) * Wide > values.Length) Array.Resize(ref values, values.Length * 2);
                int at = count++ * Wide;
                values[at] = id | (isMark ? MarkBit : 0) | (isLigature ? LigatureBit : 0) | (unsafeToBreak ? UnsafeBit : 0);
                values[at + 1] = cluster; values[at + 2] = advanceX; values[at + 3] = offsetX; values[at + 4] = sourceLength;
                values[at + 5] = advanceY; values[at + 6] = offsetY; values[at + 7] = componentCount; values[at + 8] = componentIndex;
                if (NeedsWide(advanceY, offsetY, componentCount, componentIndex)) wide = true;
            }

            // Appends glyph i of source with clusterShift added to its cluster.
            internal void Add(GlyphRun source, int i, int clusterShift)
            {
                Add(source.Id(i), source.Cluster(i) + clusterShift, source.AdvanceX(i), source.AdvanceY(i), source.OffsetX(i), source.OffsetY(i),
                    source.ComponentCount(i), source.IsMark(i), source.IsLigature(i), source.SourceLength(i), source.UnsafeToBreak(i), source.ComponentIndex(i));
            }

            internal GlyphRun Build()
            {
                var run = new GlyphRun(count, wide);
                if (wide) Array.Copy(values, run.data, count * Wide);
                else for (int i = 0; i < count; i++) Array.Copy(values, i * Wide, run.data, i * Narrow, Narrow);
                return run;
            }
        }
    }
}
