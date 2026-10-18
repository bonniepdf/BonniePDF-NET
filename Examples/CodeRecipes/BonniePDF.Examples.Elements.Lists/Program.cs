namespace BonniePDF.Examples.Elements.Lists;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Lists...");
        using (var doc = ListsExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "list_example.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}