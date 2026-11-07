using BonniePDF;
using Align = BonniePDF.TextAlignment;

namespace BonniePDF.Examples.TextStyling.Alignment;

/// <summary>
/// Left, centre, right, and justified text alignment. PDF/A-2a + PDF/UA-1: the
/// headings and paragraphs are tagged the same whatever the alignment, because
/// alignment is purely presentational.
///
/// All text is set in standard-14 Times Roman. For a demonstration of why an
/// embedded font makes right/centre/justified edges exact, see EmbeddedFontMetricsExample.
/// </summary>
[System.ComponentModel.DisplayName("Text Alignment")]
[System.ComponentModel.Category("Text Styling")]
[System.ComponentModel.Description(
    "Left, centre, right, and justified text alignment including book-style paragraphs, accessible to screen readers. PDF/A-2a + PDF/UA-1.")]
public static class TextAlignmentExample
{
    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "Text Alignment",
                Author = "BonniePDF",
                Subject = "Left, centre, right, and justified text alignment. PDF/A-2a + PDF/UA-1.",
                Keywords = "pdf, pdf/ua, pdf/a, accessibility, text, alignment, justified",
            },
            Settings = new DocumentSettings
            {
                PageSize = PdfSize.A4,
                Margin = 60,
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(StandardFonts.TimesRoman)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[600]),
            },
        };

        doc.Add(new Heading("Text Alignment Comparison")
        {
            TocIdentifier = "intro",
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 16),
        });

        doc.Add(new TextBlock
        {
            Text = "Each alignment suits a different job, so each sample below is the kind of text it is set for. Alignment is a purely visual property: every paragraph is tagged the same way.",
            Margin = new Thickness(0, 0, 0, 14),
        });

        AlignedSample(doc, "Left Aligned — letters and body text", "s-left", Align.Left, "Dear Ms Patel, thank you for your letter of 28 October. Left-aligned text keeps an even space between words and a ragged right edge, which makes it the easiest setting to read for letters, emails and most body copy.");
        AlignedSample(doc, "Centre Aligned — invitations and titles", "s-centre", Align.Center, "You are warmly invited to the opening of the new reading room\nSaturday 14 November at eleven o'clock\nSample Town Library, Market Square");
        AlignedSample(doc, "Right Aligned — addresses and dates", "s-right", Align.Right, "Sample Supplies Ltd\n12 Exmouth Market\nLondon EC1R 4QE\n5 November 2026");
        AlignedSample(doc, "Justified — books and columns", "s-justify", Align.Justify, "Justified text stretches the spaces between words so that both edges of a column line up, the setting used in most printed books and newspapers. It works best in long lines: in a narrow column the stretched spaces can open into visible gaps.");

        // ── Book-style justified paragraphs ────────────────────────────────────
        Section(doc, "Book-Style Justified Paragraphs", "book");

        string[] paragraphs =
        {
            "In the beginning, there was only the vast emptiness of space, stretching infinitely in all directions. " +
            "Then, from the primordial void, matter began to coalesce under the inexorable pull of gravity, forming " +
            "the first stars that would illuminate the cosmos.",

            "These ancient stars burned with a fierce intensity, forging heavier elements in their cores through " +
            "nuclear fusion. Carbon, oxygen, iron, and gold were all born in the hearts of dying stars, scattered " +
            "across the universe in spectacular supernova explosions.",

            "From this cosmic debris, new generations of stars formed, surrounded by disks of dust and gas that " +
            "would eventually coalesce into planets. On at least one of these worlds, conditions were just right " +
            "for something remarkable to happen: the emergence of life.",
        };

        foreach (string p in paragraphs)
        {
            doc.Add(new TextBlock
            {
                Text = p,
                FontName = StandardFonts.TimesRoman,
                Alignment = Align.Justify,
                Margin = new Thickness(0, 0, 0, 8),
            });
        }

        return doc;
    }

    private static void Section(Document doc, string title, string tocId, Align alignment = Align.Left)
    {
        doc.Add(new Heading(title)
        {
            TocIdentifier = tocId,
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 13,
            Color = ColorPalette.Indigo[600],
            Margin = new Thickness(0, 0, 0, 6),
            Alignment = alignment,
        });
    }

    private static void AlignedSample(Document doc, string title, string tocId, Align alignment, string text)
    {
        Section(doc, title, tocId, alignment);
        doc.Add(new TextBlock { Text = text, Alignment = alignment, Margin = new Thickness(0, 0, 0, 14) });
    }
}
