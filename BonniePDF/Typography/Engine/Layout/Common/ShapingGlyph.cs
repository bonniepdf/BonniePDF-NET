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

        // Returns this record to the state the constructor gives it, for reuse by a later
        // shape call on the same face. A new field must be reset here too.
        internal ShapingGlyph Reset(int id, int scalar, int cluster)
        {
            Id = id;
            Scalar = scalar;
            Cluster = cluster;
            FeatureCluster = cluster;
            UnsafeBefore = false;
            Mirrored = false;
            JoiningForm = "";
            AdvanceOverride = null;
            Ligature = false;
            ComponentCount = 1;
            MarkLigature = null;
            MarkComponent = 0;
            return this;
        }
    }
}
