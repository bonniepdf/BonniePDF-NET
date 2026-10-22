namespace BonniePDF.Examples.ServerBatch.ServerEndpoint;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Server Endpoint...");
        using (var doc = ServerEndpointExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "server-endpoint.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}