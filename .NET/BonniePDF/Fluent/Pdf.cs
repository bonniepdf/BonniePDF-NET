using System;

namespace BonniePDF.Fluent;

/// <summary>
/// Entry point for the BonniePDF fluent API.
/// Provides a static factory method for creating PDF documents with a fluent, chainable syntax.
/// </summary>
public static class Pdf
{
    /// <summary>
    /// Creates a new PDF document using the fluent API.
    /// </summary>
    /// <param name="handler">A delegate that configures the document using the fluent API.</param>
    /// <returns>A PdfBuilder that can be saved to a file, stream, or byte array.</returns>
    /// <example>
    /// <code>
    /// Pdf.Create(doc =>
    /// {
    ///     doc.Page(page =>
    ///     {
    ///         page.Size(PdfSize.A4);
    ///         page.Margin(40);
    ///         page.Content().Column(col =>
    ///         {
    ///             col.Item().Text("Hello World").FontSize(24).Bold();
    ///         });
    ///     });
    /// }).Save("hello.pdf");
    /// </code>
    /// </example>
    public static PdfBuilder Create(Action<PdfBuilder> handler)
    {
        var builder = new PdfBuilder();
        handler(builder);
        return builder;
    }
}
