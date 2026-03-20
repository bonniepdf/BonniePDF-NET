using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Typography.TextDecorations;

/// <summary>
/// Demonstrates text decorations (underline and strikethrough) using the Fluent API.
/// All sections use chained extension methods — no property-initialiser syntax.
/// </summary>
[System.ComponentModel.DisplayName("Text Decorations (Fluent API)")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("Underlines, strikethroughs, and text backgrounds using the Fluent API.")]
public static class TextDecorationsFluentExample
{

    public static string Generate(string outputPath)
    {
        Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title = "Text Decorations (Fluent)";
                metadata.Author = "BonniePDF";
            });

            doc.WithSettings(settings =>
            {
                settings.PageSize = PdfSize.A4;
                settings.Margin = 60;
                settings.Version = PdfVersion.Pdf17;
                settings.PdfAEnabled = true;
            });

            // Helvetica 12pt as document default — section headers override size only
            doc.WithDefaultTextStyle(style => style
                .WithFamily(StandardFonts.Helvetica)
                .WithSize(12));

            doc.Page(page =>
            {
                page.Content().Column(column =>
                {
                    // Document title
                    column.Item()
                        .Text("Text Decorations (Fluent API)")
                        .Bold()
                        .FontSize(24)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 20);

                    // ── Underline ────────────────────────────────────────────────
                    SectionHeader(column, "Underline");

                    column.Item()
                        .Text("This text has an underline decoration applied.")
                        .TextColor(Color.Slate700)
                        .Underline()
                        .Margin(0, 0, 0, 12);

                    column.Item()
                        .Text("This underline was applied using the fluent .Underline() extension method.")
                        .TextColor(Color.Slate700)
                        .Underline()
                        .Margin(0, 0, 0, 12);

                    // ── Strikethrough ────────────────────────────────────────────
                    SectionHeader(column, "Strikethrough");

                    column.Item()
                        .Text("This text has a strikethrough decoration, useful for showing deleted or superseded content.")
                        .TextColor(Color.Slate700)
                        .Strikethrough()
                        .Margin(0, 0, 0, 12);

                    column.Item()
                        .Text("This strikethrough was applied using the fluent .Strikethrough() extension method.")
                        .TextColor(Color.Slate700)
                        .Strikethrough()
                        .Margin(0, 0, 0, 12);

                    // ── Combined decorations ─────────────────────────────────────
                    SectionHeader(column, "Combined Decorations");

                    column.Item()
                        .Text("This text has both underline and strikethrough applied simultaneously.")
                        .TextColor(Color.Slate700)
                        .Underline()
                        .Strikethrough()
                        .Margin(0, 0, 0, 12);

                    column.Item()
                        .Text("Chained decorations are additive: .Underline().Strikethrough() — both apply.")
                        .TextColor(Color.Slate700)
                        .Underline()
                        .Strikethrough()
                        .Margin(0, 0, 0, 12);

                    // ── Decorations with different alignments ────────────────────
                    column.Item().Margin(0, 4, 0, 4).Line(1, Color.Slate300);
                    SectionHeader(column, "Decorations with Different Alignments");

                    column.Item()
                        .Text("Left-aligned underlined text")
                        .TextColor(Color.Slate700)
                        .Underline()
                        .Margin(0, 0, 0, 8);

                    column.Item()
                        .Text("Center-aligned underlined text")
                        .TextColor(Color.Slate700)
                        .Underline()
                        .AlignCenter()
                        .Margin(0, 0, 0, 8);

                    column.Item()
                        .Text("Right-aligned underlined text")
                        .TextColor(Color.Slate700)
                        .Underline()
                        .AlignRight()
                        .Margin(0, 0, 0, 8);

                    // ── Decoration thickness scales with font size ───────────────
                    column.Item().Margin(0, 4, 0, 4).Line(1, Color.Slate300);
                    SectionHeader(column, "Decoration Thickness Scales with Font Size");

                    foreach (int size in DecorationSizes)
                    {
                        column.Item()
                            .Text($"Underline at {size}pt")
                            .FontSize(size)
                            .TextColor(Color.Slate700)
                            .Underline()
                            .Margin(0, 0, 0, 6);
                    }

                    // ── Practical example: price list ────────────────────────────
                    column.Item().Margin(0, 4, 0, 4).Line(1, Color.Slate300);
                    SectionHeader(column, "Practical Example: Price List");

                    // Original price — struck through, muted
                    column.Item()
                        .Text("Original Price: $199.99")
                        .FontSize(14)
                        .TextColor(Color.Slate400)
                        .Strikethrough()
                        .Margin(0, 0, 0, 4);

                    // Sale price — bold, red, underlined for emphasis
                    column.Item()
                        .Text("Sale Price: $149.99")
                        .Bold()
                        .FontSize(18)
                        .TextColor(Color.Red)
                        .Underline()
                        .Margin(0, 0, 0, 4);

                    column.Item()
                        .Text("You save $50.00!")
                        .FontSize(11)
                        .TextColor(Color.Green600);
                });
            });
        }).Save(outputPath);

        return outputPath;
    }

    private static readonly int[] DecorationSizes = { 8, 12, 18, 24, 36 };

    /// <summary>Adds a bold indigo section header to the column.</summary>
    private static void SectionHeader(Fluent.Descriptors.ColumnDescriptor column, string title)
    {
        column.Item()
            .Text(title)
            .Bold()
            .FontSize(14)
            .TextColor(Color.Indigo600)
            .Margin(0, 12, 0, 6);
    }
}
