namespace BonniePDF.Examples.Elements.Barcodes;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Barcodes (Fluent API)...");
        using (var doc = BarcodesExampleFluent.Generate())
        {
            doc.Save(Path.Combine(outputDir, "barcodes_fluent.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}