using BonniePDF;

namespace BonniePDF.Examples.BarcodesQrCodes.Barcodes;

/// <summary>Native vector barcodes with readable explanations and accessible descriptions.</summary>
[System.ComponentModel.DisplayName("Barcodes")]
[System.ComponentModel.Category("Barcodes & QR Codes")]
[System.ComponentModel.Description("Create vector symbols with sample data, readable labels, and alt text.")]
public static class BarcodesExample
{
    /// <summary>Builds the document. Export it with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 50,
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            DefaultTextStyle = new TextStyle()
                .WithFamily(StandardFonts.Helvetica)
                .WithSize(12)
                .WithColor(ColorPalette.Slate[600]),
        });
        doc.Metadata.Title = "Barcodes";
        doc.Metadata.Author = "BonniePDF";
        TextBlock title = new()
        {
            Text = "1D Barcodes",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 16)
        };
        doc.Add(title);
        TextBlock introduction = new()
        {
            Text = "BonniePDF renders 1D barcodes as native vector graphics - sharp at any zoom, tiny in the PDF, and with zero external dependencies. The four built-in symbologies cover retail and logistics across the UK, EU, and US. Check digits and quiet zones are computed automatically; the human-readable value is printed beneath each symbol.",
            Margin = new Thickness(0, 0, 0, 20)
        };
        doc.Add(introduction);
        TextBlock code128Heading = new()
        {
            Text = "Code 128",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            Color = ColorPalette.Indigo[600],
            Margin = new Thickness(0, 0, 0, 10)
        };
        doc.Add(code128Heading);
        HorizontalStack row1 = new()
        {
            Spacing = 20,
            WidthDistribution = WidthDistribution.Weighted
        };
        Barcode barcode1 = new()
        {
            Value = "1Z9999W99999999999",
            Height = 55,
            AltText = "Code 128 - the universal logistics barcode: 1Z9999W99999999999"
        };
        Container card1 = new()
        {
            Padding = new Thickness(12),
            BackgroundColor = Color.White,
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            Child = barcode1
        };
        row1.Add(card1, 1);
        VerticalStack column1 = new();
        TextBlock text4 = new()
        {
            Text = "Code 128 - the universal logistics barcode",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 13,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 6)
        };
        column1.Add(text4);
        TextBlock text5 = new()
        {
            Text = "Encodes the full ASCII set (letters, digits, and symbols), so it carries alphanumeric references as well as numbers. It is the workhorse of shipping and courier labels - UPS, FedEx, and DHL tracking numbers, plus GS1-128 supply-chain labels - and of tickets, badges, and asset tags.",
            FontSize = 10,
            Color = ColorPalette.Slate[600],
            Margin = new Thickness(0, 0, 0, 6)
        };
        column1.Add(text5);
        TextBlock text6 = new()
        {
            Text = "Symbology: Code 128 (ISO/IEC 15417)",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 0, 0, 2)
        };
        column1.Add(text6);
        TextBlock text7 = new()
        {
            Text = "Subsets A/B/C with automatic selection",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 0, 0, 2)
        };
        column1.Add(text7);
        TextBlock text8 = new()
        {
            Text = "Modulo-103 check character",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 0, 0, 2)
        };
        column1.Add(text8);
        row1.Add(column1, 1);
        doc.Add(row1);
        Line divider1 = new()
        {
            Color = ColorPalette.Slate[200],
            Margin = new Thickness(0, 20, 0, 20)
        };
        doc.Add(divider1);
        TextBlock text9 = new()
        {
            Text = "EAN-13",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            Color = ColorPalette.Indigo[600],
            Margin = new Thickness(0, 0, 0, 10)
        };
        doc.Add(text9);
        HorizontalStack row2 = new()
        {
            Spacing = 20,
            WidthDistribution = WidthDistribution.Weighted
        };
        Barcode barcode2 = new()
        {
            Value = "400638133393",
            Symbology = BarcodeSymbology.Ean13,
            Height = 55,
            AltText = "EAN-13 - the EU / UK retail product barcode: 400638133393"
        };
        Container card2 = new()
        {
            Padding = new Thickness(12),
            BackgroundColor = Color.White,
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            Child = barcode2
        };
        row2.Add(card2, 1);
        VerticalStack column2 = new();
        TextBlock text10 = new()
        {
            Text = "EAN-13 - the EU / UK retail product barcode",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 13,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 6)
        };
        column2.Add(text10);
        TextBlock text11 = new()
        {
            Text = "The product barcode scanned at virtually every grocery and retail till outside North America. Twelve digits identify the product (a GS1 company prefix plus an item reference) and the thirteenth is a computed check digit. The leading digit is encoded implicitly through the bar parity of the left-hand half.",
            FontSize = 10,
            Color = ColorPalette.Slate[600],
            Margin = new Thickness(0, 0, 0, 6)
        };
        column2.Add(text11);
        TextBlock text12 = new()
        {
            Text = "Symbology: EAN-13 (ISO/IEC 15420)",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 0, 0, 2)
        };
        column2.Add(text12);
        TextBlock text13 = new()
        {
            Text = "12 digits in, check digit appended",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 0, 0, 2)
        };
        column2.Add(text13);
        TextBlock text14 = new()
        {
            Text = "GS1 General Specifications",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 0, 0, 2)
        };
        column2.Add(text14);
        row2.Add(column2, 1);
        doc.Add(row2);
        Line divider2 = new()
        {
            Color = ColorPalette.Slate[200],
            Margin = new Thickness(0, 20, 0, 20)
        };
        doc.Add(divider2);
        TextBlock text15 = new()
        {
            Text = "UPC-A",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            Color = ColorPalette.Indigo[600],
            Margin = new Thickness(0, 0, 0, 10)
        };
        doc.Add(text15);
        HorizontalStack row3 = new()
        {
            Spacing = 20,
            WidthDistribution = WidthDistribution.Weighted
        };
        Barcode barcode3 = new()
        {
            Value = "03600029145",
            Symbology = BarcodeSymbology.UpcA,
            Height = 55,
            AltText = "UPC-A - the US / Canada retail product barcode: 03600029145"
        };
        Container card3 = new()
        {
            Padding = new Thickness(12),
            BackgroundColor = Color.White,
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            Child = barcode3
        };
        row3.Add(card3, 1);
        VerticalStack column3 = new();
        TextBlock text16 = new()
        {
            Text = "UPC-A - the US / Canada retail product barcode",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 13,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 6)
        };
        column3.Add(text16);
        TextBlock text17 = new()
        {
            Text = "The retail product barcode of the United States and Canada. Eleven digits plus a check digit. Structurally it is an EAN-13 with an implicit leading zero, so European scanners read it natively and the two formats share an encoder.",
            FontSize = 10,
            Color = ColorPalette.Slate[600],
            Margin = new Thickness(0, 0, 0, 6)
        };
        column3.Add(text17);
        TextBlock text18 = new()
        {
            Text = "Symbology: UPC-A (ISO/IEC 15420)",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 0, 0, 2)
        };
        column3.Add(text18);
        TextBlock text19 = new()
        {
            Text = "11 digits in, check digit appended",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 0, 0, 2)
        };
        column3.Add(text19);
        TextBlock text20 = new()
        {
            Text = "Encoded as a leading-zero EAN-13",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 0, 0, 2)
        };
        column3.Add(text20);
        row3.Add(column3, 1);
        doc.Add(row3);
        Line divider3 = new()
        {
            Color = ColorPalette.Slate[200],
            Margin = new Thickness(0, 20, 0, 20)
        };
        doc.Add(divider3);
        TextBlock text21 = new()
        {
            Text = "ITF-14",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            Color = ColorPalette.Indigo[600],
            Margin = new Thickness(0, 0, 0, 10)
        };
        doc.Add(text21);
        HorizontalStack row4 = new()
        {
            Spacing = 20,
            WidthDistribution = WidthDistribution.Weighted
        };
        Barcode barcode4 = new()
        {
            Value = "1061414100041",
            Symbology = BarcodeSymbology.Itf14,
            Height = 55,
            AltText = "ITF-14 - the outer-case / shipping barcode: 1061414100041"
        };
        Container card4 = new()
        {
            Padding = new Thickness(12),
            BackgroundColor = Color.White,
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            Child = barcode4
        };
        row4.Add(card4, 1);
        VerticalStack column4 = new();
        TextBlock text22 = new()
        {
            Text = "ITF-14 - the outer-case / shipping barcode",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 13,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 6)
        };
        column4.Add(text22);
        TextBlock text23 = new()
        {
            Text = "Interleaved 2 of 5, fourteen digits. The GS1 standard for shipping cartons and outer cases (the box a retail unit ships in). It is printed directly on corrugated cardboard, so it is framed by heavy \"bearer bars\" that even out the print and stop a partial scan from being misread as a shorter code.",
            FontSize = 10,
            Color = ColorPalette.Slate[600],
            Margin = new Thickness(0, 0, 0, 6)
        };
        column4.Add(text23);
        TextBlock text24 = new()
        {
            Text = "Symbology: ITF-14 (ISO/IEC 16390 + GS1)",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 0, 0, 2)
        };
        column4.Add(text24);
        TextBlock text25 = new()
        {
            Text = "13 digits in, check digit appended",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 0, 0, 2)
        };
        column4.Add(text25);
        TextBlock text26 = new()
        {
            Text = "Bearer bars drawn by default",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 0, 0, 2)
        };
        column4.Add(text26);
        row4.Add(column4, 1);
        doc.Add(row4);
        return doc;
    }
}
