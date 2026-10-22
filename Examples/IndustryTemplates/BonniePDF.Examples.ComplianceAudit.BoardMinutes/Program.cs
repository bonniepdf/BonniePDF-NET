namespace BonniePDF.Examples.ComplianceAudit.BoardMinutes;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Board Minutes...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        using var doc = BoardMinutesExample.Generate();
        doc.Save(Path.Combine(outputDir, "board_minutes.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
