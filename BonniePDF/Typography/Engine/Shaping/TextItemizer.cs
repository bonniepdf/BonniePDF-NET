// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
namespace BonniePDF.Typography.Engine
{
    internal static class TextItemizer
    {
        internal static ItemizedParagraph Resolve(string text, Func<int, int, string, int, FontFace> selectFont, string direction = "auto", ShapingLimits limits = null)
        {
            if (selectFont == null) throw new ArgumentNullException("selectFont", "Font selector is required.");
            limits = limits ?? new ShapingLimits();
            var bidi = Bidi.ResolveParagraph(text, direction, limits.MaxTextLength, limits.MaxOperations);
            var scripts = Scripts.Runs(text, limits.MaxTextLength); var boundaries = Graphemes.Boundaries(text, limits.MaxTextLength);
            var clusters = new List<TextItem>(); int run = 0, scalar = 0;
            for (int i = 0; i + 1 < boundaries.Count; i++)
            {
                int start = boundaries[i], end = boundaries[i + 1]; while (scripts[run].End <= start) run++;
                int level = -1;
                while (scalar < bidi.Input.Points.Count && bidi.Input.Offsets[scalar] < end) { if (bidi.Levels[scalar] >= 0 && level < 0) level = bidi.Levels[scalar]; scalar++; }
                string script = scripts[run].Script; var face = selectFont(start, end, script, level);
                if (face == null) throw new NotSupportedException("No font selected for grapheme at UTF-16 offset " + start + ".");
                clusters.Add(new TextItem(start, end, script, level, face));
            }
            return new ItemizedParagraph(bidi, clusters, limits);
        }
    }
}
