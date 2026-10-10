// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;

namespace BonniePDF.Typography.Engine
{
    // Transient AMTRA ordering, followed by adjacent, font-covered composition.
    internal static class ArabicShaper
    {
        internal static List<SourceScalar> Reorder(FontFace face, List<SourceScalar> input)
        {
            var ordered = new List<SourceScalar>();
            for (int i = 0; i < input.Count;)
            {
                if (UnicodeData.CombiningClass(input[i].Scalar) == 0) { ordered.Add(input[i++]); continue; }
                int end = i;
                while (end < input.Count && UnicodeData.CombiningClass(input[end].Scalar) != 0) end++;
                var taken = new HashSet<int>();
                foreach (int c in new[] { 220, 230 })
                    for (int j = i; j < end; j++)
                    {
                        if (UnicodeData.CombiningClass(input[j].Scalar) != c) continue;
                        if (!ArabicData.IsModifier(input[j].Scalar)) break;
                        ordered.Add(input[j]); taken.Add(j);
                    }
                for (int j = i; j < end; j++)
                    if (UnicodeData.CombiningClass(input[j].Scalar) == 33) { ordered.Add(input[j]); taken.Add(j); }
                for (int j = i; j < end; j++) if (!taken.Contains(j)) ordered.Add(input[j]);
                i = end;
            }
            var result = new List<SourceScalar>();
            foreach (var token in ordered)
            {
                var prior = result.Count == 0 ? null : result[result.Count - 1];
                int cp = prior == null ? -1 : UnicodeData.Compose(prior.Scalar, token.Scalar);
                if (cp >= 0 && face.Covers(cp))
                {
                    prior.Scalar = cp; prior.Start = Math.Min(prior.Start, token.Start); prior.End = Math.Max(prior.End, token.End);
                }
                else result.Add(new SourceScalar(token.Scalar, token.Start, token.End));
            }
            return result;
        }

        internal static void Join(List<ShapingGlyph> run)
        {
            int prior = -1; string previous = "U";
            for (int i = 0; i < run.Count; i++)
            {
                string type = ArabicData.JoiningType(run[i].Scalar);
                if (type == "T") continue;
                run[i].JoiningForm = "RDL".IndexOf(type) >= 0 ? "isol" : "";
                if (prior >= 0 && "DLC".IndexOf(previous) >= 0 && "DRC".IndexOf(type) >= 0)
                {
                    if (run[prior].JoiningForm.Length != 0) run[prior].JoiningForm = run[prior].JoiningForm == "fina" ? "medi" : "init";
                    if (run[i].JoiningForm.Length != 0) run[i].JoiningForm = "fina";
                    for (int j = prior; j <= i; j++) if (run[j].Cluster != Math.Min(run[prior].Cluster, run[i].Cluster)) run[j].UnsafeBefore = true;
                }
                prior = i; previous = type;
            }
        }
    }
}
