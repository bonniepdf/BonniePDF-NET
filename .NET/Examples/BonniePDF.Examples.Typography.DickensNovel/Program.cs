namespace BonniePDF.Examples.Typography.DickensNovel;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Dickens: A Tale of Two Cities...");
        DickensNovelExample.Generate(Path.Combine(outputDir, "dickens_a_tale_of_two_cities.pdf"));
        Console.WriteLine("Generating Dickens: A Tale of Two Cities (Fluent API)...");
        DickensNovelFluentExample.Generate(Path.Combine(outputDir, "dickens_a_tale_of_two_cities_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}