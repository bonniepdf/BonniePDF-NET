namespace BonniePDF.Examples.Typography.TextDecorations;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Text Decorations...");
        using (var doc = TextDecorationExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "text_decorations.pdf"));
        }
        Console.WriteLine("Generating Text Decorations (Fluent API)...");
        using (var doc = TextDecorationExampleFluent.Generate())
        {
            doc.Save(Path.Combine(outputDir, "text_decorations_fluent.pdf"));
        }

        Console.WriteLine("Generating Text Decorations (Tagged / PDF/UA-1)...");
        using (var doc = TextDecorationTaggedExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "text_decorations_tagged.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
