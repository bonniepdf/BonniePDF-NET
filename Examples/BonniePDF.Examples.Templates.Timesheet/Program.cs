namespace BonniePDF.Examples.Templates.Timesheet;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating timesheet...");
        using (var doc = TimesheetExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "timesheet.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}