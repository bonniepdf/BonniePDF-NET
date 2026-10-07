namespace BonniePDF.Examples.Typography.RichTextStyles;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Rich Text Styles...");
        using (var doc = RichTextExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "rich_text.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}