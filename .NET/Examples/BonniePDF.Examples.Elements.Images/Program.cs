namespace BonniePDF.Examples.Elements.Images;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Images...");
        ImageExample.Generate(Path.Combine(outputDir, "image_example.pdf"));
        Console.WriteLine("Generating Images (Fluent API)...");
        ImageFluentExample.Generate(Path.Combine(outputDir, "image_example_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}