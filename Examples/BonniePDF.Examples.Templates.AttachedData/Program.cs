namespace BonniePDF.Examples.Templates.AttachedData;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Report with Embedded Data...");
        using (var doc = AttachedDataExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "attached-data.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
