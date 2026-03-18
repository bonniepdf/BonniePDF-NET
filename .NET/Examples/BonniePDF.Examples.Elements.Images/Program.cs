using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Images...");
                    BonniePDF.Examples.Examples._04_Elements.ImageExample.Generate(Path.Combine(outputDir, "image_example.pdf"));
        Console.WriteLine("Generating Images (Fluent API)...");
                    BonniePDF.Examples.Examples._04_Elements.ImageFluentExample.Generate(Path.Combine(outputDir, "image_example_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
