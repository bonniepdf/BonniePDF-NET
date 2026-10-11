namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries describing PDF fonts and their character mappings.</summary>
    internal static class Font
    {
        /// <summary>Name of the font, including its subset prefix when present.</summary>
        internal const string BaseFont = "/BaseFont";

        /// <summary>Character encoding or CMap used to interpret the font's character codes.</summary>
        internal const string Encoding = "/Encoding";

        /// <summary>Name identifying an embedded character mapping.</summary>
        internal const string CMapName = "/CMapName";

        /// <summary>Array containing the descendant CID font of a composite font.</summary>
        internal const string DescendantFonts = "/DescendantFonts";

        /// <summary>Reference to the CMap mapping character codes to Unicode text.</summary>
        internal const string ToUnicode = "/ToUnicode";

        /// <summary>Reference to the dictionary describing the font's metrics and embedded program.</summary>
        internal const string Descriptor = "/FontDescriptor";

        /// <summary>Registry, ordering and supplement identifying the CID character collection.</summary>
        internal const string CidSystemInfo = "/CIDSystemInfo";

        /// <summary>Array specifying glyph widths for character identifiers in a CID font.</summary>
        internal const string Widths = "/W";

        /// <summary>Mapping from character identifiers to glyph identifiers in a TrueType CID font.</summary>
        internal const string CidToGlyphMap = "/CIDToGIDMap";
    }
}
