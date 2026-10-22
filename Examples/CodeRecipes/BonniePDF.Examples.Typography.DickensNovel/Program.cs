namespace BonniePDF.Examples.Typography.DickensNovel;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        // The novel is a PDF/A-2a and PDF/UA-1 document. The accessibility gate collects every
        // *_tagged.pdf, so it is written under that name and validated in CI.
        Console.WriteLine("Generating Dickens: A Tale of Two Cities (PDF/A-2a, PDF/UA-1)...");
        using (var doc = DickensNovelExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "dickens_a_tale_of_two_cities_tagged.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
