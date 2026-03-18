namespace BonniePDF.Examples.Security.Encryption;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Encryption - Owner Password Only...");
        EncryptionOwnerOnlyExample.Generate(Path.Combine(outputDir, "encryption_owner_only.pdf"));
        Console.WriteLine("Generating Encryption - Password Protected...");
        EncryptionPasswordExample.Generate(Path.Combine(outputDir, "encryption_password.pdf"));
        Console.WriteLine("Generating Encryption - Permission Restrictions...");
        EncryptionPermissionsExample.Generate(Path.Combine(outputDir, "encryption_permissions.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}