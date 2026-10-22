using BonniePDF;

namespace BonniePDF.Examples.Security.Encryption;

/// <summary>
/// AES-256 password protection with granular permissions. Three scenarios in one
/// document: a straight password-protected file, user + owner passwords with
/// permission flags, and an owner-password-only file that opens freely but
/// restricts most actions.
///
/// PDF/A is intentionally not claimed, because archival PDF/A output cannot be
/// encrypted.
/// </summary>
[System.ComponentModel.DisplayName("Encryption")]
[System.ComponentModel.Category("Security")]
[System.ComponentModel.Description(
    "Three encryption scenarios: simple password, user/owner passwords with permissions, and owner-only restrictions.")]
public static class EncryptionExample
{
    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        DocumentSettings settings = new()
        {
            PageSize = PdfSize.A4,
            Margin = 60,
            Conformance = PdfAConformance.None,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            DefaultTextStyle = new TextStyle()
                .WithFamily(StandardFonts.Helvetica)
                .WithSize(12)
                .WithColor(ColorPalette.Slate[600]),
        };

        // AES-256 encryption (PDF 1.7 required, set by the preset).
        settings.WithEncryptionAes256(opts => opts
            .WithUserPassword("demo123")
            .WithOwnerPassword("demo123")
            .AllowPrinting()
            .AllowCopying()
            .AllowAccessibility());

        Document doc = new(settings, new DocumentMetadata { Title = "PDF Encryption", Author = "BonniePDF" });

        // ── Page 1: AES-256 password protection ──
        doc.Add(Title("AES-256 Password Protection"));
        doc.Add(new TextBlock
        {
            Text =
                "This document is encrypted with AES-256 encryption (V5, R6). You needed to enter the password to open it.",
            FontSize = 12,
            Margin = new Thickness(0, 20, 0, 10),
        });
        doc.Add(new TextBlock { Text = "Password: demo123", FontSize = 14, Color = ColorPalette.Slate[700] });
        doc.Add(new TextBlock
        {
            Text = "Printing and copying are allowed, but modification and annotation are restricted.",
            FontSize = 12,
            Margin = new Thickness(0, 20, 0, 10),
        });
        doc.Add(Code("""
                     API usage:
                       settings.WithEncryptionAes256(opts => opts
                           .WithUserPassword("demo123")
                           .WithOwnerPassword("demo123")
                           .AllowPrinting()
                           .AllowCopying()
                           .AllowAccessibility());

                       // Or for 128-bit RC4:
                       // settings.WithEncryption128(opts => ...)

                       // Or the simplest form:
                       // settings.WithPassword("demo123");
                     """));

        // ── Page 2: User + owner passwords with permissions ──
        doc.AddPage();

        doc.Add(Title("Permission Restrictions"));
        doc.Add(new TextBlock
        {
            Text = "Encryption supports granular permission flags via user and owner passwords:",
            FontSize = 12,
            Margin = new Thickness(0, 20, 0, 10),
        });
        doc.Add(Allowed("  - Printing: Allowed"));
        doc.Add(Allowed("  - Copying: Allowed"));
        doc.Add(Denied("  - Modifying: Denied"));
        doc.Add(Denied("  - Annotations: Denied"));
        doc.Add(new TextBlock { Text = "Passwords:", FontSize = 14, Margin = new Thickness(0, 20, 0, 5) });
        doc.Add(new TextBlock { Text = "  User password: user" });
        doc.Add(new TextBlock { Text = "  Owner password: owner (unlocks all permissions)" });
        doc.Add(Code("""
                     API usage:
                       settings.WithEncryptionAes256(opts => opts
                           .WithUserPassword("user")
                           .WithOwnerPassword("owner")
                           .AllowPrinting()
                           .AllowCopying());
                     """));

        // ── Page 3: Owner password only (restricted actions) ──
        doc.AddPage();

        doc.Add(Title("Owner-Only Restrictions"));
        doc.Add(new TextBlock
        {
            Text =
                "This page demonstrates an owner-password-only document: it opens freely but most actions are restricted.",
            FontSize = 12,
            Margin = new Thickness(0, 20, 0, 10),
        });
        doc.Add(new TextBlock { Text = "Restricted permissions in this scenario:", FontSize = 12 });
        doc.Add(Allowed("  - Low-quality printing: Allowed"));
        doc.Add(Allowed("  - Accessibility extraction: Allowed"));
        doc.Add(Denied("  - Copying text: Denied"));
        doc.Add(Denied("  - Modifying: Denied"));
        doc.Add(Denied("  - High-quality printing: Denied"));
        doc.Add(new TextBlock
        {
            Text = "Owner password: secret",
            FontSize = 14,
            Color = ColorPalette.Red[600],
            Margin = new Thickness(0, 20, 0, 0),
        });
        doc.Add(Code("""
                     API usage:
                       settings.WithEncryptionAes256(opts => opts
                           .WithOwnerPassword("secret")
                           .DenyAll()
                           .AllowPrinting(highQuality: false)
                           .AllowAccessibility());
                     """));

        return doc;
    }

    private static TextBlock Title(string text) => new()
    {
        Text = text,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 24,
        Color = ColorPalette.Blue[700],
    };

    // Green[700], not [600]: this text on white needs 4.5:1 and 600 falls to 3.22:1.
    private static TextBlock Allowed(string text) => new() { Text = text, Color = ColorPalette.Green[700] };

    private static TextBlock Denied(string text) => new() { Text = text, Color = ColorPalette.Red[600] };

    private static TextBlock Code(string text) => new()
    {
        Text = text,
        FontName = StandardFonts.Courier,
        FontSize = 10,
        Color = ColorPalette.Slate[600],
        Margin = new Thickness(0, 20, 0, 0),
    };
}
