// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    internal sealed class ShapePlan
    {
        internal readonly LookupRule[] Rules;
        internal readonly int[] Lookups, KerningLookups;
        private int operations;
        private readonly string kind;

        private void Tick()
        {
            if (++operations > 1000000) throw new InvalidDataException(kind + " feature-selection operation limit exceeded.");
        }

        private int Find(BigEndianData data, int list, string tag, int origin = -1)
        {
            int count = data.U16(list);
            data.Check(list + 2, count * 6);
            for (int i = 0; i < count; i++)
            {
                Tick();
                int p = list + 2 + i * 6;
                if (string.Equals(data.Tag(p), tag, StringComparison.Ordinal)) return OpenTypeLayout.Offset16(data, origin < 0 ? list : origin, p + 4);
            }
            return -1;
        }

        internal ShapePlan(byte[] bytes, string kind, ShapeOptions options)
        {
            this.kind = kind;
            options = options ?? new ShapeOptions();
            Lookups = KerningLookups = System.Array.Empty<int>(); Rules = System.Array.Empty<LookupRule>();
            if (bytes == null) return;
            var data = new BigEndianData(bytes);
            int minor = data.U16(2);
            if (data.U16(0) != 1 || minor > 1) throw new NotSupportedException("Unsupported " + kind + " version.");
            if (minor == 1 && data.U32(10) != 0) throw new NotSupportedException(kind + " feature variations are unsupported.");
            int scripts = OpenTypeLayout.Offset16(data, 0, 4), features = OpenTypeLayout.Offset16(data, 0, 6);
            int lookupList = OpenTypeLayout.Offset16(data, 0, 8);
            int script = Find(data, scripts, options.Script);
            if (script < 0) script = Find(data, scripts, "DFLT");
            if (script < 0) return;
            int language = string.Equals(options.Language, "dflt", StringComparison.Ordinal) ? -1 : Find(data, script + 2, options.Language, script);
            if (language < 0 && data.U16(script) != 0) language = OpenTypeLayout.Offset16(data, script, script);
            if (language < 0) return;
            if (data.U16(language) != 0) throw new InvalidDataException("Invalid " + kind + " language lookup order.");
            int required = data.U16(language + 2), count = data.U16(language + 4);
            data.Check(language + 6, count * 2);
            var indices = new List<int>();
            if (required != 65535) indices.Add(required);
            for (int i = 0; i < count; i++) indices.Add(data.U16(language + 6 + i * 2));
            var rules = new SortedDictionary<long, LookupRule>();
            var kern = new SortedSet<int>();
            foreach (int index in indices)
            {
                Tick();
                if (index >= data.U16(features)) throw new InvalidDataException("Invalid " + kind + " feature index.");
                int record = features + 2 + index * 6;
                string tag = data.Tag(record);
                bool arabic = string.Equals(options.Script, "arab", StringComparison.Ordinal) && string.Equals(kind, "GSUB", StringComparison.Ordinal);
                int stage = arabic ? ArabicStage(tag) : 0;
                int value = arabic && (string.Equals(tag, "isol", StringComparison.Ordinal) || string.Equals(tag, "fina", StringComparison.Ordinal) || string.Equals(tag, "medi", StringComparison.Ordinal) || string.Equals(tag, "init", StringComparison.Ordinal) || string.Equals(tag, "mset", StringComparison.Ordinal) || string.Equals(tag, "mark", StringComparison.Ordinal)) ? 1 : DefaultValue(tag, kind, options.Direction);
                bool requested = false;
                foreach (var feature in options.Features) { Tick(); requested |= string.Equals(feature.Tag, tag, StringComparison.Ordinal) && feature.Value != 0; }
                if (index != required && value == 0 && !requested) continue;
                int featureOffset = OpenTypeLayout.Offset16(data, features, record + 4), lookups = data.U16(featureOffset + 2);
                data.Check(featureOffset + 4, lookups * 2);
                for (int i = 0; i < lookups; i++)
                {
                    Tick();
                    int lookup = data.U16(featureOffset + 4 + i * 2);
                    if (lookup >= data.U16(lookupList)) throw new InvalidDataException("Invalid " + kind + " lookup index.");
                    LookupRule rule;
                    long key = ((long)stage << 32) | (uint)lookup;
                    if (!rules.TryGetValue(key, out rule)) { rule = new LookupRule(lookup, options); rules.Add(key, rule); }
                    rule.Add(tag, value, index == required);
                    if (string.Equals(tag, "kern", StringComparison.Ordinal)) kern.Add(lookup);
                }
            }
            Rules = new LookupRule[rules.Count]; rules.Values.CopyTo(Rules, 0);
            Lookups = new int[Rules.Length];
            for (int i = 0; i < Rules.Length; i++) Lookups[i] = Rules[i].Index;
            KerningLookups = new int[kern.Count]; kern.CopyTo(KerningLookups);
        }

        private static readonly string[] ArabicForms = { "isol", "fina", "medi", "init", "rlig", "calt" };

        private static int ArabicStage(string tag)
        {
            if (string.Equals(tag, "rtlm", StringComparison.Ordinal) || string.Equals(tag, "rtla", StringComparison.Ordinal) || string.Equals(tag, "ltra", StringComparison.Ordinal) || string.Equals(tag, "ltrm", StringComparison.Ordinal)) return 0;
            if (string.Equals(tag, "ccmp", StringComparison.Ordinal) || string.Equals(tag, "locl", StringComparison.Ordinal)) return 1;
            int form = Array.IndexOf(ArabicForms, tag);
            return form >= 0 ? form + 2 : 8;
        }

        private static int DefaultValue(string tag, string kind, TextDirection direction)
        {
            if (string.Equals(kind, "GSUB", StringComparison.Ordinal))
                return string.Equals(tag, "ccmp", StringComparison.Ordinal) || string.Equals(tag, "locl", StringComparison.Ordinal) || string.Equals(tag, "rlig", StringComparison.Ordinal) || string.Equals(tag, "liga", StringComparison.Ordinal) || string.Equals(tag, "clig", StringComparison.Ordinal) || string.Equals(tag, "calt", StringComparison.Ordinal) || string.Equals(tag, "rclt", StringComparison.Ordinal) ||
                    (direction == TextDirection.LeftToRight ? string.Equals(tag, "ltra", StringComparison.Ordinal) || string.Equals(tag, "ltrm", StringComparison.Ordinal) : string.Equals(tag, "rtla", StringComparison.Ordinal) || string.Equals(tag, "rtlm", StringComparison.Ordinal)) ? 1 : 0;
            return string.Equals(tag, "kern", StringComparison.Ordinal) || string.Equals(tag, "mark", StringComparison.Ordinal) || string.Equals(tag, "mkmk", StringComparison.Ordinal) || string.Equals(tag, "curs", StringComparison.Ordinal) || string.Equals(tag, "dist", StringComparison.Ordinal) || string.Equals(tag, "abvm", StringComparison.Ordinal) || string.Equals(tag, "blwm", StringComparison.Ordinal) ? 1 : 0;
        }
    }
}
