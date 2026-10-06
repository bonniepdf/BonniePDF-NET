using BonniePDF;

namespace BonniePDF.Examples.Layout.HorizontalStacks;

/// <summary>
/// HorizontalStack layouts - equal and weighted width distribution, cross-axis
/// alignment, key-value pairs, and two-column text - in an accessible, archival
/// document (PDF/A-2a + PDF/UA-1).
///
/// HorizontalStack is a layout primitive and claims no semantic role of its own.
/// All readable text is carried by TextBlock children that ARE tagged, so every
/// piece of text is announced as its own paragraph regardless of where it sits
/// horizontally.
///
/// Acceptance: VeraPDF profile ua1 reports zero failures.
/// </summary>
[System.ComponentModel.DisplayName("Horizontal Stacks")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description("Horizontal layout blocks - width distribution, cross-axis alignment, and side-by-side text, all tagged for screen readers.")]
public static class HorizontalStacksExample
{
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "Horizontal Stacks Demo",
                Author = "BonniePDF",
                Subject = "Demonstrates horizontal stack layouts in an accessible PDF/A-2a + PDF/UA-1 document.",
                Keywords = "pdf, accessibility, layout, horizontal stack, columns, BonniePDF",
            },
            Settings = new DocumentSettings
            {
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(StandardFonts.Helvetica)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[700]),
            },
        };

        doc.Add(new Heading("Horizontal Stack Layouts")
        {
            TocIdentifier = "intro",
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 12),
        });

        doc.Add(new TextBlock
        {
            Text = "HorizontalStack arranges child elements side-by-side. Width distribution controls how available space is divided between columns; cross-axis alignment controls how children of differing heights line up vertically. All text inside stacks is accessible to screen readers.",
            Margin = new Thickness(0, 0, 0, 16),
        });

        // ── Equal width distribution ───────────────────────────────────────────

        doc.Add(SectionHeading("Equal Width Distribution", "s-equal", 0));
        doc.Add(new TextBlock
        {
            Text = "Three columns sharing available width equally (WidthDistribution.Equal).",
            Margin = new Thickness(0, 0, 0, 8),
        });

        var equalStack = new HorizontalStack
        {
            WidthDistribution = WidthDistribution.Equal,
            Spacing = 10,
        };
        equalStack.Add(Box("Column 1", ColorPalette.Blue[600]));
        equalStack.Add(Box("Column 2", ColorPalette.Indigo[600]));
        equalStack.Add(Box("Column 3", ColorPalette.Slate[600]));
        doc.Add(equalStack);

        // ── Weighted distribution ──────────────────────────────────────────────

        doc.Add(SectionHeading("Weighted Width Distribution", "s-weighted", 16));
        doc.Add(new TextBlock
        {
            Text = "Columns with a 1:2:3 weight ratio - the second column is twice as wide as the first, and the third is three times as wide.",
            Margin = new Thickness(0, 0, 0, 8),
        });

        var weightedStack = new HorizontalStack
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 10,
        };
        // 500, not 400: white text on Slate[400] falls to 2.63:1 against the
        // 4.5:1 WCAG AA threshold; 500 clears it (4.76:1) and still reads lighter
        // than the 600/800 boxes beside it.
        weightedStack.Add(Box("1×", ColorPalette.Slate[500]), 1f);
        weightedStack.Add(Box("2×", ColorPalette.Slate[600]), 2f);
        weightedStack.Add(Box("3×", ColorPalette.Slate[800]), 3f);
        doc.Add(weightedStack);

        // ── Cross-axis (vertical) alignment ────────────────────────────────────

        doc.Add(SectionHeading("Cross-Axis Alignment", "s-align", 16));
        doc.Add(new TextBlock
        {
            Text = "When a stack has a fixed height and its children differ in height, stack.Alignment positions each child along the cross (vertical) axis. The same three boxes - 40pt, 65pt and 90pt tall - are shown top-aligned, centred, and bottom-aligned.",
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
                Text = caption,
                FontName = StandardFonts.HelveticaBold,
                FontSize = 11,
                Color = ColorPalette.Slate[600],
                Margin = new Thickness(0, 8, 0, 4),
            });

            var row = new HorizontalStack
            {
                WidthDistribution = WidthDistribution.Equal,
                Spacing = 10,
                Height = 90,
                Alignment = align,
            };
            foreach ((string label, float boxHeight, Color bg) in boxes)
            {
                row.Add(new Container
                {
                    Child = new TextBlock { Text = label, FontName = StandardFonts.HelveticaBold, FontSize = 11, Color = Color.White },
                    Padding = new Thickness(12, 6, 12, 6),
                    BackgroundColor = bg,
                    Height = boxHeight,
                });
            }
            doc.Add(row);
        }

        // ── Key-value pairs ────────────────────────────────────────────────────

        // Keep the heading with its section: no keep-with-next primitive exists
        // yet, so force the whole block onto the next page with a break.
        doc.Add(new PageBreak());

        doc.Add(SectionHeading("Key-Value Pairs", "s-kv", 16));
        doc.Add(new TextBlock
        {
            Text = "A practical pattern: a narrow bold key column paired with a wider value column using weighted distribution.",
            Margin = new Thickness(0, 0, 0, 8),
        });

        (string key, string value)[] pairs =
        {
            ("Document Title:", "Horizontal Stacks Demo"),
            ("Author:",         "BonniePDF"),
            ("Standard:",       "ISO 14289-1 (PDF/UA-1)"),
            ("Layout Engine:",  "BonniePDF Flow Layout"),
        };

        foreach ((string key, string value) in pairs)
        {
            var row = new HorizontalStack
            {
                WidthDistribution = WidthDistribution.Weighted,
                Spacing = 10,
                Margin = new Thickness(0, 0, 0, 6),
            };
            row.Add(new TextBlock { Text = key, FontName = StandardFonts.HelveticaBold, Color = ColorPalette.Slate[700] }, 1f);
            row.Add(new TextBlock { Text = value, Color = ColorPalette.Slate[800] }, 3f);
            doc.Add(row);
        }

        // ── Two-column text ────────────────────────────────────────────────────

        doc.Add(SectionHeading("Two-Column Text Layout", "s-twocol", 16));
        doc.Add(new TextBlock
        {
            Text = "A newspaper-style two-column layout achieved with an equal-weight HorizontalStack containing two TextBlocks.",
            Margin = new Thickness(0, 0, 0, 8),
        });

        var twoCol = new HorizontalStack
        {
            WidthDistribution = WidthDistribution.Equal,
            Spacing = 20,
        };
        twoCol.Add(new TextBlock
        {
            Text = "The first column contains flowing prose that demonstrates how text wraps within a constrained width. " +
                   "HorizontalStack automatically divides the available page width so each child receives its share. " +
                   "Text inside each column wraps independently, giving a true two-column reading experience.",
        });
        twoCol.Add(new TextBlock
        {
            Text = "The second column runs in parallel with the first. Both columns sit within a single HorizontalStack " +
                   "element, meaning they are rendered side-by-side. This pattern is commonly used for newsletters, " +
                   "product sheets, and comparison tables where parallel content aids comprehension.",
        });
        doc.Add(twoCol);

        return doc;
    }

    /// <summary>A padded, coloured box with a bold white label - the repeated unit above.</summary>
    private static Container Box(string label, Color background, float? height = null) => new()
    {
        Padding = new Thickness(12, 8, 12, 8),
        BackgroundColor = background,
        Height = height,
        Child = new TextBlock
        {
            Text = label,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 11,
            Color = Color.White,
        },
    };

    private static Heading SectionHeading(string text, string tocIdentifier, float topMargin) => new(text)
    {
        TocIdentifier = tocIdentifier,
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 14,
        Color = ColorPalette.Indigo[600],
        Margin = new Thickness(0, topMargin, 0, 8),
    };
}
