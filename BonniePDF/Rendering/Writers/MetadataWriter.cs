using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Configuration;
using BonniePDF.Metadata;
using BonniePDF.Rendering.Pdf;

namespace BonniePDF.Rendering.Writers;

/// <summary>
/// Writes metadata objects (DocumentInfo and XMP) to PDF.
/// </summary>
public class MetadataWriter
{
    private readonly IPdfWriter _writer;
    private readonly IAsyncPdfWriter _asyncWriter;

    /// <summary>
    /// Initializes a <see cref="MetadataWriter"/> for synchronous PDF writing.
    /// </summary>
    /// <param name="writer">The synchronous PDF writer to write metadata objects to.</param>
    public MetadataWriter(IPdfWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _asyncWriter = null;
    }

    /// <summary>
    /// Initializes a <see cref="MetadataWriter"/> for asynchronous PDF writing.
    /// </summary>
    /// <param name="asyncWriter">The asynchronous PDF writer to write metadata objects to.</param>
    public MetadataWriter(IAsyncPdfWriter asyncWriter)
    {
        _asyncWriter = asyncWriter ?? throw new ArgumentNullException(nameof(asyncWriter));
        _writer = null;
    }

    /// <summary>
    /// Writes a document info dictionary with metadata
    /// </summary>
    public int WriteDocumentInfo(int id, DocumentMetadata metadata)
    {
        Dictionary<string, string> dict = new();

        if (!string.IsNullOrEmpty(metadata.Title))
            dict["/Title"] = PdfSyntax.EncodeMetadataString(metadata.Title);

        if (!string.IsNullOrEmpty(metadata.Author))
            dict["/Author"] = PdfSyntax.EncodeMetadataString(metadata.Author);

        if (!string.IsNullOrEmpty(metadata.Subject))
            dict["/Subject"] = PdfSyntax.EncodeMetadataString(metadata.Subject);

        if (!string.IsNullOrEmpty(metadata.Keywords))
            dict["/Keywords"] = PdfSyntax.EncodeMetadataString(metadata.Keywords);

        if (!string.IsNullOrEmpty(metadata.Creator))
            dict["/Creator"] = PdfSyntax.EncodeMetadataString(metadata.Creator);

        if (!string.IsNullOrEmpty(metadata.Producer))
            dict["/Producer"] = PdfSyntax.EncodeMetadataString(metadata.Producer);

        if (metadata.CreationDate.HasValue)
            dict["/CreationDate"] = $"({PdfSyntax.FormatDate(metadata.CreationDate.Value)})";

        if (metadata.ModifiedDate.HasValue)
            dict["/ModDate"] = $"({PdfSyntax.FormatDate(metadata.ModifiedDate.Value)})";

        if (metadata.Trapped.HasValue)
            dict["/Trapped"] = metadata.Trapped.Value ? "/True" : "/False";

        foreach (KeyValuePair<string, string> kvp in metadata.CustomProperties)
            dict[$"/{PdfSyntax.Escape(kvp.Key)}"] = PdfSyntax.EncodeMetadataString(kvp.Value);

        _writer.WriteObject(id, dict);
        return id;
    }

    /// <summary>
    /// Writes a document info dictionary with metadata asynchronously
    /// </summary>
    public async Task<int> WriteDocumentInfoAsync(int id, DocumentMetadata metadata, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> dict = new();

        if (!string.IsNullOrEmpty(metadata.Title))
            dict["/Title"] = PdfSyntax.EncodeMetadataString(metadata.Title);

        if (!string.IsNullOrEmpty(metadata.Author))
            dict["/Author"] = PdfSyntax.EncodeMetadataString(metadata.Author);

        if (!string.IsNullOrEmpty(metadata.Subject))
            dict["/Subject"] = PdfSyntax.EncodeMetadataString(metadata.Subject);

        if (!string.IsNullOrEmpty(metadata.Keywords))
            dict["/Keywords"] = PdfSyntax.EncodeMetadataString(metadata.Keywords);

        if (!string.IsNullOrEmpty(metadata.Creator))
            dict["/Creator"] = PdfSyntax.EncodeMetadataString(metadata.Creator);

        if (!string.IsNullOrEmpty(metadata.Producer))
            dict["/Producer"] = PdfSyntax.EncodeMetadataString(metadata.Producer);

        if (metadata.CreationDate.HasValue)
            dict["/CreationDate"] = $"({PdfSyntax.FormatDate(metadata.CreationDate.Value)})";

        if (metadata.ModifiedDate.HasValue)
            dict["/ModDate"] = $"({PdfSyntax.FormatDate(metadata.ModifiedDate.Value)})";

        if (metadata.Trapped.HasValue)
            dict["/Trapped"] = metadata.Trapped.Value ? "/True" : "/False";

        foreach (KeyValuePair<string, string> kvp in metadata.CustomProperties)
            dict[$"/{PdfSyntax.Escape(kvp.Key)}"] = PdfSyntax.EncodeMetadataString(kvp.Value);

        await _asyncWriter.WriteObjectAsync(id, dict, null, cancellationToken).ConfigureAwait(false);
        return id;
    }

    /// <summary>
    /// Writes XMP metadata to the PDF document.
    /// XMP (Extensible Metadata Platform) provides standardized metadata
    /// plus any conformance claims the document makes (PDF/A, PDF/UA, …).
    /// The conformance blocks are derived from <paramref name="settings"/>
    /// via the additive-hook design - see <c>XmpConformanceBlock</c>.
    /// </summary>
    /// <param name="id">The object ID for the XMP metadata dictionary.</param>
    /// <param name="metadata">The document metadata.</param>
    /// <param name="settings">The document settings, read for PdfAEnabled and Accessibility.</param>
    public void WriteXmpMetadata(int id, DocumentMetadata metadata, DocumentSettings settings)
    {
        var blocks = BuildConformanceBlocks(settings);
        string xmpXml = XmpMetadataWriter.GenerateXmpXml(metadata, blocks);
        byte[] xmpBytes = Encoding.UTF8.GetBytes(xmpXml);

        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/Metadata",
            ["/Subtype"] = "/XML"
        };

        // XMP metadata must NOT be encrypted and must NOT be compressed
        _writer.WriteUnencryptedBinaryObject(id, dict, xmpBytes);
    }

    /// <summary>
    /// Writes XMP metadata to the PDF document asynchronously.
    /// </summary>
    public async Task WriteXmpMetadataAsync(int id, DocumentMetadata metadata, DocumentSettings settings, CancellationToken cancellationToken = default)
    {
        var blocks = BuildConformanceBlocks(settings);
        string xmpXml = XmpMetadataWriter.GenerateXmpXml(metadata, blocks);
        byte[] xmpBytes = Encoding.UTF8.GetBytes(xmpXml);

        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/Metadata",
            ["/Subtype"] = "/XML"
        };

        await _asyncWriter.WriteUnencryptedBinaryObjectAsync(id, dict, xmpBytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Build the list of conformance-namespace blocks the document
    /// claims, based on <paramref name="settings"/>. Currently PDF/A
    /// and PDF/UA-1; ZUGFeRD/Factur-X will register here too once
    /// implemented.
    /// </summary>
    private static IReadOnlyList<XmpConformanceBlock> BuildConformanceBlocks(DocumentSettings settings)
    {
        var blocks = new List<XmpConformanceBlock>(2);
        var pdfA = XmpConformanceBlock.ForPdfA(settings.Conformance);
        if (pdfA != null) blocks.Add(pdfA);
        if (settings.Accessibility == AccessibilityMode.PdfUa1) blocks.Add(XmpConformanceBlock.PdfUa1());
        return blocks;
    }
}
