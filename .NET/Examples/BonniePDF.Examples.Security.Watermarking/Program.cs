using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Watermark - Confidential...");
                    BonniePDF.Examples.Examples._06_Security.WatermarkConfidentialExample.Generate(Path.Combine(outputDir, "watermark_confidential.pdf"));
        Console.WriteLine("Generating Watermark - Draft...");
                    BonniePDF.Examples.Examples._06_Security.WatermarkDraftExample.Generate(Path.Combine(outputDir, "watermark_draft.pdf"));
        Console.WriteLine("Generating Watermark - Fluent API...");
                    BonniePDF.Examples.Examples._06_Security.WatermarkFluentExample.Generate(Path.Combine(outputDir, "watermark_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
