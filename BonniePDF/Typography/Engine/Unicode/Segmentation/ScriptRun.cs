// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
namespace BonniePDF.Typography.Engine
{
    internal sealed class ScriptRun
    {
        internal readonly int Start;
        internal readonly int End;
        internal readonly string Script;
        internal ScriptRun(int start, int end, string script) { Start = start; End = end; Script = script; }
    }
}
