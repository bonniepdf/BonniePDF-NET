using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Movie Catalog...");
                    BonniePDF.Examples.Examples._07_Templates.MoviesTableExample.Generate(Path.Combine(outputDir, "movies_list.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
