// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;

namespace BonniePDF.Typography.Engine
{
    internal sealed class ShapeFeature
    {
        internal readonly string Tag;
        internal readonly int Value, Start, End;

        internal ShapeFeature(string tag, int value = 1, int start = 0, int end = int.MaxValue)
        {
            ShapeOptions.ValidateTag(tag, "tag");
            if (value < 0 || value > 65535) throw new ArgumentOutOfRangeException("value");
            if (start < 0) throw new ArgumentOutOfRangeException("start");
            if (end < start) throw new ArgumentOutOfRangeException("end");
            Tag = tag; Value = value; Start = start; End = end;
        }
    }
}
