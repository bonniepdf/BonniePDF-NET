namespace BonniePDF.Examples.Typography.BookPageLayout;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Book Page Layout...");
        BookPageExample.Generate(Path.Combine(outputDir, "book_page.pdf"));
        Console.WriteLine("Generating Book Page Layout (Fluent API)...");
        BookPageLayoutFluentExample.Generate(Path.Combine(outputDir, "book_page_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}