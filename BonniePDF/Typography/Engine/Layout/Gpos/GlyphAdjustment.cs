// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
namespace BonniePDF.Typography.Engine
{
    // Design units, with positive Y upwards. Placement never moves the pen.
    internal sealed class GlyphAdjustment
    {
        internal int XPlacement, YPlacement, XAdvance, YAdvance;
        // While applying lookups, attached placement is relative to Parent's origin.
        // Resolve once after all lookups, accounting for the final pen advances.
        internal int Parent = -1;
        internal int CursiveParent = -1;
        internal bool ZeroMarkAdvance;
    }
}
