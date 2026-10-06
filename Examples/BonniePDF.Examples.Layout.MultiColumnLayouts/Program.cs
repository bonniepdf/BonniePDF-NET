namespace BonniePDF.Examples.Layout.MultiColumnLayouts;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Multi-Column Layouts...");
        using (var doc = MultiColumnLayoutsExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "multi_column_layouts.pdf"));
        }
        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
