using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;

namespace BonniePDF.Examples.Security.Encryption;

/// <summary>
/// Example demonstrating PDF encryption that opens without a password but has restricted actions.
/// Only the owner password can unlock full permissions.
/// </summary>
[System.ComponentModel.DisplayName("Encryption - Owner Password Only")]
[System.ComponentModel.Category("Security")]
[System.ComponentModel.Description("Generates a PDF that opens without a password but has restricted actions. Only the owner password can unlock full permissions.")]
public static class EncryptionOwnerOnlyExample
{
    public static Document Generate()
    {
        DocumentSettings settings = new() { Language = "en-GB" };
        settings.WithEncryption128(options => options
            .WithOwnerPassword("secret")
            .DenyAll()
            .AllowPrinting(false)
            .AllowAccessibility()
        );

        Document doc = new(settings);
        doc.Metadata.Title = "Owner-Password-Only Encryption Example";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[600]);

        doc.Add(new TextBlock
        {
            Text = "Open Without Password (Restricted)",
            FontSize = 24,
            Color = ColorPalette.Blue[700]
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

        doc.Add(new TextBlock { Text = "  - Low-quality printing: Allowed", Color = ColorPalette.Green[700] });
        doc.Add(new TextBlock { Text = "  - Accessibility extraction: Allowed", Color = ColorPalette.Green[700] });
        doc.Add(new TextBlock { Text = "  - Copying text: Denied", Color = ColorPalette.Red[600] });
        doc.Add(new TextBlock { Text = "  - Modifying: Denied", Color = ColorPalette.Red[600] });
        doc.Add(new TextBlock { Text = "  - High-quality printing: Denied", Color = ColorPalette.Red[600] });

        doc.Add(new TextBlock
        {
            Text = "Owner password: secret",
            FontSize = 14,
            Margin = new Thickness(0, 20, 0, 0),
            Color = ColorPalette.Red[600]
        });
        return doc;
    }
}

