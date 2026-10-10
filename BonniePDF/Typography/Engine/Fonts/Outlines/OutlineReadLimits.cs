// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;

namespace BonniePDF.Typography.Engine
{
    /// <summary>Immutable bounds for one unhinted glyph outline read.</summary>
    internal sealed class OutlineReadLimits
    {
        public int MaxPoints { get; private set; }
        public int MaxContours { get; private set; }
        public int MaxComponents { get; private set; }
        public int MaxDepth { get; private set; }
        public int MaxOperations { get; private set; }

        public OutlineReadLimits(int maxPoints = 65536, int maxContours = 4096,
            int maxComponents = 4096, int maxDepth = 16, int maxOperations = 1000000)
        {
            if (maxPoints < 1)
                throw new ArgumentOutOfRangeException("maxPoints");
            if (maxContours < 1)
                throw new ArgumentOutOfRangeException("maxContours");
            if (maxComponents < 1)
                throw new ArgumentOutOfRangeException("maxComponents");
            // A caller can lower the bound, but cannot permit unbounded CLR recursion.
            if (maxDepth < 0 || maxDepth > 64)
                throw new ArgumentOutOfRangeException("maxDepth");
            if (maxOperations < 1)
                throw new ArgumentOutOfRangeException("maxOperations");
            MaxPoints = maxPoints;
            MaxContours = maxContours;
            MaxComponents = maxComponents;
            MaxDepth = maxDepth;
            MaxOperations = maxOperations;
        }
    }
}
