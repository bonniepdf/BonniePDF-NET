namespace BonniePDF.Examples.Templates.AnnualReport;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Annual Report...");
        using (var doc = AnnualReportExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "annual_report.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
