using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using BonniePDF.Rendering.Writers;

namespace BonniePDF;

/// <summary>How a body page's content is held once layout has moved past it.</summary>
internal enum FinishedPageStorage
{
    /// <summary>Kept as text until the page is written (SVG export, uncompressed output).</summary>
    Keep,

    /// <summary>Encoded to its output bytes; page chrome is added around them when the page is written.</summary>
    Encoded,

    /// <summary>Encoded and compressed; for documents with no page chrome, this is the written stream.</summary>
    Compressed,

    /// <summary>Dropped (a layout dry run, whose output is never written).</summary>
    Discarded,
}

/// <summary>A finished page body, in the bytes it will be written as.</summary>
/// <param name="Bytes">The ISO-8859-1 encoding of the page text, compressed when <paramref name="Compressed"/> is set.</param>
/// <param name="Compressed">Whether <paramref name="Bytes"/> is already the FlateDecode stream.</param>
internal sealed record SealedPageBody(byte[] Bytes, bool Compressed)
{
    /// <summary>The page text's ISO-8859-1 bytes.</summary>
    internal byte[] Content() => Compressed ? ContentCompression.Decompress(Bytes) : Bytes;
}

/// <summary>
/// Holds finished body pages as bytes instead of UTF-16 text, so a long document's layout keeps
/// one or two bytes per character of page content rather than two plus builder slack, and the
/// writer does not copy each page again. Output is unchanged: a page is sealed only when every
/// character is ISO-8859-1, which the writer's encoding maps to itself.
/// </summary>
internal sealed class FinishedPageStore
{
    private readonly Dictionary<int, SealedPageBody> _bodies = new();

    internal FinishedPageStorage Storage { get; set; }

    /// <summary>
    /// Seals <paramref name="page"/>, whose layout is complete. Returns false, leaving the text
    /// in place, when sealing is off or the page holds a character outside ISO-8859-1.
    /// </summary>
    internal bool Seal(int index, StringBuilder page)
    {
        if (Storage == FinishedPageStorage.Keep) return false;
        if (Storage == FinishedPageStorage.Discarded) return true;
        foreach (ReadOnlyMemory<char> chunk in page.GetChunks())
            if (chunk.Span.IndexOfAnyExceptInRange('\0', 'ÿ') >= 0) return false;

        int length = page.Length;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(Math.Max(length, 1));
        try
        {
            int at = 0;
            foreach (ReadOnlyMemory<char> chunk in page.GetChunks())
                at += Encoding.Latin1.GetBytes(chunk.Span, buffer.AsSpan(at));
            ReadOnlySpan<byte> content = buffer.AsSpan(0, length);
            bool compressed = Storage == FinishedPageStorage.Compressed;
            _bodies[index] = new SealedPageBody(compressed ? ContentCompression.Compress(content) : content.ToArray(), compressed);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
        return true;
    }

    /// <summary>Removes and returns page <paramref name="index"/>'s sealed body, if it has one.</summary>
    internal SealedPageBody? Take(int index) => _bodies.Remove(index, out SealedPageBody? body) ? body : null;
}
