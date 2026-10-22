namespace BonniePDF.Examples.EducationPublicSector.GrantAwardLetter;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Grant Award Letter...");
        using (var doc = GrantAwardLetterExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "grant_award_letter.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}