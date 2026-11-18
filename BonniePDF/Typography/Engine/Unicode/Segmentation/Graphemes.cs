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
                bool ep = string.Equals(LayoutUnicodeData.ExtendedPictographic(p[i]), "Y", System.StringComparison.Ordinal), join = false;
                if (i > 0)
                {
                    if (string.Equals(previous, "CR", System.StringComparison.Ordinal) && string.Equals(current, "LF", System.StringComparison.Ordinal)) join = true;
                    else if (In(previous, "Control", "CR", "LF") || In(current, "Control", "CR", "LF")) join = false;
                    else if (string.Equals(previous, "L", System.StringComparison.Ordinal) && In(current, "L", "V", "LV", "LVT")) join = true;
                    else if (In(previous, "LV", "V") && In(current, "V", "T")) join = true;
                    else if (In(previous, "LVT", "T") && string.Equals(current, "T", System.StringComparison.Ordinal)) join = true;
                    else if (In(current, "Extend", "ZWJ", "SpacingMark") || string.Equals(previous, "Prepend", System.StringComparison.Ordinal)) join = true;
                    else if (string.Equals(incb, "Consonant", System.StringComparison.Ordinal) && conjunct == 2) join = true;
                    else if (ep && string.Equals(previous, "ZWJ", System.StringComparison.Ordinal) && zwjAfterPictographic) join = true;
                    else if (string.Equals(previous, "Regional_Indicator", System.StringComparison.Ordinal) && string.Equals(current, "Regional_Indicator", System.StringComparison.Ordinal) && regional % 2 == 1) join = true;
                    if (!join) result.Add(input.Offsets[i]);
                }
                zwjAfterPictographic = string.Equals(current, "ZWJ", System.StringComparison.Ordinal) && pictographic;
                if (ep) pictographic = true; else if (!string.Equals(current, "Extend", System.StringComparison.Ordinal)) pictographic = false;
                if (string.Equals(incb, "Consonant", System.StringComparison.Ordinal)) conjunct = 1;
                else if (string.Equals(incb, "Linker", System.StringComparison.Ordinal) && conjunct > 0) conjunct = 2;
                else if (!string.Equals(incb, "Extend", System.StringComparison.Ordinal) && !string.Equals(incb, "Linker", System.StringComparison.Ordinal)) conjunct = 0;
                regional = string.Equals(current, "Regional_Indicator", System.StringComparison.Ordinal) ? regional + 1 : 0; previous = current;
            }
            if (p.Count > 0) result.Add(text.Length);
            return result.AsReadOnly();
        }
        private static bool In(string value, params string[] values) { foreach (string v in values) if (string.Equals(v, value, System.StringComparison.Ordinal)) return true; return false; }
    }
}
