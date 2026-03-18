using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Hyperlinks...");
                    BonniePDF.Examples.Examples._04_Elements.HyperlinkExample.Generate(Path.Combine(outputDir, "hyperlink_example.pdf"));
        Console.WriteLine("Generating Hyperlinks (Fluent API)...");
                    BonniePDF.Examples.Examples._04_Elements.HyperlinkFluentExample.Generate(Path.Combine(outputDir, "hyperlink_example_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
