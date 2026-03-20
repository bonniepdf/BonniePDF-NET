using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Layout.VerticalStackOverflow;

/// <summary>
/// Generates a document with a VerticalStack containing many items that flows across pages
/// using the Fluent API.
/// </summary>
[System.ComponentModel.DisplayName("VerticalStack Overflow (Fluent API)")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description("VerticalStack overflow flow architecture using the Fluent API.")]
public static class VerticalStackOverflowExampleFluent
{
    /// <inheritdoc/>
    /// <inheritdoc/>
    /// <inheritdoc/>
    /// <inheritdoc/>
    /// <inheritdoc/>

    /// <summary>
    /// Generates the VerticalStack overflow example PDF and saves it to <paramref name="outputPath"/>
    /// using the Fluent API. The document illustrates 75 card-style items flowing across page
    /// boundaries via automatic pagination.
    /// </summary>
    /// <param name="outputPath">The full file path where the PDF will be written.</param>
    /// <returns>The path of the generated PDF file.</returns>
    public static string Generate(string outputPath)
    {
        Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title = "VerticalStack Overflow Demo";
                metadata.Author = "BonniePDF";
                metadata.Subject = "Demonstrates VerticalStack flowing across page boundaries";
            });

            doc.WithSettings(settings =>
            {
                settings.Version = PdfVersion.Pdf17;
                settings.PdfAEnabled = true;
            });

            doc.WithDefaultTextStyle(style => style.WithFamily(StandardFonts.Helvetica));

            doc.Page(page =>
            {
                page.Content().Column(column =>
                {
                    // Title
                    column.Item()
                        .Text("Item Catalog")
                        .Bold()
                        .FontSize(24)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 10);

                    column.Item()
                        .Text("A collection of items demonstrating automatic pagination")
                        .FontSize(12)
                        .TextColor(Color.Slate500)
                        .Margin(0, 0, 0, 20);

                    // 75 card-style items; the outer VerticalStack (Margin=0) maps to the
                    // page column directly — each item card is added as a column item.
                    for (int i = 1; i <= 75; i++)
                    {
                        int capturedI = i; // capture loop variable for lambda safety
                        column.Item()
                            .Margin(0, 0, 0, 15)
                            .Column(itemStack =>
                            {
                                itemStack.Item()
                                    .Text($"Item #{capturedI:D3}")
                                    .Bold()
                                    .FontSize(12)
                                    .TextColor(Color.Blue600)
                                    .Margin(0, 0, 0, 5);

                                itemStack.Item()
                                    .Text($"This is the description for item {capturedI}. It contains some details about what this item represents and why it's important.")
                                    .FontSize(10)
                                    .TextColor(Color.Slate600);
                            });
                    }
                });
            });
        }).Save(outputPath);

        return outputPath;
    }
}
