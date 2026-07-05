namespace BonniePDF.Examples.Basics.Margins;
internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Margins & Units...");
        using (var doc = MarginsExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "margins.pdf"));
        }
        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
