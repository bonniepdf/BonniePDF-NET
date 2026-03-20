namespace BonniePDF.Examples.Templates.BusinessInvoice;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Business Invoice...");
        using (var doc = Invoice.Generate())
        {
            doc.Save(Path.Combine(outputDir, "invoice.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
