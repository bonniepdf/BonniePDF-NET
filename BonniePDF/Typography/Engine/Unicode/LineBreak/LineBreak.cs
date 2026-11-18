// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace BonniePDF.Typography.Engine
{
    // UAX #14 rev. 55; tailoring runs only after non-tailorable rules LB2-12.
    internal static class LineBreak
    {
        private static bool In(string value, params string[] values) { foreach (string v in values) if (string.Equals(v, value, StringComparison.Ordinal)) return true; return false; }
        internal static ReadOnlyCollection<BreakOpportunity> Opportunities(string text, Func<int, bool, bool> tailoring = null, int maxTextLength = 65536)
        {
            var input = new UnicodeText(text, maxTextLength); var points = input.Points; int n = points.Count;
            if (n == 0) return new List<BreakOpportunity> { new BreakOpportunity(0, true) }.AsReadOnly();
            var raw = points.Select(cp => {
                var c = LayoutUnicodeData.LineClass(cp);
                if (In(c, "AI","SG","XX")) return "AL";
                if (string.Equals(c, "SA", StringComparison.Ordinal)) return In(LayoutUnicodeData.LineCategory(cp), "Mn","Mc") ? "CM" : "AL";
                return string.Equals(c, "CJ", StringComparison.Ordinal) ? "NS" : c;
            }).ToArray();
            var classes = new List<string>(); var categories = new List<string>(); var east = new List<bool>(); var cps = new List<int>();
            var significant = new int[n]; var inherited = new bool[n];
            for (int i = 0; i < n; i++) {
                var cm = string.Equals(raw[i], "CM", StringComparison.Ordinal) || string.Equals(raw[i], "ZWJ", StringComparison.Ordinal);
                inherited[i] = cm && classes.Count > 0 && !In(classes[classes.Count - 1], "BK","CR","LF","NL","SP","ZW");
                if (!inherited[i]) {
                    classes.Add(cm ? "AL" : raw[i]); cps.Add(cm ? 65 : points[i]); categories.Add(cm ? "Lu" : LayoutUnicodeData.LineCategory(points[i]));
                    east.Add(!cm && string.Equals(LayoutUnicodeData.EastAsianFlag(points[i]), "Y", StringComparison.Ordinal));
                }
                significant[i] = classes.Count - 1;
            }
            var nonspace = new int[classes.Count]; var numeric = new bool[classes.Count]; var ri = new int[classes.Count];
            for (int j = 0; j < classes.Count; j++) {
                var c = classes[j]; nonspace[j] = string.Equals(c, "SP", StringComparison.Ordinal) ? (j > 0 ? nonspace[j-1] : -1) : j;
                numeric[j] = string.Equals(c, "NU", StringComparison.Ordinal) || (In(c, "SY","IS") && j > 0 && numeric[j-1]);
                ri[j] = string.Equals(c, "RI", StringComparison.Ordinal) ? (j > 0 ? ri[j-1] : 0) + 1 : 0;
            }
            string At(int j) { return j < 0 || j >= classes.Count ? "sot" : classes[j]; }
            bool Brahmic(int j) { return In(At(j), "AK", "AS") || (j >= 0 && j < cps.Count && cps[j] == 0x25cc); }
            var result = new List<BreakOpportunity>();
            for (int i = 1; i < n; i++) {
                string a0 = raw[i-1], b0 = raw[i]; int j = significant[i-1], k = significant[i];
                int ns = nonspace[j]; string a = At(j), b = At(k), prev = At(j-1), next = At(k+1), s = At(ns);
                bool allowed = false, mandatory = false, fixedRule = true;
                if (string.Equals(a0, "BK", StringComparison.Ordinal) || (In(a0, "CR","LF","NL") && !(string.Equals(a0, "CR", StringComparison.Ordinal) && string.Equals(b0, "LF", StringComparison.Ordinal)))) { allowed = true; mandatory = true; }
                else if (string.Equals(a0, "CR", StringComparison.Ordinal) && string.Equals(b0, "LF", StringComparison.Ordinal)) allowed = false;
                else if (In(b0, "BK","CR","LF","NL","SP","ZW")) allowed = false;
                else if (string.Equals(s, "ZW", StringComparison.Ordinal)) allowed = true; // LB8, before LB9
                else if (string.Equals(a0, "ZWJ", StringComparison.Ordinal) || inherited[i]) allowed = false;
                else if (string.Equals(a, "WJ", StringComparison.Ordinal) || string.Equals(b, "WJ", StringComparison.Ordinal) || string.Equals(a, "GL", StringComparison.Ordinal)) allowed = false;
                else {
                    fixedRule = false;
                    if (string.Equals(b, "GL", StringComparison.Ordinal) && !In(a, "SP","BA","HY","HH")) allowed = false;
                    else if (In(b, "CL","CP","EX","SY") || string.Equals(s, "OP", StringComparison.Ordinal)) allowed = false;
                    else if (string.Equals(s, "QU", StringComparison.Ordinal) && string.Equals(categories[ns], "Pi", StringComparison.Ordinal) && (ns == 0 || In(At(ns-1), "BK","CR","LF","NL","OP","QU","GL","SP","ZW"))) allowed = false;
                    else if (string.Equals(b, "QU", StringComparison.Ordinal) && string.Equals(categories[k], "Pf", StringComparison.Ordinal) && (k + 1 == classes.Count || In(next, "SP","GL","WJ","CL","QU","CP","EX","IS","SY","BK","CR","LF","NL","ZW"))) allowed = false;
                    else if (string.Equals(a, "SP", StringComparison.Ordinal) && string.Equals(b, "IS", StringComparison.Ordinal) && string.Equals(next, "NU", StringComparison.Ordinal)) allowed = true;
                    else if (string.Equals(b, "IS", StringComparison.Ordinal)) allowed = false;
                    else if ((In(s, "CL","CP") && string.Equals(b, "NS", StringComparison.Ordinal)) || (string.Equals(s, "B2", StringComparison.Ordinal) && string.Equals(b, "B2", StringComparison.Ordinal))) allowed = false;
                    else if (string.Equals(a, "SP", StringComparison.Ordinal)) allowed = true;
                    else if ((string.Equals(b, "QU", StringComparison.Ordinal) && !string.Equals(categories[k], "Pi", StringComparison.Ordinal)) || (string.Equals(a, "QU", StringComparison.Ordinal) && !string.Equals(categories[j], "Pf", StringComparison.Ordinal))) allowed = false;
                    else if ((string.Equals(b, "QU", StringComparison.Ordinal) && (!east[j] || k + 1 == classes.Count || !east[k+1])) || (string.Equals(a, "QU", StringComparison.Ordinal) && (!east[k] || j == 0 || !east[j-1]))) allowed = false;
                    else if (string.Equals(a, "CB", StringComparison.Ordinal) || string.Equals(b, "CB", StringComparison.Ordinal)) allowed = true;
                    else if (In(a, "HY","HH") && In(b, "AL","HL") && (j == 0 || In(prev, "BK","CR","LF","NL","SP","ZW","CB","GL"))) allowed = false;
                    else if (In(b, "BA","HH","HY","NS") || string.Equals(a, "BB", StringComparison.Ordinal)) allowed = false;
                    else if ((string.Equals(prev, "HL", StringComparison.Ordinal) && In(a, "HY","HH") && !string.Equals(b, "HL", StringComparison.Ordinal)) || (string.Equals(a, "SY", StringComparison.Ordinal) && string.Equals(b, "HL", StringComparison.Ordinal)) || string.Equals(b, "IN", StringComparison.Ordinal)) allowed = false;
                    else if ((In(a, "AL","HL") && string.Equals(b, "NU", StringComparison.Ordinal)) || (string.Equals(a, "NU", StringComparison.Ordinal) && In(b, "AL","HL"))) allowed = false;
                    else if ((string.Equals(a, "PR", StringComparison.Ordinal) && In(b, "ID","EB","EM")) || (In(a, "ID","EB","EM") && string.Equals(b, "PO", StringComparison.Ordinal))) allowed = false;
                    else if ((In(a, "PR","PO") && In(b, "AL","HL")) || (In(a, "AL","HL") && In(b, "PR","PO"))) allowed = false;
                    else if ((In(b, "PO","PR") && (numeric[j] || (In(a, "CL","CP") && j > 0 && numeric[j-1]))) ||
                        (In(a, "PO","PR") && (string.Equals(b, "NU", StringComparison.Ordinal) || (string.Equals(b, "OP", StringComparison.Ordinal) && (string.Equals(next, "NU", StringComparison.Ordinal) || (string.Equals(next, "IS", StringComparison.Ordinal) && string.Equals(At(k + 2), "NU", StringComparison.Ordinal)))))) ||
                        (In(a, "HY","IS") && string.Equals(b, "NU", StringComparison.Ordinal)) || (numeric[j] && string.Equals(b, "NU", StringComparison.Ordinal))) allowed = false;
                    else if ((string.Equals(a, "JL", StringComparison.Ordinal) && In(b, "JL","JV","H2","H3")) || (In(a, "JV","H2") && In(b, "JV","JT")) || (In(a, "JT","H3") && string.Equals(b, "JT", StringComparison.Ordinal))) allowed = false;
                    else if ((In(a, "JL","JV","JT","H2","H3") && string.Equals(b, "PO", StringComparison.Ordinal)) || (string.Equals(a, "PR", StringComparison.Ordinal) && In(b, "JL","JV","JT","H2","H3"))) allowed = false;
                    else if (In(a, "AL","HL") && In(b, "AL","HL")) allowed = false;
                    else if ((string.Equals(a, "AP", StringComparison.Ordinal) && Brahmic(k)) || (Brahmic(j) && In(b, "VF","VI")) || (Brahmic(j-1) && string.Equals(a, "VI", StringComparison.Ordinal) && (string.Equals(b, "AK", StringComparison.Ordinal) || cps[k] == 0x25cc)) || (Brahmic(j) && Brahmic(k) && string.Equals(next, "VF", StringComparison.Ordinal))) allowed = false;
                    else if (string.Equals(a, "IS", StringComparison.Ordinal) && In(b, "AL","HL")) allowed = false;
                    else if ((In(a, "AL","HL","NU") && string.Equals(b, "OP", StringComparison.Ordinal) && !east[k]) || (string.Equals(a, "CP", StringComparison.Ordinal) && !east[j] && In(b, "AL","HL","NU"))) allowed = false;
                    else if (string.Equals(a, "RI", StringComparison.Ordinal) && string.Equals(b, "RI", StringComparison.Ordinal) && ri[j] % 2 == 1) allowed = false;
                    else if (string.Equals(b, "EM", StringComparison.Ordinal) && (string.Equals(a, "EB", StringComparison.Ordinal) || (string.Equals(LayoutUnicodeData.ExtendedPictographic(cps[j]), "Y", StringComparison.Ordinal) && string.Equals(categories[j], "Cn", StringComparison.Ordinal)))) allowed = false;
                    else allowed = true;
                }
                if (!fixedRule && tailoring != null) allowed = tailoring(input.Offsets[i], allowed);
                if (allowed) result.Add(new BreakOpportunity(input.Offsets[i], mandatory));
            }
            result.Add(new BreakOpportunity(text.Length, true));
            return result.AsReadOnly();
        }
    }
}
