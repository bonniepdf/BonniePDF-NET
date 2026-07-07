namespace BonniePDF.Examples.Templates.FinancialStatement;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Financial Statement...");
        using (var doc = FinancialStatementExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "financial_statement.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
