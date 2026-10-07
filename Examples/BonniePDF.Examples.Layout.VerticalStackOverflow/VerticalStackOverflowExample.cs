using BonniePDF;

namespace BonniePDF.Examples.Layout.VerticalStackOverflow;

/// <summary>
/// VerticalStack is the primitive that lays a column of elements down the page
/// AND paginates it. That second job matters for elements that have no
/// pagination of their own: a Barcode (like an Image, QRCode, or Line) renders
/// wherever the cursor happens to be, with no awareness of the page edge. Drop
/// a long list of bare barcodes straight onto the document and the ones past the
/// first page's bottom margin are simply drawn off the paper and lost. Put the
/// same list inside a VerticalStack and each barcode that would fall off the
/// page is carried to the next one instead.
///
/// The example is a warehouse asset-label sheet - one scannable Code 128 label
/// per storage location. Every label is a Figure with alt-text; the running
/// footer is marked as decorative.
/// </summary>
[System.ComponentModel.DisplayName("VerticalStack Overflow")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description(
    "A column of Code 128 barcodes paginated by a VerticalStack - barcodes have no pagination of their own, so the stack is what keeps the overflow on the page.")]
public static class VerticalStackOverflowExample
{
    private static readonly string[] Locations =
    [
        "WH1-A01-R02-B03", "WH1-A01-R02-B04", "WH1-A01-R03-B01", "WH1-A02-R01-B02",
        "WH1-A02-R04-B05", "WH1-A03-R02-B01", "WH1-A03-R02-B06", "WH1-A04-R01-B03",
        "WH1-A04-R03-B04", "WH2-B01-R01-B01", "WH2-B01-R02-B02", "WH2-B02-R03-B03",
        "WH2-B02-R03-B07", "WH2-B03-R01-B04", "WH2-B03-R04-B05", "WH2-B04-R02-B01",
        "WH2-B04-R02-B08", "WH2-B05-R01-B02", "WH2-B05-R03-B06", "WH2-B06-R04-B03",
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "Warehouse Asset-Label Sheet",
                Author = "BonniePDF",
                Subject =
                    "Demonstrates VerticalStack paginating a column of barcodes in an accessible PDF/A-2a + PDF/UA-1 document.",
                Keywords = "pdf, accessibility, layout, vertical stack, barcode, logistics, warehouse, BonniePDF",
            },
            Settings = new DocumentSettings
            {
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(StandardFonts.Helvetica)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[700]),

                FooterTemplate = pageNum => new TextBlock
                {
                    Text = $"Page {pageNum} of {TextElementBase.TotalPagesToken}",
                    FontName = StandardFonts.Helvetica,
                    FontSize = 9,
                    Color = ColorPalette.Slate[400],
                    Alignment = TextAlignment.Right,
                },
            },
        };

        doc.Add(new Heading("Warehouse Asset-Label Sheet")
        {
            TocIdentifier = "intro",
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 10),
        });

        doc.Add(new TextBlock
        {
            Text =
                "One Code 128 label per storage location. Barcodes have no pagination of their own, so a VerticalStack lays the column out and carries each label that would fall off the page onto the next one - nothing is lost off the bottom edge.",
            Margin = new Thickness(0, 0, 0, 16),
        });

        VerticalStack sheet = new() { Spacing = 18 };
        foreach (string location in Locations)
        {
            sheet.Add(new Barcode
            {
                Value = location,
                Symbology = BarcodeSymbology.Code128,
                Height = 46,
                TextLabelFontSize = 10,
                AltText = $"Warehouse location barcode {location}",
            });
        }

        doc.Add(sheet);

        return doc;
    }
}
