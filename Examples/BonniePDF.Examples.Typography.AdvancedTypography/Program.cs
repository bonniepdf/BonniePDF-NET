namespace BonniePDF.Examples.Typography.AdvancedTypography;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Advanced Typography...");
        using (var doc = AdvancedTypographyExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "advanced_typography_example.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}