namespace BonniePDF.Examples.Layout.HeadersFooters;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Headers & Footers...");
        HeadersFootersExample.Generate(Path.Combine(outputDir, "headers_footers.pdf"));
        Console.WriteLine("Generating Headers & Footers (Fluent API)...");
        HeadersFootersExampleFluent.Generate(Path.Combine(outputDir, "headers_footers_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}