using System;
using System.IO;
using BonniePDF;

namespace BonniePDF.Examples.Shared;

/// <summary>
/// Shared output helpers for the example projects' CI generation step.
/// </summary>
internal static class ExampleOutput
{
    /// <summary>
    /// Saves the document to <paramref name="pdfPath"/> and, alongside it, writes
    /// a faithful first-page SVG preview (<c>&lt;name&gt;.svg</c>) for the veraPDF
    /// card report. The preview reuses the same layout pipeline as PDF output, so
    /// it matches the validated PDF.
    ///
    /// Best-effort: a preview failure must never break the conformance gate, so it
    /// is swallowed and logged rather than thrown.
    /// </summary>
    public static void SaveWithPreview(this Document doc, string pdfPath)
    {
        doc.Save(pdfPath);
        try
        {
            string svgPath = Path.ChangeExtension(pdfPath, ".svg");
            File.WriteAllText(svgPath, doc.GenerateFirstPageSvg());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BonniePDF/preview] warn: SVG preview failed for {pdfPath}: {ex.Message}");
        }
    }
}
