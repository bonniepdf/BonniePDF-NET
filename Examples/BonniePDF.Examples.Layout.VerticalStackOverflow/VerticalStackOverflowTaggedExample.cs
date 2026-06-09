using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Prototyping;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Layout.VerticalStackOverflow;

/// <summary>
/// PDF/UA-1 tagged VerticalStack overflow example.
///
/// Seventy-five card-style items are laid out in a VerticalStack that spans multiple pages.
/// VerticalStack is a layout primitive with no structure-tree role; each card's heading and
/// description are tagged as /H3 and /P respectively. The structure tree contains one entry
/// per readable item regardless of which page it lands on.
///
/// Acceptance: VeraPDF profile ua1 reports zero failures.
/// </summary>
[System.ComponentModel.DisplayName("VerticalStack Overflow (PDF/UA-1)")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description("PDF/UA-1 tagged VerticalStack spanning multiple pages - each item's text tagged semantically.")]
public static class VerticalStackOverflowTaggedExample
{
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Item Catalogue - Tagged (PDF/UA-1)",
                Author   = "BonniePDF",
                Subject  = "Demonstrates a VerticalStack spanning multiple pages in a PDF/UA-1 accessible document.",
                Keywords = "pdf, pdf/ua, accessibility, vertical stack, multi-page",
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
            },
        };

        doc.Add(new Heading("Item Catalogue")
        {
            TocIdentifier = "catalogue",
            Level         = 1,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 22,
            TextColor     = Color.Slate800,
            Margin        = new Thickness(0, 0, 0, 8),
        });

        doc.Add(new TextBlock
        {
            Text   = "A collection of items demonstrating automatic pagination. Each item is an independent tagged element - its heading is /H3 and its description is /P in the structure tree.",
            Margin = new Thickness(0, 0, 0, 14),
        });

        // 75 items - enough to span several pages
        var stack = new VerticalStack();

        var source = Placeholders.WithSeed(401);
        for (int i = 1; i <= 75; i++)
        {
            stack.AddChild(new Heading($"Item #{i:D3}")
            {
                TocIdentifier = $"item-{i}",
                Level         = 3,
                FontName      = StandardFonts.HelveticaBold,
                FontSize      = 12,
                TextColor     = Color.Blue600,
                Margin        = new Thickness(0, 10, 0, 2),
            });

            stack.AddChild(new TextBlock
            {
                Text   = source.Sentence(),
                FontSize = 10,
                TextColor = Color.Slate600,
                Margin  = new Thickness(0, 0, 0, 4),
            });
        }

        doc.Add(stack);

        return doc;
    }
}
