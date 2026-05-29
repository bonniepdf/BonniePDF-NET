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
        Console.WriteLine("Generating Book Page Layout (Fluent API)...");
        using (var doc = BookPageLayoutExampleFluent.Generate())
        {
            doc.Save(Path.Combine(outputDir, "book_page_fluent.pdf"));
        }

        Console.WriteLine("Generating Book Page Layout (Tagged / PDF/UA-1)...");
        using (var doc = BookPageLayoutTaggedExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "book_page_tagged.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
