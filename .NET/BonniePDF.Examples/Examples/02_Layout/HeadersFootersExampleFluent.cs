using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Extensions;
using BonniePDF.Typography;
using BonniePDF.Examples.Core;

namespace BonniePDF.Examples.Examples._02_Layout;

/// <summary>
/// Generates a document with header and footer templates on each page using the Fluent API.
/// </summary>
public class HeadersFootersExampleFluent : ExampleBase
{
    /// <inheritdoc/>
    public override string Id => "HeadersFootersFluent";
    /// <inheritdoc/>
    public override string Title => "Headers & Footers (Fluent API)";
    /// <inheritdoc/>
    public override string Description => "Page template headers and footers using the Fluent API.";
    /// <inheritdoc/>
    public override string FileName => "headers_footers_fluent.pdf";
    /// <inheritdoc/>
    public override ExampleCategory Category => ExampleCategory.Layout;

    /// <summary>
    /// Generates the headers and footers example PDF and saves it to <paramref name="outputPath"/>
    /// using the Fluent API. The document illustrates header and footer templates rendered on
    /// each page across multi-page content.
    /// </summary>
    /// <param name="outputPath">The full file path where the PDF will be written.</param>
    /// <returns>The path of the generated PDF file.</returns>
    public override string Generate(string outputPath)
    {
        Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title = "Headers and Footers Demo";
                metadata.Author = "BonniePDF";
            });

            doc.WithSettings(settings =>
            {
                settings.PageSize = PdfSize.A4;
                settings.Margin = 60;
                settings.DefaultZoom = ZoomLevel.FitPage;
                settings.Version = PdfVersion.Pdf17;
                settings.PdfAEnabled = true;

                // Header/footer templates are rendered during save, outside the Page() scope,
                // so they don't inherit the document default style — explicit font calls needed.
                settings.Header((pageNumber, header) =>
                {
                    header.Text($"BonniePDF Documentation - Page {pageNumber}")
                          .Bold()
                          .FontSize(10)
                          .TextColor(Color.Slate600)
                          .AlignCenter();
                });

                settings.Footer((pageNumber, footer) =>
                {
                    footer.Text($"Page {pageNumber} - Confidential")
                          .FontSize(10)
                          .TextColor(Color.Slate500)
                          .AlignCenter();
                });
            });

            doc.WithDefaultTextStyle(style => style.WithFamily(StandardFonts.Helvetica).WithSize(11));

            doc.Page(page =>
            {
                page.Content().Column(column =>
                {
                    column.Item()
                        .Text("Chapter 1: Introduction")
                        .Bold()
                        .FontSize(20)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 15);

                    // Generate content that will span multiple pages
                    for (int section = 1; section <= 8; section++)
                    {
                        column.Item()
                            .Text($"1.{section} Section Title")
                            .Bold()
                            .FontSize(14)
                            .TextColor(Color.Slate700)
                            .Margin(0, 15, 0, 10);

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

                        // Add multiple paragraphs per section
                        for (int p = 0; p < 3; p++)
                            column.Item()
                                .Text(paragraphText + ' ' + paragraphText)
                                .TextColor(Color.Slate600)
                                .Margin(0, 0, 0, 12);
                    }
                });
            });
        }).Save(outputPath);

        return outputPath;
    }
}
