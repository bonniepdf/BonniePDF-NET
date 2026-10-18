namespace BonniePDF.Examples.Templates.AuditReport;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating GDPR ISO 27001 Audit Report...");
        using (var doc = AuditReportExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "audit_report.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}