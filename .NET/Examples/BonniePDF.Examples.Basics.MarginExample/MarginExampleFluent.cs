using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Demo;

/// <summary>
/// Demonstrates the margin feature using the Fluent API for controlling spacing around elements
/// </summary>
public static class MarginExampleFluent
{
    /// <inheritdoc/>
    /// <inheritdoc/>
    /// <inheritdoc/>
    /// <inheritdoc/>
    /// <inheritdoc/>

    /// <summary>
    /// Generates the margins example PDF and saves it to <paramref name="outputPath"/>.
    /// The document illustrates top/bottom/left/right margin control on text, containers,
    /// images, tables, and stacked layouts using the Fluent API.
    /// </summary>
    /// <param name="outputPath">The full file path where the PDF will be written.</param>
    /// <returns>The path of the generated PDF file.</returns>
    public static string Generate(string outputPath)
    {
        Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title = "Margin Example (Fluent API)";
                metadata.Author = "BonniePDF";
                metadata.Subject = "Demonstrating margin support using the Fluent API";
            });

            doc.WithSettings(settings =>
            {
                settings.Version = PdfVersion.Pdf17;
                settings.PdfAEnabled = true;
            });

            // Document-wide default: all text inherits Helvetica 11pt
            doc.WithDefaultTextStyle(style => style.WithFamily(StandardFonts.Helvetica).WithSize(11));

            doc.Page(page =>
            {
                page.Content().Column(column =>
                {
                    // Title — only override size and weight (font family inherited from default)
                    column.Item()
                        .Text("Margin Example (Fluent API)")
                        .Bold()
                        .FontSize(24)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 20); // Bottom margin of 20pt

                    // Paragraph with margin on all sides
                    column.Item()
                        .Text("This paragraph has a 10pt margin on all sides. Margins create space around elements, helping to visually separate content and improve readability.")
                        .FontSize(12)
                        .TextColor(Color.Slate600)
                        .Margin(10); // 10pt margin on all sides

                    // Container with background color and margin
                    column.Item()
                        .Margin(0, 10, 0, 10) // Top and bottom margin
                        .Padding(15)
                        .Background(Color.Blue100)
                        .Border(1, Color.Blue500)
                        .Text("This container has a 10pt top and bottom margin, plus 15pt padding inside. Notice how the margin creates space outside the border, while padding creates space inside.")
                        .TextColor(Color.Blue900);

                    // Section header with top margin
                    column.Item()
                        .Text("Image with Margin")
                        .Bold()
                        .FontSize(16)
                        .TextColor(Color.Slate700)
                        .Margin(0, 20, 0, 10); // 20pt top, 10pt bottom

                    column.Item()
                        .Text("Images can also have margins. The image below has a 20pt margin on all sides:")
                        .TextColor(Color.Slate600)
                        .Margin(0, 0, 0, 10);

                    // Create a simple colored rectangle as a placeholder "image"
                    column.Item()
                        .Margin(20) // 20pt margin on all sides
                        .Background(Color.Amber400)
                        .Padding(10, 25, 10, 25)
                        .Text("Image Placeholder (100x60)")
                        .FontSize(10)
                        .TextColor(Color.Amber900)
                        .AlignCenter();

                    // Table with margin
                    column.Item()
                        .Text("Table with Margin")
                        .Bold()
                        .FontSize(16)
                        .TextColor(Color.Slate700)
                        .Margin(0, 20, 0, 10);

                    column.Item()
                        .Margin(10, 0, 10, 0) // Left and right margin
                        .Table(table =>
                        {
                            table.Columns(1f, 2f);
                            table.CellPadding(8);
                            table.Border(1, Color.Slate300);
                            table.HeaderBackground(Color.Slate200);
                            table.HeaderTextColor(Color.Slate800);

                            table.Header(row =>
                            {
                                row.Cell().Text("Property").Bold();
                                row.Cell().Text("Description").Bold();
                            });

                            // Row 1
                            table.Cell().Text("Margin.Top").FontSize(10);
                            table.Cell().Text("Space above the element").FontSize(10);

                            // Row 2
                            table.Cell().Text("Margin.Bottom").FontSize(10);
                            table.Cell().Text("Space below the element").FontSize(10);

                            // Row 3
                            table.Cell().Text("Margin.Left").FontSize(10);
                            table.Cell().Text("Space to the left of the element").FontSize(10);

                            // Row 4
                            table.Cell().Text("Margin.Right").FontSize(10);
                            table.Cell().Text("Space to the right of the element").FontSize(10);
                        });

                    // VerticalStack with margin
                    column.Item()
                        .Text("VerticalStack with Margin")
                        .Bold()
                        .FontSize(16)
                        .TextColor(Color.Slate700)
                        .Margin(0, 20, 0, 10);

                    column.Item()
                        .Margin(20, 10, 20, 10) // 20pt horizontal, 10pt vertical
                        .Column(stack =>
                        {
                            stack.Item()
                                .Background(Color.Green100)
                                .Padding(10)
                                .Text("Item 1 in stack")
                                .FontSize(10)
                                .TextColor(Color.Green800);

                            stack.Item()
                                .Margin(0, 5, 0, 0) // 5pt top margin
                                .Background(Color.Purple100)
                                .Padding(10)
                                .Text("Item 2 in stack (with top margin)")
                                .FontSize(10)
                                .TextColor(Color.Purple800);

                            stack.Item()
                                .Margin(0, 5, 0, 0) // 5pt top margin
                                .Background(Color.Red100)
                                .Padding(10)
                                .Text("Item 3 in stack (with top margin)")
                                .FontSize(10)
                                .TextColor(Color.Red800);
                        });

                    // Final note — Italic overrides the default weight
                    column.Item()
                        .Text("Note: Margins collapse with adjacent content, creating clean spacing throughout the document.")
                        .Italic()
                        .FontSize(10)
                        .TextColor(Color.Slate500)
                        .Margin(0, 30, 0, 0);
                });
            });
        }).Save(outputPath);

        return outputPath;
    }
}
