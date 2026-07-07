namespace BonniePDF.Examples.Templates.ShelfLabel;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Retail Shelf Label...");
        using (var doc = ShelfLabelExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "shelf_label.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
