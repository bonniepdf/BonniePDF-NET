// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
namespace BonniePDF.Typography.Engine
{
    internal sealed class FontNameRecord
    {
        internal readonly int PlatformId, EncodingId, LanguageId, NameId;
        internal readonly string LanguageTag, Value;
        internal FontNameRecord(int platform, int encoding, int language, int name, string tag, string value)
        {
            PlatformId = platform; EncodingId = encoding; LanguageId = language; NameId = name;
            LanguageTag = tag; Value = value;
        }
    }
}
