namespace BonniePDF.Examples.Typography.CustomFonts;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Custom Fonts...");
        using (var doc = CustomFontExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "embedded_font.pdf"));
        }
        Console.WriteLine("Generating Custom Fonts (Fluent API)...");
        using (var doc = CustomFontExampleFluent.Generate())
        {
            doc.Save(Path.Combine(outputDir, "embedded_font_fluent.pdf"));
        }

        Console.WriteLine("Generating Custom Fonts (Tagged / PDF/UA-1)...");
        var taggedFont = CustomFontTaggedExample.Generate();
        if (taggedFont is not null)
        {
            using (taggedFont)
                taggedFont.Save(Path.Combine(outputDir, "embedded_font_tagged.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
