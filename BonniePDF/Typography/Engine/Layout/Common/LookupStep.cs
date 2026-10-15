// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
namespace BonniePDF.Typography.Engine
{
    // What GSUB and GPOS work out for one selected lookup before walking the run. It depends only
    // on the font, the options and the limits, which a program fixes, so a program keeps one per
    // selected lookup instead of recomputing it on every shape call. The operations spent working
    // it out (the feature rule search in GPOS, the reverse-chaining test in GSUB) are recorded and
    // charged again on each later call, so a call's charges are unchanged. A step whose working
    // out throws is never kept.
    internal sealed class LookupStep
    {
        internal LookupRule Rule;
        internal FirstGlyphSet First;
        internal int RejectedCharge;
        internal int InputCharge;
        internal ContextFormat3[] Plans;
        internal ContextFormat3[] Charged;
        internal bool Reverse;
        internal int Ticks;
    }
}
