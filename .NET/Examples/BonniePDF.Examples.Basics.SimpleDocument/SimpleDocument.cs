using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Basics.SimpleDocument;

/// <summary>
/// Demonstrates the new high-level Document API for creating PDFs
/// </summary>
[System.ComponentModel.DisplayName("Simple Document")]
[System.ComponentModel.Category("Basics")]
[System.ComponentModel.Description("A basic text-only PDF generation example.")]
public static class SimpleDocumentExample
{
    
    public static string Generate(string outputPath)
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

        doc.Metadata = new DocumentMetadata
        {
            Title = "Simple Document Example",
            Author = "BonniePDF",
            Subject = "Demonstrating the new Document API",
            Keywords = "pdf, document, example, BonniePDF"
        };

        doc.Settings = new DocumentSettings
        {
            DefaultZoom = ZoomLevel.FitPage,
            PageMode = PageMode.UseNone,
            DisplayDocTitle = false,
            Version = PdfVersion.Pdf17,
            PdfAEnabled = true
        };

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
            TextColor = Color.Slate600
        });

        doc.Add(new Spacer(16));

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

        // Add a custom size page (Square)
        PdfSize customSize = new PdfSize(300, 300, "Square Card");

        doc.AddPage(customSize);
        doc.Add(new TextBlock
        {
            Text = $"Page 3: {customSize.Name}",
            FontName = File.Exists(fontPath) ? fontPath : StandardFonts.HelveticaBold,
            FontSize = 16,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 50, 0, 0)
        });

        doc.Add(new TextBlock
        {
            Text = $"{customSize.Width}pt x {customSize.Height}pt",
            FontName = File.Exists(fontPath) ? fontPath : StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate600,
            Alignment = TextAlignment.Center
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
