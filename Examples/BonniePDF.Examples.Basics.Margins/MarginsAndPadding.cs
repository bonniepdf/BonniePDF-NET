using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Basics.Margins;

/// <summary>
/// Demonstrates the margin and padding features for controlling spacing around and inside elements
/// </summary>
[System.ComponentModel.DisplayName("Margins and Padding")]
[System.ComponentModel.Category("Basics")]
[System.ComponentModel.Description("Shows how margins space elements apart and padding spaces content inside them.")]
public static class MarginsAndPadding
{
    public static Document Generate()
    {
        Document doc = new();

        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[600]);

        doc.Metadata.Title = "Margins and Padding";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "Demonstrating margin and padding support for elements";

        // H1 document title - gives assistive technology a navigable outline.
        doc.Add(new Heading("Margins and Padding")
        {
            TocIdentifier = "title",
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 20,
            TextColor = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 6)
        });

        doc.Add(new TextBlock
        {
            Text =
                "Margin is the space outside an element; padding is the space inside it, between the border and the content. " +
                "The sections below show both applied to text, containers, tables, and stacks.",
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 6)
        });

        // H2 - the side-by-side comparison that anchors the whole example. Both boxes
        // end up with an identical-looking gutter, but one gutter is the OUTER box's
        // padding and the other is the INNER box's margin - same spacing, different owner.
        doc.Add(SectionHeading("Margin vs. padding, side by side"));

        HorizontalStack comparison = new()
        {
            WidthDistribution = WidthDistribution.Equal,
            Spacing = 16
        };
        comparison.AddChild(ComparisonColumn(
            "Padding on the outer box",
            "The blue band is 16pt of padding: space the container reserves inside its own border.",
            // Outer box owns the space via padding; the inner content sits on a
            // contrasting fill so the padding band is actually visible.
            PaddingDemo()));
        comparison.AddChild(ComparisonColumn(
            "Margin on the inner box",
            "The blue band is 16pt of margin: space the inner element holds outside itself.",
            // Outer box has no padding; the inner element pushes itself away from the
            // border with its own margin, producing the same gutter a different way.
            MarginDemo()));
        doc.Add(comparison);

        // H2 - margin on a paragraph.
        doc.Add(SectionHeading("Margin on all sides"));

        doc.Add(new TextBlock
        {
            Text =
                "This paragraph has a 10pt margin on all sides. Margins create space around elements, helping to visually separate content and improve readability.",
            FontSize = 11,
            Margin = new Thickness(10) // 10pt margin on all sides
        });

        // H2 - margin on an image placeholder.
        doc.Add(SectionHeading("Image with margin"));

        doc.Add(new TextBlock
        {
            Text = "Images can also have margins. The 100x60 placeholder below has a 20pt margin on all sides:",
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Create a simple colored rectangle as a placeholder "image"
        doc.Add(new Container
        {
            Margin = new Thickness(20), // 20pt margin on all sides
            Width = 100,  // Fixed size so the label's stated dimensions are honest.
            Height = 60,
            BackgroundColor = ColorPalette.Amber[400],
            Child = new TextBlock
            {
                Text = "Image 100x60",
                FontName = StandardFonts.Helvetica,
                FontSize = 10,
                TextColor = ColorPalette.Amber[900],
                Alignment = TextAlignment.Center
            },
            Padding = new Thickness(6)
        });

        // H2 - margin on a table.
        doc.Add(SectionHeading("Table with margin"));

        Table table = new(1, 2)
        {
            Margin = new Thickness(10, 0, 10, 0), // Left and right margin
            CellPadding = 6,
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Slate[300]),
            HeaderBackgroundColor = ColorPalette.Slate[200],
            HeaderTextColor = ColorPalette.Slate[800]
        };

        table.AddHeader(
            new TextBlock { Text = "Property", FontName = StandardFonts.HelveticaBold, FontSize = 11 },
            new TextBlock { Text = "Description", FontName = StandardFonts.HelveticaBold, FontSize = 11 }
        );

        table.AddRow(
            new TextBlock { Text = "Margin.Top", FontSize = 10 },
            new TextBlock { Text = "Space above the element", FontSize = 10 }
        );

        table.AddRow(
            new TextBlock { Text = "Margin.Bottom", FontSize = 10 },
            new TextBlock { Text = "Space below the element", FontSize = 10 }
        );

        table.AddRow(
            new TextBlock { Text = "Margin.Left", FontSize = 10 },
            new TextBlock { Text = "Space to the left of the element", FontSize = 10 }
        );

        table.AddRow(
            new TextBlock { Text = "Margin.Right", FontSize = 10 },
            new TextBlock { Text = "Space to the right of the element", FontSize = 10 }
        );

        doc.Add(table);

        // H2 - margin on a vertical stack.
        doc.Add(SectionHeading("VerticalStack with margin"));

        VerticalStack stack = new()
        {
            Margin = new Thickness(20, 6, 20, 6) // 20pt horizontal, 6pt vertical
        };

        stack.AddChild(new Container
        {
            BackgroundColor = ColorPalette.Green[100],
            Padding = new Thickness(8),
            Child = new TextBlock
            {
                Text = "Item 1 in stack",
                FontSize = 10,
                TextColor = ColorPalette.Green[800]
            }
        });

        stack.AddChild(new Container
        {
            BackgroundColor = ColorPalette.Purple[100],
            Padding = new Thickness(8),
            Margin = new Thickness(0, 5, 0, 0), // 5pt top margin
            Child = new TextBlock
            {
                Text = "Item 2 in stack (with top margin)",
                FontSize = 10,
                TextColor = ColorPalette.Purple[800]
            }
        });

        stack.AddChild(new Container
        {
            BackgroundColor = ColorPalette.Red[100],
            Padding = new Thickness(8),
            Margin = new Thickness(0, 5, 0, 0), // 5pt top margin
            Child = new TextBlock
            {
                Text = "Item 3 in stack (with top margin)",
                FontSize = 10,
                TextColor = ColorPalette.Red[800]
            }
        });

        doc.Add(stack);

        // Closing note - no top margin so the example stays on a single page.
        doc.Add(new TextBlock
        {
            Text =
                "Note: adjacent margins stack additively - this engine does not collapse them the way CSS does, " +
                "so a 12pt bottom margin above an 8pt top margin leaves a 20pt gap.",
            FontName = StandardFonts.HelveticaOblique,
            FontSize = 10,
            TextColor = ColorPalette.Slate[500]
        });
        return doc;
    }

    /// <summary>Builds a level-2 section heading with the shared style used across this example.</summary>
    private static Heading SectionHeading(string text) => new(text)
    {
        TocIdentifier = text,
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 14,
        TextColor = ColorPalette.Slate[700],
        Margin = new Thickness(0, 8, 0, 4)
    };

    /// <summary>A comparison column: a demo box above a caption, stacked vertically.</summary>
    private static VerticalStack ComparisonColumn(string heading, string caption, Container demo)
    {
        VerticalStack column = new() { Spacing = 4 };
        column.AddChild(new TextBlock
        {
            Text = heading,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 10,
            TextColor = ColorPalette.Slate[700]
        });
        column.AddChild(demo);
        column.AddChild(new TextBlock
        {
            Text = caption,
            FontSize = 9,
            TextColor = ColorPalette.Slate[500]
        });
        return column;
    }

    /// <summary>The inner "content" chip shown inside both comparison boxes.</summary>
    private static Container ContentChip() => new()
    {
        BackgroundColor = ColorPalette.Slate[700],
        Padding = new Thickness(6),
        Child = new TextBlock
        {
            Text = "content",
            FontSize = 10,
            TextColor = ColorPalette.Slate[50],
            Alignment = TextAlignment.Center
        }
    };

    /// <summary>Space created by the OUTER box's padding (the blue band is padding).</summary>
    private static Container PaddingDemo() => new()
    {
        BackgroundColor = ColorPalette.Blue[200],
        Border = new Border(1),
        BorderColors = new BorderColors(ColorPalette.Blue[500]),
        Padding = new Thickness(16), // The gutter you see IS this padding.
        Child = ContentChip()
    };

    /// <summary>Space created by the INNER element's margin (the blue band is margin).</summary>
    private static Container MarginDemo()
    {
        Container inner = ContentChip();
        inner.Margin = new Thickness(16); // The gutter you see IS this margin.
        return new Container
        {
            BackgroundColor = ColorPalette.Blue[200],
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Blue[500]),
            Padding = new Thickness(0), // No padding: the inner element spaces itself.
            Child = inner
        };
    }
}
