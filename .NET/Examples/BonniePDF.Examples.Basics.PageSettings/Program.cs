namespace BonniePDF.Examples.Basics.PageSettings;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Page Settings...");
        AllPagesExample.Generate(Path.Combine(outputDir, "all_page_sizes.pdf"));
        Console.WriteLine("Generating Page Settings (Fluent API)...");
        AllPagesFluentExample.Generate(Path.Combine(outputDir, "all_page_sizes_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}