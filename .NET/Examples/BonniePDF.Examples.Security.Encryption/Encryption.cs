using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Security.Permissions;

namespace BonniePDF.Examples.Security.Encryption;

/// <summary>
/// Examples demonstrating PDF encryption with passwords and permission controls.
/// </summary>
public static class EncryptionExample
{
    /// <summary>
    /// Generates a password-protected PDF (128-bit RC4 encryption).
    /// The document requires a password to open.
    /// </summary>
    public static void GeneratePasswordProtected(string path)
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

        doc.Save(path);
    }

    /// <summary>
    /// Generates a PDF with restricted permissions.
    /// Users can open with user password but have limited actions.
    /// Owner password unlocks all permissions.
    /// </summary>
    public static void GenerateWithPermissions(string path)
    {
        DocumentSettings settings = new();
        settings.WithEncryption128(options => options
            .WithUserPassword("user")
            .WithOwnerPassword("owner")
            .WithPermissions(PdfPermissions.Print | PdfPermissions.Copy)
        );

        using Document doc = new(settings);

        doc.Add(new TextBlock
        {
            Text = "Document with Permission Restrictions",
            FontSize = 24,
            TextColor = Color.Blue700
        });

        doc.Add(new TextBlock
        {
            Text = "This document has restricted permissions:",
            Margin = new Thickness(0, 20, 0, 10)
        });

        doc.Add(new TextBlock { Text = "  - Printing: Allowed", TextColor = Color.Green600 });
        doc.Add(new TextBlock { Text = "  - Copying: Allowed", TextColor = Color.Green600 });
        doc.Add(new TextBlock { Text = "  - Modifying: Denied", TextColor = Color.Red600 });
        doc.Add(new TextBlock { Text = "  - Annotations: Denied", TextColor = Color.Red600 });

        doc.Add(new TextBlock
        {
            Text = "Passwords:",
            FontSize = 14,
            Margin = new Thickness(0, 20, 0, 5)
        });

        doc.Add(new TextBlock { Text = "  User password: user" });
        doc.Add(new TextBlock { Text = "  Owner password: owner (unlocks all permissions)" });

        doc.Save(path);
    }

    /// <summary>
    /// Generates a PDF that opens without a password but has restricted actions.
    /// Only the owner password can unlock full permissions.
    /// </summary>
    public static void GenerateOwnerPasswordOnly(string path)
    {
        DocumentSettings settings = new();
        settings.WithEncryption128(options => options
            .WithOwnerPassword("secret")
            .DenyAll()
            .AllowPrinting(false)
            .AllowAccessibility()
        );

        using Document doc = new(settings);

        doc.Add(new TextBlock
        {
            Text = "Open Without Password (Restricted)",
            FontSize = 24,
            TextColor = Color.Blue700
        });

        doc.Add(new TextBlock
        {
            Text = "This document opens without requiring a password.",
            Margin = new Thickness(0, 20, 0, 10)
        });

        doc.Add(new TextBlock
        {
            Text = "However, most actions are restricted:",
            Margin = new Thickness(0, 10, 0, 5)
        });

        doc.Add(new TextBlock { Text = "  - Low-quality printing: Allowed", TextColor = Color.Green600 });
        doc.Add(new TextBlock { Text = "  - Accessibility extraction: Allowed", TextColor = Color.Green600 });
        doc.Add(new TextBlock { Text = "  - Copying text: Denied", TextColor = Color.Red600 });
        doc.Add(new TextBlock { Text = "  - Modifying: Denied", TextColor = Color.Red600 });
        doc.Add(new TextBlock { Text = "  - High-quality printing: Denied", TextColor = Color.Red600 });

        doc.Add(new TextBlock
        {
            Text = "Owner password: secret",
            FontSize = 14,
            Margin = new Thickness(0, 20, 0, 0),
            TextColor = Color.Red600
        });

        doc.Save(path);
    }
}
