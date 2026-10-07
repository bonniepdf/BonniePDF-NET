namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries describing annotations and their actions.</summary>
    internal static class Annotation
    {
        /// <summary>Rectangle enclosing the annotation, in PDF coordinates.</summary>
        internal const string Rectangle = "/Rect";

        /// <summary>Annotation flags controlling printing, visibility and other viewer behavior.</summary>
        internal const string Flags = "/F";

        /// <summary>Human-readable annotation text, including the accessible description of a link.</summary>
        internal const string Contents = "/Contents";

        /// <summary>Parent-tree key linking this annotation to its structure element.</summary>
        internal const string StructureParent = "/StructParent";

        /// <summary>Action executed when the annotation is activated.</summary>
        internal const string Action = "/A";
    }
}
