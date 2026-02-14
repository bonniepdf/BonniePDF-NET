namespace BonniePDF.Configuration;

/// <summary>
/// Specifies the PDF version for the output document.
/// </summary>
public enum PdfVersion
{
    /// <summary>
    /// PDF 1.4 (default). RC4 encryption support.
    /// </summary>
    Pdf14,

    /// <summary>
    /// PDF 1.7. AES-256 encryption, PDF/A-1b compliance, XMP metadata, and bookmarks.
    /// </summary>
    Pdf17
}
