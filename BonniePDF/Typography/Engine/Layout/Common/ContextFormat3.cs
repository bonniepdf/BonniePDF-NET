// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;

namespace BonniePDF.Typography.Engine
{
    // A context subtable of format 3 (GSUB 5/6, GPOS 7/8), read and checked once. Such a
    // subtable names a coverage table per position, and a glyph its first input coverage does
    // not list cannot start a match. Matching it anyway walks back over the backtrack before
    // reaching that test, so the lookup charges the walk; the caller charges the same steps
    // from this plan without reading the subtable. Built only when every count, array and
    // coverage table a failed match reads is in bounds and maps exactly (CoverageMap), so a
    // skipped match could not have thrown.
    internal sealed class ContextFormat3
    {
        internal readonly bool Chained;
        internal readonly CoverageMap[] Backtrack;
        internal readonly CoverageMap FirstInput;
        internal readonly int FirstInputOffset;
        internal readonly int InputCount;

        private ContextFormat3(bool chained, CoverageMap[] backtrack, CoverageMap firstInput, int firstInputOffset, int inputCount)
        {
            Chained = chained; Backtrack = backtrack;
            FirstInput = firstInput; FirstInputOffset = firstInputOffset; InputCount = inputCount;
        }

        // The plan for the format 3 subtable at sub, or null for any other subtable or one
        // whose failed match reads something that could throw.
        internal static ContextFormat3 TryBuild(BigEndianData data, LayoutReadCache reads, int sub, bool chained)
        {
            try
            {
                if (data.U16(sub) != 3) return null;
                int p = sub + 2;
                var backtrack = Array.Empty<CoverageMap>();
                if (chained)
                {
                    int back = data.U16(p);
                    p += 2;
                    data.Check(p, back * 2);
                    backtrack = new CoverageMap[back];
                    for (int i = 0; i < back; i++)
                        if ((backtrack[i] = reads.CoverageMap(data, checked(sub + data.U16(p + i * 2)))) == null) return null;
                    p += back * 2;
                }
                int input = data.U16(p);
                p += 2;
                if (!chained) { data.U16(p); p += 2; }
                if (input < 1) return null;
                data.Check(p, input * 2);
                int firstOffset = checked(sub + data.U16(p));
                var first = reads.CoverageMap(data, firstOffset);
                if (first == null) return null;
                p += input * 2;
                if (chained)
                {
                    int ahead = data.U16(p);
                    p += 2;
                    data.Check(p, ahead * 2);
                    p += ahead * 2;
                    data.U16(p);
                }
                return new ContextFormat3(chained, backtrack, first, firstOffset, input);
            }
            catch (Exception) { return null; }
        }
    }
}

