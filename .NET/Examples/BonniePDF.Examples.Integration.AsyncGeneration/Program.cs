namespace BonniePDF.Examples.Integration.AsyncGeneration;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Async Generation...");
        AsyncExample.GenerateAsync(Path.Combine(outputDir, "async_document.pdf")).GetAwaiter().GetResult();

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}