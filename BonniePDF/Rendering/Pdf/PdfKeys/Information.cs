namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries in the document information dictionary.</summary>
    internal static class Information
    {
        /// <summary>Title of the document recorded in its information dictionary.</summary>
        internal const string Title = "/Title";

        /// <summary>Author of the document recorded in its information dictionary.</summary>
        internal const string Author = "/Author";

        /// <summary>Subject of the document recorded in its information dictionary.</summary>
        internal const string Subject = "/Subject";

        /// <summary>Keywords describing the document's content.</summary>
        internal const string Keywords = "/Keywords";

        /// <summary>Application that created the original document before PDF conversion.</summary>
        internal const string Creator = "/Creator";

        /// <summary>Application or library that produced the PDF file.</summary>
        internal const string Producer = "/Producer";

        /// <summary>Date and time when the document was created.</summary>
        internal const string CreationDate = "/CreationDate";

        /// <summary>Date and time when the document was last modified.</summary>
        internal const string ModifiedDate = "/ModDate";

        /// <summary>Whether trapping has been applied to the document's printed output.</summary>
        internal const string Trapped = "/Trapped";
    }
}
