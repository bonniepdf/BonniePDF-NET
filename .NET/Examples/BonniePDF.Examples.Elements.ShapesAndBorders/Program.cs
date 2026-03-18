namespace BonniePDF.Examples.Elements.ShapesAndBorders;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Shapes & Borders...");
        RoundedRectangleExample.Generate(Path.Combine(outputDir, "rounded_rectangles.pdf"));
        Console.WriteLine("Generating Shapes & Borders (Fluent API)...");
        RoundedRectangleFluentExample.Generate(Path.Combine(outputDir, "rounded_rectangles_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}