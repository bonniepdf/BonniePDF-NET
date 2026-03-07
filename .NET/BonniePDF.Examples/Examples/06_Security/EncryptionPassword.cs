using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Examples.Core;

namespace BonniePDF.Examples.Examples._06_Security;

/// <summary>
/// Example demonstrating PDF encryption with password protection (128-bit RC4 encryption).
/// The document requires a password to open.
/// </summary>
public class EncryptionPasswordExample : ExampleBase
{
    public override string Id => "encryption-password";
    public override string Title => "Encryption - Password Protected";
    public override string Description => "Generates a password-protected PDF (128-bit RC4 encryption) that requires a password to open.";
    public override string FileName => "encryption_password.pdf";
    public override ExampleCategory Category => ExampleCategory.Security;
    public override string Password => "demo123";

    public override string Generate(string outputPath)
    {
        DocumentSettings settings = new();
        settings.WithPassword("demo123");

        using Document doc = new(settings);

        doc.Add(new TextBlock
        {
            Text = "Password Protected Document",
            FontSize = 24,
            TextColor = Color.Blue700
        });

        doc.Add(new TextBlock
        {
            Text = "This PDF is encrypted with 128-bit RC4 encryption.",
            Margin = new Thickness(0, 20, 0, 10)
        });

        doc.Add(new TextBlock
        {
            Text = "Password: demo123",
            FontSize = 14,
            TextColor = Color.Red600
        });

        doc.Add(new TextBlock
        {
            Text = "Without the correct password, this document cannot be opened.",
            Margin = new Thickness(0, 20, 0, 0)
        });

        doc.Save(outputPath);
        return outputPath;
    }
}
