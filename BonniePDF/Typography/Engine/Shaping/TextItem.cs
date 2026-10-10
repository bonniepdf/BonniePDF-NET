// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.ObjectModel;
namespace BonniePDF.Typography.Engine
{
    internal sealed class TextItem
    {
        internal readonly int Start, End, Level;
        internal readonly string Script;
        internal readonly FontFace Face;
        internal TextItem(int start, int end, string script, int level, FontFace face)
        { Start = start; End = end; Script = script; Level = level; Face = face; }
    }
    internal sealed class ShapedTextItem
    {
        internal readonly TextItem Item;
        internal readonly ReadOnlyCollection<GlyphInfo> Glyphs;
        internal ShapedTextItem(TextItem item, GlyphInfo[] glyphs)
        { Item = item; Glyphs = Array.AsReadOnly((GlyphInfo[])glyphs.Clone()); }
    }
}
