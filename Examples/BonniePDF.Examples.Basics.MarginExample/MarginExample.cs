using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Basics.Margins;

/// <summary>
/// Demonstrates the margin feature for controlling spacing around elements
/// </summary>
[System.ComponentModel.DisplayName("Margins & Units")]
[System.ComponentModel.Category("Basics")]
[System.ComponentModel.Description("Shows how to configure page margins and use different units.")]
public static class MarginExample
{
    public static Document Generate()
    {
        Document doc = new();

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "Margin Example";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "Demonstrating margin support for elements";

        // Title with bottom margin
        doc.Add(new TextBlock
        {
            Text = "Margin Example",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 20) // Bottom margin of 20pt
        });

        // Paragraph with margin on all sides
        doc.Add(new TextBlock
        {
            Text =
                "This paragraph has a 10pt margin on all sides. Margins create space around elements, helping to visually separate content and improve readability.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate600,
            Margin = new Thickness(10) // 10pt margin on all sides
        });

        // Container with background color and margin
        doc.Add(new Container
        {
            Margin = new Thickness(0, 10, 0, 10), // Top and bottom margin
            Padding = new Thickness(15),
            BackgroundColor = Color.Blue100,
            Border = new Border(1),
            BorderColors = new BorderColors(Color.Blue500),
            Child = new TextBlock
            {
                Text =
                    "This container has a 10pt top and bottom margin, plus 15pt padding inside. Notice how the margin creates space outside the border, while padding creates space inside.",
                FontName = StandardFonts.Helvetica,
                FontSize = 11,
                TextColor = Color.Blue900
            }
        });

        // Section header with top margin
        doc.Add(new TextBlock
        {
            Text = "Image with Margin",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 20, 0, 10) // 20pt top, 10pt bottom
        });

        doc.Add(new TextBlock
        {
            Text = "Images can also have margins. The image below has a 20pt margin on all sides:",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600,
            Margin = new Thickness(0, 0, 0, 10)
        });

        // Create a simple colored rectangle as a placeholder "image"
        doc.Add(new Container
        {
            Margin = new Thickness(20), // 20pt margin on all sides
            BackgroundColor = Color.Amber400,
            Child = new TextBlock
            {
                Text = "Image Placeholder (100x60)",
                FontName = StandardFonts.Helvetica,
                FontSize = 10,
                TextColor = Color.Amber900,
                Alignment = TextAlignment.Center
            },
            Padding = new Thickness(10, 25, 10, 25)
        });

        // Table with margin
        doc.Add(new TextBlock
        {
            Text = "Table with Margin",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 20, 0, 10)
        });

        Table table = new(1, 2)
        {
            Margin = new Thickness(10, 0, 10, 0), // Left and right margin
            CellPadding = 8,
            Border = new Border(1),
            BorderColors = new BorderColors(Color.Slate300),
            HeaderBackgroundColor = Color.Slate200,
            HeaderTextColor = Color.Slate800
        };

        table.AddHeader(
            new TextBlock { Text = "Property", FontName = StandardFonts.HelveticaBold, FontSize = 11 },
            new TextBlock { Text = "Description", FontName = StandardFonts.HelveticaBold, FontSize = 11 }
        );

        table.AddRow(
            new TextBlock { Text = "Margin.Top", FontName = StandardFonts.Helvetica, FontSize = 10 },
            new TextBlock { Text = "Space above the element", FontName = StandardFonts.Helvetica, FontSize = 10 }
        );

        table.AddRow(
            new TextBlock { Text = "Margin.Bottom", FontName = StandardFonts.Helvetica, FontSize = 10 },
            new TextBlock { Text = "Space below the element", FontName = StandardFonts.Helvetica, FontSize = 10 }
        );

        table.AddRow(
            new TextBlock { Text = "Margin.Left", FontName = StandardFonts.Helvetica, FontSize = 10 },
            new TextBlock
                { Text = "Space to the left of the element", FontName = StandardFonts.Helvetica, FontSize = 10 }
        );

        table.AddRow(
            new TextBlock { Text = "Margin.Right", FontName = StandardFonts.Helvetica, FontSize = 10 },
            new TextBlock
                { Text = "Space to the right of the element", FontName = StandardFonts.Helvetica, FontSize = 10 }
        );

        doc.Add(table);

        // VerticalStack with margin
        doc.Add(new TextBlock
        {
            Text = "VerticalStack with Margin",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 20, 0, 10)
        });

        VerticalStack stack = new()
        {
            Margin = new Thickness(20, 10, 20, 10) // 20pt horizontal, 10pt vertical
        };

        stack.AddChild(new Container
        {
            BackgroundColor = Color.Green100,
            Padding = new Thickness(10),
            Child = new TextBlock
            {
                Text = "Item 1 in stack",
                FontName = StandardFonts.Helvetica,
                FontSize = 10,
                TextColor = Color.Green800
            }
        });

        stack.AddChild(new Container
        {
            BackgroundColor = Color.Purple100,
            Padding = new Thickness(10),
            Margin = new Thickness(0, 5, 0, 0), // 5pt top margin
            Child = new TextBlock
            {
                Text = "Item 2 in stack (with top margin)",
                FontName = StandardFonts.Helvetica,
                FontSize = 10,
                TextColor = Color.Purple800
            }
        });

        stack.AddChild(new Container
        {
            BackgroundColor = Color.Red100,
            Padding = new Thickness(10),
            Margin = new Thickness(0, 5, 0, 0), // 5pt top margin
            Child = new TextBlock
            {
                Text = "Item 3 in stack (with top margin)",
                FontName = StandardFonts.Helvetica,
                FontSize = 10,
                TextColor = Color.Red800
            }
        });

        doc.Add(stack);

        // Final note
        doc.Add(new TextBlock
        {
            Text = "Note: Margins collapse with adjacent content, creating clean spacing throughout the document.",
            FontName = StandardFonts.HelveticaOblique,
            FontSize = 10,
            TextColor = Color.Slate500,
            Margin = new Thickness(0, 30, 0, 0)
        });
        return doc;
    }
}

