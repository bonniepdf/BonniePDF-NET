using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Custom Fonts...");
                    BonniePDF.Examples.Examples._03_Typography.EmbeddedFontExample.Generate(Path.Combine(outputDir, "embedded_font.pdf"));
        Console.WriteLine("Generating Custom Fonts (Fluent API)...");
                    BonniePDF.Examples.Examples._03_Typography.CustomFontsFluentExample.Generate(Path.Combine(outputDir, "embedded_font_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
