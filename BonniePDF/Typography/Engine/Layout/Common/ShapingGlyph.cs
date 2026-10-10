// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
namespace BonniePDF.Typography.Engine
{
    internal sealed class ShapingGlyph
    {
        internal int Id, Scalar, Cluster, FeatureCluster;
        internal bool UnsafeBefore;
        internal bool Mirrored;
        internal string JoiningForm = "";
        internal int? AdvanceOverride;
        internal bool Ligature;
        internal int ComponentCount = 1;
        internal ShapingGlyph MarkLigature;
        internal int MarkComponent;
        internal ShapingGlyph(int id, int scalar, int cluster)
        {
            Id = id;
            Scalar = scalar;
            Cluster = cluster;
            FeatureCluster = cluster;
        }
    }
}
