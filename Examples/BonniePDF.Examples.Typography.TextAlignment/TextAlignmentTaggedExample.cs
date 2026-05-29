using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;
using Align = BonniePDF.Elements.Text.TextAlignment;

namespace BonniePDF.Examples.Typography.TextAlignment;

/// <summary>
/// PDF/UA-1 tagged text-alignment example.
///
/// Demonstrates left, centre, right, and justified alignment. Section headers are /H2
/// structure elements; each aligned paragraph is a /P. Alignment is purely presentational
/// and does not affect the structure-tree type — all four paragraphs are /P.
///
/// Acceptance: VeraPDF profile ua1 reports zero failures.
/// </summary>
[System.ComponentModel.DisplayName("Text Alignment (PDF/UA-1)")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("PDF/UA-1 tagged text showing left, centre, right, and justified alignment — each paragraph tagged as /P.")]
public static class TextAlignmentTaggedExample
{
    private const string SampleText =
        "The quick brown fox jumps over the lazy dog. This sentence demonstrates how text " +
        "alignment affects the visual appearance of a paragraph. When text is justified, the " +
        "word spacing is adjusted so that both the left and right edges align perfectly with " +
        "the margins, creating a clean, professional look commonly seen in books and formal documents.";

    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Text Alignment — Tagged (PDF/UA-1)",
                Author   = "BonniePDF",
                Subject  = "Demonstrates all four text alignments in a PDF/UA-1 accessible document.",
                Keywords = "pdf, pdf/ua, accessibility, text, alignment, justified",
            },
            Settings = new DocumentSettings
            {
                PageSize      = PdfSize.A4,
                Margin        = 60,
                Version       = PdfVersion.Pdf17,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(StandardFonts.TimesRoman)
                    .WithSize(11)
                    .WithColor(Color.Slate700),
            },
        };

        doc.Add(new Heading("Text Alignment Comparison")
        {
            TocIdentifier = "intro",
            Level         = 1,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 22,
            TextColor     = Color.Slate800,
            Margin        = new Thickness(0, 0, 0, 16),
        });

        doc.Add(new TextBlock
        {
            Text   = "The four standard alignments are shown below. Each paragraph carries the same text and the same /P structure-tree role — alignment is a purely visual property.",
            Margin = new Thickness(0, 0, 0, 14),
        });

        // Left-aligned (default)
        Section(doc, "Left Aligned (Default)", "s-left");
        doc.Add(new TextBlock { Text = SampleText, Alignment = Align.Left,    Margin = new Thickness(0, 0, 0, 14) });

        // Centre
        Section(doc, "Centre Aligned", "s-centre");
        doc.Add(new TextBlock { Text = SampleText, Alignment = Align.Center,  Margin = new Thickness(0, 0, 0, 14) });

        // Right
        Section(doc, "Right Aligned", "s-right");
        doc.Add(new TextBlock { Text = SampleText, Alignment = Align.Right,   Margin = new Thickness(0, 0, 0, 14) });

        // Justified
        Section(doc, "Justified", "s-justify");
        doc.Add(new TextBlock { Text = SampleText, Alignment = Align.Justify, Margin = new Thickness(0, 0, 0, 14) });

        // Book-style section
        doc.Add(new Heading("Book-Style Justified Paragraphs")
        {
            TocIdentifier = "book",
            Level         = 2,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 13,
            TextColor     = Color.Indigo600,
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
                FontName  = StandardFonts.TimesRoman,
                FontSize  = 11,
                TextColor = Color.Slate800,
                Alignment = Align.Justify,
                Margin    = new Thickness(0, 0, 0, 8),
            });
        }

        return doc;
    }

    private static void Section(Document doc, string title, string tocId)
    {
        doc.Add(new Heading(title)
        {
            TocIdentifier = tocId,
            Level         = 2,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 13,
            TextColor     = Color.Indigo600,
            Margin        = new Thickness(0, 0, 0, 6),
        });
    }
}
