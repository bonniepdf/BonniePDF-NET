namespace BonniePDF.Examples.Templates.FinancialDashboard;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Financial Dashboard...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        using var doc = FinancialDashboardExample.Generate();
        doc.Save(Path.Combine(outputDir, "financial_dashboard.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}