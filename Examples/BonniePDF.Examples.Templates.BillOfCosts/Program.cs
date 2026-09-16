namespace BonniePDF.Examples.Templates.BillOfCosts;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating bill-of-costs...");
        using (var doc = BillOfCostsExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "bill-of-costs.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
