using BonniePDF.Demo;
namespace BonniePDF.Examples.Templates.MovieCatalog;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Movie Catalog...");
        using (var doc = MoviesTableExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "movies_list.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
