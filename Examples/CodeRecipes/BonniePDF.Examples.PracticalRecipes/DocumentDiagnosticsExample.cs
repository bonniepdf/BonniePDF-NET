using BonniePDF;

namespace BonniePDF.Examples.PracticalRecipes;

/// <summary>Inspect diagnostics after generation and build a clean retry.</summary>
public static class DocumentDiagnosticsExample
{
    public static Document Generate()
    {
        var doc = new Document(new DocumentSettings { Language = "en-GB" }) { Metadata = new DocumentMetadata { Title = "Check the generated document" } };
        doc.Add(new Heading("Check the generated document") { Level = 1, FontSize = 24 });
        doc.Add(new TextBlock("Await generation, then inspect document.diagnostics. A recoverable problem may produce a PDF while still dropping content."));
        doc.Add(new TextBlock("For strict text coverage, select missingGlyphs: throw. Repair the input or provide a covering font, and build a fresh document before retrying.") { Margin = new Thickness(0, 12, 0, 0) });
        doc.Add(new TextBlock("Diagnostics are not a PDF/A or PDF/UA certificate. Validate the final file with the required external validator.") { Margin = new Thickness(0, 12, 0, 0) });
        return doc;
    }

    public static async Task VerifyDiagnosticsAsync()
    {
        using (var incomplete = Generate())
        {
            incomplete.Add(new TextBlock(char.ConvertFromUtf32(0x10ffff)));
            await incomplete.GenerateBytesAsync();
            if (!incomplete.Diagnostics.Any(d => d.Category == DiagnosticCategory.Font && d.Severity == DiagnosticSeverity.Warning))
                throw new InvalidOperationException("Expected a missing-character diagnostic.");
        }
        using var corrected = Generate();
        await corrected.GenerateBytesAsync();
        if (corrected.Diagnostics.Any(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning))
            throw new InvalidOperationException("The corrected document still reports a problem.");
    }
}
