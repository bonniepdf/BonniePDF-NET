namespace BonniePDF.Examples.SalesSupplyChain.CommercialInvoice;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Commercial Invoice...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        using var doc = CommercialInvoiceExample.Generate();
        doc.Save(Path.Combine(outputDir, "commercial_invoice.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}