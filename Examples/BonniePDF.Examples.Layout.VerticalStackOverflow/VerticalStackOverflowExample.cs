using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Prototyping;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Layout.VerticalStackOverflow;

/// <summary>
/// Generates a document with a VerticalStack containing many items that flows across pages.
/// </summary>
[System.ComponentModel.DisplayName("VerticalStack Overflow")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description("VerticalStack overflow flow architecture.")]
public static class VerticalStackOverflowExample
{
    
    public static Document Generate()
    {
        Document doc = new();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10).WithColor(ColorPalette.Slate[600]);

        doc.Metadata.Title = "VerticalStack Overflow Demo";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "Demonstrates VerticalStack flowing across page boundaries";

        // Add a title
        doc.Add(new TextBlock
        {
            Text = "Item Catalog",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 10)
        });

        doc.Add(new TextBlock
        {
            Text = "A collection of items demonstrating automatic pagination",
            FontSize = 12,
            TextColor = ColorPalette.Slate[500],
            Margin = new Thickness(0, 0, 0, 20)
        });

        VerticalStack stack = new() { Margin = new Thickness(0) };

        var source = Placeholders.WithSeed(400);
        for (int i = 1; i <= 75; i++)
        {
            VerticalStack itemStack = new()
            {
                Margin = new Thickness(0, 0, 0, 15)
            };

            itemStack.AddChild(new TextBlock
            {
                Text = $"Item #{i:D3}",
                FontName = StandardFonts.HelveticaBold,
                FontSize = 12,
                TextColor = ColorPalette.Blue[600],
                Margin = new Thickness(0, 0, 0, 5)
            });

            itemStack.AddChild(new TextBlock
            {
                Text = source.Sentence()
            });

            stack.AddChild(itemStack);
        }

        doc.Add(stack);
        return doc;
    }
}

