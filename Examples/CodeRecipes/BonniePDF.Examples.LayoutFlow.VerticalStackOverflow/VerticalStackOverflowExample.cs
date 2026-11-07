using BonniePDF;

namespace BonniePDF.Examples.LayoutFlow.VerticalStackOverflow;

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
[System.ComponentModel.Category("Layout & Flow")]
[System.ComponentModel.Description(
    "A three-across sheet of Code 128 asset labels, one row per VerticalStack child - barcodes have no pagination of their own, so the stack is what keeps the overflow on the page.")]
public static class VerticalStackOverflowExample
{
    private static readonly string[] Locations =
    [
        "WH1-A01-R02-B03", "WH1-A01-R02-B04", "WH1-A01-R03-B01", "WH1-A02-R01-B02",
        "WH1-A02-R04-B05", "WH1-A03-R02-B01", "WH1-A03-R02-B06", "WH1-A04-R01-B03",
        "WH1-A04-R03-B04", "WH2-B01-R01-B01", "WH2-B01-R02-B02", "WH2-B02-R03-B03",
        "WH2-B02-R03-B07", "WH2-B03-R01-B04", "WH2-B03-R04-B05", "WH2-B04-R02-B01",
        "WH2-B04-R02-B08", "WH2-B05-R01-B02", "WH2-B05-R03-B06", "WH2-B06-R04-B03",
        "WH3-C01-R01-B01", "WH3-C01-R01-B02", "WH3-C01-R02-B01", "WH3-C02-R01-B03",
        "WH3-C02-R03-B02", "WH3-C03-R01-B01", "WH3-C03-R02-B04", "WH3-C04-R01-B02",
        "WH3-C04-R04-B01", "WH3-C05-R02-B03",
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

                FooterTemplate = pageNum => PageNumber.Text().Font(StandardFonts.Helvetica, 9).Color(ColorPalette.Slate[400]).AlignRight(),
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
                "One Code 128 label per storage location, three across. Barcodes have no pagination of their own, so a VerticalStack lays the rows out and carries each row that would fall off the page onto the next one - nothing is lost off the bottom edge.",
            Margin = new Thickness(0, 0, 0, 16),
        });

        // Three labels per row: each row is one child of the VerticalStack, so a row moves to the next page whole.
        VerticalStack sheet = new() { Spacing = 10 };
        for (int i = 0; i < Locations.Length; i += 3)
        {
            HorizontalStack row = new() { Spacing = 10, WidthDistribution = WidthDistribution.Equal };
            foreach (string location in Locations.Skip(i).Take(3)) row.Add(Label(location));
            sheet.Add(row);
        }

        doc.Add(sheet);

        return doc;
    }

    /// <summary>One cut-out label: the warehouse and aisle above the location's barcode, inside a thin cut line.</summary>
    private static Container Label(string location)
    {
        string[] parts = location.Split('-');
        VerticalStack stack = new() { Spacing = 6 };
        stack.Add(new TextBlock($"WAREHOUSE {parts[0][2..]}  ·  AISLE {parts[1]}")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 7,
            LetterSpacing = 0.8f,
            Color = ColorPalette.Slate[600],
        });
        stack.Add(new Barcode
        {
            Value = location,
            Symbology = BarcodeSymbology.Code128,
            ModuleWidth = 0.75f,
            Height = 40,
            TextLabelFontSize = 8,
            AltText = $"Warehouse location barcode {location}",
        });
        return new Container
        {
            Border = new Border(0.5f),
            BorderColors = new BorderColors(ColorPalette.Slate[300]),
            CornerRadius = 4,
            Padding = new Thickness(10, 8, 10, 8),
            Child = stack,
        };
    }
}
