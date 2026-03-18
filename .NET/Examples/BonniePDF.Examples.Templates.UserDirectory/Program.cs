using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating User Directory...");
                    BonniePDF.Examples.Examples._07_Templates.UsersTableExample.Generate(Path.Combine(outputDir, "users_list.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
