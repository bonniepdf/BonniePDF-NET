using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Prototyping;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Layout.TextBlockOverflow;

/// <summary>
/// Generates a document with a long TextBlock that flows across multiple pages.
/// </summary>
[System.ComponentModel.DisplayName("TextBlock Overflow")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description("TextBlock overflow flow architecture.")]
public static class TextBlockOverflowExample
{
    
    public static Document Generate()
    {
        Document doc = new();
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[600]);
        doc.Settings.FooterTemplate = pageNumber => new TextBlock
        {
            Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken}",
            FontName = StandardFonts.Helvetica,
            FontSize = 10,
            TextColor = ColorPalette.Slate[500],
            Alignment = TextAlignment.Right
        };

        doc.Metadata.Title = "TextBlock Overflow Demo";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "Demonstrates text flowing across page boundaries";

        // Add a title
        doc.Add(new TextBlock
        {
            Text = "Long Text Document",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 20)
        });

        var source = Placeholders.WithSeed(500);
        string longText = string.Join("\n\n", Enumerable.Range(1, 20).Select(i =>
            $"Section {i}\n\n{source.Paragraph()}\n\n{source.Paragraph()}"));

        // Uses DefaultTextStyle: Helvetica, 11, Slate700
        doc.Add(new TextBlock
        {
            Text = longText
        });
        return doc;
    }
}

