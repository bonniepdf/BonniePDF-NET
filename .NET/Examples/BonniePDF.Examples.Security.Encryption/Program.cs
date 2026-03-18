using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Encryption - Owner Password Only...");
                    BonniePDF.Examples.Examples._06_Security.EncryptionOwnerOnlyExample.Generate(Path.Combine(outputDir, "encryption_owner_only.pdf"));
        Console.WriteLine("Generating Encryption - Password Protected...");
                    BonniePDF.Examples.Examples._06_Security.EncryptionPasswordExample.Generate(Path.Combine(outputDir, "encryption_password.pdf"));
        Console.WriteLine("Generating Encryption - Permission Restrictions...");
                    BonniePDF.Examples.Examples._06_Security.EncryptionPermissionsExample.Generate(Path.Combine(outputDir, "encryption_permissions.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
