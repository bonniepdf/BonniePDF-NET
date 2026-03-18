using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Text Alignment...");
                    BonniePDF.Examples.Examples._03_Typography.JustifiedTextExample.Generate(Path.Combine(outputDir, "justified_text.pdf"));
        Console.WriteLine("Generating Text Alignment (Fluent API)...");
                    BonniePDF.Examples.Examples._03_Typography.TextAlignmentFluentExample.Generate(Path.Combine(outputDir, "justified_text_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
