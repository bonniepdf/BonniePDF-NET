
namespace BonniePDF;

/// <summary>
/// Conformance rules for embedded files. Kept in one place so the .NET and
/// TypeScript implementations enforce the same honest-claim behaviour: an
/// attachment is only embedded when the declared PDF/A level actually permits
/// embedded files. Where it does not, the serializer drops the attachment and
/// records a diagnostic rather than emitting a PDF that fails its own claim.
/// </summary>
public static class AttachmentRules
{
    /// <summary>
    /// True when a document declaring <paramref name="conformance"/> may carry
    /// embedded files.
    ///
    /// <list type="bullet">
    /// <item><see cref="PdfAConformance.None"/> - allowed (ordinary PDF).</item>
    /// <item>PDF/A-1 (1a/1b) - forbidden; ISO 19005-1 disallows embedded
    /// files entirely.</item>
    /// <item>PDF/A-2 (2a/2b) - forbidden here; ISO 19005-2 permits embedding
    /// only files that are themselves PDF/A, which the engine cannot verify
    /// for an arbitrary byte payload.</item>
    /// <item>PDF/A-3 (3a/3b) - allowed; ISO 19005-3 exists precisely to carry
    /// associated files of any type (the ZUGFeRD / Factur-X basis).</item>
    /// <item>PDF/A-4 - allowed; built on PDF 2.0, which permits associated
    /// files.</item>
    /// </list>
    /// </summary>
    public static bool AllowsEmbeddedFiles(PdfAConformance conformance) => conformance switch
    {
        PdfAConformance.None => true,
        PdfAConformance.PdfA3a => true,
        PdfAConformance.PdfA3b => true,
        PdfAConformance.PdfA4 => true,
        _ => false,
    };
}
