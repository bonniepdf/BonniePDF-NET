using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Layout.TextBlockOverflow;

/// <summary>
/// Generates a document with a long TextBlock that flows across multiple pages using the Fluent API.
/// </summary>
public static class TextBlockOverflowExampleFluent
{
    /// <inheritdoc/>
    /// <inheritdoc/>
    /// <inheritdoc/>
    /// <inheritdoc/>
    /// <inheritdoc/>

    private static readonly string[] handler = new[]
                    {
                        "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.",
                        "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum. Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo.",
                        "Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, adipisci velit, sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam aliquam quaerat voluptatem.",
                        "Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid ex ea commodi consequatur? Quis autem vel eum iure reprehenderit qui in ea voluptate velit esse quam nihil molestiae consequatur, vel illum qui dolorem eum fugiat quo voluptas nulla pariatur?",
                        "At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis praesentium voluptatum deleniti atque corrupti quos dolores et quas molestias excepturi sint occaecati cupiditate non provident, similique sunt in culpa qui officia deserunt mollitia animi, id est laborum et dolorum fuga."
                    };

    /// <summary>
    /// Generates the TextBlock overflow example PDF and saves it to <paramref name="outputPath"/>
    /// using the Fluent API. The document illustrates long text flowing across page boundaries
    /// with a footer template showing page numbers.
    /// </summary>
    /// <param name="outputPath">The full file path where the PDF will be written.</param>
    /// <returns>The path of the generated PDF file.</returns>
    public static string Generate(string outputPath)
    {
        Pdf.Create(doc =>
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

                    // Generate a long text that will overflow across pages
                    string[] paragraphs = handler;

                    // Repeat paragraphs to create enough content for multiple pages
                    string longText = string.Join("\n\n", Enumerable.Range(1, 20).Select(i =>
                        $"Section {i}\n\n{paragraphs[i % paragraphs.Length]}\n\n{paragraphs[(i + 1) % paragraphs.Length]}"));

                    column.Item()
                        .Text(longText)
                        .TextColor(Color.Slate700);
                });
            });
        }).Save(outputPath);

        return outputPath;
    }
}
