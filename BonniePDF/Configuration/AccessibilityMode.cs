namespace BonniePDF.Configuration;

/// <summary>
/// Accessibility / tagged-PDF conformance mode for the document.
/// Independent of <see cref="DocumentSettings.PdfAEnabled"/>; both can be
/// enabled together, separately, or not at all.
///
/// When set to a non-<see cref="None"/> value, the writer emits a PDF
/// structure tree, marked-content (BDC/EMC) wrapping every visible text
/// run and image, the XMP <c>pdfuaid</c> conformance claim, and the
/// catalog-level <c>/MarkInfo</c> and <c>/Lang</c> entries - enough for
/// screen readers and assistive tech to interpret the document.
///
/// <para>
/// <b>Why no <c>PdfUa2</c> here yet:</b> PDF/UA-2 (ISO 14289-2:2024) is
/// bound to PDF 2.0 output. A PDF 2.0 writer now exists
/// (<see cref="PdfVersion.Pdf20"/>, used by the PDF/A-4 baseline), but the
/// PDF 2.0 tagged-PDF structure model that PDF/UA-2 requires is not
/// implemented - the structure tree is still emitted for PDF 1.7 only.
/// Adding a <c>PdfUa2</c> value before that lands would create a "lying
/// enum" - the same kind of claim-without-substance the honest-claim
/// guard exists to prevent. Requesting <see cref="PdfUa1"/> on a PDF 2.0
/// document throws rather than silently dropping the tagging. Roadmap, not
/// launch - do not add <c>PdfUa2</c> speculatively.
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
}
