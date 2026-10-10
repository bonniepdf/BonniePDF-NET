// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
namespace BonniePDF.Typography.Engine
{
    internal sealed class BitmapStrikeData
    {
        internal readonly int Index, FirstGlyph, LastGlyph, PpemX, PpemY, BitDepth, Flags;
        internal readonly int ArrayOffset, ArrayEnd, SubtableCount;
        internal BitmapStrikeData(int index, int firstGlyph, int lastGlyph, int ppemX, int ppemY,
            int bitDepth, int flags, int arrayOffset, int arrayEnd, int subtableCount)
        {
            Index = index; FirstGlyph = firstGlyph; LastGlyph = lastGlyph;
            PpemX = ppemX; PpemY = ppemY; BitDepth = bitDepth; Flags = flags;
            ArrayOffset = arrayOffset; ArrayEnd = arrayEnd; SubtableCount = subtableCount;
        }
    }
}
