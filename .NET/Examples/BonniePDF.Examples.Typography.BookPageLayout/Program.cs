using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Book Page Layout...");
                    BonniePDF.Examples.Examples._03_Typography.BookPageExample.Generate(Path.Combine(outputDir, "book_page.pdf"));
        Console.WriteLine("Generating Book Page Layout (Fluent API)...");
                    BonniePDF.Examples.Examples._03_Typography.BookPageLayoutFluentExample.Generate(Path.Combine(outputDir, "book_page_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
