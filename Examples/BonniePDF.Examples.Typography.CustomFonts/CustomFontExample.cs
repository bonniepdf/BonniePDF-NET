using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Typography.CustomFonts;

/// <summary>
/// Demonstrates TrueType font embedding with full Unicode support
/// </summary>
[System.ComponentModel.DisplayName("Custom Fonts")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("Embedding and using custom TTF fonts.")]
public static class CustomFontExample
{
    public static Document Generate()
    {
        // Embed the bundled Noto fonts (see BundledFont): NotoSans for text, with
        // arrows/maths routed through NotoSansMath and dingbats through NotoSansSymbols2.
        string fontPath = BundledFont.SansPath;

        Document doc = new();

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily(fontPath).WithSize(11).WithColor(ColorPalette.Slate[600]);

        // Set metadata
        doc.Metadata.Title = "Embedded Font Example";
        doc.Metadata.Author = "BonniePDF";

        // Title using embedded font
        doc.Add(new TextBlock
        {
            Text = "TrueType Font Embedding Demo",
            FontName = fontPath,
            FontSize = 24,
            TextColor = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Subtitle
        doc.Add(new TextBlock
        {
            Text = "Full Unicode Support with Embedded Fonts",
            FontName = fontPath,
            FontSize = 14,
            TextColor = ColorPalette.Slate[600],
            Margin = new Thickness(0, 0, 0, 16)
        });

        // Unicode symbols section
        doc.Add(new TextBlock
        {
            Text = "Unicode Symbols:",
            FontName = fontPath,
            FontSize = 12,
            TextColor = ColorPalette.Slate[700],
            Margin = new Thickness(0, 0, 0, 6)
        });

        // Bullets and common symbols
        doc.Add(new TextBlock { Text = "\u2022 Bullet point (U+2022)" });
        doc.Add(new TextBlock { Text = "\u00A9 Copyright symbol (U+00A9)" });
        doc.Add(new TextBlock { Text = "\u00AE Registered trademark (U+00AE)" });
        doc.Add(new TextBlock { Text = "\u2122 Trademark (U+2122)" });
        doc.Add(new TextBlock { Text = "\u2013 En-dash (U+2013) \u2014 Em-dash (U+2014)" });
        // Arrows (U+2190..) live in NotoSansMath, not NotoSans.
        doc.Add(new TextBlock { Text = "\u2190 \u2191 \u2192 \u2193 Arrows", FontName = BundledFont.MathPath });

        // The check mark (U+2713) is a dingbat - NotoSansSymbols2.
        doc.Add(new TextBlock { Text = "\u2713 Check mark (U+2713)", FontName = BundledFont.SymbolsPath, Margin = new Thickness(0, 0, 0, 12) });

        // Currency symbols
        doc.Add(new TextBlock
        {
            Text = "Currency Symbols:",
            FontName = fontPath,
            FontSize = 12,
            TextColor = ColorPalette.Slate[700],
            Margin = new Thickness(0, 0, 0, 6)
        });

        doc.Add(new TextBlock { Text = "\u20AC Euro \u00A3 Pound \u00A5 Yen \u20B9 Rupee", Margin = new Thickness(0, 0, 0, 12) });

        // Math symbols
        doc.Add(new TextBlock
        {
            Text = "Mathematical Symbols:",
            FontName = fontPath,
            FontSize = 12,
            TextColor = ColorPalette.Slate[700],
            Margin = new Thickness(0, 0, 0, 6)
        });

        // Mathematical operators (U+2200..) live in NotoSansMath.
        doc.Add(new TextBlock { Text = "\u00B1 Plus-minus \u00D7 Multiply \u00F7 Divide \u2260 Not equal", FontName = BundledFont.MathPath });
        doc.Add(new TextBlock { Text = "\u2264 Less-equal \u2265 Greater-equal \u221E Infinity", FontName = BundledFont.MathPath, Margin = new Thickness(0, 0, 0, 12) });

        // Greek letters
        doc.Add(new TextBlock
        {
            Text = "Greek Letters:",
            FontName = fontPath,
            FontSize = 12,
            TextColor = ColorPalette.Slate[700],
            Margin = new Thickness(0, 0, 0, 6)
        });

        doc.Add(new TextBlock { Text = "\u03B1 Alpha \u03B2 Beta \u03B3 Gamma \u03B4 Delta \u03C0 Pi \u03A9 Omega", Margin = new Thickness(0, 0, 0, 16) });

        // Mixed fonts demonstration
        doc.Add(new TextBlock
        {
            Text = "Mixed Font Demonstration:",
            FontName = fontPath,
            FontSize = 12,
            TextColor = ColorPalette.Slate[700],
            Margin = new Thickness(0, 0, 0, 6)
        });

        // Standard font (Helvetica)
        doc.Add(new TextBlock
        {
            Text = "This line uses standard Helvetica (Type1, not embedded)",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = ColorPalette.Slate[600]
        });

        // Embedded font
        doc.Add(new TextBlock
        {
            Text = "This line uses embedded Noto Sans (TrueType, full Unicode)",
            Margin = new Thickness(0, 0, 0, 16)
        });

        // Footer
        doc.Add(new TextBlock
        {
            Text = $"\u00A9 {DateTime.Now.Year} BonniePDF \u2022 TrueType Font Embedding",
            FontSize = 10,
            TextColor = ColorPalette.Slate[400]
        });
        return doc;
    }
}

