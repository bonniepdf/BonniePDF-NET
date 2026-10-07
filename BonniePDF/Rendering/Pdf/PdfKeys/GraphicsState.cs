namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries configuring transparency in extended graphics states.</summary>
    internal static class GraphicsState
    {
        /// <summary>Constant alpha applied to nonstroking painting operations.</summary>
        internal const string FillAlpha = "/ca";

        /// <summary>Constant alpha applied to stroking painting operations.</summary>
        internal const string StrokeAlpha = "/CA";

        /// <summary>Soft-mask dictionary or name controlling transparency for subsequent painting.</summary>
        internal const string SoftMask = "/SMask";
    }
}
