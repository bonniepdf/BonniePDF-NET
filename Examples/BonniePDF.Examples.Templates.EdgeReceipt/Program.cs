namespace BonniePDF.Examples.Templates.EdgeReceipt;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Edge Receipt...");
        using (var doc = EdgeReceiptExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "edge_receipt.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
