namespace BonniePDF.Examples.Templates.ComplianceFindingsRegister;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Compliance Findings Register...");
        using (var doc = ComplianceFindingsRegisterExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "compliance_findings_register.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
