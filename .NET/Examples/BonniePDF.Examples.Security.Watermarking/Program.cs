namespace BonniePDF.Examples.Security.Watermarking;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Watermark - Confidential...");
        WatermarkConfidentialExample.Generate(Path.Combine(outputDir, "watermark_confidential.pdf"));
        Console.WriteLine("Generating Watermark - Draft...");
        WatermarkDraftExample.Generate(Path.Combine(outputDir, "watermark_draft.pdf"));
        Console.WriteLine("Generating Watermark - Fluent API...");
        WatermarkFluentExample.Generate(Path.Combine(outputDir, "watermark_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}