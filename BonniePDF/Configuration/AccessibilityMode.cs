namespace BonniePDF.Configuration;

/// <summary>
/// Accessibility / tagged-PDF conformance mode for the document.
/// Independent of <see cref="DocumentSettings.PdfAEnabled"/>; both can be
/// enabled together, separately, or not at all.
///
/// When set to a non-<see cref="None"/> value, the writer emits a PDF
/// structure tree, marked-content (BDC/EMC) wrapping every visible text
/// run and image, the XMP <c>pdfuaid</c> conformance claim, and the
/// catalog-level <c>/MarkInfo</c> and <c>/Lang</c> entries — enough for
/// screen readers and assistive tech to interpret the document.
///
/// See <c>the specification</c> for the
/// full design and <c>this feature-implementation-plan.md</c> for the staged
/// implementation.
/// </summary>
public enum AccessibilityMode
{
    /// <summary>
    /// No tagging. Default. Produces a visually-correct but
    /// structurally-untagged PDF — same output as BonniePDF has always
    /// produced.
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
