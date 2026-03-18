using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Shapes & Borders...");
                    BonniePDF.Examples.Examples._04_Elements.RoundedRectangleExample.Generate(Path.Combine(outputDir, "rounded_rectangles.pdf"));
        Console.WriteLine("Generating Shapes & Borders (Fluent API)...");
                    BonniePDF.Examples.Examples._04_Elements.RoundedRectangleFluentExample.Generate(Path.Combine(outputDir, "rounded_rectangles_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
