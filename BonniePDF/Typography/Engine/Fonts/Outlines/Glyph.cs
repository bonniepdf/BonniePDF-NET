// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System.Collections.Generic;

namespace BonniePDF.Typography.Engine
{
    internal sealed class Glyph
    {
        public readonly List<double[]> Commands = new List<double[]>();
        public readonly List<List<FontPoint>> Contours = new List<List<FontPoint>>();
        public int XMin, YMin, XMax, YMax;
    }
}
