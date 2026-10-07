namespace BonniePDF.Examples.Elements.QRCodes;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating QR Codes (Fluent API)...");
        using (var doc = QRCodesExampleFluent.Generate())
        {
            doc.Save(Path.Combine(outputDir, "qrcodes_fluent.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}