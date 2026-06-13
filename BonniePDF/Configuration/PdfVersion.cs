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
    Pdf17,

    /// <summary>
    /// PDF 2.0 (ISO 32000-2). Foundation for PDF/A-4 and PDF/UA-2;
    /// initially feature-equivalent to PDF 1.7 output.
    /// </summary>
    Pdf20,
}
