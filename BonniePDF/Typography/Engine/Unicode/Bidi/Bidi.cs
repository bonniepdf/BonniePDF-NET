// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace BonniePDF.Typography.Engine
{
    // UAX #9 rev. 51. The explicit depth limit is fixed by Unicode at 125.
    internal static class Bidi
    {
        private static bool In(string value, params string[] values) { foreach (string v in values) if (v == value) return true; return false; }
        private static bool Isolate(string t) { return In(t, "FSI", "LRI", "RLI"); }
        private static bool Removed(string t) { return In(t, "LRE", "RLE", "LRO", "RLO", "PDF", "BN"); }
        private static string Direction(int level) { return level % 2 != 0 ? "R" : "L"; }
        private static string Strong(string t) { return In(t, "EN", "AN") ? "R" : t; }

        internal static BidiParagraph ResolveParagraph(string text, string baseDirection = "auto", int maxTextLength = 65536, int maxOperations = 10000000)
        {
            if (!In(baseDirection, "auto", "ltr", "rtl") || maxOperations < 1) throw new ArgumentException("Invalid bidi options.");
            var input = new UnicodeText(text, maxTextLength); var points = input.Points; int n = points.Count;
            string[] original = points.Select(LayoutUnicodeData.BidiClass).ToArray(), types = (string[])original.Clone();
            for (int i = 0; i < n - 1; i++) if (original[i] == "B" && !(i == n - 2 && points[i] == 13 && points[i + 1] == 10)) throw new ArgumentException("Resolve one paragraph at a time, including its final separator.");
            int operations = 0;
            void Tick() { if (operations == maxOperations) throw new InvalidOperationException("Unicode operation limit exceeded."); operations++; }
            var matching = Enumerable.Repeat(-1, n).ToArray(); var isolateStack = new List<int>();
            for (int i = 0; i < n; i++)
            {
                Tick();
                if (Isolate(original[i])) isolateStack.Add(i);
                else if (original[i] == "PDI" && isolateStack.Count > 0)
                {
                    int open = isolateStack[isolateStack.Count - 1]; isolateStack.RemoveAt(isolateStack.Count - 1);
                    matching[open] = i; matching[i] = open;
                }
            }
            string FirstStrong(int start, int end)
            {
                for (int i = start; i < end; i++)
                {
                    Tick(); string t = original[i];
                    if (t == "L") return "L"; if (t == "R" || t == "AL") return "R";
                    if (Isolate(t)) i = matching[i] >= 0 ? matching[i] : end;
                }
                return "L";
            }
            int paragraphLevel = baseDirection == "rtl" ? 1 : baseDirection == "ltr" ? 0 : FirstStrong(0, n) == "R" ? 1 : 0;
            var levels = Enumerable.Repeat(paragraphLevel, n).ToArray();
            var stack = new List<Tuple<int, string, bool>> { Tuple.Create(paragraphLevel, "", false) };
            int overflowIsolate = 0, overflowEmbedding = 0, validIsolate = 0;
            for (int i = 0; i < n; i++)
            {
                Tick(); string t = original[i]; var top = stack[stack.Count - 1]; levels[i] = top.Item1;
                if (In(t, "RLE", "LRE", "RLO", "LRO") || Isolate(t))
                {
                    bool iso = Isolate(t), right = t == "FSI" ? FirstStrong(i + 1, matching[i] < 0 ? n : matching[i]) == "R" : In(t, "RLE", "RLO", "RLI");
                    int next = top.Item1 + (right ? (top.Item1 % 2 != 0 ? 2 : 1) : (top.Item1 % 2 != 0 ? 1 : 2));
                    if (iso && top.Item2.Length > 0) types[i] = top.Item2;
                    if (next <= 125 && overflowIsolate == 0 && overflowEmbedding == 0)
                    {
                        stack.Add(Tuple.Create(next, t == "RLO" ? "R" : t == "LRO" ? "L" : "", iso));
                        if (iso) validIsolate++;
                    }
                    else if (iso) overflowIsolate++; else if (overflowIsolate == 0) overflowEmbedding++;
                }
                else if (t == "PDI")
                {
                    if (overflowIsolate > 0) overflowIsolate--;
                    else if (validIsolate > 0)
                    {
                        overflowEmbedding = 0;
                        while (!stack[stack.Count - 1].Item3) stack.RemoveAt(stack.Count - 1);
                        stack.RemoveAt(stack.Count - 1); validIsolate--;
                    }
                    top = stack[stack.Count - 1]; levels[i] = top.Item1; if (top.Item2.Length > 0) types[i] = top.Item2;
                }
                else if (t == "PDF")
                {
                    if (overflowIsolate == 0)
                    {
                        if (overflowEmbedding > 0) overflowEmbedding--;
                        else if (stack.Count > 1 && !top.Item3) stack.RemoveAt(stack.Count - 1);
                    }
                }
                else if (t == "B") levels[i] = paragraphLevel;
                else if (t != "BN" && top.Item2.Length > 0) types[i] = top.Item2;
            }
            // X9/X10: level runs, then maximal isolating run sequences.
            var active = new List<int>(); var positions = Enumerable.Repeat(-1, n).ToArray();
            for (int i = 0; i < n; i++) if (!Removed(original[i])) { positions[i] = active.Count; active.Add(i); } else levels[i] = -1;
            var runs = new List<List<int>>(); var runOf = Enumerable.Repeat(-1, n).ToArray();
            foreach (int i in active)
            {
                if (runs.Count == 0 || levels[i] != levels[runs[runs.Count - 1][0]]) runs.Add(new List<int>());
                runOf[i] = runs.Count - 1; runs[runs.Count - 1].Add(i);
            }
            var links = Enumerable.Repeat(-1, runs.Count).ToArray(); var incoming = new bool[runs.Count];
            for (int r = 0; r < runs.Count; r++)
            {
                int end = runs[r][runs[r].Count - 1], close = matching[end];
                if (Isolate(original[end]) && close >= 0 && runOf[close] != r && levels[end] == levels[close]) { links[r] = runOf[close]; incoming[runOf[close]] = true; }
            }
            var explicitLevels = (int[])levels.Clone();
            for (int r = 0; r < runs.Count; r++) if (!incoming[r])
            {
                var seq = new List<int>();
                for (int at = r; at >= 0; at = links[at]) seq.AddRange(runs[at]);
                int first = seq[0], last = seq[seq.Count - 1], before = positions[first] - 1, after = positions[last] + 1;
                string sos = Direction(Math.Max(explicitLevels[first], before < 0 ? paragraphLevel : explicitLevels[active[before]]));
                string eos = Direction(Math.Max(explicitLevels[last], after >= active.Count || Isolate(original[last]) ? paragraphLevel : explicitLevels[active[after]]));
                string[] t = seq.Select(i => types[i]).ToArray(); string embedding = Direction(explicitLevels[first]), prev = sos;
                for (int j = 0; j < t.Length; j++) { Tick(); if (t[j] == "NSM") t[j] = Isolate(prev) || prev == "PDI" ? "ON" : prev; prev = t[j]; }
                prev = sos;
                for (int j = 0; j < t.Length; j++) { if (In(t[j], "R", "L", "AL")) prev = t[j]; else if (t[j] == "EN" && prev == "AL") t[j] = "AN"; }
                for (int j = 0; j < t.Length; j++) if (t[j] == "AL") t[j] = "R";
                for (int j = 1; j + 1 < t.Length; j++)
                {
                    if (t[j] == "ES" && t[j - 1] == "EN" && t[j + 1] == "EN") t[j] = "EN";
                    else if (t[j] == "CS" && t[j - 1] == t[j + 1] && In(t[j - 1], "EN", "AN")) t[j] = t[j - 1];
                }
                for (int j = 0; j < t.Length;)
                {
                    if (t[j] != "ET") { j++; continue; } int end = j + 1; while (end < t.Length && t[end] == "ET") end++;
                    if ((j > 0 && t[j - 1] == "EN") || (end < t.Length && t[end] == "EN")) for (int k = j; k < end; k++) t[k] = "EN";
                    j = end;
                }
                for (int j = 0; j < t.Length; j++) if (In(t[j], "ES", "ET", "CS")) t[j] = "ON";
                prev = sos;
                for (int j = 0; j < t.Length; j++) { if (t[j] == "R" || t[j] == "L") prev = t[j]; else if (t[j] == "EN" && prev == "L") t[j] = "L"; }
                // BD16/N0: at most 63 bracket entries; canonical angle brackets agree.
                var opens = new List<int>(); var pairs = new List<Tuple<int, int>>();
                for (int j = 0; j < t.Length; j++) if (t[j] == "ON")
                {
                    int cp = points[seq[j]], kind = LayoutUnicodeData.Bracket(cp, true);
                    if (kind == 1) { if (opens.Count == 63) { pairs.Clear(); break; } opens.Add(j); }
                    else if (kind == 2) for (int k = opens.Count - 1; k >= 0; k--)
                    {
                        Tick(); int pair = LayoutUnicodeData.Bracket(points[seq[opens[k]]]);
                        if ((pair == 0x232a ? 0x3009 : pair) == (cp == 0x232a ? 0x3009 : cp)) { pairs.Add(Tuple.Create(opens[k], j)); opens.RemoveRange(k, opens.Count - k); break; }
                    }
                }
                pairs.Sort((a, b) => a.Item1.CompareTo(b.Item1));
                foreach (var pair in pairs)
                {
                    int open = pair.Item1, close = pair.Item2; string found = "", context = sos;
                    for (int j = open + 1; j < close; j++) { Tick(); string v = Strong(t[j]); if (v == embedding) { found = embedding; break; } if (v == "L" || v == "R") found = v; }
                    if (found.Length == 0) continue;
                    if (found != embedding)
                    {
                        for (int j = open - 1; j >= 0; j--) { Tick(); string v = Strong(t[j]); if (v == "L" || v == "R") { context = v; break; } }
                        found = context;
                    }
                    t[open] = found; t[close] = found;
                    foreach (int bracket in new[] { open, close }) for (int j = bracket + 1; j < t.Length && original[seq[j]] == "NSM"; j++) { Tick(); t[j] = found; }
                }
                bool Neutral(string v) { return In(v, "B", "S", "WS", "ON", "FSI", "LRI", "RLI", "PDI"); }
                for (int j = 0; j < t.Length;)
                {
                    if (!Neutral(t[j])) { j++; continue; } int end = j + 1; while (end < t.Length && Neutral(t[end])) end++;
                    string left = j > 0 ? Strong(t[j - 1]) : sos, right = end < t.Length ? Strong(t[end]) : eos, result = left == right ? left : embedding;
                    for (int k = j; k < end; k++) t[k] = result; j = end;
                }
                for (int j = 0; j < seq.Count; j++)
                {
                    int i = seq[j], odd = explicitLevels[i] % 2;
                    levels[i] += odd != 0 ? (t[j] == "R" ? 0 : 1) : t[j] == "R" ? 1 : In(t[j], "EN", "AN") ? 2 : 0;
                }
            }
            return new BidiParagraph(input, paragraphLevel, levels, original);
        }
    }
}
