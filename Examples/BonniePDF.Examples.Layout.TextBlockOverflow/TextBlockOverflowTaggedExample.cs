using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Prototyping;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Layout.TextBlockOverflow;

/// <summary>
/// PDF/UA-1 tagged TextBlock overflow example.
///
/// A single long TextBlock that spans many pages demonstrates that the structure tree
/// keeps the /P paragraph element open across page boundaries. A footer template adds
/// page numbers as /Artifact so screen readers skip them.
///
/// Acceptance: VeraPDF profile ua1 reports zero failures.
/// </summary>
[System.ComponentModel.DisplayName("TextBlock Overflow (PDF/UA-1)")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description("PDF/UA-1 tagged long text flowing across multiple pages with page-number footer artifacts.")]
public static class TextBlockOverflowTaggedExample
{
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Long Text Document - Tagged (PDF/UA-1)",
                Author   = "BonniePDF",
                Subject  = "Demonstrates a TextBlock spanning multiple pages in a PDF/UA-1 accessible document.",
                Keywords = "pdf, pdf/ua, accessibility, text overflow, multi-page",
            },
            Settings = new DocumentSettings
            {
                Version       = PdfVersion.Pdf17,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(StandardFonts.Helvetica)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[600]),

                // Page numbers in the footer are /Artifact - screen readers skip them.
                FooterTemplate = pageNum => new TextBlock
                {
                    Text      = $"Page {pageNum} of {TextElementBase.TotalPagesToken}",
                    FontName  = StandardFonts.Helvetica,
                    FontSize  = 9,
                    TextColor = ColorPalette.Slate[400],
                    Alignment = TextAlignment.Right,
                },
            },
        };

        doc.Add(new Heading("Long Text Document")
        {
            TocIdentifier = "intro",
            Level         = 1,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 22,
            TextColor     = ColorPalette.Slate[800],
            Margin        = new Thickness(0, 0, 0, 14),
        });

        doc.Add(new TextBlock
        {
            Text   = "The following paragraphs are repeated to produce enough content to flow across multiple pages. " +
                     "Each paragraph is an independent /P structure element. The footer on each page is tagged as an " +
                     "/Artifact so assistive technology skips it.",
            Margin = new Thickness(0, 0, 0, 14),
        });

        var source = Placeholders.WithSeed(501);
        for (int i = 1; i <= 20; i++)
        {
            doc.Add(new Heading($"Section {i}")
            {
                TocIdentifier = $"s{i}",
                Level         = 2,
                FontName      = StandardFonts.HelveticaBold,
                FontSize      = 13,
                TextColor     = ColorPalette.Indigo[600],
                Margin        = new Thickness(0, 10, 0, 4),
            });

            doc.Add(new TextBlock
            {
                Text   = source.Paragraph(),
                Margin = new Thickness(0, 0, 0, 6),
            });

            doc.Add(new TextBlock
            {
                Text   = source.Paragraph(),
                Margin = new Thickness(0, 0, 0, 6),
            });
        }

        return doc;
    }
}
