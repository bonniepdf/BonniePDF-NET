namespace BonniePDF.Examples.Templates.PolicySchedule;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Policy Schedule...");
        using (var doc = PolicyScheduleExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "policy-schedule.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}