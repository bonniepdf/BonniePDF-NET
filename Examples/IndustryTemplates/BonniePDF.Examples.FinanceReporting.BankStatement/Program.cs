namespace BonniePDF.Examples.FinanceReporting.BankStatement;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Bank Statement...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        using var doc = BankStatementExample.Generate();
        doc.Save(Path.Combine(outputDir, "bank_statement.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}