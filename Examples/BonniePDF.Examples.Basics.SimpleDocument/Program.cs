namespace BonniePDF.Examples.Basics.SimpleDocument;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Simple Document...");
        using (var doc = SimpleDocumentExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "simple_document.pdf"));
        }
        Console.WriteLine("Generating Archival & Accessible PDF...");
        using (var doc = PdfComplianceExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "pdf_compliance.pdf"));
        }
        Console.WriteLine("Generating PDF/A-1b validation fixture...");
        using (var doc = PdfA1ValidationExample.GenerateBasic())
        {
            doc.Save(Path.Combine(outputDir, "pdfa1b_baseline.pdf"));
        }
        Console.WriteLine("Generating PDF/A-1a validation fixture...");
        using (var doc = PdfA1ValidationExample.GenerateAccessible())
        {
            doc.Save(Path.Combine(outputDir, "pdfa1a_baseline.pdf"));
        }
        Console.WriteLine("Generating PDF/A-3a validation fixture...");
        using (var doc = PdfA3aValidationExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "pdfa3a_baseline.pdf"));
        }
        Console.WriteLine("Generating PDF/A-4 validation fixture...");
        using (var doc = PdfA4ValidationExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "pdfa4_baseline.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
