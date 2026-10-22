namespace BonniePDF.Examples.LinksReferences.CrossReferences;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Cross-References...");
        using (var doc = CrossReferencesExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "crossreferences_example.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}