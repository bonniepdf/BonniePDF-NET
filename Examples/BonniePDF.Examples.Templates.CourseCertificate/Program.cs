namespace BonniePDF.Examples.Templates.CourseCertificate;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Course Certificate...");
        using (var doc = CourseCertificateExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "course-certificate.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}