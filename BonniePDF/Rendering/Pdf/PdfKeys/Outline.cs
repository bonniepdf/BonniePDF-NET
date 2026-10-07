namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries describing the bookmark outline tree.</summary>
    internal static class Outline
    {
        /// <summary>Number of visible descendants; a negative value indicates a closed outline item.</summary>
        internal const string Count = "/Count";

        /// <summary>Reference to the first child outline item.</summary>
        internal const string First = "/First";

        /// <summary>Reference to the last child outline item.</summary>
        internal const string Last = "/Last";

        /// <summary>Text displayed for the bookmark in the viewer's outline panel.</summary>
        internal const string Title = "/Title";

        /// <summary>Reference to the parent outline item or outline root.</summary>
        internal const string Parent = "/Parent";

        /// <summary>Destination displayed when the outline item is activated.</summary>
        internal const string Destination = "/Dest";

        /// <summary>Reference to the previous sibling outline item.</summary>
        internal const string Previous = "/Prev";

        /// <summary>Reference to the next sibling outline item.</summary>
        internal const string Next = "/Next";
    }
}
