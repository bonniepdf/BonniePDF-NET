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
public static class EmbeddedFontExample
{
    
    public static Document Generate()
    {
        // Use Arial as the embedded font (available on Windows)
        string arialPath = @"C:\Windows\Fonts\arial.ttf";

        // Check if Arial exists, fall back to another common font
        if (!File.Exists(arialPath))
        {
            Console.WriteLine("Arial not found, trying Segoe UI...");
            arialPath = @"C:\Windows\Fonts\segoeui.ttf";
        }

        if (!File.Exists(arialPath))
        {
            Console.WriteLine("No suitable TTF font found. Skipping embedded font example.");
            return null;
        }

        Document doc = new();

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        // Set metadata
        doc.Metadata.Title = "Embedded Font Example";
        doc.Metadata.Author = "BonniePDF";

        // Title using embedded font
        doc.Add(new TextBlock
        {
            Text = "TrueType Font Embedding Demo",
            FontName = arialPath,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Subtitle
        doc.Add(new TextBlock
        {
            Text = "Full Unicode Support with Embedded Fonts",
            FontName = arialPath,
            FontSize = 14,
            TextColor = Color.Slate600,
            Margin = new Thickness(0, 0, 0, 16)
        });

        // Unicode symbols section
        doc.Add(new TextBlock
        {
            Text = "Unicode Symbols:",
            FontName = arialPath,
            FontSize = 12,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 6)
        });

        // Bullets and common symbols
        doc.Add(new TextBlock
        {
            Text = "\u2022 Bullet point (U+2022)",
            FontName = arialPath,
            FontSize = 11,
            TextColor = Color.Slate600
        });

        doc.Add(new TextBlock
        {
            Text = "\u00A9 Copyright symbol (U+00A9)",
            FontName = arialPath,
            FontSize = 11,
            TextColor = Color.Slate600
        });

        doc.Add(new TextBlock
        {
            Text = "\u00AE Registered trademark (U+00AE)",
            FontName = arialPath,
            FontSize = 11,
            TextColor = Color.Slate600
        });

        doc.Add(new TextBlock
        {
            Text = "\u2122 Trademark (U+2122)",
            FontName = arialPath,
            FontSize = 11,
            TextColor = Color.Slate600
        });

        doc.Add(new TextBlock
        {
            Text = "\u2013 En-dash (U+2013) \u2014 Em-dash (U+2014)",
            FontName = arialPath,
            FontSize = 11,
            TextColor = Color.Slate600
        });

        doc.Add(new TextBlock
        {
            Text = "\u2190 \u2191 \u2192 \u2193 Arrows",
            FontName = arialPath,
            FontSize = 11,
            TextColor = Color.Slate600
        });

        doc.Add(new TextBlock
        {
            Text = "\u2713 Check mark (U+2713)",
            FontName = arialPath,
            FontSize = 11,
            TextColor = Color.Slate600,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Currency symbols
        doc.Add(new TextBlock
        {
            Text = "Currency Symbols:",
            FontName = arialPath,
            FontSize = 12,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 6)
        });

        doc.Add(new TextBlock
        {
            Text = "\u20AC Euro \u00A3 Pound \u00A5 Yen \u20B9 Rupee",
            FontName = arialPath,
            FontSize = 11,
            TextColor = Color.Slate600,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Math symbols
        doc.Add(new TextBlock
        {
            Text = "Mathematical Symbols:",
            FontName = arialPath,
            FontSize = 12,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 6)
        });

        doc.Add(new TextBlock
        {
            Text = "\u00B1 Plus-minus \u00D7 Multiply \u00F7 Divide \u2260 Not equal",
            FontName = arialPath,
            FontSize = 11,
            TextColor = Color.Slate600
        });

        doc.Add(new TextBlock
        {
            Text = "\u2264 Less-equal \u2265 Greater-equal \u221E Infinity",
            FontName = arialPath,
            FontSize = 11,
            TextColor = Color.Slate600,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Greek letters
        doc.Add(new TextBlock
        {
            Text = "Greek Letters:",
            FontName = arialPath,
            FontSize = 12,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 6)
        });

        doc.Add(new TextBlock
        {
            Text = "\u03B1 Alpha \u03B2 Beta \u03B3 Gamma \u03B4 Delta \u03C0 Pi \u03A9 Omega",
            FontName = arialPath,
            FontSize = 11,
            TextColor = Color.Slate600,
            Margin = new Thickness(0, 0, 0, 16)
        });

        // Mixed fonts demonstration
        doc.Add(new TextBlock
        {
            Text = "Mixed Font Demonstration:",
            FontName = arialPath,
            FontSize = 12,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 6)
        });

        // Standard font (Helvetica)
        doc.Add(new TextBlock
        {
            Text = "This line uses standard Helvetica (Type1, not embedded)",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });

        // Embedded font
        doc.Add(new TextBlock
        {
            Text = "This line uses embedded Arial (TrueType, full Unicode)",
            FontName = arialPath,
            FontSize = 11,
            TextColor = Color.Slate600,
            Margin = new Thickness(0, 0, 0, 16)
        });

        // Footer
        doc.Add(new TextBlock
        {
            Text = $"\u00A9 {DateTime.Now.Year} BonniePDF \u2022 TrueType Font Embedding",
            FontName = arialPath,
            FontSize = 10,
            TextColor = Color.Slate400
        });
        return doc;
    }
}

