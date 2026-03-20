namespace BonniePDF.Examples.Typography.CustomFonts;

sealed class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Custom Fonts...");
        using (var doc = EmbeddedFontExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "embedded_font.pdf"));
        }
        Console.WriteLine("Generating Custom Fonts (Fluent API)...");
        using (var doc = CustomFontsFluentExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "embedded_font_fluent.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
