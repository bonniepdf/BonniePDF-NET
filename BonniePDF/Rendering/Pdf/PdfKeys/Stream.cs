namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries describing encoded or embedded stream data.</summary>
    internal static class Stream
    {
        /// <summary>Decoding filter or filter array applied to the encoded stream bytes.</summary>
        internal const string Filter = "/Filter";

        /// <summary>Length of decoded data or its first segment, as recorded for embedded streams.</summary>
        internal const string DecodedLength = "/Length1";
    }
}
