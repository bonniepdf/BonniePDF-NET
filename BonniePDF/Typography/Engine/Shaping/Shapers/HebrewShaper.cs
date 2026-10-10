// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
namespace BonniePDF.Typography.Engine
{
    // Presentation composition is a font fallback, not Unicode NFC.
    internal static class HebrewShaper
    {
        // Rendering order established through the public HarfBuzz API only.
        internal static int CombiningClass(int cp)
        {
            int c = UnicodeData.CombiningClass(cp);
            return c >= 10 && c <= 25 ? "\u0016\u000f\u0010\u0011\u0017\u0012\u0013\u0014\u0015\u000e\u0018\u000c\u0019\u000d\u000a\u000b"[c - 10] : c;
        }
        internal static List<SourceScalar> Compose(FontFace face, List<SourceScalar> input)
        {
            if (face.Font.HasTable("GPOS")) return input;
            var output = new List<SourceScalar>();
            int starter = -1, lastClass = 0;
            for (int i = 0; i < input.Count; i++)
            {
                var item = input[i];
                if (item.Scalar == 0x5e9 && i + 2 < input.Count && (input[i + 1].Scalar == 0x5c1 || input[i + 1].Scalar == 0x5c2) && input[i + 2].Scalar == 0x5bc)
                {
                    int combined = input[i + 1].Scalar == 0x5c1 ? 0xfb2c : 0xfb2d;
                    if (face.Covers(combined)) { item = new SourceScalar(combined, Math.Min(item.Start, Math.Min(input[i + 1].Start, input[i + 2].Start)), Math.Max(item.End, Math.Max(input[i + 1].End, input[i + 2].End))); i += 2; }
                }
                int ccc = CombiningClass(item.Scalar);
                int cp = starter >= 0 && (lastClass < ccc || lastClass == 0) ? HebrewData.Compose(output[starter].Scalar, item.Scalar) : -1;
                if (cp >= 0 && face.Covers(cp)) { var prior = output[starter]; output[starter] = new SourceScalar(cp, Math.Min(prior.Start, item.Start), Math.Max(prior.End, item.End)); }
                else { if (ccc == 0) starter = output.Count; output.Add(item); lastClass = ccc; }
            }
            return output;
        }
    }
}
