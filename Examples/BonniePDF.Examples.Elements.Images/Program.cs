namespace BonniePDF.Examples.Elements.Images;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Images...");
        using (var doc = ImagesExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "image_example.pdf"));
        }
        Console.WriteLine("Generating Images (Fluent API)...");
        using (var doc = ImagesExampleFluent.Generate())
        {
            doc.Save(Path.Combine(outputDir, "image_example_fluent.pdf"));
        }
        Console.WriteLine("Generating Images (Tagged / PDF/UA-1)...");
        using (var doc = ImagesTaggedExample.Generate())
        {
            doc.SaveWithPreview(Path.Combine(outputDir, "images_tagged.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
