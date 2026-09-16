namespace BonniePDF.Examples.Templates.ExhibitBundleIndex;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating exhibit-bundle-index...");
        using (var doc = ExhibitBundleIndexExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "exhibit-bundle-index.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
