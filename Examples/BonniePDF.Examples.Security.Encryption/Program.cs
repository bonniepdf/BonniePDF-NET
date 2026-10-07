namespace BonniePDF.Examples.Security.Encryption;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Encryption - Owner Password Only...");
        using (var doc = EncryptionOwnerOnlyExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "encryption_owner_only.pdf"));
        }

        Console.WriteLine("Generating Encryption - Password Protected...");
        using (var doc = EncryptionPasswordExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "encryption_password.pdf"));
        }

        Console.WriteLine("Generating Encryption - Permission Restrictions...");
        using (var doc = EncryptionPermissionsExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "encryption_permissions.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}