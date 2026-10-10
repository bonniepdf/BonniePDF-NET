// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
namespace BonniePDF.Typography.Engine
{
    internal sealed class BreakOpportunity
    {
        internal readonly int Offset;
        internal readonly bool Mandatory;
        internal BreakOpportunity(int offset, bool mandatory) { Offset = offset; Mandatory = mandatory; }
    }
}
