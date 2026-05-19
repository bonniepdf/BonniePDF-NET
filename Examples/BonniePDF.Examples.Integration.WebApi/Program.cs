namespace BonniePDF.Examples.Integration.WebApi;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Web API - Basic...");
        using (var doc = WebApiBasicExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "webapi_basic.pdf"));
        }
        Console.WriteLine("Generating Web API - Invoice...");
        using (var doc = WebApiInvoiceExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "webapi_invoice.pdf"));
        }
        Console.WriteLine("Generating Web API - Streaming...");
        using (var doc = WebApiStreamingExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "webapi_streaming.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
