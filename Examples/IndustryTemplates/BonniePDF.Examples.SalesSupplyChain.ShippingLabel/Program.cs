namespace BonniePDF.Examples.SalesSupplyChain.ShippingLabel;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Shipping Label...");
        using (var doc = ShippingLabelExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "shipping_label_tagged.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}