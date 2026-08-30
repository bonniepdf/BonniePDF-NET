namespace BonniePDF.Examples.Templates.UtilityBill;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Utility Bill...");
        using (var doc = UtilityBillExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "utility-bill.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
