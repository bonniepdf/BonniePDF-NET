// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;

namespace BonniePDF.Typography.Engine
{
    /// <summary>Per-run bounds, independent of text size or rendering resolution.</summary>
    internal sealed class ShapingLimits
    {
        internal int MaxGlyphs { get; private set; }
        internal int MaxTextLength { get; private set; }
        internal int MaxOperations { get; private set; }
        internal int MaxNesting { get; private set; }
        internal OutlineReadLimits Outlines { get; private set; }
        internal ColourReadLimits Colours { get; private set; }

        internal ShapingLimits(int maxGlyphs = 65536, int maxTextLength = 65536,
            int maxOperations = 1000000, int maxNesting = 16, OutlineReadLimits outlines = null, ColourReadLimits colours = null)
        {
            if (maxGlyphs < 1 || maxGlyphs > 1000000)
                throw new ArgumentOutOfRangeException("maxGlyphs");
            if (maxTextLength < 1 || maxTextLength > 1000000)
                throw new ArgumentOutOfRangeException("maxTextLength");
            if (maxOperations < 1)
                throw new ArgumentOutOfRangeException("maxOperations");
            if (maxNesting < 0 || maxNesting > 64)
                throw new ArgumentOutOfRangeException("maxNesting");
            MaxGlyphs = maxGlyphs;
            MaxTextLength = maxTextLength;
            MaxOperations = maxOperations;
            MaxNesting = maxNesting;
            Outlines = outlines ?? new OutlineReadLimits();
            Colours = colours ?? new ColourReadLimits();
        }
    }
}
