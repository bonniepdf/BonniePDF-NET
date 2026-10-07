namespace BonniePDF.Examples.Basics.PageSettings;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Page Settings...");
        using (var doc = PageSettingsExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "all_page_sizes.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}