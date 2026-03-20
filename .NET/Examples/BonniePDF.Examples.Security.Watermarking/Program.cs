namespace BonniePDF.Examples.Security.Watermarking;

sealed class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Watermark - Confidential...");
        using (var doc = WatermarkConfidentialExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "watermark_confidential.pdf"));
        }
        Console.WriteLine("Generating Watermark - Draft...");
        using (var doc = WatermarkDraftExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "watermark_draft.pdf"));
        }
        Console.WriteLine("Generating Watermark - Fluent API...");
        using (var doc = WatermarkFluentExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "watermark_fluent.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
