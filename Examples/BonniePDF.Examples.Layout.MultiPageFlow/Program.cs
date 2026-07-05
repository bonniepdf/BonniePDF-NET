namespace BonniePDF.Examples.Layout.MultiPageFlow;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Tables...");
        using (var doc = TablesExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "tables.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
