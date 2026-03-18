using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating QR Codes (Fluent API)...");
                    BonniePDF.Examples.Examples._04_Elements.QRCodesFluentExample.Generate(Path.Combine(outputDir, "qrcodes_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
