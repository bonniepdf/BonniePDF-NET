namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Dictionary entries shared by multiple kinds of PDF objects.</summary>
    internal static class Common
    {
        /// <summary>Identifies the kind of PDF object represented by the dictionary.</summary>
        internal const string Type = "/Type";

        /// <summary>Identifies a more specific kind of object, such as an image or link.</summary>
        internal const string Subtype = "/Subtype";
    }
}
