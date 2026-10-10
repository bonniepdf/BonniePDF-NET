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
        private static bool In(string value, params string[] values) { foreach (string v in values) if (v == value) return true; return false; }
        internal static ReadOnlyCollection<BreakOpportunity> Opportunities(string text, Func<int, bool, bool> tailoring = null, int maxTextLength = 65536)
        {
            var input = new UnicodeText(text, maxTextLength); var points = input.Points; int n = points.Count;
            if (n == 0) return new List<BreakOpportunity> { new BreakOpportunity(0, true) }.AsReadOnly();
            var raw = points.Select(cp => {
                var c = LayoutUnicodeData.LineClass(cp);
                if (In(c, "AI","SG","XX")) return "AL";
                if (c == "SA") return In(LayoutUnicodeData.LineCategory(cp), "Mn","Mc") ? "CM" : "AL";
                return c == "CJ" ? "NS" : c;
            }).ToArray();
            var classes = new List<string>(); var categories = new List<string>(); var east = new List<bool>(); var cps = new List<int>();
            var significant = new int[n]; var inherited = new bool[n];
            for (int i = 0; i < n; i++) {
                var cm = raw[i] == "CM" || raw[i] == "ZWJ";
                inherited[i] = cm && classes.Count > 0 && !In(classes[classes.Count - 1], "BK","CR","LF","NL","SP","ZW");
                if (!inherited[i]) {
                    classes.Add(cm ? "AL" : raw[i]); cps.Add(cm ? 65 : points[i]); categories.Add(cm ? "Lu" : LayoutUnicodeData.LineCategory(points[i]));
                    east.Add(!cm && LayoutUnicodeData.EastAsianFlag(points[i]) == "Y");
                }
                significant[i] = classes.Count - 1;
            }
            var nonspace = new int[classes.Count]; var numeric = new bool[classes.Count]; var ri = new int[classes.Count];
            for (int j = 0; j < classes.Count; j++) {
                var c = classes[j]; nonspace[j] = c == "SP" ? (j > 0 ? nonspace[j-1] : -1) : j;
                numeric[j] = c == "NU" || (In(c, "SY","IS") && j > 0 && numeric[j-1]);
                ri[j] = c == "RI" ? (j > 0 ? ri[j-1] : 0) + 1 : 0;
            }
            string At(int j) { return j < 0 || j >= classes.Count ? "sot" : classes[j]; }
            bool Brahmic(int j) { return In(At(j), "AK", "AS") || (j >= 0 && j < cps.Count && cps[j] == 0x25cc); }
            var result = new List<BreakOpportunity>();
            for (int i = 1; i < n; i++) {
                string a0 = raw[i-1], b0 = raw[i]; int j = significant[i-1], k = significant[i];
                int ns = nonspace[j]; string a = At(j), b = At(k), prev = At(j-1), next = At(k+1), s = At(ns);
                bool allowed = false, mandatory = false, fixedRule = true;
                if (a0 == "BK" || (In(a0, "CR","LF","NL") && !(a0 == "CR" && b0 == "LF"))) { allowed = true; mandatory = true; }
                else if (a0 == "CR" && b0 == "LF") allowed = false;
                else if (In(b0, "BK","CR","LF","NL","SP","ZW")) allowed = false;
                else if (s == "ZW") allowed = true; // LB8, before LB9
                else if (a0 == "ZWJ" || inherited[i]) allowed = false;
                else if (a == "WJ" || b == "WJ" || a == "GL") allowed = false;
                else {
                    fixedRule = false;
                    if (b == "GL" && !In(a, "SP","BA","HY","HH")) allowed = false;
                    else if (In(b, "CL","CP","EX","SY") || s == "OP") allowed = false;
                    else if (s == "QU" && categories[ns] == "Pi" && (ns == 0 || In(At(ns-1), "BK","CR","LF","NL","OP","QU","GL","SP","ZW"))) allowed = false;
                    else if (b == "QU" && categories[k] == "Pf" && (k + 1 == classes.Count || In(next, "SP","GL","WJ","CL","QU","CP","EX","IS","SY","BK","CR","LF","NL","ZW"))) allowed = false;
                    else if (a == "SP" && b == "IS" && next == "NU") allowed = true;
                    else if (b == "IS") allowed = false;
                    else if ((In(s, "CL","CP") && b == "NS") || (s == "B2" && b == "B2")) allowed = false;
                    else if (a == "SP") allowed = true;
                    else if ((b == "QU" && categories[k] != "Pi") || (a == "QU" && categories[j] != "Pf")) allowed = false;
                    else if ((b == "QU" && (!east[j] || k + 1 == classes.Count || !east[k+1])) || (a == "QU" && (!east[k] || j == 0 || !east[j-1]))) allowed = false;
                    else if (a == "CB" || b == "CB") allowed = true;
                    else if (In(a, "HY","HH") && In(b, "AL","HL") && (j == 0 || In(prev, "BK","CR","LF","NL","SP","ZW","CB","GL"))) allowed = false;
                    else if (In(b, "BA","HH","HY","NS") || a == "BB") allowed = false;
                    else if ((prev == "HL" && In(a, "HY","HH") && b != "HL") || (a == "SY" && b == "HL") || b == "IN") allowed = false;
                    else if ((In(a, "AL","HL") && b == "NU") || (a == "NU" && In(b, "AL","HL"))) allowed = false;
                    else if ((a == "PR" && In(b, "ID","EB","EM")) || (In(a, "ID","EB","EM") && b == "PO")) allowed = false;
                    else if ((In(a, "PR","PO") && In(b, "AL","HL")) || (In(a, "AL","HL") && In(b, "PR","PO"))) allowed = false;
                    else if ((In(b, "PO","PR") && (numeric[j] || (In(a, "CL","CP") && j > 0 && numeric[j-1]))) ||
                        (In(a, "PO","PR") && (b == "NU" || (b == "OP" && (next == "NU" || (next == "IS" && At(k+2) == "NU"))))) ||
                        (In(a, "HY","IS") && b == "NU") || (numeric[j] && b == "NU")) allowed = false;
                    else if ((a == "JL" && In(b, "JL","JV","H2","H3")) || (In(a, "JV","H2") && In(b, "JV","JT")) || (In(a, "JT","H3") && b == "JT")) allowed = false;
                    else if ((In(a, "JL","JV","JT","H2","H3") && b == "PO") || (a == "PR" && In(b, "JL","JV","JT","H2","H3"))) allowed = false;
                    else if (In(a, "AL","HL") && In(b, "AL","HL")) allowed = false;
                    else if ((a == "AP" && Brahmic(k)) || (Brahmic(j) && In(b, "VF","VI")) || (Brahmic(j-1) && a == "VI" && (b == "AK" || cps[k] == 0x25cc)) || (Brahmic(j) && Brahmic(k) && next == "VF")) allowed = false;
                    else if (a == "IS" && In(b, "AL","HL")) allowed = false;
                    else if ((In(a, "AL","HL","NU") && b == "OP" && !east[k]) || (a == "CP" && !east[j] && In(b, "AL","HL","NU"))) allowed = false;
                    else if (a == "RI" && b == "RI" && ri[j] % 2 == 1) allowed = false;
                    else if (b == "EM" && (a == "EB" || (LayoutUnicodeData.ExtendedPictographic(cps[j]) == "Y" && categories[j] == "Cn"))) allowed = false;
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
