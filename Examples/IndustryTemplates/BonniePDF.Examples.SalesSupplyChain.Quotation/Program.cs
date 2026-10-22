namespace BonniePDF.Examples.SalesSupplyChain.Quotation;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Quotation...");
        using (var doc = QuotationExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "quotation.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}