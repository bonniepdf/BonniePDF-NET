using System;
using System.IO;
using System.IO.Compression;

namespace BonniePDF.Rendering.Writers;

/// <summary>
/// The one FlateDecode encoding of a page content stream. The writers and the page store both
/// call it, so a page compressed while layout is still running is byte-for-byte the stream the
/// writer would have produced from the same text.
/// </summary>
internal static class ContentCompression
{
    /// <summary>Compresses <paramref name="content"/> with zlib framing, in a single write.</summary>
    internal static byte[] Compress(ReadOnlySpan<byte> content)
    {
        using MemoryStream compressed = new();
        return Compress(content, compressed);
    }

    /// <summary>
    /// Compresses <paramref name="content"/> through <paramref name="scratch"/>, which is emptied
    /// first and keeps its buffer, so a caller compressing page after page does not regrow one each time.
    /// </summary>
    internal static byte[] Compress(ReadOnlySpan<byte> content, MemoryStream scratch)
    {
        scratch.SetLength(0);
        using (ZLibStream zlib = new(scratch, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            zlib.Write(content);
        }
        return scratch.ToArray();
    }

    /// <summary>Reverses <see cref="Compress"/>.</summary>
    internal static byte[] Decompress(byte[] compressed)
    {
        using MemoryStream source = new(compressed);
        using ZLibStream zlib = new(source, CompressionMode.Decompress);
        using MemoryStream content = new();
        zlib.CopyTo(content);
        return content.ToArray();
    }
}
