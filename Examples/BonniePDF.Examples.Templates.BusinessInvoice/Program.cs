namespace BonniePDF.Examples.Templates.BusinessInvoice;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Business Invoice...");
        using (var doc = BusinessInvoiceExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "invoice_tagged.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
