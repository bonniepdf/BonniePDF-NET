namespace BonniePDF;

/// <summary>
/// Accessibility / tagged-PDF conformance mode for the document.
/// Independent of <see cref="DocumentSettings.Conformance"/>; both can be
/// enabled together, separately, or not at all.
///
/// When set to a non-<see cref="None"/> value, the writer emits a PDF
/// structure tree, marked-content (BDC/EMC) wrapping every visible text
/// run and image, the XMP <c>pdfuaid</c> conformance claim, and the
/// catalog-level <c>/MarkInfo</c> and <c>/Lang</c> entries - enough for
/// screen readers and assistive tech to interpret the document.
///
/// <para>
/// <see cref="PdfUa1"/> is PDF 1.7 output and <see cref="PdfUa2"/> is PDF 2.0
/// output; each selects its PDF version automatically. Asking for a version the
/// level cannot use (PDF/UA-1 with <see cref="PdfVersion.Pdf20"/>, PDF/UA-2 with
/// <see cref="PdfVersion.Pdf17"/> or a PDF/A-1, -2 or -3 level) throws rather
/// than dropping the tagging or the claim.
/// </para>
///
/// Tagged-PDF behaviour is defined by the shared specification, which both
/// implementations conform to.
/// </summary>
public enum AccessibilityMode
{
    /// <summary>
    /// No tagging. Produces a visually-correct but structurally-untagged PDF.
    /// </summary>
    None = 0,

    /// <summary>
    /// PDF/UA-1 (ISO 14289-1) tagged output. The library emits a
    /// structure tree, marked-content operators, and the corresponding
    /// metadata required for accessibility validators (VeraPDF profile
    /// <c>ua1</c>) to pass.
    /// </summary>
    PdfUa1 = 1,

    /// <summary>
    /// PDF/UA-2 (ISO 14289-2:2024) tagged output on PDF 2.0. Everything
    /// <see cref="PdfUa1"/> writes, with the structure tree in the PDF 2.0
    /// namespace, structure destinations for in-document links and bookmarks,
    /// list numbering on lists, and table-of-contents entries that reference
    /// their targets (VeraPDF profile <c>ua2</c>). Combines with
    /// <see cref="PdfAConformance.PdfA4"/>, not with PDF/A-1, -2 or -3.
    /// </summary>
    PdfUa2 = 2,
}

/// <summary>What each <see cref="AccessibilityMode"/> means to the engine.</summary>
internal static class AccessibilityModeExtensions
{
    /// <summary>True for either PDF/UA level: the document is tagged and claims PDF/UA.</summary>
    public static bool IsTagged(this AccessibilityMode mode) => mode is AccessibilityMode.PdfUa1 or AccessibilityMode.PdfUa2;

    /// <summary>The claim's spec token: <c>pdf/ua-1</c> or <c>pdf/ua-2</c>.</summary>
    public static string ClaimToken(this AccessibilityMode mode) => mode == AccessibilityMode.PdfUa2 ? "pdf/ua-2" : "pdf/ua-1";
}
