namespace BonniePDF.Examples.Basics.SimpleDocument;

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
        Console.WriteLine("Generating Simple Document (Fluent API)...");
        using (var doc = SimpleDocumentFluentExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "simple_document_fluent.pdf"));
        }
        Console.WriteLine("Generating Simple Document (Tagged / PDF/UA-1)...");
        using (var doc = SimpleDocumentTaggedExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "simple_document_tagged.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
