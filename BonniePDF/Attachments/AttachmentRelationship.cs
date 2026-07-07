namespace BonniePDF.Attachments;

/// <summary>
/// The relationship an embedded file has to the PDF that carries it, written
/// as the <c>/AFRelationship</c> entry of the file's <c>/Filespec</c>
/// dictionary (PDF 2.0 §7.11.3, ISO 19005-3 for PDF/A-3). Associated files
/// are the mechanism e-invoicing standards (ZUGFeRD / Factur-X) use to attach
/// the source XML alongside the rendered invoice.
/// </summary>
public enum AttachmentRelationship
{
    /// <summary>
    /// The relationship is not known or does not fit the other categories.
    /// Emitted as <c>/Unspecified</c>. Default.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// The file is the source from which the PDF's visible content was
    /// derived (e.g. the XML invoice a ZUGFeRD PDF renders). <c>/Source</c>.
    /// </summary>
    Source,

    /// <summary>
    /// The file holds data used to generate the visible content.
    /// <c>/Data</c>. The relationship Factur-X uses for its invoice XML.
    /// </summary>
    Data,

    /// <summary>
    /// The file is an alternative (e.g. a different format) representation
    /// of the PDF's content. <c>/Alternative</c>.
    /// </summary>
    Alternative,

    /// <summary>
    /// The file supplements the PDF's content with additional material.
    /// <c>/Supplement</c>.
    /// </summary>
    Supplement,

    /// <summary>
    /// The file is an encrypted payload; the PDF is a wrapper carrying it.
    /// <c>/EncryptedPayload</c>.
    /// </summary>
    EncryptedPayload,

    /// <summary>
    /// The file contains data associated with an AcroForm. <c>/FormData</c>.
    /// </summary>
    FormData,

    /// <summary>
    /// The file is a schema definition for other associated files.
    /// <c>/Schema</c>.
    /// </summary>
    Schema,
}
