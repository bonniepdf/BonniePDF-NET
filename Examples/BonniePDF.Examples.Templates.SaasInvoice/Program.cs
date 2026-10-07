namespace BonniePDF.Examples.Templates.SaasInvoice;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Accessible SaaS Invoice...");
        using (var doc = SaasInvoiceExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "saas_invoice_tagged.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}