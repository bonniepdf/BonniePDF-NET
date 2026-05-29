using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Layout.HorizontalStacks;

/// <summary>
/// PDF/UA-1 tagged horizontal-stacks example.
///
/// HorizontalStack is a layout primitive; it does not claim a semantic structure role.
/// All readable text within the stack is carried by TextBlock children that ARE tagged,
/// so the structure tree sees each piece of text as a /P paragraph regardless of its
/// horizontal position on the page.
///
/// Acceptance: VeraPDF profile ua1 reports zero failures.
/// </summary>
[System.ComponentModel.DisplayName("Horizontal Stacks (PDF/UA-1)")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description("PDF/UA-1 tagged horizontal layout blocks — text content inside stacks tagged as /P paragraphs.")]
public static class HorizontalStacksTaggedExample
{
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Horizontal Stacks — Tagged (PDF/UA-1)",
                Author   = "BonniePDF",
                Subject  = "Demonstrates HorizontalStack layout in a PDF/UA-1 accessible document.",
                Keywords = "pdf, pdf/ua, accessibility, layout, horizontal stack",
            },
            Settings = new DocumentSettings
            {
                Version       = PdfVersion.Pdf17,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(StandardFonts.Helvetica)
                    .WithSize(12)
                    .WithColor(Color.Slate700),
            },
        };

        // ── Equal width distribution ───────────────────────────────────────────

        doc.Add(new Heading("Horizontal Layout Examples")
        {
            TocIdentifier = "intro",
            Level         = 1,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 22,
            TextColor     = Color.Slate800,
            Margin        = new Thickness(0, 0, 0, 12),
        });

        doc.Add(new TextBlock
        {
            Text   = "HorizontalStack arranges child elements side-by-side. Each child carries its own tagged content; the stack itself is a layout primitive with no structure-tree role.",
            Margin = new Thickness(0, 0, 0, 12),
        });

        doc.Add(new Heading("Equal width distribution")
        {
            TocIdentifier = "equal-width",
            Level         = 2,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 14,
            TextColor     = Color.Slate700,
            Margin        = new Thickness(0, 0, 0, 8),
        });

        var equalStack = new HorizontalStack
        {
            WidthDistribution = WidthDistribution.Equal,
            Spacing           = 10,
        };
        equalStack.AddChild(new TextBlock { Text = "Column A", FontName = StandardFonts.HelveticaBold });
        equalStack.AddChild(new TextBlock { Text = "Column B", FontName = StandardFonts.HelveticaBold });
        equalStack.AddChild(new TextBlock { Text = "Column C", FontName = StandardFonts.HelveticaBold });
        doc.Add(equalStack);

        doc.Add(new TextBlock
        {
            Text   = "All three columns share the available width equally. Each TextBlock child is tagged as a /P paragraph in the structure tree.",
            Margin = new Thickness(0, 8, 0, 12),
        });

        // ── Key-value pairs ────────────────────────────────────────────────────

        doc.Add(new Heading("Key-value pairs")
        {
            TocIdentifier = "key-value",
            Level         = 2,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 14,
            TextColor     = Color.Slate700,
            Margin        = new Thickness(0, 0, 0, 8),
        });

        doc.Add(new TextBlock
        {
            Text   = "A common pattern pairs a narrow label column with a wider value column.",
            Margin = new Thickness(0, 0, 0, 6),
        });

        (string key, string value)[] pairs =
        {
            ("Name:",    "Ada Lovelace"),
            ("Role:",    "Mathematician"),
            ("Born:",    "10 December 1815"),
            ("Known for:", "First algorithm intended for a computing machine"),
        };

        foreach ((string key, string value) in pairs)
        {
            var row = new HorizontalStack { Spacing = 10 };
            row.AddChild(new TextBlock { Text = key,   FontName = StandardFonts.HelveticaBold, FontSize = 12 }, 120f);
            row.AddChild(new TextBlock { Text = value, FontSize = 12 });
            doc.Add(row);
        }

        // ── Multi-column text ──────────────────────────────────────────────────

        doc.Add(new Heading("Multi-column description")
        {
            TocIdentifier = "multi-column",
            Level         = 2,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 14,
            TextColor     = Color.Slate700,
            Margin        = new Thickness(0, 14, 0, 8),
        });

        doc.Add(new TextBlock
        {
            Text   = "Side-by-side text columns are equally accessible when each column is a tagged TextBlock.",
            Margin = new Thickness(0, 0, 0, 6),
        });

        var twoCol = new HorizontalStack { WidthDistribution = WidthDistribution.Equal, Spacing = 20 };
        twoCol.AddChild(new TextBlock
        {
            Text     = "PDF/UA-1 (ISO 14289-1) is the standard for accessible PDFs. It requires that all content be tagged with semantic structure elements such as headings, paragraphs, lists, and figures.",
            FontSize = 11,
        });
        twoCol.AddChild(new TextBlock
        {
            Text     = "PDF/A-1b (ISO 19005-1) is the standard for long-term archival. Together with PDF/UA-1 they cover both preservation and accessibility requirements for professional documents.",
            FontSize = 11,
        });
        doc.Add(twoCol);

        // ── Summary ────────────────────────────────────────────────────────────

        doc.Add(new Heading("Summary")
        {
            TocIdentifier = "summary",
            Level         = 2,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 14,
            TextColor     = Color.Slate700,
            Margin        = new Thickness(0, 14, 0, 8),
        });

        doc.Add(new TextBlock
        {
            Text = "HorizontalStack is a transparent layout container. VeraPDF does not require it to carry a structure-tree role because the readable content is held by its tagged TextBlock children. " +
                   "The reading order follows the order in which children were added to the stack.",
        });

        return doc;
    }
}
