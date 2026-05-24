using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
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
                TextColor = Color.Slate600,
                Alignment = TextAlignment.Center
            },

            // Footer template - shows page number
            FooterTemplate = pageNumber => new TextBlock
            {
                Text = $"Page {pageNumber} - Confidential",
                FontName = StandardFonts.Helvetica,
                FontSize = 10,
                TextColor = Color.Slate500,
                Alignment = TextAlignment.Center
            }
        };

        Document doc = new(settings);
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(Color.Slate600);

        doc.Metadata.Title = "Headers and Footers Demo";
        doc.Metadata.Author = "BonniePDF";

        // Add content that will span multiple pages
        doc.Add(new TextBlock
        {
            Text = "Chapter 1: Introduction",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 20,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 15)
        });

        // Generate content for several pages
        for (int section = 1; section <= 8; section++)
        {
            doc.Add(new TextBlock
            {
                Text = $"1.{section} Section Title",
                FontName = StandardFonts.HelveticaBold,
                FontSize = 14,
                TextColor = Color.Slate700,
                Margin = new Thickness(0, 15, 0, 10)
            });

            string paragraphText = section switch
            {
                1 =>
                    "This document demonstrates the header and footer template system in BonniePDF. Headers and footers are automatically rendered on each page after all content has been laid out.",
                2 =>
                    "The header template function receives the page number and returns an IElement that is rendered at the top of each page. This allows for dynamic content like section titles or logos.",
                3 =>
                    "Similarly, the footer template function creates elements for the bottom of each page. Common uses include page numbers, copyright notices, and document dates.",
                4 =>
                    "Templates can contain any valid IElement, including HorizontalStack for multi-column layouts, TextBlocks for simple text, or even Images for logos and branding.",
                5 =>
                    "The page number passed to templates is 1-based, making it easy to display 'Page 1 of N' style pagination. The total page count is available after rendering completes.",
                6 =>
                    "Templates are rendered after all page content is finalized, ensuring accurate page counts and proper positioning regardless of how content flows across pages.",
                7 =>
                    "You can conditionally render headers or footers by returning null from the template function. For example, you might omit the header on the first page.",
                8 =>
                    "This flexible system supports professional document layouts while maintaining the simplicity that makes BonniePDF easy to use.",
                _ => "Additional content section."
            };

            // Add multiple paragraphs per section (uses DefaultTextStyle: Helvetica, 11, Slate600)
            for (int p = 0; p < 3; p++)
                doc.Add(new TextBlock
                {
                    Text = paragraphText + ' ' + paragraphText,
                    Margin = new Thickness(0, 0, 0, 12)
                });
        }
        return doc;
    }
}

