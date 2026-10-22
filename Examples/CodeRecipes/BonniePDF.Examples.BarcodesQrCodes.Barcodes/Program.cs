namespace BonniePDF.Examples.BarcodesQrCodes.Barcodes;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Barcodes...");
        using (var doc = BarcodesExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "barcodes.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}