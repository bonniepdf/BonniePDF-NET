using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;

namespace BonniePDF.Examples.Security.Encryption;

/// <summary>
/// Example demonstrating PDF encryption with password protection (128-bit RC4 encryption).
/// The document requires a password to open.
/// </summary>
[System.ComponentModel.DisplayName("Encryption - Password Protected")]
[System.ComponentModel.Category("Security")]
[System.ComponentModel.Description("Generates a password-protected PDF (128-bit RC4 encryption) that requires a password to open.")]
public static class EncryptionPasswordExample
{
    public static Document Generate()
    {
        DocumentSettings settings = new() { Language = "en-GB" };
        settings.WithPassword("demo123");

        Document doc = new(settings);
        doc.Metadata.Title = "Password-Protected Encryption Example";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[600]);

        doc.Add(new TextBlock
        {
            Text = "Password Protected Document",
            FontSize = 24,
            TextColor = ColorPalette.Blue[700]
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
            TextColor = ColorPalette.Red[600]
        });

        doc.Add(new TextBlock
        {
            Text = "Without the correct password, this document cannot be opened.",
            Margin = new Thickness(0, 20, 0, 0)
        });
        return doc;
    }
}

