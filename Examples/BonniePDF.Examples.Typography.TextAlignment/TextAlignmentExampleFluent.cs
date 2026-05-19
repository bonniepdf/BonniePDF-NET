using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Typography.TextAlignment;

/// <summary>
/// Demonstrates all four text alignments using the Fluent API.
/// Shows left, center, right, and justified alignment with both
/// Times Roman body text and a book-style justified paragraph section.
/// </summary>
public static class TextAlignmentExampleFluent
{
    public static Document Generate()
    {
        return Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title = "Justified Text Alignment (Fluent)";
                metadata.Author = "BonniePDF";
            });

            doc.WithSettings(settings =>
            {
                settings.PageSize = PdfSize.A4;
                settings.Margin = 60;
                settings.Version = PdfVersion.Pdf17;
                settings.PdfAEnabled = true;
            });

            // Body text defaults to Times Roman 11pt — only headers need overrides
            doc.WithDefaultTextStyle(style => style
                .WithFamily(StandardFonts.TimesRoman)
                .WithSize(11));

            doc.Page(page =>
            {
                page.Content().Column(column =>
                {
                    // Title — override family and size; weight resolved automatically
                    column.Item()
                        .Text("Text Alignment Comparison")
                        .Font(StandardFonts.Helvetica)
                        .Bold()
                        .FontSize(22)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 20);

                    const string sampleText =
                        "The quick brown fox jumps over the lazy dog. This sentence demonstrates how text " +
                        "alignment affects the visual appearance of a paragraph. When text is justified, the " +
                        "word spacing is adjusted so that both the left and right edges align perfectly with " +
                        "the margins, creating a clean, professional look commonly seen in books, newspapers, " +
                        "and formal documents.";

                    // Left-aligned (default) — no alignment call needed
                    column.Item().Text("Left Aligned (Default)")
                        .Font(StandardFonts.Helvetica).Bold().FontSize(13)
                        .TextColor(Color.Indigo600).Margin(0, 0, 0, 6);

                    column.Item()
                        .Text(sampleText)
                        .TextColor(Color.Slate700)
                        .Margin(0, 0, 0, 20);

                    // Center-aligned
                    column.Item().Text("Center Aligned")
                        .Font(StandardFonts.Helvetica).Bold().FontSize(13)
                        .TextColor(Color.Indigo600).Margin(0, 0, 0, 6);

                    column.Item()
                        .Text(sampleText)
                        .TextColor(Color.Slate700)
                        .AlignCenter()
                        .Margin(0, 0, 0, 20);

                    // Right-aligned
                    column.Item().Text("Right Aligned")
                        .Font(StandardFonts.Helvetica).Bold().FontSize(13)
                        .TextColor(Color.Indigo600).Margin(0, 0, 0, 6);

                    column.Item()
                        .Text(sampleText)
                        .TextColor(Color.Slate700)
                        .AlignRight()
                        .Margin(0, 0, 0, 20);

                    // Justified — fluent .AlignJustify() extension
                    column.Item().Text("Justified")
                        .Font(StandardFonts.Helvetica).Bold().FontSize(13)
                        .TextColor(Color.Indigo600).Margin(0, 0, 0, 6);

                    column.Item()
                        .Text(sampleText)
                        .TextColor(Color.Slate700)
                        .AlignJustify()
                        .Margin(0, 0, 0, 20);

                    // Divider before book-style section
                    column.Item()
                        .Margin(0, 4, 0, 4)
                        .Line(1, Color.Slate300);

                    column.Item().Text("Book-Style Justified Paragraphs")
                        .Font(StandardFonts.Helvetica).Bold().FontSize(13)
                        .TextColor(Color.Indigo600).Margin(0, 6, 0, 6);

                    string[] paragraphs =
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
                        column.Item()
                            .Text(paragraph)
                            .TextColor(Color.Slate800)
                            .AlignJustify()
                            .Margin(0, 0, 0, 8);
                    }
                });
            });
        }).Build();

    }
}


