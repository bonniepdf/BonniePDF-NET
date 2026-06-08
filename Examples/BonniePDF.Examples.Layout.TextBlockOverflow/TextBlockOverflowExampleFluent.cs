using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Prototyping;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Layout.TextBlockOverflow;

/// <summary>
/// Generates a document with a long TextBlock that flows across multiple pages using the Fluent API.
/// </summary>
[System.ComponentModel.DisplayName("TextBlock Overflow (Fluent API)")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description("TextBlock overflow flow architecture using the Fluent API.")]
public static class TextBlockOverflowExampleFluent
{
    /// <summary>
    /// Generates the TextBlock overflow example PDF and saves it to <paramref name="outputPath"/>
    /// using the Fluent API. The document illustrates long text flowing across page boundaries
    /// with a footer template showing page numbers.
    /// </summary>
    /// <param name="outputPath">The full file path where the PDF will be written.</param>
    /// <returns>The path of the generated PDF file.</returns>
    public static Document Generate()
    {
        return Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title = "TextBlock Overflow Demo";
                metadata.Author = "BonniePDF";
                metadata.Subject = "Demonstrates text flowing across page boundaries";
            });

            doc.WithSettings(settings =>
            {
                settings.Version = PdfVersion.Pdf17;
                settings.PdfAEnabled = true;

                settings.Footer((pageNumber, footer) =>
                {
                    footer.Text($"Page {pageNumber} of {TextElementBase.TotalPagesToken}")
                          .FontSize(10)
                          .TextColor(Color.Slate500)
                          .AlignRight();
                });
            });

            doc.WithDefaultTextStyle(style => style.WithFamily(StandardFonts.Helvetica).WithSize(11));

            doc.Page(page =>
            {
                page.Content().Column(column =>
                {
                    // Title
                    column.Item()
                        .Text("Long Text Document")
                        .Bold()
                        .FontSize(24)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 20);

                    var source = Placeholders.WithSeed(500);
                    string longText = string.Join("\n\n", Enumerable.Range(1, 20).Select(i =>
                        $"Section {i}\n\n{source.Paragraph()}\n\n{source.Paragraph()}"));

                    column.Item()
                        .Text(longText)
                        .TextColor(Color.Slate700);
                });
            });
        }).Build();

    }
}

