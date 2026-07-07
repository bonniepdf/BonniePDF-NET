namespace BonniePDF.Examples.Templates.UkDegreeTranscript;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating UK Degree Transcript...");
        using (var doc = UkDegreeTranscriptExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "uk_degree_transcript.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
