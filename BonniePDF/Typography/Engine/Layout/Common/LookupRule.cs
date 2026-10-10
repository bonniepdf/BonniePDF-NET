// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System.Collections.Generic;

namespace BonniePDF.Typography.Engine
{
    internal sealed class LookupRule
    {
        internal readonly int Index;
        private readonly ShapeOptions options;
        private readonly List<string> tags = new List<string>();
        private readonly List<int> defaults = new List<int>();
        private bool required;
        private readonly HashSet<string> added = new HashSet<string>();

        internal LookupRule(int index, ShapeOptions options) { Index = index; this.options = options; }
        internal void Add(string tag, int value, bool isRequired)
        {
            required |= isRequired;
            if (added.Add(tag)) { tags.Add(tag); defaults.Add(value); }
        }
        internal int Value(int cluster, System.Action tick, bool mirrored = false, string joiningForm = "")
        {
            if (required) return 1;
            for (int i = 0; i < tags.Count; i++)
            {
                tick();
                int fallback = options.Script == "arab" && (tags[i] == "isol" || tags[i] == "init" || tags[i] == "medi" || tags[i] == "fina") && tags[i] != joiningForm ? 0 : defaults[i];
                if (mirrored && tags[i] == "rtlm") continue;
                int value = options.FeatureValue(tags[i], cluster, fallback, tick);
                if (value != 0) return value;
            }
            return 0;
        }
    }
}
