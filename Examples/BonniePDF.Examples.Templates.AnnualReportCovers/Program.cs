namespace BonniePDF.Examples.Templates.AnnualReportCovers;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Annual Report Cover - Gradient...");
        using (var doc = AnnualReportGradientCoverExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "annual_report_gradient_cover.pdf"));
        }

        Console.WriteLine("Generating Annual Report Cover - Masked Image...");
        using (var doc = AnnualReportMaskedCoverExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "annual_report_masked_cover.pdf"));
        }

        Console.WriteLine("Generating Annual Report Cover - Geometric...");
        using (var doc = AnnualReportGeometricCoverExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "annual_report_geometric_cover.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
