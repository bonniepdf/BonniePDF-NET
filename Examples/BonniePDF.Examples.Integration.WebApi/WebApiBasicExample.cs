using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Integration.WebApi;

/// <summary>
/// Demonstrates how to use BonniePDF in ASP.NET Core Web APIs.
/// This shows the real-world scenario of generating PDFs in-memory and returning them to clients.
/// </summary>
[System.ComponentModel.DisplayName("Web API - Basic")]
[System.ComponentModel.Category("Integration")]
[System.ComponentModel.Description("Demonstrates basic Web API integration with in-memory PDF generation, returning byte arrays directly to clients without temporary files.")]
public static class WebApiBasicExample
{
    public static Document Generate()
    {
        // For demonstration, we'll save to file instead of returning bytes
        Document doc = new();

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        // Set metadata
        doc.Metadata.Title = "Web API PDF";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Creator = "BonniePDF Web API";

        // Add content
        doc.Add(new TextBlock
        {
            Text = "PDF from Web API",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 12)
        });

        doc.Add(new TextBlock
        {
            Text = "This PDF was generated in-memory and returned directly to the client.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate600,
            Margin = new Thickness(0, 0, 0, 8)
        });

        VerticalStack features = new();
        features.AddChild(new TextBlock
        {
            Text = "Benefits of this approach:",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 6)
        });
        features.AddChild(new TextBlock
        {
            Text = "• No temporary files on disk",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "• Better security (sensitive data never hits disk)",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "• Non-blocking async I/O",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "• Better scalability under load",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "• Cleaner code (no temp file cleanup)",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });

        doc.Add(features);
        return doc;
    }

    /// <summary>
    /// Example ASP.NET Core controller action that returns a PDF.
    /// This is the pattern used in real web applications.
    /// </summary>
    public static async Task<byte[]> GeneratePdfForWebApiAsync()
    {
        Document doc = new();

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        // Set metadata
        doc.Metadata.Title = "Web API PDF";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Creator = "BonniePDF Web API";

        // Add content
        doc.Add(new TextBlock
        {
            Text = "PDF from Web API",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 12)
        });

        doc.Add(new TextBlock
        {
            Text = "This PDF was generated in-memory and returned directly to the client.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate600,
            Margin = new Thickness(0, 0, 0, 8)
        });

        VerticalStack features = new();
        features.AddChild(new TextBlock
        {
            Text = "Benefits of this approach:",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 6)
        });
        features.AddChild(new TextBlock
        {
            Text = "• No temporary files on disk",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "• Better security (sensitive data never hits disk)",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "• Non-blocking async I/O",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "• Better scalability under load",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "• Cleaner code (no temp file cleanup)",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });

        doc.Add(features);

        using MemoryStream ms = new();
        await doc.SaveAsync(ms);

        return ms.ToArray();
    }
}


