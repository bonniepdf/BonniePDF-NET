namespace BonniePDF.Examples.Templates.MovieCatalog;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Movie Catalog...");
        using (var doc = MovieCatalogExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "movie_catalog.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
