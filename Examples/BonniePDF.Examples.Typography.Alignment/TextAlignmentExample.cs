using BonniePDF;
using Align = BonniePDF.TextAlignment;

namespace BonniePDF.Examples.Typography.Alignment;

/// <summary>
/// Left, centre, right, and justified text alignment. PDF/A-2a + PDF/UA-1: the
/// headings and paragraphs are tagged the same whatever the alignment, because
/// alignment is purely presentational.
///
/// All text is set in standard-14 Times Roman. For a demonstration of why an
/// embedded font makes right/centre/justified edges exact, see EmbeddedFontMetricsExample.
/// </summary>
[System.ComponentModel.DisplayName("Text Alignment")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description(
    "Left, centre, right, and justified text alignment including book-style paragraphs, accessible to screen readers. PDF/A-2a + PDF/UA-1.")]
public static class TextAlignmentExample
{
    private const string SampleText =
        "The quick brown fox jumps over the lazy dog. This sentence demonstrates how text " +
        "alignment affects the visual appearance of a paragraph. When text is justified, the " +
        "word spacing is adjusted so that both the left and right edges align perfectly with " +
        "the margins, creating a clean, professional look commonly seen in books and formal documents.";

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
            Text = "The four standard alignments are shown below. Each paragraph carries the same text and the " +
                   "same tagging — alignment is a purely visual property.",
            Margin = new Thickness(0, 0, 0, 14),
        });

        AlignedSample(doc, "Left Aligned (Default)", "s-left", Align.Left);
        AlignedSample(doc, "Centre Aligned", "s-centre", Align.Center);
        AlignedSample(doc, "Right Aligned", "s-right", Align.Right);
        AlignedSample(doc, "Justified", "s-justify", Align.Justify);

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

    private static void AlignedSample(Document doc, string title, string tocId, Align alignment)
    {
        Section(doc, title, tocId, alignment);
        doc.Add(new TextBlock { Text = SampleText, Alignment = alignment, Margin = new Thickness(0, 0, 0, 14) });
    }
}
