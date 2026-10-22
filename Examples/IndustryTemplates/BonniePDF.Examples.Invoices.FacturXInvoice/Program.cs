namespace BonniePDF.Examples.Invoices.FacturXInvoice;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Factur-X (ZUGFeRD) e-invoice...");
        using (var doc = FacturXInvoiceExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "factur-x_invoice.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}