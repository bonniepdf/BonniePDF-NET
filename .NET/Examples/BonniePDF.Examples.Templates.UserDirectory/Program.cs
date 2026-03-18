namespace BonniePDF.Examples.Templates.UserDirectory;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating User Directory...");
        UsersTableExample.Generate(Path.Combine(outputDir, "users_list.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}