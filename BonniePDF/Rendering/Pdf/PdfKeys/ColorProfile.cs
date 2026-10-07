namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries describing ICC-based color profiles.</summary>
    internal static class ColorProfile
    {
        /// <summary>Number of color components described by the ICC profile.</summary>
        internal const string Components = "/N";

        /// <summary>Fallback color space used when the ICC profile cannot be interpreted.</summary>
        internal const string Alternate = "/Alternate";
    }
}
