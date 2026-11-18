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
        private bool constantKnown;
        private int constantValue = -1, constantTicks;

        // The value and operation charge of Value when they cannot depend on the glyph: no
        // Arabic joining forms, no rtlm, and every feature override covering the whole text.
        internal bool Constant(out int value, out int ticks)
        {
            if (!constantKnown) { constantKnown = true; ComputeConstant(); }
            value = constantValue; ticks = constantTicks;
            return constantValue >= 0;
        }

        private void ComputeConstant()
        {
            if (required) { constantValue = 1; constantTicks = 0; return; }
            if (string.Equals(options.Script, "arab", System.StringComparison.Ordinal) || tags.Contains("rtlm")) return;
            foreach (var feature in options.Features)
                if (feature.Start != 0 || feature.End != int.MaxValue) return;
            int ticks = 0;
            for (int i = 0; i < tags.Count; i++)
            {
                ticks++;
                int value = defaults[i];
                foreach (var feature in options.Features) { ticks++; if (string.Equals(feature.Tag, tags[i], System.StringComparison.Ordinal)) value = feature.Value; }
                if (value != 0) { constantValue = value; constantTicks = ticks; return; }
            }
            constantValue = 0; constantTicks = ticks;
        }

        internal int Value(int cluster, System.Action tick, bool mirrored = false, string joiningForm = "")
        {
            if (required) return 1;
            for (int i = 0; i < tags.Count; i++)
            {
                tick();
                int fallback = string.Equals(options.Script, "arab", System.StringComparison.Ordinal) && (string.Equals(tags[i], "isol", System.StringComparison.Ordinal) || string.Equals(tags[i], "init", System.StringComparison.Ordinal) || string.Equals(tags[i], "medi", System.StringComparison.Ordinal) || string.Equals(tags[i], "fina", System.StringComparison.Ordinal)) && !string.Equals(tags[i], joiningForm, System.StringComparison.Ordinal) ? 0 : defaults[i];
                if (mirrored && string.Equals(tags[i], "rtlm", System.StringComparison.Ordinal)) continue;
                int value = options.FeatureValue(tags[i], cluster, fallback, tick);
                if (value != 0) return value;
            }
            return 0;
        }
    }
}
