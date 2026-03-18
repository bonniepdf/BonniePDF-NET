using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Dickens: A Tale of Two Cities...");
                    BonniePDF.Examples.Examples._03_Typography.DickensNovelExample.Generate(Path.Combine(outputDir, "dickens_a_tale_of_two_cities.pdf"));
        Console.WriteLine("Generating Dickens: A Tale of Two Cities (Fluent API)...");
                    BonniePDF.Examples.Examples._03_Typography.DickensNovelFluentExample.Generate(Path.Combine(outputDir, "dickens_a_tale_of_two_cities_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
