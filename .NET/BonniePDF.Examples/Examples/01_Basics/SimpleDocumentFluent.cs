using System.IO;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Extensions;
using BonniePDF.Typography;
using BonniePDF.Examples.Core;

namespace BonniePDF.Examples.Examples._01_Basics;

/// <summary>
/// Demonstrates basic PDF creation using the Fluent API.
/// Covers metadata, document settings, multi-page documents with a custom page size,
/// and feature lists — all using chained extension methods instead of property-initialiser syntax.
/// </summary>
public class SimpleDocumentFluentExample : ExampleBase
{
    public override string Id => "SimpleDocumentFluent";
    public override string Title => "Simple Document (Fluent API)";
    public override string Description => "A basic text-only PDF generation example using the Fluent API.";
    public override string FileName => "simple_document_fluent.pdf";
    public override ExampleCategory Category => ExampleCategory.Basics;

    public override string Generate(string outputPath)
    {
        // Prefer Arial; fall back to Segoe UI on systems where Arial is absent
        string fontPath = @"C:\Windows\Fonts\Arial.ttf";
        if (!File.Exists(fontPath))
            fontPath = @"C:\Windows\Fonts\segoeui.ttf";

        // Resolve to a usable font name: embedded TTF if found, else standard Helvetica
        string bodyFont = File.Exists(fontPath) ? fontPath : StandardFonts.Helvetica;
        string boldFont  = File.Exists(fontPath) ? fontPath : StandardFonts.HelveticaBold;

        PdfSize squareCard = new(300, 300, "Square Card");

        Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title    = "Simple Document Example (Fluent API)";
                metadata.Author   = "BonniePDF";
                metadata.Subject  = "Demonstrating the BonniePDF Fluent API";
                metadata.Keywords = "pdf, document, example, BonniePDF, fluent";
            });

            doc.WithSettings(settings =>
            {
                settings.DefaultZoom    = ZoomLevel.FitPage;
                settings.PageMode       = PageMode.UseNone;
                settings.DisplayDocTitle = false;
                settings.Version        = PdfVersion.Pdf17;
                settings.PdfAEnabled    = true;
            });

            // Document-wide default: body font at 12pt
            doc.WithDefaultTextStyle(style => style
                .WithFamily(bodyFont)
                .WithSize(12));

            // ── Page 1: Introduction and feature list ─────────────────────────
            doc.Page(page =>
            {
                page.Content().Column(col =>
                {
                    col.Item()
                        .Text("Hello, Fluent API!")
                        .Font(boldFont)
                        .FontSize(24)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 12);

                    col.Item()
                        .Text("This PDF was created using the BonniePDF Fluent API.")
                        .TextColor(Color.Slate600)
                        .Margin(0, 0, 0, 8);

                    col.Item()
                        .Text("Key Features:")
                        .Font(boldFont)
                        .FontSize(14)
                        .TextColor(Color.Slate700)
                        .Margin(0, 0, 0, 6);

                    foreach (string feature in Features)
                    {
                        col.Item()
                            .Text(feature)
                            .FontSize(11)
                            .TextColor(Color.Slate600);
                    }
                });
            });

            // ── Page 2: Second page ───────────────────────────────────────────
            doc.Page(page =>
            {
                page.Content().Column(col =>
                {
                    col.Item()
                        .Text("Page 2")
                        .Font(boldFont)
                        .FontSize(20)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 12);

                    col.Item()
                        .Text("This demonstrates multi-page support using the Fluent API. " +
                              "Each doc.Page() call adds a new page to the document, " +
                              "and content flows automatically.")
                        .TextColor(Color.Slate600);
                });
            });

            // ── Page 3: Custom page size (300 × 300 pt square) ────────────────
            doc.Page(page =>
            {
                page.Size(squareCard);

                page.Content().Column(col =>
                {
                    col.Item()
                        .Text($"Page 3: {squareCard.Name}")
                        .Font(boldFont)
                        .FontSize(16)
                        .TextColor(Color.Slate800)
                        .AlignCenter()
                        .Margin(0, 50, 0, 8);

                    col.Item()
                        .Text($"{squareCard.Width}pt × {squareCard.Height}pt")
                        .TextColor(Color.Slate600)
                        .AlignCenter()
                        .Margin(0, 0, 0, 16);

                    col.Item()
                        .Text("Custom page sizes are set per page via page.Size().")
                        .FontSize(10)
                        .TextColor(Color.Slate400)
                        .AlignCenter();
                });
            });

        }).Save(outputPath);

        return outputPath;
    }

    private static readonly string[] Features =
    [
        "- Simple, intuitive fluent API",
        "- Built-in metadata support",
        "- Configurable zoom levels",
        "- Viewer preferences",
        "- Automatic page management",
        "- Custom page sizes per page",
    ];
}
