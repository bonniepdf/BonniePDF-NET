namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries describing the intended color reproduction conditions.</summary>
    internal static class OutputIntent
    {
        /// <summary>Name identifying the output-intent category, such as the PDF/A output intent.</summary>
        internal const string Subtype = "/S";

        /// <summary>Identifier of the intended output condition, such as the sRGB profile name.</summary>
        internal const string ConditionIdentifier = "/OutputConditionIdentifier";

        /// <summary>Registry identifying the source of the output-condition identifier.</summary>
        internal const string RegistryName = "/RegistryName";

        /// <summary>Reference to the ICC profile describing the intended output condition.</summary>
        internal const string DestinationProfile = "/DestOutputProfile";
    }
}
