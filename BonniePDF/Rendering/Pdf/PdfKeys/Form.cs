namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries describing reusable form XObjects.</summary>
    internal static class Form
    {
        /// <summary>Rectangle bounding the form XObject in its own coordinate system.</summary>
        internal const string BoundingBox = "/BBox";

        /// <summary>Resource dictionary used by the form XObject's drawing instructions.</summary>
        internal const string Resources = "/Resources";

        /// <summary>Form XObject format identifier; the supported value is 1.</summary>
        internal const string FormType = "/FormType";

        /// <summary>Group dictionary describing the form's transparency-group behavior.</summary>
        internal const string Group = "/Group";
    }
}
