namespace BonniePDF.Configuration;

/// <summary>
/// PDF/A archival-conformance level for the document.
///
/// Orthogonal to <see cref="AccessibilityMode"/> in storage, but the
/// two interact: "a" (accessible) levels presuppose tagged output, so
/// selecting <see cref="PdfA1a"/>, <see cref="PdfA2a"/>, or
/// <see cref="PdfA3a"/> requires
/// <c>DocumentSettings.Accessibility = AccessibilityMode.PdfUa1</c>.
/// The serializer enforces this at write time and throws a
/// <c>PdfAConformanceException</c> if the combination is inconsistent.
///
/// "b" (basic) levels guarantee visual reproducibility only - they do
/// not require structure tagging and so are the safe upgrade path for
/// documents that have not yet adopted PDF/UA-1.
///
/// See <c>the specification</c> for the design.
/// </summary>
public enum PdfAConformance
{
    /// <summary>
    /// No PDF/A claim. Default. The document is a normal PDF.
    /// </summary>
    None = 0,

    /// <summary>
    /// PDF/A-1b (ISO 19005-1:2005, conformance B). Visual reproducibility
    /// against the PDF 1.4 feature set. Disallows transparency, JPEG2000,
    /// and embedded files. Retained for compatibility with legacy archival
    /// workflows whose checklists name "PDF/A-1b" specifically.
    /// </summary>
    PdfA1b = 1,

    /// <summary>
    /// PDF/A-1a (ISO 19005-1:2005, conformance A). Adds the PDF/A-1b
    /// requirements plus tagged-PDF structure for accessibility. Requires
    /// <see cref="AccessibilityMode.PdfUa1"/>.
    /// </summary>
    PdfA1a = 2,

    /// <summary>
    /// PDF/A-2b (ISO 19005-2:2011, conformance B). Modernised visual
    /// reproducibility based on PDF 1.7 - allows transparency, JPEG2000,
    /// optional content (layers), and OpenType font embedding. The
    /// default baseline for new BonniePDF documents that opt in via the
    /// obsolete <c>PdfAEnabled = true</c>.
    /// </summary>
    PdfA2b = 3,

    /// <summary>
    /// PDF/A-2a (ISO 19005-2:2011, conformance A). PDF/A-2b plus tagged
    /// structure for accessibility. Requires
    /// <see cref="AccessibilityMode.PdfUa1"/>. Recommended for new
    /// archival workflows that also need to satisfy WCAG / EAA.
    /// </summary>
    PdfA2a = 4,

    /// <summary>
    /// PDF/A-3b (ISO 19005-3:2012, conformance B). PDF/A-2b plus the
    /// ability to embed arbitrary file attachments - the basis for
    /// ZUGFeRD / Factur-X e-invoicing.
    /// </summary>
    PdfA3b = 5,

    /// <summary>
    /// PDF/A-3a (ISO 19005-3:2012, conformance A). PDF/A-3b plus tagged
    /// structure. Requires <see cref="AccessibilityMode.PdfUa1"/>.
    /// </summary>
    PdfA3a = 6,

    /// <summary>
    /// PDF/A-4 (ISO 19005-4:2020 baseline). PDF 2.0-based archival output.
    /// Drops the legacy a/b/u conformance split - there is a single
    /// PDF/A-4 baseline with no "a"/"b" suffix. Requires
    /// <see cref="PdfVersion.Pdf20"/>; the serializer auto-upgrades the
    /// version when this value is selected. The PDF/A-4f (embedded files)
    /// and PDF/A-4e (engineering) profiles are not implemented yet.
    /// </summary>
    PdfA4 = 7,
}
