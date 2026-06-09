using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Prototyping;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Layout.HeadersFooters;

/// <summary>
/// Generates a document with header and footer templates on each page using the Fluent API.
/// </summary>
[System.ComponentModel.DisplayName("Headers & Footers (Fluent API)")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description("Page template headers and footers using the Fluent API.")]
public static class HeadersFootersExampleFluent
{
    /// <inheritdoc/>
    /// <inheritdoc/>
    /// <inheritdoc/>
    /// <inheritdoc/>
    /// <inheritdoc/>

    /// <summary>
    /// Generates the headers and footers example PDF and saves it to <paramref name="outputPath"/>
    /// using the Fluent API. The document illustrates header and footer templates rendered on
    /// each page across multi-page content.
    /// </summary>
    /// <param name="outputPath">The full file path where the PDF will be written.</param>
    /// <returns>The path of the generated PDF file.</returns>
    public static Document Generate()
    {
        return Pdf.Create(doc =>
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
                // so they don't inherit the document default style - explicit font calls needed.
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

                    var source = Placeholders.WithSeed(200);
                    for (int section = 1; section <= 8; section++)
                    {
                        column.Item()
                            .Text($"1.{section} Section Title")
                            .Bold()
                            .FontSize(14)
                            .TextColor(Color.Slate700)
                            .Margin(0, 15, 0, 10);

                        for (int p = 0; p < 3; p++)
                            column.Item()
                                .Text(source.Paragraph())
                                .TextColor(Color.Slate600)
                                .Margin(0, 0, 0, 12);
                    }
                });
            });
        }).Build();

    }
}

