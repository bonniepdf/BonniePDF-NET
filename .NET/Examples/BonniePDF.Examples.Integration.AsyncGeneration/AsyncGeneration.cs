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
public static class AsyncExample
{
    
    /// <summary>
    /// Generates a PDF asynchronously to a file
    /// </summary>
    public static async Task<string> GenerateAsync(string outputPath)
    {
        Document doc = new();

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

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
            TextColor = Color.Slate800
        });

        doc.Add(new TextBlock
        {
            Text = "This PDF was generated asynchronously using async/await.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate600
        });

        VerticalStack features = new();
        features.AddChild(new TextBlock
        {
            Text = "✓ Non-blocking I/O - perfect for web applications",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "✓ Better thread pool utilization",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "✓ Supports cancellation tokens",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "✓ Scalable for high-throughput scenarios",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });

        doc.Add(features);

        // Save asynchronously
        await doc.SaveAsync(outputPath);

        return outputPath;
    }

    /// <summary>
    /// Generates a PDF asynchronously to a MemoryStream
    /// </summary>
    public static async Task<byte[]> GenerateToMemoryAsync()
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
            TextColor = Color.Slate800
        });

        doc.Add(new TextBlock
        {
            Text = "This PDF was generated entirely in memory without touching the disk.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate600
        });

        // Save to MemoryStream asynchronously
        using MemoryStream ms = new();
        await doc.SaveAsync(ms);

        return ms.ToArray();
    }

    /// <summary>
    /// Demonstrates cancellation token usage
    /// </summary>
    public static async Task<string> GenerateWithCancellationAsync(
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
            TextColor = Color.Slate800
        });

        doc.Add(new TextBlock
        {
            Text = "This PDF generation can be cancelled using the cancellation token.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate600
        });

        // Save with cancellation token
        await doc.SaveAsync(outputPath, cancellationToken);

        return outputPath;
    }
}
