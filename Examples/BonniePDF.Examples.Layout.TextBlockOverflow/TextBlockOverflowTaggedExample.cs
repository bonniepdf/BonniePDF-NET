using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
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
    private static readonly string[] Paragraphs =
    {
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.",
        "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum. Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo.",
        "Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, adipisci velit, sed quia non numquam eius modi tempora incidunt.",
        "Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid ex ea commodi consequatur? Quis autem vel eum iure reprehenderit qui in ea voluptate velit esse quam nihil molestiae consequatur, vel illum qui dolorem eum fugiat quo voluptas nulla pariatur?",
        "At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis praesentium voluptatum deleniti atque corrupti quos dolores et quas molestias excepturi sint occaecati cupiditate non provident, similique sunt in culpa qui officia deserunt mollitia animi, id est laborum et dolorum fuga.",
    };

    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Long Text Document — Tagged (PDF/UA-1)",
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
                    .WithColor(Color.Slate700),

                // Page numbers in the footer are /Artifact — screen readers skip them.
                FooterTemplate = pageNum => new TextBlock
                {
                    Text      = $"Page {pageNum}",
                    FontName  = StandardFonts.Helvetica,
                    FontSize  = 9,
                    TextColor = Color.Slate400,
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
            TextColor     = Color.Slate800,
            Margin        = new Thickness(0, 0, 0, 14),
        });

        doc.Add(new TextBlock
        {
            Text   = "The following paragraphs are repeated to produce enough content to flow across multiple pages. " +
                     "Each paragraph is an independent /P structure element. The footer on each page is tagged as an " +
                     "/Artifact so assistive technology skips it.",
            Margin = new Thickness(0, 0, 0, 14),
        });

        // Repeat paragraphs enough times to fill several pages.
        for (int i = 1; i <= 20; i++)
        {
            doc.Add(new Heading($"Section {i}")
            {
                TocIdentifier = $"s{i}",
                Level         = 2,
                FontName      = StandardFonts.HelveticaBold,
                FontSize      = 13,
                TextColor     = Color.Slate700,
                Margin        = new Thickness(0, 10, 0, 4),
            });

            doc.Add(new TextBlock
            {
                Text   = Paragraphs[i % Paragraphs.Length],
                Margin = new Thickness(0, 0, 0, 6),
            });

            doc.Add(new TextBlock
            {
                Text   = Paragraphs[(i + 1) % Paragraphs.Length],
                Margin = new Thickness(0, 0, 0, 6),
            });
        }

        return doc;
    }
}
