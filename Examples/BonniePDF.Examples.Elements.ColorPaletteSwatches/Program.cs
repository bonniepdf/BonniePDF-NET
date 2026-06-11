namespace BonniePDF.Examples.Elements.ColorPaletteSwatches;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Colour Palette...");
        using (var doc = ColorPaletteExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "color_palette.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
