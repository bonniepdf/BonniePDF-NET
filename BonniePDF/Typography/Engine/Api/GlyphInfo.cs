// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
namespace BonniePDF.Typography.Engine
{
    // Integer design units; Cluster is an offset in the original UTF-16 string.
    internal sealed class GlyphInfo
    {
        internal readonly int Id, Cluster, AdvanceX, AdvanceY, OffsetX, OffsetY, ComponentCount;
        internal readonly bool IsMark, IsLigature;
        internal readonly bool UnsafeToBreak;
        internal readonly int SourceLength, ComponentIndex;

        internal GlyphInfo(int id, int cluster, int advanceX, int advanceY, int offsetX, int offsetY,
            int componentCount, bool isMark, bool isLigature, int sourceLength = 0, bool unsafeToBreak = false, int componentIndex = 0)
        {
            Id = id;
            Cluster = cluster;
            AdvanceX = advanceX;
            AdvanceY = advanceY;
            OffsetX = offsetX;
            OffsetY = offsetY;
            ComponentCount = componentCount;
            IsMark = isMark;
            IsLigature = isLigature;
            SourceLength = sourceLength; UnsafeToBreak = unsafeToBreak; ComponentIndex = componentIndex;
        }
    }
}
