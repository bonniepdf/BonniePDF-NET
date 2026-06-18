namespace BonniePDF.Examples.Templates.ProductRoadmap;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Product Roadmap...");
        using (var doc = ProductRoadmapExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "product_roadmap.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
