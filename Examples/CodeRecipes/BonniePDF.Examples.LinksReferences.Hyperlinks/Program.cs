namespace BonniePDF.Examples.LinksReferences.Hyperlinks;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Hyperlinks...");
        using (var doc = HyperlinksExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "hyperlink_example.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}