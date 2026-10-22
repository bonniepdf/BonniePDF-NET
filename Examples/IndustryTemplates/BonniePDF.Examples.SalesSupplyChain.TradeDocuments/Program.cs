namespace BonniePDF.Examples.SalesSupplyChain.TradeDocuments;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating trade document examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        (string File, Func<BonniePDF.Document> Generate)[] examples =
        [
            ("purchase-order.pdf", PurchaseOrderExample.Generate),
            ("delivery-note.pdf", DeliveryNoteExample.Generate),
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