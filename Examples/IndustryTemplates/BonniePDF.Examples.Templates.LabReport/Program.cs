namespace BonniePDF.Examples.Templates.LabReport;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Pathology Lab Report...");
        using (var doc = LabReportExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "lab_report.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}