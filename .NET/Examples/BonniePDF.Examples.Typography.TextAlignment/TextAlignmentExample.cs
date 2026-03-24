using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Typography.TextAlignment;

/// <summary>
/// Demonstrates justified text alignment where word spacing is distributed
/// evenly to fill the available width, similar to newspaper/book typesetting.
/// </summary>
public static class TextAlignmentExample
{
    public static Document Generate()
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 60
        });

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "Justified Text Alignment";
        doc.Metadata.Author = "BonniePDF";

        // Title
        doc.Add(new TextBlock
        {
            Text = "Text Alignment Comparison",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 20)
        });

        string sampleText =
            "The quick brown fox jumps over the lazy dog. This sentence demonstrates how text " +
            "alignment affects the visual appearance of a paragraph. When text is justified, the " +
            "word spacing is adjusted so that both the left and right edges align perfectly with " +
            "the margins, creating a clean, professional look commonly seen in books, newspapers, " +
            "and formal documents.";

        // Left-aligned (default)
        doc.Add(CreateSectionHeader("Left Aligned (Default)"));
        doc.Add(new TextBlock
        {
            Text = sampleText,
            FontName = StandardFonts.TimesRoman,
            FontSize = 11,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Center-aligned
        doc.Add(CreateSectionHeader("Center Aligned"));
        doc.Add(new TextBlock
        {
            Text = sampleText,
            FontName = StandardFonts.TimesRoman,
            FontSize = 11,
            TextColor = Color.Slate700,
            Alignment = Elements.Text.TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Right-aligned
        doc.Add(CreateSectionHeader("Right Aligned"));
        doc.Add(new TextBlock
        {
            Text = sampleText,
            FontName = StandardFonts.TimesRoman,
            FontSize = 11,
            TextColor = Color.Slate700,
            Alignment = Elements.Text.TextAlignment.Right,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Justified
        doc.Add(CreateSectionHeader("Justified"));
        doc.Add(new TextBlock
        {
            Text = sampleText,
            FontName = StandardFonts.TimesRoman,
            FontSize = 11,
            TextColor = Color.Slate700,
            Alignment = Elements.Text.TextAlignment.Justify,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Justified with fluent API
        doc.Add(CreateSectionHeader("Justified (Fluent API)"));
        doc.Add(new TextBlock
        {
            Text = sampleText,
            FontName = StandardFonts.TimesRoman,
            FontSize = 11,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 20)
        }.AlignJustify());

        // Multi-paragraph justified text (like a book page)
        doc.Add(new Line { Color = Color.Slate300, Thickness = 1 });
        doc.Add(CreateSectionHeader("Book-Style Justified Paragraphs"));

        string[] paragraphs = new[]
        {
            "In the beginning, there was only the vast emptiness of space, stretching infinitely in " +
            "all directions. Then, from the primordial void, matter began to coalesce under the " +
            "inexorable pull of gravity, forming the first stars that would illuminate the cosmos.",

            "These ancient stars burned with a fierce intensity, forging heavier elements in their " +
            "cores through nuclear fusion. Carbon, oxygen, iron, and gold were all born in the " +
            "hearts of dying stars, scattered across the universe in spectacular supernova explosions.",

            "From this cosmic debris, new generations of stars formed, surrounded by disks of dust " +
            "and gas that would eventually coalesce into planets. On at least one of these worlds, " +
            "conditions were just right for something remarkable to happen: the emergence of life."
        };

        foreach (string paragraph in paragraphs)
        {
            doc.Add(new TextBlock
            {
                Text = paragraph,
                FontName = StandardFonts.TimesRoman,
                FontSize = 11,
                TextColor = Color.Slate800,
                Alignment = Elements.Text.TextAlignment.Justify,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }
        return doc;
    }

    private static TextBlock CreateSectionHeader(string title)
    {
        return new TextBlock
        {
            Text = title,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 13,
            TextColor = Color.Indigo600,
            Margin = new Thickness(0, 0, 0, 6)
        };
    }
}


