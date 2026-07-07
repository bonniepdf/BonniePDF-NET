namespace BonniePDF.Examples.Templates.RegionalInvoices;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating regional invoice examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        (string File, Func<BonniePDF.Document> Generate)[] examples =
        [
            ("us-sales-invoice.pdf", UsSalesInvoiceExample.Generate),
            ("uk-vat-invoice.pdf", UkVatInvoiceExample.Generate),
            ("india-gst-invoice.pdf", IndiaGstInvoiceExample.Generate),
        ];

        foreach ((string file, Func<BonniePDF.Document> generate) in examples)
        {
            Console.WriteLine($"Generating {file}...");
            using BonniePDF.Document doc = generate();
            doc.Save(Path.Combine(outputDir, file));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
