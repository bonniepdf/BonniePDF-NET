namespace BonniePDF.Examples.PeopleHr.ExpenseReport;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Expense Report...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        using var doc = ExpenseReportExample.Generate();
        doc.Save(Path.Combine(outputDir, "expense_report.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}