using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Integration.AsyncGeneration;

/// <summary>
/// Demonstrates async/await PDF generation for non-blocking I/O
/// </summary>
[System.ComponentModel.DisplayName("Async Generation")]
[System.ComponentModel.Category("Integration")]
[System.ComponentModel.Description("Generating PDFs asynchronously.")]
public static class AsyncGenerationExample
{
    /// <summary>
    /// Generates a PDF asynchronously to a file
    /// </summary>
    public static async Task<Document> GenerateAsync()
    {
        Document doc = new();

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[600]);

        // Set document metadata
        doc.Metadata.Title = "Async PDF Generation";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "Demonstrating async/await support";

        // Add content
        doc.Add(new TextBlock
        {
            Text = "Async PDF Generation",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = ColorPalette.Slate[800]
        });

        // Uses DefaultTextStyle (Helvetica, 11, Slate600)
        doc.Add(new TextBlock
        {
            Text = "This PDF was generated asynchronously using async/await.",
            FontSize = 12
        });

        VerticalStack features = new();
        features.Add(new TextBlock { Text = "✓ Non-blocking I/O - perfect for web applications" });
        features.Add(new TextBlock { Text = "✓ Better thread pool utilization" });
        features.Add(new TextBlock { Text = "✓ Supports cancellation tokens" });
        features.Add(new TextBlock { Text = "✓ Scalable for high-throughput scenarios" });

        doc.Add(features);

        // Save asynchronously
        return doc;
    }

    /// <summary>
    /// Generates a PDF asynchronously to a MemoryStream
    /// </summary>
    public static async Task<global::BonniePDF.Document> GenerateToMemoryAsync()
    {
        Document doc = new();

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "In-Memory PDF";
        doc.Metadata.Author = "BonniePDF";

        doc.Add(new TextBlock
        {
            Text = "In-Memory PDF Generation",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = ColorPalette.Slate[800]
        });

        doc.Add(new TextBlock
        {
            Text = "This PDF was generated entirely in memory without touching the disk.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            Color = ColorPalette.Slate[600]
        });

        // Save to MemoryStream asynchronously
        return doc;
    }

    /// <summary>
    /// Demonstrates cancellation token usage
    /// </summary>
    public static async Task<global::BonniePDF.Document> GenerateWithCancellationAsync(
        string outputPath = "cancellable_document.pdf",
        CancellationToken cancellationToken = default)
    {
        Document doc = new();

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "Cancellable PDF";
        doc.Metadata.Author = "BonniePDF";

        doc.Add(new TextBlock
        {
            Text = "Cancellable PDF Generation",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = ColorPalette.Slate[800]
        });

        doc.Add(new TextBlock
        {
            Text = "This PDF generation can be cancelled using the cancellation token.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            Color = ColorPalette.Slate[600]
        });

        // Save with cancellation token

        return doc;
    }
}


