namespace BonniePDF.Examples.Typography.TextAlignment;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Text Alignment...");
        JustifiedTextExample.Generate(Path.Combine(outputDir, "justified_text.pdf"));
        Console.WriteLine("Generating Text Alignment (Fluent API)...");
        TextAlignmentFluentExample.Generate(Path.Combine(outputDir, "justified_text_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}