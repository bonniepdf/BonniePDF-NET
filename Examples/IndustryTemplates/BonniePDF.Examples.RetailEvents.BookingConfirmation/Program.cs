namespace BonniePDF.Examples.RetailEvents.BookingConfirmation;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Booking Confirmation...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        using var doc = BookingConfirmationExample.Generate();
        doc.Save(Path.Combine(outputDir, "booking_confirmation.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}