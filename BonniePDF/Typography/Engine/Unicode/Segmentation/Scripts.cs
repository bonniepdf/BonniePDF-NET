// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace BonniePDF.Typography.Engine
{
    // UAX #24 properties, intersected extensions, weak-character/bracket policy.
    // Run boundaries always preserve extended grapheme clusters.
    internal static class Scripts
    {
        internal static ReadOnlyCollection<ScriptRun> Runs(string text, int maxTextLength = 65536)
        {
            var input = new UnicodeText(text, maxTextLength);
            var boundaries = new HashSet<int>(Graphemes.Boundaries(text, maxTextLength));
            var stack = new List<Tuple<int, string>>(); var result = new List<ScriptRun>();
            var candidates = new List<string>(); string preferred = ""; int start = 0; bool anchored = false;
            string Choose() { return !anchored ? "Zyyy" : candidates.Contains(preferred) ? preferred : candidates.Count > 0 ? candidates[0] : "Zyyy"; }
            for (int i = 0; i < input.Points.Count; i++)
            {
                int cp = input.Points[i], offset = input.Offsets[i], kind = LayoutUnicodeData.Bracket(cp, true);
                string primary = LayoutUnicodeData.Script(cp), ext = LayoutUnicodeData.ScriptExtensions(cp);
                var next = (ext.Length > 0 ? ext.Split(',') : new[] { primary }).Where(s => s != "Zyyy" && s != "Zinh").ToList();
                if (kind == 2) for (int j = stack.Count - 1; j >= 0; j--)
                {
                    int close = stack[j].Item1;
                    if ((close == 0x232a ? 0x3009 : close) == (cp == 0x232a ? 0x3009 : cp))
                    {
                        if (stack[j].Item2 != null) next = new List<string> { stack[j].Item2 };
                        stack.RemoveRange(j, stack.Count - j); break;
                    }
                }
                if (next.Count > 0)
                {
                    var intersection = candidates.Where(s => next.Contains(s)).ToList();
                    if (candidates.Count == 0) candidates = next;
                    else if (intersection.Count > 0) candidates = intersection;
                    else if (!anchored) candidates = next;
                    else if (boundaries.Contains(offset)) { preferred = Choose(); result.Add(new ScriptRun(start, offset, preferred)); start = offset; candidates = next; anchored = false; }
                    if (primary != "Zinh") anchored = true;
                    string selected = Choose();
                    if (anchored) for (int j = 0; j < stack.Count; j++) if (stack[j].Item2 == null) stack[j] = Tuple.Create(stack[j].Item1, selected);
                }
                if (kind == 1)
                {
                    if (stack.Count == 128) throw new InvalidOperationException("Script bracket nesting limit exceeded.");
                    stack.Add(Tuple.Create(LayoutUnicodeData.Bracket(cp), anchored ? Choose() : null));
                }
            }
            if (input.Points.Count > 0) result.Add(new ScriptRun(start, text.Length, Choose()));
            return result.AsReadOnly();
        }
    }
}
