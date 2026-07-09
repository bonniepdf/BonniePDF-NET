using System;
using System.Diagnostics.CodeAnalysis;

namespace BonniePDF.Attachments;

/// <summary>
/// A file embedded into a generated PDF as an associated file (PDF 2.0
/// §7.11.4 / ISO 19005-3 for PDF/A-3). The engine writes it as an
/// <c>/EmbeddedFile</c> stream referenced from a <c>/Filespec</c> in the
/// document's <c>/Names /EmbeddedFiles</c> name tree, and lists the filespec
/// in the catalog's <c>/AF</c> array so conforming readers surface it.
///
/// <para>Attaching a file only affects generated documents; it does not read
/// or modify an existing PDF (that is the separate document-operations
/// feature, this feature).</para>
/// </summary>
public sealed class FileAttachment
{
    /// <summary>
    /// The file name shown to the user and stored in the filespec's
    /// <c>/F</c> and <c>/UF</c> entries (e.g. <c>"invoice.xml"</c>).
    /// Required.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// The raw bytes of the file. Required and non-empty.
    /// </summary>
    public required byte[] Content { get; set; }

    /// <summary>
    /// The relationship of this file to the document, written as
    /// <c>/AFRelationship</c>. Defaults to
    /// <see cref="AttachmentRelationship.Unspecified"/>.
    /// </summary>
    public AttachmentRelationship Relationship { get; set; } = AttachmentRelationship.Unspecified;

    /// <summary>
    /// The MIME media type of the file (e.g. <c>"text/xml"</c>), written as
    /// the embedded-file stream's <c>/Subtype</c>. Optional but recommended;
    /// PDF/A-3 and e-invoicing validators expect it.
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// A human-readable description, written as the filespec's <c>/Desc</c>.
    /// Optional.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The file's modification timestamp, written to the embedded-file
    /// stream's <c>/Params /ModDate</c>. Optional.
    /// </summary>
    public DateTimeOffset? ModificationDate { get; set; }

    /// <summary>
    /// The file's creation timestamp, written to the embedded-file stream's
    /// <c>/Params /CreationDate</c>. Optional.
    /// </summary>
    public DateTimeOffset? CreationDate { get; set; }

    /// <summary>Creates an empty attachment; set properties before use.</summary>
    public FileAttachment() { }

    /// <summary>
    /// Creates an attachment from a name and bytes with an optional
    /// relationship and media type.
    /// </summary>
    [SetsRequiredMembers]
    public FileAttachment(string fileName, byte[] content,
        AttachmentRelationship relationship = AttachmentRelationship.Unspecified,
        string? mediaType = null)
    {
        FileName = fileName;
        Content = content;
        Relationship = relationship;
        MediaType = mediaType;
    }
}
