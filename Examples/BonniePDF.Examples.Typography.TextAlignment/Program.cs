namespace BonniePDF.Examples.Typography.TextAlignment;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Text Alignment...");
        using (var doc = TextAlignmentExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "justified_text.pdf"));
        }
        Console.WriteLine("Generating Text Alignment (Fluent API)...");
        using (var doc = TextAlignmentExampleFluent.Generate())
        {
            doc.Save(Path.Combine(outputDir, "justified_text_fluent.pdf"));
        }

        Console.WriteLine("Generating Text Alignment (Tagged / PDF/UA-1)...");
        using (var doc = TextAlignmentTaggedExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "justified_text_tagged.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
