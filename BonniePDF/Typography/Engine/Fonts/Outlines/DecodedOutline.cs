// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
namespace BonniePDF.Typography.Engine
{
    // Private cache data; callers receive their own copy of the mutable Glyph.
    internal sealed class DecodedOutline
    {
        internal Glyph Glyph;
        internal int Points;
        internal int Depth;
        internal int Components;
        internal long Operations;
    }
}
