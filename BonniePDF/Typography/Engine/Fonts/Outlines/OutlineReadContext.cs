// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System.Collections.Generic;
using System.IO;

namespace BonniePDF.Typography.Engine
{
    internal sealed class OutlineReadContext
    {
        internal readonly OutlineReadLimits Limits;
        internal readonly HashSet<int> ActiveGlyphs = new HashSet<int>();
        internal long Operations;
        internal int Components;

        internal OutlineReadContext(OutlineReadLimits limits)
        {
            Limits = limits;
        }

        // Cached outlines are charged their full logical cost, so acceptance never
        // depends on which glyphs happened to have been read before this request.
        internal void Charge(long operations, int components = 0)
        {
            if (operations > Limits.MaxOperations - Operations)
                throw new InvalidDataException("Outline operation limit exceeded.");
            if (components > Limits.MaxComponents - Components)
                throw new InvalidDataException("Outline component limit exceeded.");
            Operations += operations;
            Components += components;
        }

        internal void CheckSize(long points, long contours)
        {
            if (points > Limits.MaxPoints)
                throw new InvalidDataException("Outline point limit exceeded.");
            if (contours > Limits.MaxContours)
                throw new InvalidDataException("Outline contour limit exceeded.");
        }
    }
}
