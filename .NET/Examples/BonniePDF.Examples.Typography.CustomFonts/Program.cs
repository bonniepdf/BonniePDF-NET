namespace BonniePDF.Examples.Typography.CustomFonts;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Custom Fonts...");
        EmbeddedFontExample.Generate(Path.Combine(outputDir, "embedded_font.pdf"));
        Console.WriteLine("Generating Custom Fonts (Fluent API)...");
        CustomFontsFluentExample.Generate(Path.Combine(outputDir, "embedded_font_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}