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

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}