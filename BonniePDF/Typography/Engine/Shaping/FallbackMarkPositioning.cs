// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    // Independent black-box observations establish this default fallback:
    // advance-box centering, combining-class alignment and an em/16 separation.
    // OpenType anchors always take precedence whenever a GPOS table is present.
    internal static class FallbackMarkPositioning
    {
        internal static void Apply(TrueTypeFont font, List<ShapingGlyph> glyphs, GlyphAdjustment[] adjustments, ShapingLimits limits, bool hebrew = false)
        {
            if (font.HasTable("GPOS")) return;
            int pen = 0, basePen = 0, baseAdvance = 0, baseId = 0, priorClass = -1, stacked = 0, operations = 0;
            bool hasBase = false;
            for (int i = 0; i < glyphs.Count; i++)
            {
                if (++operations > limits.MaxOperations) throw new InvalidDataException("Fallback mark operation limit exceeded.");
                var glyph = glyphs[i]; var a = adjustments[i];
                int ccc = UnicodeData.CombiningClass(glyph.Scalar);
                if (hebrew)
                {
                    int cp = glyph.Scalar;
                    if ((cp >= 0x5b0 && cp <= 0x5b8) || cp == 0x5bb || cp == 0x5bd || cp == 0x5c7) ccc = 220;
                    else if (cp == 0x5b9 || cp == 0x5ba || cp == 0x5c2) ccc = 228;
                    else if (cp == 0x5bc) ccc = 1;
                    else if (cp == 0x5bf) ccc = 214;
                    else if (cp == 0x5c1) ccc = 232;
                }
                if (a.ZeroMarkAdvance && ccc != 0 && hasBase)
                {
                    Glyph mark = font.ReadGlyph(glyph.Id);
                    Glyph outline = font.ReadGlyph(baseId);
                    int gap = ccc == 202 || ccc == 214 || ccc == 216 ? 0 : font.UnitsPerEm / 16;
                    int center = baseAdvance / 2 - (mark.XMin + mark.XMax) / 2;
                    if (ccc == 233 || ccc == 234) center = baseAdvance - (mark.XMin + mark.XMax) / 2;
                    if (ccc == 218 || ccc == 228) center = -mark.XMin;
                    else if (ccc == 222 || ccc == 232 || ccc == 216) center = baseAdvance - mark.XMax;
                    a.XPlacement = checked(a.XPlacement + basePen + center - pen + (hebrew ? baseAdvance : 0));
                    if (ccc != priorClass) stacked = 0;
                    if (ccc == 202 || ccc == 233 || (ccc >= 218 && ccc <= 222))
                        a.YPlacement = checked(a.YPlacement + outline.YMin - mark.YMax - gap - stacked);
                    else if (ccc == 214 || ccc == 216 || (ccc >= 228 && ccc <= 234))
                        a.YPlacement = checked(a.YPlacement + outline.YMax - mark.YMin + gap + stacked);
                    if (ccc != 1) stacked = checked(stacked + mark.YMax - mark.YMin + gap);
                    priorClass = ccc;
                }
                else if (!a.ZeroMarkAdvance && !EmojiClusters.Ignorable(glyph.Scalar))
                {
                    hasBase = true; basePen = pen; baseAdvance = checked(font.AdvanceWidth(glyph.Id) + a.XAdvance);
                    baseId = glyph.Id; priorClass = -1; stacked = 0;
                }
                pen = checked(pen + font.AdvanceWidth(glyph.Id) + a.XAdvance);
            }
        }
    }
}
