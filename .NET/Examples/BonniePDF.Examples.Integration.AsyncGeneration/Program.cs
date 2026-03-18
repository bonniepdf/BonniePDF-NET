using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Async Generation...");
                    BonniePDF.Examples.Examples._08_Integration.AsyncExample.GenerateAsync(Path.Combine(outputDir, "async_document.pdf")).GetAwaiter().GetResult();

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
