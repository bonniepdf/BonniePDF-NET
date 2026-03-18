using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Demo;

/// <summary>
/// Demonstrates the RichText element with mixed styling within flowing paragraphs.
/// Shows bold, italic, colored, underlined, and multi-size text all within a single
/// paragraph that word-wraps correctly across span boundaries.
/// </summary>
public static class RichTextExample
{
    
    public static string Generate(string outputPath)
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 60
        });

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "Rich Text";
        doc.Metadata.Author = "BonniePDF";

        // Title
        doc.Add(new TextBlock
        {
            Text = "Rich Text (Mixed Styling)",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // --- Section 1: Bold words in a paragraph ---
        doc.Add(CreateSectionHeader("Bold Words in a Paragraph"));

        doc.Add(new RichText
            {
                Margin = new Thickness(0, 0, 0, 16)
            }
            .Span("This paragraph contains ")
            .Span("bold text", new TextStyle().Bold())
            .Span(" mixed with regular text. The ")
            .Span("bold words", new TextStyle().Bold())
            .Span(" stand out while the text flows naturally as a single paragraph with automatic word wrapping."));

        // --- Section 2: Italic text ---
        doc.Add(CreateSectionHeader("Italic Text"));

        doc.Add(new RichText
            {
                Margin = new Thickness(0, 0, 0, 16)
            }
            .Span("Scientific names are typically written in ")
            .Span("italic", new TextStyle().WithItalic())
            .Span(", such as ")
            .Span("Homo sapiens", new TextStyle().WithItalic())
            .Span(" or ")
            .Span("Escherichia coli", new TextStyle().WithItalic())
            .Span(". This convention helps distinguish taxonomic nomenclature from surrounding text."));

        // --- Section 3: Bold + Italic combined ---
        doc.Add(CreateSectionHeader("Bold and Italic Combined"));

        doc.Add(new RichText
            {
                Margin = new Thickness(0, 0, 0, 16)
            }
            .Span("You can combine ")
            .Span("bold", new TextStyle().Bold())
            .Span(", ")
            .Span("italic", new TextStyle().WithItalic())
            .Span(", and ")
            .Span("bold italic", new TextStyle { Weight = FontWeight.Bold, Italic = true })
            .Span(" styles within a single flowing paragraph."));

        // --- Section 4: Colored spans ---
        doc.Add(CreateSectionHeader("Colored Spans"));

        doc.Add(new RichText
            {
                Margin = new Thickness(0, 0, 0, 16)
            }
            .Span("Text can be ")
            .Span("red", new TextStyle().WithColor(Color.Red))
            .Span(", ")
            .Span("blue", new TextStyle().WithColor(Color.Blue600))
            .Span(", ")
            .Span("green", new TextStyle().WithColor(Color.Green600))
            .Span(", or ")
            .Span("any color", new TextStyle().WithColor(Color.Purple600))
            .Span(" you choose. Colors can be combined with ")
            .Span("bold colored text", new TextStyle().Bold().WithColor(Color.Indigo600))
            .Span(" for maximum emphasis."));

        // --- Section 5: Text decorations ---
        doc.Add(CreateSectionHeader("Inline Decorations"));

        doc.Add(new RichText
            {
                Margin = new Thickness(0, 0, 0, 16)
            }
            .Span("Decorations work per-span: ")
            .Span("underlined words", new TextStyle().Underline())
            .Span(" and ")
            .Span("strikethrough words", new TextStyle().Strikethrough())
            .Span(" can appear anywhere in the paragraph, including ")
            .Span("bold underlined", new TextStyle().Bold().Underline())
            .Span(" combinations."));

        // --- Section 6: Mixed font sizes ---
        doc.Add(CreateSectionHeader("Mixed Font Sizes"));

        doc.Add(new RichText
            {
                Margin = new Thickness(0, 0, 0, 16)
            }
            .Span("Normal text can include ")
            .Span("larger text", new TextStyle().WithSize(18))
            .Span(" and ")
            .Span("smaller text", new TextStyle().WithSize(8))
            .Span(" within the same paragraph. The line height adjusts to accommodate the tallest text on each line."));

        // --- Section 7: Background highlights ---
        doc.Add(CreateSectionHeader("Background Highlights"));

        doc.Add(new RichText
            {
                Margin = new Thickness(0, 0, 0, 16)
            }
            .Span("You can ")
            .Span("highlight important text", new TextStyle().WithBackground(Color.FromHex("#fef08a")))
            .Span(" with background colors, similar to a ")
            .Span("marker pen", new TextStyle().WithBackground(Color.FromHex("#bbf7d0")).Bold())
            .Span(" effect. Great for ")
            .Span("drawing attention", new TextStyle().WithBackground(Color.FromHex("#fecaca")).WithColor(Color.FromHex("#991b1b")))
            .Span(" to key information."));

        // --- Section 8: Reusable styles ---
        doc.Add(CreateSectionHeader("Reusable TextStyle Objects"));

        TextStyle label = new() { FontFamily = "Helvetica", Weight = FontWeight.Bold, Color = Color.Slate800 };
        TextStyle value = new() { FontFamily = "Helvetica", Color = Color.Blue600 };
        TextStyle unit = new() { FontFamily = "Helvetica", FontSize = 9, Color = Color.Slate500 };

        doc.Add(new RichText { Margin = new Thickness(0, 0, 0, 4) }
            .Span("Temperature: ", label)
            .Span("23.5", value)
            .Span(" degrees Celsius", unit));

        doc.Add(new RichText { Margin = new Thickness(0, 0, 0, 4) }
            .Span("Pressure: ", label)
            .Span("1013.25", value)
            .Span(" hPa", unit));

        doc.Add(new RichText { Margin = new Thickness(0, 0, 0, 16) }
            .Span("Humidity: ", label)
            .Span("65", value)
            .Span(" %", unit));

        // --- Section 9: Font weight variants ---
        doc.Add(CreateSectionHeader("Font Weight Variants"));

        doc.Add(new RichText
            {
                Margin = new Thickness(0, 0, 0, 16)
            }
            .Span("Normal (400) ", new TextStyle { Weight = FontWeight.Normal })
            .Span("Bold (700) ", new TextStyle { Weight = FontWeight.Bold })
            .Span("Normal again. ", new TextStyle { Weight = FontWeight.Normal })
            .Span("Standard PDF fonts support Normal and Bold weights. Custom TTF fonts can support the full 100-900 range."));

        // --- Section 10: Justified rich text ---
        doc.Add(CreateSectionHeader("Justified Alignment"));

        doc.Add(new RichText
            {
                Alignment = TextAlignment.Justify,
                Margin = new Thickness(0, 0, 0, 16)
            }
            .Span("Rich text supports ")
            .Span("full justification", new TextStyle().Bold())
            .Span(" where the text is evenly distributed across the line width. This works with ")
            .Span("mixed styles", new TextStyle().WithItalic())
            .Span(" — each span maintains its own formatting while the spacing between words is adjusted to fill the available width. The last line remains left-aligned as per standard typographic convention."));

        // --- Section 11: Practical example — legal disclaimer ---
        doc.Add(new Line { Color = Color.Slate300, Thickness = 1, Margin = new Thickness(0, 12, 0, 0) });
        doc.Add(CreateSectionHeader("Practical Example: Legal Disclaimer"));

        doc.Add(new RichText
            {
                DefaultStyle = new TextStyle { FontFamily = "Helvetica", FontSize = 10, Color = Color.Slate600 },
                Alignment = TextAlignment.Justify,
                Margin = new Thickness(0, 0, 0, 16)
            }
            .Span("IMPORTANT: ", new TextStyle().Bold().WithColor(Color.Red))
            .Span("By using this software, you agree to the ")
            .Span("Terms of Service", new TextStyle().Bold().Underline())
            .Span(" and ")
            .Span("Privacy Policy", new TextStyle().Bold().Underline())
            .Span(". All data is processed in accordance with ")
            .Span("GDPR", new TextStyle().Bold())
            .Span(" regulations. For questions, contact ")
            .Span("support@example.com", new TextStyle().WithColor(Color.Blue600))
            .Span("."));

        // --- Section 12: Callback-based styling ---
        doc.Add(CreateSectionHeader("Callback-based Styling"));

        doc.Add(new RichText
            {
                Margin = new Thickness(0, 0, 0, 16)
            }
            .Span("The callback API lets you configure styles inline: ")
            .Span("bold blue", s => s.Bold().WithColor(Color.Blue600))
            .Span(", ")
            .Span("italic green", s => s.WithItalic().WithColor(Color.Green600))
            .Span(", ")
            .Span("underlined red", s => s.Underline().WithColor(Color.Red))
            .Span(". Useful for one-off styles."));

        doc.Save(outputPath);
        return outputPath;
    }

    private static TextBlock CreateSectionHeader(string title)
    {
        return new TextBlock
        {
            Text = title,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Indigo600,
            Margin = new Thickness(0, 12, 0, 6)
        };
    }
}
