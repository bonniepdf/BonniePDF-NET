// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;

namespace BonniePDF.Typography.Engine
{
    // The glyphs of a shaped run, packed into one integer array: the same values as GlyphInfo,
    // without an object per glyph. Three layouts, chosen by what the glyphs need:
    //   compact (2 integers): one glyph per UTF-16 unit in source order, clusters consecutive from
    //     the first, no horizontal offset, no mark or ligature, and nothing the wide layout holds
    //     (plain text in a simple script, the usual case);
    //   narrow (5): no vertical advance or offset, one component each and component index 0;
    //   wide (9): anything else.
    // Immutable once built.
    internal sealed class GlyphRun
    {
        // All: [Id | IsMark << 16 | IsLigature << 17 | UnsafeToBreak << 18]. Compact: AdvanceX; the
        // cluster is firstCluster + i and the source length 1. Narrow and wide: Cluster, AdvanceX,
        // OffsetX, SourceLength. Wide only: AdvanceY, OffsetY, ComponentCount, ComponentIndex.
        private const int Compact = 2, Narrow = 5, Wide = 9;
        private const int MarkBit = 1 << 16, LigatureBit = 1 << 17, UnsafeBit = 1 << 18;
        private readonly int[] data;
        private readonly int stride;
        private readonly int firstCluster;
        internal readonly int Count;

        // An empty run of count glyphs, filled by Set before anything else sees it.
        internal GlyphRun(int count, bool wide) : this(count, wide ? Wide : Narrow, 0) { }

        // An empty compact run of count glyphs whose clusters run from firstCluster, filled by Set.
        internal static GlyphRun CompactRun(int count, int firstCluster) { return new GlyphRun(count, Compact, firstCluster); }

        private GlyphRun(int count, int stride, int firstCluster)
        {
            Count = count;
            this.stride = stride;
            this.firstCluster = firstCluster;
            data = new int[count * stride];
        }

        // Whether the run uses the compact layout (for tests).
        internal bool IsCompact { get { return stride == Compact; } }

        internal int Id(int i) { return data[i * stride] & 0xffff; }
        internal int Cluster(int i) { return stride == Compact ? firstCluster + i : data[i * stride + 1]; }
        internal int AdvanceX(int i) { return data[i * stride + (stride == Compact ? 1 : 2)]; }
        internal int OffsetX(int i) { return stride == Compact ? 0 : data[i * stride + 3]; }
        internal int SourceLength(int i) { return stride == Compact ? 1 : data[i * stride + 4]; }
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

        // Whether glyph i, at cluster, fits the compact layout of a run whose clusters start at firstCluster.
        private static bool FitsCompact(int i, int firstCluster, int cluster, int offsetX, int sourceLength, bool isMark, bool isLigature)
        {
            return cluster == firstCluster + i && offsetX == 0 && sourceLength == 1 && !isMark && !isLigature;
        }

        internal void Set(int i, int id, int cluster, int advanceX, int advanceY, int offsetX, int offsetY,
            int componentCount, bool isMark, bool isLigature, int sourceLength, bool unsafeToBreak, int componentIndex)
        {
            if (id < 0 || id > 0xffff) throw new ArgumentOutOfRangeException("id");
            int at = i * stride;
            data[at] = id | (isMark ? MarkBit : 0) | (isLigature ? LigatureBit : 0) | (unsafeToBreak ? UnsafeBit : 0);
            if (stride == Compact)
            {
                if (!FitsCompact(i, firstCluster, cluster, offsetX, sourceLength, isMark, isLigature) ||
                    NeedsWide(advanceY, offsetY, componentCount, componentIndex))
                    throw new InvalidOperationException("Glyph does not fit a compact run.");
                data[at + 1] = advanceX;
                return;
            }
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
            for (int i = 0, at = stride == Compact ? 1 : 2; i < Count; i++, at += stride) total += data[at];
            return total;
        }

        internal static GlyphRun From(IReadOnlyList<GlyphInfo> glyphs)
        {
            bool wide = false, compact = glyphs.Count > 0;
            int first = glyphs.Count > 0 ? glyphs[0].Cluster : 0;
            for (int i = 0; i < glyphs.Count && !wide; i++)
            {
                var g = glyphs[i];
                wide = NeedsWide(g.AdvanceY, g.OffsetY, g.ComponentCount, g.ComponentIndex);
                compact = compact && FitsCompact(i, first, g.Cluster, g.OffsetX, g.SourceLength, g.IsMark, g.IsLigature);
            }
            var run = new GlyphRun(glyphs.Count, wide ? Wide : compact ? Compact : Narrow, first);
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
            var run = new GlyphRun(end - first, stride, stride == Compact ? checked(firstCluster + first + clusterShift) : 0);
            Array.Copy(data, first * stride, run.data, 0, run.data.Length);
            if (clusterShift != 0 && stride != Compact) for (int at = 1; at < run.data.Length; at += stride) run.data[at] += clusterShift;
            return run;
        }

        // Builds a run glyph by glyph. Glyphs are kept wide until Build, which narrows them when it can.
        internal sealed class Builder
        {
            private int[] values;
            private int count;
            private bool wide;
            private bool compact = true;

            internal Builder(int capacity = 16) { values = new int[Math.Max(capacity, 1) * Wide]; }

            internal int Count { get { return count; } }

            internal void Add(int id, int cluster, int advanceX, int advanceY, int offsetX, int offsetY,
                int componentCount, bool isMark, bool isLigature, int sourceLength, bool unsafeToBreak, int componentIndex)
            {
                if (id < 0 || id > 0xffff) throw new ArgumentOutOfRangeException("id");
                if ((count + 1) * Wide > values.Length) Array.Resize(ref values, values.Length * 2);
                if (compact && !FitsCompact(count, count == 0 ? cluster : values[1], cluster, offsetX, sourceLength, isMark, isLigature)) compact = false;
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
                int stride = wide ? Wide : compact && count > 0 ? Compact : Narrow;
                var run = new GlyphRun(count, stride, stride == Compact ? values[1] : 0);
                if (wide) Array.Copy(values, run.data, count * Wide);
                else if (stride == Compact)
                    for (int i = 0; i < count; i++) { run.data[i * Compact] = values[i * Wide]; run.data[i * Compact + 1] = values[i * Wide + 2]; }
                else for (int i = 0; i < count; i++) Array.Copy(values, i * Wide, run.data, i * Narrow, Narrow);
                return run;
            }
        }
    }
}
