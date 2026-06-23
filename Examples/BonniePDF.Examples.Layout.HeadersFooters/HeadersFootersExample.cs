using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Prototyping;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Layout.HeadersFooters;

/// <summary>
/// Generates a document with header and footer templates on each page.
/// </summary>
[System.ComponentModel.DisplayName("Headers & Footers")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description("Page template headers and footers.")]
public static class HeadersFootersExample
{
    public static Document Generate()
    {
        // Create document with custom settings
        DocumentSettings settings = new()
        {
            PageSize = PdfSize.A4,
            Margin = 60,
            DefaultZoom = ZoomLevel.FitPage,

            // Header template - shows document title
            HeaderTemplate = pageNumber => new TextBlock
            {
                Text = $"BonniePDF Documentation - Page {pageNumber}",
                FontName = StandardFonts.HelveticaBold,
                FontSize = 10,
                TextColor = ColorPalette.Slate[600],
                Alignment = TextAlignment.Center
            },

            // Footer template - shows page number
            FooterTemplate = pageNumber => new TextBlock
            {
                Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken} - Confidential",
                FontName = StandardFonts.Helvetica,
                FontSize = 10,
                TextColor = ColorPalette.Slate[500],
                Alignment = TextAlignment.Center
            }
        };

        Document doc = new(settings);
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[600]);

        doc.Metadata.Title = "Headers and Footers Demo";
        doc.Metadata.Author = "BonniePDF";

        // Add content that will span multiple pages
        doc.Add(new TextBlock
        {
            Text = "Chapter 1: Introduction",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 20,
            TextColor = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 15)
        });

        var source = Placeholders.WithSeed(200);

        for (int section = 1; section <= 8; section++)
        {
            doc.Add(new TextBlock
            {
                Text = $"1.{section} Section Title",
                FontName = StandardFonts.HelveticaBold,
                FontSize = 14,
                TextColor = ColorPalette.Slate[600],
                Margin = new Thickness(0, 15, 0, 10)
            });

            for (int p = 0; p < 3; p++)
                doc.Add(new TextBlock
                {
                    Text = source.Paragraph(),
                    Margin = new Thickness(0, 0, 0, 12)
                });
        }
        return doc;
    }
}

