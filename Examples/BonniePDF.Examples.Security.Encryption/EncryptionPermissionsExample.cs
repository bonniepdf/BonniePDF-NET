using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Security.Permissions;

namespace BonniePDF.Examples.Security.Encryption;

/// <summary>
/// Example demonstrating PDF encryption with restricted permissions.
/// Users can open with user password but have limited actions.
/// Owner password unlocks all permissions.
/// </summary>
[System.ComponentModel.DisplayName("Encryption - Permission Restrictions")]
[System.ComponentModel.Category("Security")]
[System.ComponentModel.Description("Generates a PDF with restricted permissions. Users can open with user password but have limited actions. Owner password unlocks all permissions.")]
public static class EncryptionPermissionsExample
{
    public static Document Generate()
    {
        DocumentSettings settings = new() { Language = "en-GB" };
        settings.WithEncryption128(options => options
            .WithUserPassword("user")
            .WithOwnerPassword("owner")
            .WithPermissions(PdfPermissions.Print | PdfPermissions.Copy)
        );

        Document doc = new(settings);
        doc.Metadata.Title = "Permission-Restricted Encryption Example";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[600]);

        doc.Add(new TextBlock
        {
            Text = "Document with Permission Restrictions",
            FontSize = 24,
            Color = ColorPalette.Blue[700]
        });

        doc.Add(new TextBlock
        {
            Text = "This document has restricted permissions:",
            Margin = new Thickness(0, 20, 0, 10)
        });

        doc.Add(new TextBlock { Text = "  - Printing: Allowed", Color = ColorPalette.Green[700] });
        doc.Add(new TextBlock { Text = "  - Copying: Allowed", Color = ColorPalette.Green[700] });
        doc.Add(new TextBlock { Text = "  - Modifying: Denied", Color = ColorPalette.Red[600] });
        doc.Add(new TextBlock { Text = "  - Annotations: Denied", Color = ColorPalette.Red[600] });

        doc.Add(new TextBlock
        {
            Text = "Passwords:",
            FontSize = 14,
            Margin = new Thickness(0, 20, 0, 5)
        });

        doc.Add(new TextBlock { Text = "  User password: user" });
        doc.Add(new TextBlock { Text = "  Owner password: owner (unlocks all permissions)" });
        return doc;
    }
}

