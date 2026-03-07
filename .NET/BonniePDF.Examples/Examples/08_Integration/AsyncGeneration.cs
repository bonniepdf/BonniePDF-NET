using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using BonniePDF.Examples.Core;

namespace BonniePDF.Examples.Examples._08_Integration;

/// <summary>
/// Demonstrates async/await PDF generation for non-blocking I/O
/// </summary>
public class AsyncExample : ExampleBase
{
    public override string Id => "AsyncGeneration";
    public override string Title => "Async Generation";
    public override string Description => "Generating PDFs asynchronously.";
    public override string FileName => "async_document.pdf";
    public override ExampleCategory Category => ExampleCategory.Integration;
    
    /// <summary>
    /// Generates a PDF asynchronously to a file
    /// </summary>
    public override async Task<string> GenerateAsync(string outputPath)
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