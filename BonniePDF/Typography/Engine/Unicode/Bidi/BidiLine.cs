// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.ObjectModel;
namespace BonniePDF.Typography.Engine
{
    internal sealed class BidiLine
    {
        internal readonly ReadOnlyCollection<int> Levels;
        internal readonly ReadOnlyCollection<int> VisualMap;
        internal readonly ReadOnlyCollection<int> MirroredScalars;
        internal BidiLine(int[] levels, int[] visualMap, int[] mirroredScalars)
        { Levels = Array.AsReadOnly((int[])levels.Clone()); VisualMap = Array.AsReadOnly((int[])visualMap.Clone()); MirroredScalars = Array.AsReadOnly((int[])mirroredScalars.Clone()); }
    }
}
