namespace BonniePDF.Examples.Templates.UserDirectory;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating User Directory...");
        using (var doc = UserDirectoryExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "user_directory.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
