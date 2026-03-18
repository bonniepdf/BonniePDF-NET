namespace BonniePDF.Examples.Typography.RichTextStyles;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Rich Text Styles...");
        RichTextExample.Generate(Path.Combine(outputDir, "rich_text.pdf"));
        Console.WriteLine("Generating Rich Text Styles (Fluent API)...");
        RichTextStylesFluentExample.Generate(Path.Combine(outputDir, "rich_text_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}