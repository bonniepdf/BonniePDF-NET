namespace BonniePDF.Examples.Security.Encryption;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Encryption (AES-256: password, permissions and owner-only)...");
        using (var doc = EncryptionExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "encryption.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
