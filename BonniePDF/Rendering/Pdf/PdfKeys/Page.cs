namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries describing an individual page.</summary>
    internal static class Page
    {
        /// <summary>Reference to the parent node in the page tree.</summary>
        internal const string Parent = "/Parent";

        /// <summary>Rectangle defining the physical page boundaries, in default user-space coordinates.</summary>
        internal const string MediaBox = "/MediaBox";

        /// <summary>Content stream or stream array containing the page's drawing instructions.</summary>
        internal const string Contents = "/Contents";

        /// <summary>Resource dictionary used by the page's content streams.</summary>
        internal const string Resources = "/Resources";

        /// <summary>Parent-tree key for the page's marked-content identifiers.</summary>
        internal const string StructureParents = "/StructParents";

        /// <summary>Ordering rule used to navigate the page's annotations with the keyboard.</summary>
        internal const string TabOrder = "/Tabs";

        /// <summary>Array of annotation references belonging to the page.</summary>
        internal const string Annotations = "/Annots";
    }
}
