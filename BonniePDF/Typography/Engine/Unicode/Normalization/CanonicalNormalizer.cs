// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    internal sealed class CanonicalNormalizer
    {
        private readonly ShapingLimits limits;
        private readonly Func<int, bool> covers;
        private readonly Func<int, int> combiningClass;
        private readonly Func<int, bool> preserve;
        private readonly int reorderLimit;
        private int operations;

        internal CanonicalNormalizer(ShapingLimits limits, Func<int, bool> covers = null, Func<int, int> combiningClass = null, Func<int, bool> preserve = null, int reorderLimit = int.MaxValue)
        {
            this.limits = limits; this.covers = covers;
            this.combiningClass = combiningClass ?? UnicodeData.CombiningClass;
            this.preserve = preserve;
            this.reorderLimit = reorderLimit;
        }
        private void Tick()
        {
            if (++operations > limits.MaxOperations) throw new InvalidDataException("Normalization operation limit exceeded.");
        }

        internal List<SourceScalar> Normalize(List<SourceScalar> input, bool compose = true)
        {
            operations = 0;
            var result = new List<SourceScalar>();
            foreach (var scalar in input)
            {
                if (preserve != null && preserve(scalar.Scalar)) { Tick(); result.Add(new SourceScalar(scalar.Scalar, scalar.Start, scalar.End)); if (result.Count > limits.MaxGlyphs) throw new InvalidDataException("Normalization expansion exceeds the glyph limit."); continue; }
                var expanded = new List<SourceScalar>();
                Expand(scalar, expanded, 0);
                bool supported = true;
                if (covers != null)
                    foreach (var item in expanded) { Tick(); supported &= covers(item.Scalar); }
                if (!supported && covers(scalar.Scalar)) result.Add(new SourceScalar(scalar.Scalar, scalar.Start, scalar.End));
                else result.AddRange(expanded);
                if (result.Count > limits.MaxGlyphs) throw new InvalidDataException("Normalization expansion exceeds the glyph limit.");
            }
            // Stable canonical ordering. Equal classes and class-zero blockers
            // never cross; every comparison is charged to the operation bound.
            for (int i = 1; i < result.Count; i++)
            {
                if (reorderLimit != int.MaxValue && (i == 1 || combiningClass(result[i - 1].Scalar) == 0))
                {
                    int end = i; while (end < result.Count && combiningClass(result[end].Scalar) != 0) { Tick(); end++; }
                    if (end - i + (i == 1 && combiningClass(result[0].Scalar) != 0 ? 1 : 0) > reorderLimit) { i = end - 1; continue; }
                }
                int ccc = combiningClass(result[i].Scalar);
                if (ccc == 0) continue;
                int p = i;
                while (p > 0)
                {
                    Tick();
                    int prior = combiningClass(result[p - 1].Scalar);
                    if (prior == 0 || prior <= ccc) break;
                    var swap = result[p - 1]; result[p - 1] = result[p]; result[p] = swap; p--;
                }
            }
            if (!compose || result.Count == 0) return result;
            var output = new List<SourceScalar>();
            int starter = -1, lastClass = 0;
            foreach (var scalar in result)
            {
                Tick();
                int ccc = combiningClass(scalar.Scalar);
                int candidate = starter >= 0 && (lastClass < ccc || lastClass == 0) ? Compose(output[starter].Scalar, scalar.Scalar) : -1;
                if (candidate >= 0 && (covers == null || covers(candidate)))
                {
                    var first = output[starter];
                    first.Scalar = candidate; first.Start = Math.Min(first.Start, scalar.Start); first.End = Math.Max(first.End, scalar.End);
                }
                else
                {
                    if (ccc == 0) starter = output.Count;
                    output.Add(scalar); lastClass = ccc;
                }
            }
            return output;
        }

        private void Expand(SourceScalar scalar, List<SourceScalar> output, int depth)
        {
            Tick();
            if (depth > limits.MaxNesting) throw new InvalidDataException("Normalization nesting limit exceeded.");
            int value = scalar.Scalar, syllable = value - 0xac00;
            int[] decomposition = null;
            if (syllable >= 0 && syllable < 11172)
                decomposition = syllable % 28 == 0 ? new[] { 0x1100 + syllable / 588, 0x1161 + syllable % 588 / 28 } :
                    new[] { 0x1100 + syllable / 588, 0x1161 + syllable % 588 / 28, 0x11a7 + syllable % 28 };
            else decomposition = UnicodeData.Decompose(value);
            if (decomposition == null) output.Add(new SourceScalar(value, scalar.Start, scalar.End));
            else foreach (int item in decomposition) Expand(new SourceScalar(item, scalar.Start, scalar.End), output, depth + 1);
            if (output.Count > limits.MaxGlyphs) throw new InvalidDataException("Normalization expansion exceeds the glyph limit.");
        }

        private static int Compose(int first, int second)
        {
            int l = first - 0x1100, v = second - 0x1161, s = first - 0xac00, t = second - 0x11a7;
            if (l >= 0 && l < 19 && v >= 0 && v < 21) return 0xac00 + (l * 21 + v) * 28;
            if (s >= 0 && s < 11172 && s % 28 == 0 && t > 0 && t < 28) return first + t;
            return UnicodeData.Compose(first, second);
        }
    }
}
