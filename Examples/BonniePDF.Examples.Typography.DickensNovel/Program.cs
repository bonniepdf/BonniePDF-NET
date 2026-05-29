namespace BonniePDF.Examples.Typography.DickensNovel;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Dickens: A Tale of Two Cities...");
        using (var doc = DickensNovelExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "dickens_a_tale_of_two_cities.pdf"));
        }
        Console.WriteLine("Generating Dickens: A Tale of Two Cities (Fluent API)...");
        using (var doc = DickensNovelExampleFluent.Generate())
        {
            doc.Save(Path.Combine(outputDir, "dickens_a_tale_of_two_cities_fluent.pdf"));
        }

        Console.WriteLine("Generating Dickens: A Tale of Two Cities (Tagged / PDF/UA-1)...");
        using (var doc = DickensNovelTaggedExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "dickens_a_tale_of_two_cities_tagged.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
