namespace BonniePDF.Examples.Basics.Margins;
internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Margins and Padding...");
        using (var doc = MarginsAndPadding.Generate())
        {
            doc.Save(Path.Combine(outputDir, "margins.pdf"));
        }
        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
