namespace BonniePDF.Examples.Templates.Payslip;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Payslip...");
        using (var doc = PayslipExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "payslip.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}