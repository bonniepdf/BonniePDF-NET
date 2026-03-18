using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Text Decorations...");
                    BonniePDF.Examples.Examples._03_Typography.TextDecorationExample.Generate(Path.Combine(outputDir, "text_decorations.pdf"));
        Console.WriteLine("Generating Text Decorations (Fluent API)...");
                    BonniePDF.Examples.Examples._03_Typography.TextDecorationsFluentExample.Generate(Path.Combine(outputDir, "text_decorations_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
