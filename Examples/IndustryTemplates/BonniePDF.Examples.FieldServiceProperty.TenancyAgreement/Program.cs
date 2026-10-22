namespace BonniePDF.Examples.FieldServiceProperty.TenancyAgreement;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Tenancy Agreement...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        using var doc = TenancyAgreementExample.Generate();
        doc.Save(Path.Combine(outputDir, "tenancy_agreement.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}