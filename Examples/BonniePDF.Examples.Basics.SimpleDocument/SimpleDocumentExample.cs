using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Lists;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Basics.SimpleDocument;

/// <summary>
/// The core high-level Document API - three pages of text, structured spacing, and a
/// custom square page - authored as an accessible, archival PDF by default:
/// <c>Conformance = PdfA2a</c> + <c>Accessibility = PdfUa1</c>. Headings use
/// the semantic <see cref="Heading"/> element, so they nest correctly for a
/// screen reader; <see cref="TextBlock"/> copy is tagged as paragraphs without
/// you asking; the feature list is structured as a real list; and the
/// <see cref="QRCode"/> carries alt text.
/// </summary>
[System.ComponentModel.DisplayName("Simple Document")]
[System.ComponentModel.Category("Basics")]
[System.ComponentModel.Description("A 3-page PDF/UA-1 tagged text document using the core Document API.")]
public static class SimpleDocumentExample
{
    public static Document Generate()
    {
        // Headings use the standard-14 "Helvetica-Bold" name. Under PDF/A
        // (and PDF/UA-1) BonniePDF auto-substitutes and embeds the bundled,
        // metric-compatible Liberation Sans Bold face - so the output is
        // identical on every platform with no system fonts or file paths
        // involved. (See Typography.CustomFonts for embedding your own TTF.)
        string heading = StandardFonts.HelveticaBold;

        Document doc = new();

        doc.Metadata = new DocumentMetadata
        {
            Title = "Simple Document Example",
            Author = "BonniePDF",
            Subject = "Demonstrating the Document API",
            Keywords = "pdf, document, example, accessibility, BonniePDF"
        };

        doc.Settings = new DocumentSettings
        {
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            DefaultZoom = ZoomLevel.FitPage,
            PageMode = PageMode.UseNone,
            DisplayDocTitle = true,
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(12).WithColor(ColorPalette.Slate[600])
        };

        // ── Page 1 ──
        doc.Add(new Heading("Hello, Document API!")
        {
            TocIdentifier = "intro",
            Level = 1,
            FontName = heading,
            FontSize = 24,
            TextColor = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Body text uses DefaultTextStyle (family=Helvetica, size=12, color=Slate600)
        doc.Add(new TextBlock
        {
            Text = "This PDF was created using the high-level Document API.",
            Margin = new Thickness(0, 0, 0, 16)
        });

        doc.Add(new Heading("Key Features")
        {
            TocIdentifier = "features",
            Level = 2,
            FontName = heading,
            FontSize = 14,
            TextColor = ColorPalette.Slate[700],
            Margin = new Thickness(0, 0, 0, 6)
        });

        UnorderedList features = new()
        {
            FontSize = 11,
            ItemSpacing = 2
        };
        features.AddItem("Simple, intuitive API");
        features.AddItem("Built-in metadata support");
        features.AddItem("Configurable zoom levels");
        features.AddItem("Viewer preferences");
        features.AddItem("Automatic page management");

        doc.Add(features);

        // ── Divider, then the accessibility note ──
        doc.Add(new Line
        {
            Thickness = 1,
            Color = ColorPalette.Slate[300],
            Margin = new Thickness(0, 16, 0, 16)
        });

        doc.Add(new TextBlock
        {
            Text = "Existing elements (Heading, TextBlock, UnorderedList, Image, QRCode) tag themselves automatically - no semantic verbs required.",
            Margin = new Thickness(0, 0, 0, 16)
        });

        doc.Add(new Heading("Scan to verify")
        {
            TocIdentifier = "verify",
            Level = 2,
            FontName = heading,
            FontSize = 14,
            TextColor = ColorPalette.Slate[700],
            Margin = new Thickness(0, 0, 0, 6)
        });

        doc.Add(new QRCode
        {
            Data = "https://bonniepdf.dev",
            Size = 100,
            AltText = "QR code linking to https://bonniepdf.dev"
        });

        // ── Page 2 ──
        doc.AddPage();

        doc.Add(new Heading("Page 2")
        {
            TocIdentifier = "page2",
            Level = 1,
            FontName = heading,
            FontSize = 20,
            TextColor = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 12)
        });

        doc.Add(new TextBlock
        {
            Text = "This demonstrates multipage support with automatic page management."
        });

        // ── Page 3 - custom square ──
        PdfSize customSize = new PdfSize(300, 300, "Square Card");

        doc.AddPage(customSize);
        doc.Add(new Heading($"Page 3: {customSize.Name}")
        {
            TocIdentifier = "square",
            Level = 1,
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

        return doc;
    }
}
