namespace BonniePDF.Examples.Layout.HeadersFooters;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Headers & Footers...");
        using (var doc = HeadersFootersExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "headers_footers.pdf"));
        }
        Console.WriteLine("Generating Headers & Footers (Fluent API)...");
        using (var doc = HeadersFootersExampleFluent.Generate())
        {
            doc.Save(Path.Combine(outputDir, "headers_footers_fluent.pdf"));
        }

        Console.WriteLine("Generating Headers & Footers (Tagged / PDF/UA-1)...");
        using (var doc = HeadersFootersTaggedExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "headers_footers_tagged.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
