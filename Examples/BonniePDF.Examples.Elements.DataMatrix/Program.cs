namespace BonniePDF.Examples.Elements.DataMatrix;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Data Matrix...");
        using (var doc = DataMatrixExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "datamatrix.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}