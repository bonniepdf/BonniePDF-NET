using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;
using Align = BonniePDF.Elements.Text.TextAlignment;

namespace BonniePDF.Examples.Typography.Alignment;

/// <summary>
/// Embedded-font metrics — why an embedded font makes alignment edges exact.
///
/// The standard-14 Type1 fonts are measured from approximate AFM width tables. An
/// embedded TrueType font instead carries its own <c>hmtx</c> advance widths, so
/// BonniePDF measures and places every glyph from the real metrics. That precision
/// is what makes right, centre, and justified edges land exactly on the margin.
///
/// This example renders the four alignments entirely in the bundled NotoSans TTF
/// (embedded as a compact glyph subset). PDF/A-2a + PDF/UA-1.
/// </summary>
[System.ComponentModel.DisplayName("Embedded-Font Metrics")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("How an embedded TrueType font makes alignment edges exact, using its own hmtx advance widths rather than the approximate standard-14 width tables. PDF/A-2a + PDF/UA-1.")]
public static class EmbeddedFontMetricsExample
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
                Title    = "Embedded-Font Metrics",
                Author   = "BonniePDF",
                Subject  = "How an embedded TrueType font makes alignment edges exact, using its own hmtx advance widths.",
                Keywords = "pdf, pdf/ua, pdf/a, fonts, embedding, metrics, hmtx, alignment, truetype, notosans",
            },
            Settings = new DocumentSettings
            {
                PageSize      = PdfSize.A4,
                Margin        = 60,
                Conformance   = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(sansPath)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[600]),
            },
        };

        doc.Add(new Heading("Embedded-Font Metrics")
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
            Text   = "The standard-14 fonts are measured from approximate width tables. An embedded TrueType font carries " +
                     "its own hmtx advance widths, so glyph placement is exact — which is what makes the right, centre, and " +
                     "justified edges below land precisely on the margin. Every element here is set in embedded NotoSans.",
            Margin = new Thickness(0, 0, 0, 14),
        });

        AlignedSample(doc, "Left Aligned (Default)", "s-left", Align.Left, sansPath);
        AlignedSample(doc, "Centre Aligned", "s-centre", Align.Center, sansPath);
        AlignedSample(doc, "Right Aligned", "s-right", Align.Right, sansPath);
        AlignedSample(doc, "Justified", "s-justify", Align.Justify, sansPath);

        // ── Book-style justified paragraphs ────────────────────────────────────
        Section(doc, "Book-Style Justified Paragraphs", "book", sansPath);

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

    private static void AlignedSample(Document doc, string title, string tocId, Align alignment, string fontPath)
    {
        Section(doc, title, tocId, fontPath);
        doc.Add(new TextBlock { Text = SampleText, Alignment = alignment, Margin = new Thickness(0, 0, 0, 14) });
    }
}
