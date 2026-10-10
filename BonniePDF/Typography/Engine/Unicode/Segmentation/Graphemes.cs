// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BonniePDF.Typography.Engine
{
    // UAX #29 rev. 47: extended grapheme clusters, original UTF-16 offsets.
    internal static class Graphemes
    {
        internal static ReadOnlyCollection<int> Boundaries(string text, int maxTextLength = 65536)
        {
            var input = new UnicodeText(text, maxTextLength); var p = input.Points;
            var result = new List<int> { 0 };
            string previous = ""; int regional = 0, conjunct = 0;
            bool pictographic = false, zwjAfterPictographic = false;
            for (int i = 0; i < p.Count; i++)
            {
                string current = LayoutUnicodeData.GraphemeClass(p[i]), incb = LayoutUnicodeData.IndicConjunct(p[i]);
                bool ep = LayoutUnicodeData.ExtendedPictographic(p[i]) == "Y", join = false;
                if (i > 0)
                {
                    if (previous == "CR" && current == "LF") join = true;
                    else if (In(previous, "Control", "CR", "LF") || In(current, "Control", "CR", "LF")) join = false;
                    else if (previous == "L" && In(current, "L", "V", "LV", "LVT")) join = true;
                    else if (In(previous, "LV", "V") && In(current, "V", "T")) join = true;
                    else if (In(previous, "LVT", "T") && current == "T") join = true;
                    else if (In(current, "Extend", "ZWJ", "SpacingMark") || previous == "Prepend") join = true;
                    else if (incb == "Consonant" && conjunct == 2) join = true;
                    else if (ep && previous == "ZWJ" && zwjAfterPictographic) join = true;
                    else if (previous == "Regional_Indicator" && current == "Regional_Indicator" && regional % 2 == 1) join = true;
                    if (!join) result.Add(input.Offsets[i]);
                }
                zwjAfterPictographic = current == "ZWJ" && pictographic;
                if (ep) pictographic = true; else if (current != "Extend") pictographic = false;
                if (incb == "Consonant") conjunct = 1;
                else if (incb == "Linker" && conjunct > 0) conjunct = 2;
                else if (incb != "Extend" && incb != "Linker") conjunct = 0;
                regional = current == "Regional_Indicator" ? regional + 1 : 0; previous = current;
            }
            if (p.Count > 0) result.Add(text.Length);
            return result.AsReadOnly();
        }
        private static bool In(string value, params string[] values) { foreach (string v in values) if (v == value) return true; return false; }
    }
}
