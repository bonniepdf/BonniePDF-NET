namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries in the document catalog.</summary>
    internal static class Catalog
    {
        /// <summary>Reference to the root of the document's page tree.</summary>
        internal const string Pages = "/Pages";

        /// <summary>Destination or action applied when the document is opened.</summary>
        internal const string OpenAction = "/OpenAction";

        /// <summary>Viewer panel or display mode requested when the document is opened.</summary>
        internal const string PageMode = "/PageMode";

        /// <summary>Reference to the dictionary containing viewer presentation preferences.</summary>
        internal const string ViewerPreferences = "/ViewerPreferences";

        /// <summary>Reference to the root of the document's bookmark outline tree.</summary>
        internal const string Outlines = "/Outlines";

        /// <summary>Reference to the document-level metadata stream, typically XMP.</summary>
        internal const string Metadata = "/Metadata";

        /// <summary>Array of output-intent dictionaries describing intended color reproduction.</summary>
        internal const string OutputIntents = "/OutputIntents";

        /// <summary>Dictionary recording whether the document contains logical structure information.</summary>
        internal const string MarkInfo = "/MarkInfo";

        /// <summary>Reference to the root of the document's logical structure tree.</summary>
        internal const string StructureTreeRoot = "/StructTreeRoot";

        /// <summary>Default natural language of the document's text.</summary>
        internal const string Language = "/Lang";

        /// <summary>Reference to the dictionary of named destinations.</summary>
        internal const string Destinations = "/Dests";

        /// <summary>Reference to the number tree defining displayed page labels.</summary>
        internal const string PageLabels = "/PageLabels";

        /// <summary>Dictionary containing document-level name trees, including embedded files.</summary>
        internal const string Names = "/Names";

        /// <summary>Array of file-specification references associated with the document.</summary>
        internal const string AssociatedFiles = "/AF";
    }
}
