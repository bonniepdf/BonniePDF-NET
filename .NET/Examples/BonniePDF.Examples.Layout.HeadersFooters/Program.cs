using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Headers & Footers...");
                    BonniePDF.Examples.Examples._02_Layout.HeadersFootersExample.Generate(Path.Combine(outputDir, "headers_footers.pdf"));
        Console.WriteLine("Generating Headers & Footers (Fluent API)...");
                    BonniePDF.Examples.Examples._02_Layout.HeadersFootersExampleFluent.Generate(Path.Combine(outputDir, "headers_footers_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
