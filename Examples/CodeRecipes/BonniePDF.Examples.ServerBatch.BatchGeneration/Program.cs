namespace BonniePDF.Examples.ServerBatch.BatchGeneration;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Batch Generation...");
        using (var doc = BatchGenerationExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "batch-generation.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}