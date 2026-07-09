using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Attachments;
using BonniePDF.Rendering.Pdf;

namespace BonniePDF.Rendering.Writers;

/// <summary>
/// Writes embedded-file objects for document attachments: one
/// <c>/EmbeddedFile</c> stream and one <c>/Filespec</c> per attachment, plus
/// the shared <c>/EmbeddedFiles</c> name tree the catalog references from
/// <c>/Names</c>. The filespec object IDs are returned so the caller can list
/// them in the catalog's <c>/AF</c> associated-files array (PDF/A-3 Â§6.8).
/// </summary>
public class AttachmentWriter
{
    private readonly IPdfWriter? _writer;
    private readonly IAsyncPdfWriter? _asyncWriter;

    public AttachmentWriter(IPdfWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _asyncWriter = null;
    }

    public AttachmentWriter(IAsyncPdfWriter asyncWriter)
    {
        _asyncWriter = asyncWriter ?? throw new ArgumentNullException(nameof(asyncWriter));
        _writer = null;
    }

    /// <summary>
    /// The result of writing a document's attachments: the object ID of the
    /// <c>/EmbeddedFiles</c> name-tree dictionary (referenced from the catalog
    /// as <c>/Names &lt;&lt; /EmbeddedFiles N 0 R &gt;&gt;</c>) and the filespec
    /// object IDs for the catalog's <c>/AF</c> array.
    /// </summary>
    public readonly record struct Result(int NamesEmbeddedFilesId, IList<int> AssociatedFileIds);

    /// <summary>
    /// Writes every attachment and the enclosing name tree. Returns a
    /// <see cref="Result"/> with <c>NamesEmbeddedFilesId == 0</c> when there
    /// is nothing to write.
    /// </summary>
    public Result WriteAttachments(IList<FileAttachment> attachments)
    {
        List<FileAttachment> ordered = OrderAndValidate(attachments);
        if (ordered.Count == 0)
            return new Result(0, new List<int>());

        List<int> afIds = new(ordered.Count);
        List<(string Name, int FilespecId)> nameEntries = new(ordered.Count);

        foreach (FileAttachment a in ordered)
        {
            int streamId = _writer!.GetNewId();
            _writer!.WriteBinaryObject(streamId, BuildEmbeddedFileDict(a), a.Content);

            int filespecId = _writer!.GetNewId();
            _writer!.WriteObject(filespecId, BuildFilespecDict(a, streamId));

            afIds.Add(filespecId);
            nameEntries.Add((a.FileName, filespecId));
        }

        int namesId = _writer!.GetNewId();
        _writer!.WriteObject(namesId, BuildNameTreeDict(nameEntries));

        return new Result(namesId, afIds);
    }

    /// <summary>Asynchronous sibling of <see cref="WriteAttachments"/>.</summary>
    public async Task<Result> WriteAttachmentsAsync(IList<FileAttachment> attachments,
        CancellationToken cancellationToken = default)
    {
        List<FileAttachment> ordered = OrderAndValidate(attachments);
        if (ordered.Count == 0)
            return new Result(0, new List<int>());

        List<int> afIds = new(ordered.Count);
        List<(string Name, int FilespecId)> nameEntries = new(ordered.Count);

        foreach (FileAttachment a in ordered)
        {
            int streamId = _asyncWriter!.GetNewId();
            await _asyncWriter!.WriteBinaryObjectAsync(streamId, BuildEmbeddedFileDict(a), a.Content, a.Content.Length, cancellationToken)
                .ConfigureAwait(false);

            int filespecId = _asyncWriter!.GetNewId();
            await _asyncWriter!.WriteObjectAsync(filespecId, BuildFilespecDict(a, streamId), null, cancellationToken)
                .ConfigureAwait(false);

            afIds.Add(filespecId);
            nameEntries.Add((a.FileName, filespecId));
        }

        int namesId = _asyncWriter!.GetNewId();
        await _asyncWriter!.WriteObjectAsync(namesId, BuildNameTreeDict(nameEntries), null, cancellationToken)
            .ConfigureAwait(false);

        return new Result(namesId, afIds);
    }

