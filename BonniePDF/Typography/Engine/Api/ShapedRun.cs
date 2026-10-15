// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace BonniePDF.Typography.Engine
{
    internal sealed class ShapedRun
    {
        // The glyphs, packed. Glyphs gives the same values as records, built on first use.
        internal readonly GlyphRun Run;
        internal readonly string Text;
        private IReadOnlyList<GlyphInfo> glyphs;

        internal ShapedRun(string text, GlyphInfo[] glyphs)
        {
            Text = text;
            Run = GlyphRun.From(glyphs);
        }

        internal ShapedRun(string text, GlyphRun run)
        {
            Text = text;
            Run = run;
        }

        internal IReadOnlyList<GlyphInfo> Glyphs
        {
            get
            {
                var current = Volatile.Read(ref glyphs);
                if (current != null) return current;
                var records = new GlyphInfo[Run.Count];
                for (int i = 0; i < records.Length; i++) records[i] = Run.Info(i);
                return Interlocked.CompareExchange(ref glyphs, System.Array.AsReadOnly(records), null) ?? glyphs;
            }
        }
    }
}
