namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries describing raster image XObjects.</summary>
    internal static class Image
    {
        /// <summary>Width of the image in samples.</summary>
        internal const string Width = "/Width";

        /// <summary>Height of the image in samples.</summary>
        internal const string Height = "/Height";

        /// <summary>Color space used to interpret the image's color samples.</summary>
        internal const string ColorSpace = "/ColorSpace";

        /// <summary>Number of bits representing each image color component.</summary>
        internal const string BitsPerComponent = "/BitsPerComponent";

        /// <summary>Pairs of values mapping encoded image samples to color-component values.</summary>
        internal const string Decode = "/Decode";

        /// <summary>Reference to the grayscale image supplying this image's per-pixel alpha.</summary>
        internal const string SoftMask = "/SMask";
    }
}
