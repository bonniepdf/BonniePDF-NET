namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries describing font metrics and embedded font programs.</summary>
    internal static class FontDescriptor
    {
        /// <summary>Font name recorded by the font descriptor, including any subset prefix.</summary>
        internal const string Name = "/FontName";

        /// <summary>Bit field describing font characteristics such as serif, symbolic and italic.</summary>
        internal const string Flags = "/Flags";

        /// <summary>Rectangle enclosing all glyph outlines in the font's coordinate system.</summary>
        internal const string BoundingBox = "/FontBBox";

        /// <summary>Counterclockwise angle in degrees from vertical of the font's dominant strokes.</summary>
        internal const string ItalicAngle = "/ItalicAngle";

        /// <summary>Maximum height above the baseline, expressed in glyph-space units.</summary>
        internal const string Ascent = "/Ascent";

        /// <summary>Maximum depth below the baseline, expressed in glyph-space units and normally negative.</summary>
        internal const string Descent = "/Descent";

        /// <summary>Height of flat capital letters above the baseline, in glyph-space units.</summary>
        internal const string CapHeight = "/CapHeight";

        /// <summary>Width of a representative vertical stem, in glyph-space units.</summary>
        internal const string VerticalStemWidth = "/StemV";

        /// <summary>Reference to the embedded TrueType font-program stream.</summary>
        internal const string TrueTypeProgram = "/FontFile2";

        /// <summary>Reference to the bit-set stream identifying character identifiers present in the font.</summary>
        internal const string CidSet = "/CIDSet";
    }
}
