using BonniePDF.Demo;
namespace BonniePDF.Examples.Typography.TextAlignment;

sealed class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Text Alignment...");
        using (var doc = JustifiedTextExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "justified_text.pdf"));
        }
        Console.WriteLine("Generating Text Alignment (Fluent API)...");
        using (var doc = TextAlignmentFluentExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "justified_text_fluent.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
