using BonniePDF;

namespace BonniePDF.Examples.GettingStarted.SimpleDocument;

/// <summary>
/// The core high-level Document API - a title block, a feature panel, a summary
/// table and a custom square page - authored as an accessible, archival PDF by default:
/// <c>Conformance = PdfA2a</c> + <c>Accessibility = PdfUa1</c>. Headings use
/// the semantic <see cref="Heading"/> element, so they nest correctly for a
/// screen reader; <see cref="TextBlock"/> copy is tagged as paragraphs without
/// you asking; the feature list is structured as a real list; and the
/// <see cref="QRCode"/> carries alt text.
/// </summary>
[System.ComponentModel.DisplayName("Simple Document")]
[System.ComponentModel.Category("Getting Started")]
[System.ComponentModel.Description("A three-page tour of the core Document API: a title block, a feature panel, a QR code with alt text, a summary table and a custom square page.")]
public static class SimpleDocumentExample
{
    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
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

        // ── Page 1: a title block with an accent rule, then the feature panel ──
        VerticalStack titleStack = new() { Spacing = 6 };
        titleStack.Add(new TextBlock("GETTING STARTED")
        {
            FontName = heading,
            FontSize = 9,
            LetterSpacing = 1.5f,
            Color = ColorPalette.Indigo[600]
        });
        titleStack.Add(new Heading("Hello, Document API!")
        {
            TocIdentifier = "intro",
            Level = 1,
            FontName = heading,
            FontSize = 28,
            Color = ColorPalette.Slate[900]
        });
        // Body text uses DefaultTextStyle (family=Helvetica, size=12, color=Slate600)
        titleStack.Add(new TextBlock("Headings, text, a list, a table and a QR code across three pages, written with the high-level Document API."));

        doc.Add(new Container
        {
            Border = new Border(4, 0, 0, 0),
            BorderColors = new BorderColors(ColorPalette.Indigo[500]),
            Padding = new Thickness(16, 2, 0, 2),
            Margin = new Thickness(0, 0, 0, 24),
            Child = titleStack
        });

        UnorderedList features = new()
        {
            FontSize = 11,
            ItemSpacing = 3,
            BulletColor = ColorPalette.Indigo[500]
        };
        features.AddItem("Simple, intuitive API");
        features.AddItem("Built-in metadata support");
        features.AddItem("Configurable zoom levels");
        features.AddItem("Viewer preferences");
        features.AddItem("Automatic page management");

        VerticalStack featureStack = new() { Spacing = 8 };
        featureStack.Add(new Heading("Key features")
        {
            TocIdentifier = "features",
            Level = 2,
            FontName = heading,
            FontSize = 14,
            Color = ColorPalette.Indigo[900]
        });
        featureStack.Add(features);

        doc.Add(new Container
        {
            BackgroundColor = ColorPalette.Indigo[50],
            CornerRadius = 8,
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 20),
            Child = featureStack
        });

        doc.Add(new TextBlock
        {
            Text =
                "Existing elements (Heading, TextBlock, UnorderedList, Image, QRCode) tag themselves automatically - no semantic verbs required.",
            Margin = new Thickness(0, 0, 0, 20)
        });

        // ── The QR code beside its explanation: a weighted row, 1 part code to 4 parts text ──
        VerticalStack scanStack = new() { Spacing = 6 };
        scanStack.Add(new Heading("Scan to verify")
        {
            TocIdentifier = "verify",
            Level = 2,
            FontName = heading,
            FontSize = 14,
            Color = ColorPalette.Slate[900]
        });
        scanStack.Add(new TextBlock("Point a phone camera at the code to open bonniepdf.dev. The code carries alt text, so a screen reader says where it leads.")
        {
            FontSize = 11
        });

        HorizontalStack scanRow = new() { Spacing = 18, WidthDistribution = WidthDistribution.Weighted };
        scanRow.Add(new QRCode
        {
            Data = "https://bonniepdf.dev",
            Size = 90,
            AltText = "QR code linking to https://bonniepdf.dev"
        }, 1);
        scanRow.Add(scanStack, 4);
        doc.Add(scanRow);

        // ── Page 2 ──
        doc.AddPage();

        doc.Add(new Heading("Page 2: Summary table")
        {
            TocIdentifier = "page2",
            Level = 1,
            FontName = heading,
            FontSize = 20,
            Color = ColorPalette.Slate[900],
            Margin = new Thickness(0, 0, 0, 12)
        });

        doc.Add(new TextBlock
        {
            Text = "This demonstrates multipage support with automatic page management.",
            Margin = new Thickness(0, 0, 0, 16)
        });

        var table = new Table(160f, 160f, 175f)
        {
            HeaderBackgroundColor = ColorPalette.Indigo[700],
            HeaderTextColor = Color.White,
        };

        table.AddHeader(
            new TextBlock { Text = "Component", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Runtime", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Status", FontName = StandardFonts.HelveticaBold, FontSize = 10 }
        );

        foreach (var (component, runtime) in new[]
        {
            ("Core Engine", ".NET / TypeScript"),
            ("Typography Engine", "Embedded Fonts"),
            ("Vector Graphics", "SVG / QR / Barcode"),
            ("Charts", "Bar / Line / Pie"),
            ("Compliance", "PDF/A / PDF/UA"),
        })
        {
            table.AddRow(
                new TextBlock { Text = component, FontSize = 10 },
                new TextBlock { Text = runtime, FontSize = 10 },
                new TextBlock { Text = "Available", FontSize = 10, Color = ColorPalette.Emerald[700] }
            );
        }

        doc.Add(table);

        doc.Add(new Container
        {
            BackgroundColor = ColorPalette.Slate[100],
            CornerRadius = 8,
            Padding = new Thickness(14),
            Margin = new Thickness(0, 20, 0, 0),
            Child = new TextBlock("Content flows onto new pages by itself as the document grows. Start a page yourself only when you want a break on purpose, as before this page and before the square card that follows.")
            {
                FontSize = 11
            }
        });

        // ── Page 3 - custom square: a card that fills the page body ──
        PdfSize customSize = new PdfSize(300, 300, "Square Card");

        doc.AddPage(customSize);

        VerticalStack cardStack = new() { Spacing = 6 };
        cardStack.Add(new Heading($"Page 3: {customSize.Name}")
        {
            TocIdentifier = "square",
            Level = 1,
            FontName = heading,
            FontSize = 18,
            Color = Color.White,
            Alignment = TextAlignment.Center
        });
        cardStack.Add(new TextBlock
        {
            Text = $"{customSize.Width}pt × {customSize.Height}pt",
            Alignment = TextAlignment.Center,
            Color = ColorPalette.Indigo[100]
        });

        doc.Add(new Container
        {
            FillHeight = true,
            BackgroundColor = ColorPalette.Indigo[600],
            CornerRadius = 12,
            Padding = new Thickness(20),
            VerticalAlignment = VerticalAlignment.Center,
            Child = cardStack
        });

        return doc;
    }
}
