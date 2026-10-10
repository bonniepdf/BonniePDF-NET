// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System.Collections.Generic;

namespace BonniePDF.Typography.Engine
{
    internal sealed class ShapedRun
    {
        internal readonly IReadOnlyList<GlyphInfo> Glyphs;
        internal readonly string Text;

        internal ShapedRun(string text, GlyphInfo[] glyphs)
        {
            Text = text;
            Glyphs = System.Array.AsReadOnly((GlyphInfo[])glyphs.Clone());
        }
    }
}
