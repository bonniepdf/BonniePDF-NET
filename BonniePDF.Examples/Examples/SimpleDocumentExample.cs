using System.IO;
using BonniePDF.Configuration;
using BonniePDF.Extensions;
using BonniePDF.Layout.Elements;
using BonniePDF.Models;

namespace BonniePDF.Examples.Examples;

/// <summary>
/// Demonstrates the new high-level Document API for creating PDFs
/// </summary>
public class SimpleDocumentExample
{
    public static string Generate(string outputPath = "simple_document.pdf")
    {
        // Use Arial as the embedded font (available on Windows)
        string fontPath = @"C:\Windows\Fonts\Arial.ttf";

        // Check if Arial exists, fall back to another common font
        if (!File.Exists(fontPath))
        {
            fontPath = @"C:\Windows\Fonts\segoeui.ttf";
        }

        if (!File.Exists(fontPath))
        {
            // If no system font is found, we can't fully demonstrate embedding,
            // but we'll proceed. In a real scenario, you'd want to handle this.
            // For this example, we'll just stick with what we have if nothing is found,
            // but the goal is to use the found font.
        }

        // Create a document with metadata and settings
        Document doc = new();

        // Set document metadata
        doc.Metadata.Title = "Simple Document Example";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "Demonstrating the new Document API";
        doc.Metadata.Keywords = "pdf, document, example, rawpdfengine";

        // Configure document settings
        doc.Settings.DefaultZoom = ZoomLevel.FitPage;
        doc.Settings.PageMode = PageMode.UseNone;
        doc.Settings.DisplayDocTitle = false;
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        // Add content to the first page
        doc.Add(new TextBlock
        {
            Text = "Hello, Document API!",
            FontName = File.Exists(fontPath) ? fontPath : StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 12)
        });

        doc.Add(new TextBlock
        {
            Text = "This PDF was created using the new high-level Document API.",
            FontName = File.Exists(fontPath) ? fontPath : StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate600,
            Margin = new Thickness(0, 0, 0, 8)
        });

        doc.Add(new TextBlock
        {
            Text = "Key Features:",
            FontName = File.Exists(fontPath) ? fontPath : StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 6)
        });

        VerticalStack features = new();
        features.AddChild(new TextBlock
        {
            Text = "- Simple, intuitive API", FontName = File.Exists(fontPath) ? fontPath : StandardFonts.Helvetica, FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "- Built-in metadata support", FontName = File.Exists(fontPath) ? fontPath : StandardFonts.Helvetica, FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "- Configurable zoom levels", FontName = File.Exists(fontPath) ? fontPath : StandardFonts.Helvetica, FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "- Viewer preferences", FontName = File.Exists(fontPath) ? fontPath : StandardFonts.Helvetica, FontSize = 11, TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "- Automatic page management", FontName = File.Exists(fontPath) ? fontPath : StandardFonts.Helvetica, FontSize = 11,
            TextColor = Color.Slate600
        });

        doc.Add(features);

        // Add a second page
        doc.AddPage();
        doc.Add(new TextBlock
        {
            Text = "Page 2",
            FontName = File.Exists(fontPath) ? fontPath : StandardFonts.HelveticaBold,
            FontSize = 20,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 12)
        });

        doc.Add(new TextBlock
        {
            Text = "This demonstrates multipage support with automatic page management.",
            FontName = File.Exists(fontPath) ? fontPath : StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate600
        });

        // Save the document
        doc.Save(outputPath);

        // Export all pages to SVG
        for (int i = 0; i < doc.Pages.Count; i++)
        {
            string svgPagePath = outputPath.Replace(".pdf", $"_page{i + 1}.svg");
            string svgPage = doc.Pages[i].ToSvg();
            File.WriteAllText(svgPagePath, svgPage);
        }

        return outputPath;
    }
}