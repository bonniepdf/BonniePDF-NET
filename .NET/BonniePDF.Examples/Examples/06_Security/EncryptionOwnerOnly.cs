using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Examples.Core;

namespace BonniePDF.Examples.Examples._06_Security;

/// <summary>
/// Example demonstrating PDF encryption that opens without a password but has restricted actions.
/// Only the owner password can unlock full permissions.
/// </summary>
public class EncryptionOwnerOnlyExample : ExampleBase
{
    public override string Id => "encryption-owner-only";
    public override string Title => "Encryption - Owner Password Only";
    public override string Description => "Generates a PDF that opens without a password but has restricted actions. Only the owner password can unlock full permissions.";
    public override string FileName => "encryption_owner_only.pdf";
    public override ExampleCategory Category => ExampleCategory.Security;
    public override string Password => "secret";

    public override string Generate(string outputPath)
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
