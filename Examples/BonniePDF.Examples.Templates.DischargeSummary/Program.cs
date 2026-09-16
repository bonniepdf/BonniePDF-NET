namespace BonniePDF.Examples.Templates.DischargeSummary;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating discharge-summary...");
        using (var doc = DischargeSummaryExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "discharge-summary.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
