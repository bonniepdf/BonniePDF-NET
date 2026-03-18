using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Lists...");
                    BonniePDF.Examples.Examples._04_Elements.ListExample.Generate(Path.Combine(outputDir, "list_example.pdf"));
        Console.WriteLine("Generating Lists (Fluent API)...");
                    BonniePDF.Examples.Examples._04_Elements.ListFluentExample.Generate(Path.Combine(outputDir, "list_example_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
