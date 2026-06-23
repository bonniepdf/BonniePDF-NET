using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;
using Align = BonniePDF.Elements.Text.TextAlignment;

namespace BonniePDF.Examples.Typography.TextAlignment;

/// <summary>
/// Text alignment example using an embedded NotoSans TrueType font.
///
/// Identical in content to <see cref="TextAlignmentTaggedExample"/>, but every text
/// element is rendered with the bundled NotoSans-Regular.ttf instead of the
/// standard-14 Type1 fonts. Because the font is embedded, both layout measurement
/// and glyph placement use the font's own hmtx advance widths - so right/centre/
/// justified alignment is exact, with no reliance on the approximate standard-font
/// width tables.
///
/// Mirror of the TypeScript examples/src/typography/text-alignment-liberation.ts.
/// Acceptance: VeraPDF profile ua1 reports zero failures.
/// </summary>
[System.ComponentModel.DisplayName("Text Alignment (Embedded Font)")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("All four text alignments rendered with an embedded NotoSans TrueType font for precise layout.")]
public static class TextAlignmentLiberationExample
{
    private const string SampleText =
        "The quick brown fox jumps over the lazy dog. This sentence demonstrates how text " +
        "alignment affects the visual appearance of a paragraph. When text is justified, the " +
        "word spacing is adjusted so that both the left and right edges align perfectly with " +
        "the margins, creating a clean, professional look commonly seen in books and formal documents.";

    public static Document Generate()
    {
        string sansPath = BundledFont.SansPath;

        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Text Alignment - NotoSans (PDF/UA-1)",
                Author   = "BonniePDF",
                Subject  = "All four text alignments rendered with an embedded NotoSans TrueType font.",
                Keywords = "pdf, pdf/ua, accessibility, text, alignment, justified, truetype, notosans",
            },
            Settings = new DocumentSettings
            {
                Conformance   = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(sansPath)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[600]),
            },
        };

        doc.Add(new Heading("Text Alignment Comparison")
        {
            TocIdentifier = "intro",
            Level         = 1,
            FontName      = sansPath,
            FontSize      = 22,
            TextColor     = ColorPalette.Slate[800],
            Margin        = new Thickness(0, 0, 0, 16),
        });

        doc.Add(new TextBlock
        {
            Text   = "The four standard alignments are shown below, all set in embedded NotoSans. Each paragraph " +
                     "carries the same text and the same /P structure-tree role - alignment is a purely visual property.",
            Margin = new Thickness(0, 0, 0, 14),
        });

        // Left-aligned (default)
        Section(doc, "Left Aligned (Default)", "s-left", sansPath);
        doc.Add(new TextBlock { Text = SampleText, Alignment = Align.Left, Margin = new Thickness(0, 0, 0, 14) });

        // Centre
        Section(doc, "Centre Aligned", "s-centre", sansPath);
        doc.Add(new TextBlock { Text = SampleText, Alignment = Align.Center, Margin = new Thickness(0, 0, 0, 14) });

        // Right
        Section(doc, "Right Aligned", "s-right", sansPath);
        doc.Add(new TextBlock { Text = SampleText, Alignment = Align.Right, Margin = new Thickness(0, 0, 0, 14) });

        // Justified
        Section(doc, "Justified", "s-justify", sansPath);
        doc.Add(new TextBlock { Text = SampleText, Alignment = Align.Justify, Margin = new Thickness(0, 0, 0, 14) });

        // Book-style section
        doc.Add(new Heading("Book-Style Justified Paragraphs")
        {
            TocIdentifier = "book",
            Level         = 2,
            FontName      = sansPath,
            FontSize      = 13,
            TextColor     = ColorPalette.Indigo[600],
            Margin        = new Thickness(0, 6, 0, 6),
        });

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
                Text      = p,
                FontName  = sansPath,
                FontSize  = 11,
                TextColor = ColorPalette.Slate[800],
                Alignment = Align.Justify,
                Margin    = new Thickness(0, 0, 0, 8),
            });
        }

        return doc;
    }

    private static void Section(Document doc, string title, string tocId, string fontPath)
    {
        doc.Add(new Heading(title)
        {
            TocIdentifier = tocId,
            Level         = 2,
            FontName      = fontPath,
            FontSize      = 13,
            TextColor     = ColorPalette.Indigo[600],
            Margin        = new Thickness(0, 0, 0, 6),
        });
    }
}
