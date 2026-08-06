namespace BonniePDF.Examples.Templates.OutpatientAppointmentLetter;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Outpatient Appointment...");
        using (var doc = OutpatientAppointmentLetterExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "outpatient_appointment_letter.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
