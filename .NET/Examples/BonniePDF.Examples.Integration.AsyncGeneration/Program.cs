namespace BonniePDF.Examples.Integration.AsyncGeneration;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Async Generation...");
        using (var doc = AsyncGenerationExample.GenerateAsync().GetAwaiter().GetResult())
        {
            doc.Save(Path.Combine(outputDir, "async_document.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
