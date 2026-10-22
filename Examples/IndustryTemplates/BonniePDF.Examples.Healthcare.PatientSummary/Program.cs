namespace BonniePDF.Examples.Healthcare.PatientSummary;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Patient Summary...");
        using (var doc = PatientSummaryExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "patient_summary.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}