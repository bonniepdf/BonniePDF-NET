namespace BonniePDF.Examples.Typography.BookPageLayout;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Book Page Layout...");
        using (var doc = BookPageLayoutExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "book_page.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