    /// <summary>
    /// Drops attachments with no name or empty content, then orders the
    /// survivors by file name. The name tree's <c>/Names</c> array must be
    /// sorted by key (PDF Â§7.9.6), and a stable order also keeps output
    /// deterministic across the two implementations.
    /// </summary>
    private static List<FileAttachment> OrderAndValidate(IList<FileAttachment> attachments)
    {
        if (attachments == null || attachments.Count == 0)
            return new List<FileAttachment>();

        return attachments
            .Where(a => a != null && !string.IsNullOrEmpty(a.FileName) && a.Content is { Length: > 0 })
            .OrderBy(a => a.FileName, StringComparer.Ordinal)
            .ToList();
    }

    private static Dictionary<string, string> BuildEmbeddedFileDict(FileAttachment a)
    {
        StringBuilder paramsDict = new();
        paramsDict.Append("<< /Size ").Append(a.Content.Length.ToString(CultureInfo.InvariantCulture));

        if (a.ModificationDate is { } mod)
            paramsDict.Append(" /ModDate (").Append(PdfSyntax.FormatDate(mod)).Append(')');
        if (a.CreationDate is { } created)
            paramsDict.Append(" /CreationDate (").Append(PdfSyntax.FormatDate(created)).Append(')');

        paramsDict.Append(" /CheckSum <").Append(Md5Hex(a.Content)).Append("> >>");

        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/EmbeddedFile",
            ["/Params"] = paramsDict.ToString(),
        };

        if (!string.IsNullOrEmpty(a.MediaType))
            dict["/Subtype"] = "/" + EncodeName(a.MediaType);

        return dict;
    }

    private static Dictionary<string, string> BuildFilespecDict(FileAttachment a, int streamId)
    {
        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/Filespec",
            ["/F"] = PdfSyntax.EncodeMetadataString(a.FileName),
            ["/UF"] = PdfSyntax.EncodeMetadataString(a.FileName),
            ["/EF"] = $"<< /F {streamId} 0 R /UF {streamId} 0 R >>",
            ["/AFRelationship"] = "/" + a.Relationship,
        };

        if (!string.IsNullOrEmpty(a.Description))
            dict["/Desc"] = PdfSyntax.EncodeMetadataString(a.Description);

        return dict;
    }

    private static Dictionary<string, string> BuildNameTreeDict(IEnumerable<(string Name, int FilespecId)> entries)
    {
        StringBuilder names = new("[ ");
        foreach ((string name, int filespecId) in entries)
            names.Append(PdfSyntax.EncodeMetadataString(name)).Append(' ').Append(filespecId).Append(" 0 R ");
        names.Append(']');

        return new Dictionary<string, string> { ["/Names"] = names.ToString() };
    }

    private static string Md5Hex(byte[] data)
    {
        byte[] hash = MD5.HashData(data);
        StringBuilder sb = new(hash.Length * 2);
        foreach (byte b in hash)
            sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    /// <summary>
    /// Encodes an arbitrary string as a PDF name body, escaping any character
    /// outside the regular name set as <c>#XX</c> over its UTF-8 bytes
    /// (PDF Â§7.3.5). E.g. <c>"text/xml"</c> becomes <c>text#2Fxml</c>.
    /// </summary>
    private static string EncodeName(string value)
    {
        StringBuilder sb = new(value.Length + 4);
        foreach (byte b in Encoding.UTF8.GetBytes(value))
        {
            bool regular = b is (>= (byte)'a' and <= (byte)'z')
                or (>= (byte)'A' and <= (byte)'Z')
                or (>= (byte)'0' and <= (byte)'9')
                or (byte)'-' or (byte)'_' or (byte)'.';
            if (regular)
                sb.Append((char)b);
            else
                sb.Append('#').Append(b.ToString("X2", CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }
}
