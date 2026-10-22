namespace BonniePDF.Examples.PeopleHr.EmploymentContract;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Employment Contract...");
        using (var doc = EmploymentContractExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "employment-contract.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}