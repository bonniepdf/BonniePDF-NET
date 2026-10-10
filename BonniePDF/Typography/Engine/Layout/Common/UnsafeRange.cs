// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System.Collections.Generic;

namespace BonniePDF.Typography.Engine
{
    internal static class UnsafeRange
    {
        internal static void Mark(List<ShapingGlyph> glyphs, int first, int last, System.Action tick)
        {
            if (first < 0 || last >= glyphs.Count || first >= last) return;
            int cluster = glyphs[first].Cluster;
            for (int i = first + 1; i <= last; i++) { tick(); cluster = System.Math.Min(cluster, glyphs[i].Cluster); }
            for (int i = first; i <= last; i++)
            {
                tick();
                if (glyphs[i].Cluster != cluster) glyphs[i].UnsafeBefore = true;
            }
        }
    }
}
