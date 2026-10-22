namespace BonniePDF.Examples.EducationPublicSector.PublicLibraryMembershipLetter;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Public Library Membership...");
        using (var doc = PublicLibraryMembershipLetterExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "public_library_membership_letter.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}