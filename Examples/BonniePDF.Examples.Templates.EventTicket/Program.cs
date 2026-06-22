namespace BonniePDF.Examples.Templates.EventTicket;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Event Ticket...");
        using (var doc = EventTicketExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "event_ticket_tagged.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
