namespace BonniePDF.Examples.Integration.WebApi;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Web API - Basic...");
        WebApiBasicExample.Generate(Path.Combine(outputDir, "webapi_basic.pdf"));
        Console.WriteLine("Generating Web API - Invoice...");
        WebApiInvoiceExample.Generate(Path.Combine(outputDir, "webapi_invoice.pdf"));
        Console.WriteLine("Generating Web API - Streaming...");
        WebApiStreamingExample.Generate(Path.Combine(outputDir, "webapi_streaming.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}