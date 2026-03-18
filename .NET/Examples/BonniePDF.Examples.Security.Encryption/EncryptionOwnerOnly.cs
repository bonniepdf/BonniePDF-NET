using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;

namespace BonniePDF.Examples.Security.Encryption;

/// <summary>
/// Example demonstrating PDF encryption that opens without a password but has restricted actions.
/// Only the owner password can unlock full permissions.
/// </summary>
public static class EncryptionOwnerOnlyExample
{

    public static string Generate(string outputPath)
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

        doc.Save(outputPath);
        return outputPath;
    }
}
