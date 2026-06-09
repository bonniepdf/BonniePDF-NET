namespace BonniePDF.Validation.Models;

/// <summary>
/// VeraPDF validation profile enumeration. Includes the PDF/A
/// conformance levels (1a / 1b / 2a / 2b / 3a / 3b) and PDF/UA-1
/// for accessibility validation.
/// </summary>
public enum ValidationProfile
{
    /// <summary>
    /// PDF/A-1b - Basic level
    /// </summary>
    PDF_A_1b,

    /// <summary>
    /// PDF/A-2b - Basic level
    /// </summary>
    PDF_A_2b,

    /// <summary>
    /// PDF/A-3b - Basic level
    /// </summary>
    PDF_A_3b,

    /// <summary>
    /// PDF/A-1a - Accessible level
    /// </summary>
    PDF_A_1a,

    /// <summary>
    /// PDF/A-2a - Accessible level
    /// </summary>
    PDF_A_2a,

    /// <summary>
    /// PDF/A-3a - Accessible level
    /// </summary>
    PDF_A_3a,

    /// <summary>
    /// PDF/UA-1 - accessibility / Universal Accessibility (ISO 14289-1).
    /// Independent from PDF/A; a document can claim both.
    /// </summary>
    PDF_UA_1
}
