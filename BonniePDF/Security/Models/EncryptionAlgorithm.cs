namespace BonniePDF;

/// <summary>
/// Encryption algorithms supported by PDF encryption.
/// </summary>
public enum EncryptionAlgorithm
{
    /// <summary>
    /// 40-bit RC4 encryption (PDF 1.1+).
    /// Legacy encryption, not recommended for new documents.
    /// </summary>
    RC4_40 = 40,

    /// <summary>
    /// 128-bit RC4 encryption (PDF 1.4+).
    /// Standard encryption for PDF 1.4 compatibility.
    /// </summary>
    RC4_128 = 128,

    /// <summary>
    /// 256-bit AES encryption (PDF 1.7 Extension Level 3 / PDF 2.0).
    /// Requires PDF 1.7 mode.
    /// </summary>
    AES_256 = 256
}