namespace BonniePDF.Examples.ImagesSvgColour.ShapesAndBorders;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Shapes & Borders...");
        using (var doc = ShapesAndBordersExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "rounded_rectangles.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}