// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
namespace BonniePDF.Typography.Engine
{
    internal static class SpaceFallback
    {
        internal static int? Width(TrueTypeFont font, int scalar)
        {
            int em = font.UnitsPerEm, denominator;
            switch (scalar)
            {
                case 0xa0: return font.GlyphIndex(32) == 0 ? (int?)null : font.AdvanceWidth(font.GlyphIndex(32));
                case 0x2000: case 0x2002: denominator = 2; break;
                case 0x2001: case 0x2003: case 0x3000: denominator = 1; break;
                case 0x2004: denominator = 3; break;
                case 0x2005: denominator = 4; break;
                case 0x2006: denominator = 6; break;
                case 0x2009: denominator = 5; break;
                case 0x200a: denominator = 16; break;
                case 0x205f: return (em * 2 + 4) / 9;
                case 0x202f: return (font.AdvanceWidth(font.GlyphIndex(32)) + 1) / 2;
                case 0x2007: case 0x2008:
                    int reference = font.GlyphIndex(scalar == 0x2007 ? '0' : '.');
                    return font.AdvanceWidth(reference == 0 ? font.GlyphIndex(32) : reference);
                default: return null;
            }
            return (em + denominator / 2) / denominator;
        }
    }
}
