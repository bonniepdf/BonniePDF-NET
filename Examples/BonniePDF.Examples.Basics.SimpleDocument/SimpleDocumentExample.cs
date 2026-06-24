using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
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
    public static Document Generate()
    {
        // Headings use the standard-14 "Helvetica-Bold" name. With PDF/A enabled
        // (below) BonniePDF auto-substitutes and embeds the bundled, metric-compatible
        // Liberation Sans Bold face - so the output is identical on every platform with
        // no system fonts or file paths involved. (See Typography.CustomFonts for
        // embedding your own TTF.)
        string heading = StandardFonts.HelveticaBold;

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
            PdfAEnabled = true,
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(12).WithColor(ColorPalette.Slate[600])
        };

        // Add content to the first page
        doc.Add(new TextBlock
        {
            Text = "Hello, Document API!",
            FontName = heading,
            FontSize = 24,
            TextColor = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Body text uses DefaultTextStyle (family=Helvetica, size=12, color=Slate600)
        doc.Add(new TextBlock
        {
            Text = "This PDF was created using the new high-level Document API."
        });

        doc.Add(new Spacer(16));

        doc.Add(new TextBlock
        {
            Text = "Key Features:",
            FontName = heading,
            FontSize = 14,
            TextColor = ColorPalette.Slate[600],
            Margin = new Thickness(0, 0, 0, 6)
        });

        VerticalStack features = new();
        features.AddChild(new TextBlock { Text = "- Simple, intuitive API", FontSize = 11 });
        features.AddChild(new TextBlock { Text = "- Built-in metadata support", FontSize = 11 });
        features.AddChild(new TextBlock { Text = "- Configurable zoom levels", FontSize = 11 });
        features.AddChild(new TextBlock { Text = "- Viewer preferences", FontSize = 11 });
        features.AddChild(new TextBlock { Text = "- Automatic page management", FontSize = 11 });

        doc.Add(features);

        // Add a second page
        doc.AddPage();

        doc.Add(new TextBlock
        {
            Text = "Page 2",
            FontName = heading,
            FontSize = 20,
            TextColor = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Body text inherits DefaultTextStyle
        doc.Add(new TextBlock
        {
            Text = "This demonstrates multipage support with automatic page management."
        });

        // Add a custom size page (Square)
        PdfSize customSize = new PdfSize(300, 300, "Square Card");

        doc.AddPage(customSize);
        doc.Add(new TextBlock
        {
            Text = $"Page 3: {customSize.Name}",
            FontName = heading,
            FontSize = 16,
            TextColor = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 50, 0, 0)
        });

        doc.Add(new TextBlock
        {
            Text = $"{customSize.Width}pt x {customSize.Height}pt",
            Alignment = TextAlignment.Center
        });

        // Save the document

        //// Export all pages to SVG
        //for (int i = 0; i < doc.Pages.Count; i++)
        //{
        //    string svgPagePath = outputPath.Replace(".pdf", $"_page{i + 1}.svg");
        //    string svgPage = doc.Pages[i].ToSvg();
        //    File.WriteAllText(svgPagePath, svgPage);
        //}
        return doc;
    }
}

