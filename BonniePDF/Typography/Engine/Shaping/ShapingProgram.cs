// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
namespace BonniePDF.Typography.Engine
{
    internal sealed class ShapingProgram
    {
        internal readonly GlyphSubstitution Substitution;
        internal readonly GlyphPositioning Positioning;
        internal ShapingProgram(GlyphSubstitution substitution, GlyphPositioning positioning)
        {
            Substitution = substitution; Positioning = positioning;
        }
    }
}
