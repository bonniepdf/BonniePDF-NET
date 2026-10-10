// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace BonniePDF.Typography.Engine
{
    internal sealed class BidiParagraph
    {
        internal readonly UnicodeText Input;
        internal readonly int ParagraphLevel;
        internal readonly ReadOnlyCollection<int> Levels;
        internal readonly ReadOnlyCollection<string> OriginalTypes;
        internal BidiParagraph(UnicodeText input, int paragraphLevel, int[] levels, string[] types)
        { Input = input; ParagraphLevel = paragraphLevel; Levels = Array.AsReadOnly((int[])levels.Clone()); OriginalTypes = Array.AsReadOnly((string[])types.Clone()); }
        internal BidiLine ReorderLine(int start = 0, int? end = null)
        {
            int first = Input.Offsets.IndexOf(start), last = Input.Offsets.IndexOf(end ?? Input.Text.Length);
            if (first < 0 || last < first) throw new ArgumentException("Line range must contain complete scalars inside the paragraph.");
            int[] levels = Levels.Skip(first).Take(last - first).ToArray();
            bool White(int i) { string t = OriginalTypes[i]; return t == "WS" || t == "FSI" || t == "LRI" || t == "RLI" || t == "PDI"; }
            // L1 uses original classes and skips all X9 characters.
            for (int i = first; i < last; i++) if (OriginalTypes[i] == "S" || OriginalTypes[i] == "B")
            {
                levels[i - first] = ParagraphLevel;
                for (int j = i - 1; j >= first; j--) { if (levels[j - first] < 0) continue; if (!White(j)) break; levels[j - first] = ParagraphLevel; }
            }
            for (int j = last - 1; j >= first; j--) { if (levels[j - first] < 0) continue; if (!White(j)) break; levels[j - first] = ParagraphLevel; }
            var map = new List<int>(); int max = 0, minOdd = 127;
            for (int i = first; i < last; i++) if (levels[i - first] >= 0)
            { map.Add(i); max = Math.Max(max, levels[i - first]); if (levels[i - first] % 2 != 0) minOdd = Math.Min(minOdd, levels[i - first]); }
            for (int level = max; level >= minOdd; level--) for (int i = 0; i < map.Count;)
            {
                if (levels[map[i] - first] < level) { i++; continue; }
                int endRun = i; while (endRun < map.Count && levels[map[endRun] - first] >= level) endRun++;
                for (int a = i, b = endRun - 1; a < b; a++, b--) { int temp = map[a]; map[a] = map[b]; map[b] = temp; }
                i = endRun;
            }
            return new BidiLine(levels, map.Select(i => Input.Offsets[i]).ToArray(), map.Select(i => levels[i - first] % 2 != 0 ? UnicodeData.Mirror(Input.Points[i]) : Input.Points[i]).ToArray());
        }
    }
}
