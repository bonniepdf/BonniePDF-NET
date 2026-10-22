namespace BonniePDF.Examples.GettingStarted.SimpleDocument;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Simple Document...");
        using (var doc = SimpleDocumentExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "simple_document.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}