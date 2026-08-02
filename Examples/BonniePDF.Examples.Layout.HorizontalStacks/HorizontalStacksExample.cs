using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Layout.HorizontalStacks;

/// <summary>
/// HorizontalStack layouts in an accessible, archival document (PDF/A-2a +
/// PDF/UA-1). HorizontalStack is a layout primitive; it does not claim a
/// semantic structure role. All readable text within the stack is carried by
/// TextBlock children that ARE tagged, so the structure tree sees each piece
/// of text as a /P paragraph regardless of its horizontal position.
///
/// Acceptance: VeraPDF profile ua1 reports zero failures.
/// </summary>
[System.ComponentModel.DisplayName("Horizontal Stacks")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description("Horizontal layout blocks - width distribution, cross-axis alignment, and side-by-side text, all tagged as /P paragraphs.")]
public static class HorizontalStacksExample
{
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Horizontal Stacks Demo",
                Author   = "BonniePDF",
                Subject  = "Demonstrates HorizontalStack layout in an accessible PDF/A-2a + PDF/UA-1 document.",
                Keywords = "pdf, accessibility, layout, horizontal stack, columns, BonniePDF",
            },
            Settings = new DocumentSettings
            {
                Conformance   = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(StandardFonts.Helvetica)
                    .WithSize(12)
                    .WithColor(ColorPalette.Slate[600]),
            },
        };

        doc.Add(new Heading("Horizontal Layout Examples")
        {
            TocIdentifier = "intro",
            Level         = 1,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 22,
            TextColor     = ColorPalette.Slate[800],
            Margin        = new Thickness(0, 0, 0, 12),
        });

        doc.Add(new TextBlock
        {
            Text   = "HorizontalStack arranges child elements side-by-side. Each child carries its own tagged content; the stack itself is a layout primitive with no structure-tree role.",
            Margin = new Thickness(0, 0, 0, 12),
        });

        // ── Equal width distribution ───────────────────────────────────────────

        doc.Add(new Heading("Equal width distribution")
        {
            TocIdentifier = "equal-width",
            Level         = 2,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 14,
            TextColor     = ColorPalette.Indigo[600],
            Margin        = new Thickness(0, 0, 0, 8),
        });

        var equalStack = new HorizontalStack
        {
            WidthDistribution = WidthDistribution.Equal,
            Spacing           = 10,
        };
        equalStack.Add(new TextBlock { Text = "Column A", FontName = StandardFonts.HelveticaBold });
        equalStack.Add(new TextBlock { Text = "Column B", FontName = StandardFonts.HelveticaBold });
        equalStack.Add(new TextBlock { Text = "Column C", FontName = StandardFonts.HelveticaBold });
        doc.Add(equalStack);

        doc.Add(new TextBlock
        {
            Text   = "All three columns share the available width equally. Each TextBlock child is tagged as a /P paragraph in the structure tree.",
            Margin = new Thickness(0, 8, 0, 12),
        });

        // ── Cross-axis (vertical) alignment ────────────────────────────────────

        doc.Add(new Heading("Cross-axis alignment")
        {
            TocIdentifier = "cross-axis",
            Level         = 2,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 14,
            TextColor     = ColorPalette.Indigo[600],
            Margin        = new Thickness(0, 0, 0, 8),
        });

        doc.Add(new TextBlock
        {
            Text   = "When a stack has a fixed height and its children differ in height, Alignment positions each child along the cross (vertical) axis. The same three boxes - 40pt, 65pt and 90pt tall - are shown top-aligned, centred, and bottom-aligned.",
            Margin = new Thickness(0, 0, 0, 8),
        });

        (string label, float boxHeight, Color bg)[] boxes =
        {
            ("40pt", 40f, ColorPalette.Blue[600]),
            ("65pt", 65f, ColorPalette.Indigo[600]),
            ("90pt", 90f, ColorPalette.Slate[600]),
        };

        (string caption, HorizontalAlignment align)[] rows =
        {
            ("Top (Start)",     HorizontalAlignment.Start),
            ("Centre (Center)", HorizontalAlignment.Center),
            ("Bottom (End)",    HorizontalAlignment.End),
        };

        foreach ((string caption, HorizontalAlignment align) in rows)
        {
            doc.Add(new TextBlock
            {
                Text      = caption,
                FontName  = StandardFonts.HelveticaBold,
                FontSize  = 11,
                TextColor = ColorPalette.Slate[600],
                Margin    = new Thickness(0, 8, 0, 4),
            });

            var row = new HorizontalStack
            {
                WidthDistribution = WidthDistribution.Equal,
                Spacing           = 10,
                Height            = 90,
                Alignment         = align,
            };
            foreach ((string label, float boxHeight, Color bg) in boxes)
            {
                row.Add(new Container
                {
                    Child           = new TextBlock { Text = label, FontName = StandardFonts.HelveticaBold, FontSize = 11, TextColor = Color.White },
                    Padding         = new Thickness(12, 6, 12, 6),
                    BackgroundColor = bg,
                    Height          = boxHeight,
                });
            }
            doc.Add(row);
        }

        // ── Key-value pairs ────────────────────────────────────────────────────

        // Keep the heading with its section: no keep-with-next primitive exists
        // yet, so force the whole block onto the next page with a break.
        doc.Add(new PageBreak());

        doc.Add(new Heading("Key-value pairs")
        {
            TocIdentifier = "key-value",
            Level         = 2,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 14,
            TextColor     = ColorPalette.Indigo[600],
            Margin        = new Thickness(0, 14, 0, 8),
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
            row.Add(new TextBlock { Text = key,   FontName = StandardFonts.HelveticaBold, FontSize = 12 }, 120f);
            row.Add(new TextBlock { Text = value, FontSize = 12 });
            doc.Add(row);
        }

        // ── Multi-column text ──────────────────────────────────────────────────

        doc.Add(new Heading("Multi-column description")
        {
            TocIdentifier = "multi-column",
            Level         = 2,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 14,
            TextColor     = ColorPalette.Indigo[600],
            Margin        = new Thickness(0, 14, 0, 8),
        });

        doc.Add(new TextBlock
        {
            Text   = "Side-by-side text columns are equally accessible when each column is a tagged TextBlock.",
            Margin = new Thickness(0, 0, 0, 6),
        });

        var twoCol = new HorizontalStack { WidthDistribution = WidthDistribution.Equal, Spacing = 20 };
        twoCol.Add(new TextBlock
        {
            Text     = "PDF/UA-1 (ISO 14289-1) is the standard for accessible PDFs. It requires that all content be tagged with semantic structure elements such as headings, paragraphs, lists, and figures.",
            FontSize = 11,
        });
        twoCol.Add(new TextBlock
        {
            Text     = "PDF/A-2a (ISO 19005-2) is the modern archival profile. Together with PDF/UA-1 they cover both preservation and accessibility requirements for professional documents.",
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
            TextColor     = ColorPalette.Indigo[600],
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
