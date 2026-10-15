using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using BonniePDF.Rendering.Writers;

namespace BonniePDF;

/// <summary>
/// Data emitted for a single rendered page
/// </summary>
internal class PageRenderOutput
{
    private string? _contentStream;

    /// <summary>
    /// The page's content stream as text. For a page whose body was sealed, this is rebuilt on
    /// request; the writer uses <see cref="CompressedContent"/> instead and never asks.
    /// </summary>
    public string ContentStream
    {
        get => _contentStream ??= Prefix + Encoding.Latin1.GetString(Body!.Content()) + Suffix;
        set => _contentStream = value;
    }

    /// <summary>The sealed body, when layout stored it as bytes; null when <see cref="ContentStream"/> holds everything.</summary>
    internal SealedPageBody? Body { get; init; }

    /// <summary>Chrome painted behind a sealed body (page colour, background slot).</summary>
    internal string Prefix { get; init; } = "";

    /// <summary>Chrome painted over a sealed body (header, footer, watermark, foreground slot).</summary>
    internal string Suffix { get; init; } = "";

    /// <summary>
    /// The FlateDecode content stream of a page with a sealed body: <see cref="Prefix"/>, body and
    /// <see cref="Suffix"/>, encoded and compressed exactly as the writer compresses a text stream.
    /// </summary>
    internal byte[] CompressedContent()
    {
        SealedPageBody body = Body ?? throw new InvalidOperationException("The page has no sealed body.");
        if (body.Compressed && Prefix.Length == 0 && Suffix.Length == 0) return body.Bytes;

        byte[] content = body.Content();
        Encoding latin1 = Encoding.Latin1;
        int length = latin1.GetByteCount(Prefix) + content.Length + latin1.GetByteCount(Suffix);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(Math.Max(length, 1));
        try
        {
            int at = latin1.GetBytes(Prefix, buffer);
            content.CopyTo(buffer, at);
            at += content.Length;
            at += latin1.GetBytes(Suffix, buffer.AsSpan(at));
            return ContentCompression.Compress(buffer.AsSpan(0, at));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public PdfSize Size { get; set; }
    public required IList<Hyperlink> Hyperlinks { get; set; }

    /// <summary>
    /// The page's resolved solid fill (a per-page PageColor overriding the document
    /// default), when one is set. Recorded for the layout snapshot so the contrast
    /// preflight can resolve a backdrop for text with no coloured ancestor.
    /// </summary>
    public Color? PageColor { get; set; }

    /// <summary>
    /// True when this page paints a background <em>element</em> rather than (or as
    /// well as) a flat colour, so its backdrop cannot be reduced to one opaque
    /// colour.
    /// </summary>
    public bool PageBackdropUnknown { get; set; }
}