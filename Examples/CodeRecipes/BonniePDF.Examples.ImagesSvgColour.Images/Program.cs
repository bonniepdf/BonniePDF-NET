namespace BonniePDF.Examples.ImagesSvgColour.Images;

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

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}