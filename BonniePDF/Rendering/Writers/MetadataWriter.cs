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
    private readonly IPdfWriter? _writer;
    private readonly IAsyncPdfWriter? _asyncWriter;

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
        _writer!.WriteObject(id, BuildDocumentInfoDict(metadata));
        return id;
    }

    /// <summary>
    /// Writes a document info dictionary with metadata asynchronously
    /// </summary>
    public async Task<int> WriteDocumentInfoAsync(int id, DocumentMetadata metadata, CancellationToken cancellationToken = default)
    {
        await _asyncWriter!.WriteObjectAsync(id, BuildDocumentInfoDict(metadata), null, cancellationToken).ConfigureAwait(false);
        return id;
    }

    /// <summary>
    /// Builds the /Info dictionary shared by the sync and async paths.
    /// </summary>
    private static Dictionary<string, string> BuildDocumentInfoDict(DocumentMetadata metadata)
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

        return dict;
    }

    /// <summary>
    /// Writes XMP metadata to the PDF document.
    /// XMP (Extensible Metadata Platform) provides standardized metadata
    /// plus any conformance claims the document makes (PDF/A, PDF/UA, â€¦).
    /// The conformance blocks are derived from <paramref name="settings"/>
    /// via the additive-hook design - see <c>XmpConformanceBlock</c>.
    /// </summary>
    /// <param name="id">The object ID for the XMP metadata dictionary.</param>
    /// <param name="metadata">The document metadata.</param>
    /// <param name="settings">The document settings, read for PdfAEnabled and Accessibility.</param>
    public void WriteXmpMetadata(int id, DocumentMetadata metadata, DocumentSettings settings) =>
        WriteXmpMetadata(id, metadata, settings, settings.Conformance, settings.Accessibility == AccessibilityMode.PdfUa1);

    /// <summary>
    /// Writes XMP metadata claiming <paramref name="conformance"/> and, when
    /// <paramref name="claimPdfUa"/> is set, PDF/UA-1: the claims the honest-claim
    /// guard left standing, which can be fewer than <paramref name="settings"/> requests.
    /// </summary>
    public void WriteXmpMetadata(int id, DocumentMetadata metadata, DocumentSettings settings, PdfAConformance conformance, bool claimPdfUa)
    {
        var blocks = BuildConformanceBlocks(settings, conformance, claimPdfUa);
        string xmpXml = XmpMetadataWriter.GenerateXmpXml(metadata, blocks);
        byte[] xmpBytes = Encoding.UTF8.GetBytes(xmpXml);

        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/Metadata",
            ["/Subtype"] = "/XML"
        };

        // XMP metadata must NOT be encrypted and must NOT be compressed
        _writer!.WriteUnencryptedBinaryObject(id, dict, xmpBytes);
    }

    /// <summary>
    /// Writes XMP metadata to the PDF document asynchronously.
    /// </summary>
    public Task WriteXmpMetadataAsync(int id, DocumentMetadata metadata, DocumentSettings settings, CancellationToken cancellationToken = default) =>
        WriteXmpMetadataAsync(id, metadata, settings, settings.Conformance, settings.Accessibility == AccessibilityMode.PdfUa1, cancellationToken);

    /// <summary>Asynchronous sibling of <see cref="WriteXmpMetadata(int, DocumentMetadata, DocumentSettings, PdfAConformance, bool)"/>.</summary>
    public async Task WriteXmpMetadataAsync(int id, DocumentMetadata metadata, DocumentSettings settings, PdfAConformance conformance, bool claimPdfUa, CancellationToken cancellationToken = default)
    {
        var blocks = BuildConformanceBlocks(settings, conformance, claimPdfUa);
        string xmpXml = XmpMetadataWriter.GenerateXmpXml(metadata, blocks);
        byte[] xmpBytes = Encoding.UTF8.GetBytes(xmpXml);

        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/Metadata",
            ["/Subtype"] = "/XML"
        };

        await _asyncWriter!.WriteUnencryptedBinaryObjectAsync(id, dict, xmpBytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Build the list of conformance-namespace blocks the document
    /// claims, based on <paramref name="settings"/>. Currently PDF/A
    /// and PDF/UA-1; ZUGFeRD/Factur-X will register here too once
    /// implemented.
    /// </summary>
    private static IReadOnlyList<XmpConformanceBlock> BuildConformanceBlocks(DocumentSettings settings, PdfAConformance conformance, bool claimPdfUa)
    {
        var blocks = new List<XmpConformanceBlock>(3);
        var pdfA = XmpConformanceBlock.ForPdfA(conformance);
        if (pdfA != null) blocks.Add(pdfA);
        if (claimPdfUa) blocks.Add(XmpConformanceBlock.PdfUa1());

        // ZUGFeRD / Factur-X: only claim it on a PDF/A-3 document. Gating on
        // the (possibly guard-reset) Conformance means a suppressed PDF/A
        // claim also suppresses the fx block - Factur-X without PDF/A-3 is
        // meaningless, so the honest-claim guard cascades for free.
        if (settings.FacturX != null &&
            conformance is PdfAConformance.PdfA3b or PdfAConformance.PdfA3a)
        {
            blocks.Add(XmpConformanceBlock.FacturX(settings.FacturX));
        }

        return blocks;
    }
}
