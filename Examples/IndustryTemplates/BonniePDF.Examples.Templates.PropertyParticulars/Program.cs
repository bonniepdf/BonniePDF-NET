namespace BonniePDF.Examples.Templates.PropertyParticulars;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating property-particulars...");
        using (var doc = PropertyParticularsExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "property-particulars.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}