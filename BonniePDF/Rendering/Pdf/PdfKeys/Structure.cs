namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries describing logical structure elements and their parent tree.</summary>
    internal static class Structure
    {
        /// <summary>Structure type identifying the element's semantic role.</summary>
        internal const string Role = "/S";

        /// <summary>Reference to the parent structure element or structure-tree root.</summary>
        internal const string Parent = "/P";

        /// <summary>Child structure elements, marked-content identifiers or content-object references.</summary>
        internal const string Kids = "/K";

        /// <summary>Reference to the page containing the structure element's content.</summary>
        internal const string Page = "/Pg";

        /// <summary>Alternative description of the structure element and its content.</summary>
        internal const string AlternativeText = "/Alt";

        /// <summary>Replacement text used when extracting or presenting the element's content.</summary>
        internal const string ActualText = "/ActualText";

        /// <summary>Natural language of this structure element's content.</summary>
        internal const string Language = "/Lang";

        /// <summary>Human-readable title of the structure element.</summary>
        internal const string Title = "/T";

        /// <summary>Attribute dictionary or array describing this structure element.</summary>
        internal const string Attributes = "/A";

        /// <summary>Number tree mapping content and annotation keys to their parent structure elements.</summary>
        internal const string ParentTree = "/ParentTree";

        /// <summary>Next unused integer key available for the structure parent tree.</summary>
        internal const string ParentTreeNextKey = "/ParentTreeNextKey";
    }
}
