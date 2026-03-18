using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Web API - Basic...");
                    BonniePDF.Examples.Examples._08_Integration.WebApiBasicExample.Generate(Path.Combine(outputDir, "webapi_basic.pdf"));
        Console.WriteLine("Generating Web API - Invoice...");
                    BonniePDF.Examples.Examples._08_Integration.WebApiInvoiceExample.Generate(Path.Combine(outputDir, "webapi_invoice.pdf"));
        Console.WriteLine("Generating Web API - Streaming...");
                    BonniePDF.Examples.Examples._08_Integration.WebApiStreamingExample.Generate(Path.Combine(outputDir, "webapi_streaming.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
