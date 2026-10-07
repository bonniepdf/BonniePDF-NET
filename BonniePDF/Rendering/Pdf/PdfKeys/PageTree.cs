namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries describing nodes in the page tree.</summary>
    internal static class PageTree
    {
        /// <summary>References to the immediate child pages or page-tree nodes.</summary>
        internal const string Kids = "/Kids";

        /// <summary>Total number of descendant pages in this page-tree node.</summary>
        internal const string Count = "/Count";
    }
}
