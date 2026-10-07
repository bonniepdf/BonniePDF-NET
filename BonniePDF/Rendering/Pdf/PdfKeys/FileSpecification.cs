namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries describing files associated with the document.</summary>
    internal static class FileSpecification
    {
        /// <summary>Filename in the file-specification dictionary.</summary>
        internal const string FileName = "/F";

        /// <summary>Unicode filename in the file-specification dictionary.</summary>
        internal const string UnicodeFileName = "/UF";

        /// <summary>Dictionary mapping filename entries to embedded-file stream references.</summary>
        internal const string EmbeddedFiles = "/EF";

        /// <summary>Relationship between the associated file and the PDF document.</summary>
        internal const string AssociatedFileRelationship = "/AFRelationship";

        /// <summary>Human-readable description of the file.</summary>
        internal const string Description = "/Desc";
    }
}
