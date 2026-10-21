namespace BonniePDF.Examples.Templates.StatutoryComplianceReport;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Statutory Compliance Report...");
        using (var doc = StatutoryComplianceReportExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "statutory_compliance_report.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
