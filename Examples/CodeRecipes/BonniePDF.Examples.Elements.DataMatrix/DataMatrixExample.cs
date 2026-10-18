using BonniePDF;
using DataMatrixSymbol = BonniePDF.DataMatrix;

namespace BonniePDF.Examples.Elements.DataMatrix;

/// <summary>Native vector Data Matrix symbols with readable explanations and accessible descriptions.</summary>
[System.ComponentModel.DisplayName("Data Matrix")]
[System.ComponentModel.Category("Elements")]
[System.ComponentModel.Description("Create vector symbols with sample data, readable labels, and alt text.")]
public static class DataMatrixExample
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
        doc.Metadata.Title = "Data Matrix";
        doc.Metadata.Author = "BonniePDF";
        TextBlock title = new()
        {
            Text = "Data Matrix Generator",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 20)
        };
        doc.Add(title);
        TextBlock introduction = new()
        {
            Text = "BonniePDF includes a native, vector-based Data Matrix (ECC200) element implementing a full ISO/IEC 16022 encoder: automatic encodation across ASCII, C40, Text, X12, EDIFACT and Base-256 schemes, interleaved Reed-Solomon error correction, and both square and rectangular symbols. Because it draws vector shapes rather than a bitmap, every symbol is razor-sharp at any zoom and barely adds to the file size.",
            Margin = new Thickness(0, 0, 0, 20)
        };
        doc.Add(introduction);
        TextBlock dataMatrixHeading = new()
        {
            Text = "Basic Data Matrix",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            Color = ColorPalette.Indigo[600],
            Margin = new Thickness(0, 0, 0, 10)
        };
        doc.Add(dataMatrixHeading);
        HorizontalStack row1 = new()
        {
            Spacing = 20,
            WidthDistribution = WidthDistribution.Weighted
        };
        DataMatrixSymbol symbol1 = new()
        {
            Data = "https://www.bonniepdf.dev",
            Size = 120,
            AltText = "Data Matrix linking to https://www.bonniepdf.dev"
        };
        Container card1 = new()
        {
            Padding = new Thickness(10),
            Child = symbol1
        };
        row1.Add(card1, 1);
        VerticalStack column1 = new();
        TextBlock text4 = new()
        {
            Text = "A simple square symbol",
            FontName = StandardFonts.HelveticaBold,
            Margin = new Thickness(0, 0, 0, 8)
        };
        column1.Add(text4);
        TextBlock text5 = new()
        {
            Text = "Size: 120pt",
            Margin = new Thickness(0, 0, 0, 4)
        };
        column1.Add(text5);
        TextBlock text6 = new()
        {
            Text = "Quiet Zone: 2 modules",
            Margin = new Thickness(0, 0, 0, 4)
        };
        column1.Add(text6);
        TextBlock text7 = new()
        {
            Text = "Encodation: chosen automatically",
            Margin = new Thickness(0, 0, 0, 4)
        };
        column1.Add(text7);
        TextBlock text8 = new()
        {
            Text = "Data: https://www.bonniepdf.dev"
        };
        column1.Add(text8);
        row1.Add(column1, 2);
        doc.Add(row1);
        Line divider1 = new()
        {
            Color = ColorPalette.Slate[200],
            Margin = new Thickness(0, 20, 0, 20)
        };
        doc.Add(divider1);
        TextBlock text9 = new()
        {
            Text = "Automatic Encodation",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            Color = ColorPalette.Indigo[600],
            Margin = new Thickness(0, 0, 0, 10)
        };
        doc.Add(text9);
        TextBlock text10 = new()
        {
            Text = "The encoder analyses the message and selects the most compact scheme automatically - numeric, alphanumeric, structured, or raw bytes for Unicode - so dense data still fits a small symbol.",
            Margin = new Thickness(0, 0, 0, 15)
        };
        doc.Add(text10);
        HorizontalStack row2 = new()
        {
            Spacing = 15,
            WidthDistribution = WidthDistribution.Equal
        };
        VerticalStack column2 = new();
        DataMatrixSymbol symbol2 = new()
        {
            Data = "0123456789012345",
            Size = 95,
            AltText = "Data Matrix encoding 0123456789012345"
        };
        Container card2 = new()
        {
            Padding = new Thickness(10),
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            Child = symbol2
        };
        column2.Add(card2);
        TextBlock text11 = new()
        {
            Text = "Numeric",
            FontName = StandardFonts.HelveticaBold,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        column2.Add(text11);
        TextBlock text12 = new()
        {
            Text = "Digit pairs",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center
        };
        column2.Add(text12);
        TextBlock text13 = new()
        {
            Text = "0123456789012345",
            FontSize = 8,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        };
        column2.Add(text13);
        row2.Add(column2);
        VerticalStack column3 = new();
        DataMatrixSymbol symbol3 = new()
        {
            Data = "SHIP-LABEL-A7",
            Size = 95,
            AltText = "Data Matrix encoding SHIP-LABEL-A7"
        };
        Container card3 = new()
        {
            Padding = new Thickness(10),
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            Child = symbol3
        };
        column3.Add(card3);
        TextBlock text14 = new()
        {
            Text = "Alphanumeric",
            FontName = StandardFonts.HelveticaBold,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        column3.Add(text14);
        TextBlock text15 = new()
        {
            Text = "Text scheme",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center
        };
        column3.Add(text15);
        TextBlock text16 = new()
        {
            Text = "SHIP-LABEL-A7",
            FontSize = 8,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        };
        column3.Add(text16);
        row2.Add(column3);
        VerticalStack column4 = new();
        DataMatrixSymbol symbol4 = new()
        {
            Data = "ABCDEF1234",
            Size = 95,
            AltText = "Data Matrix encoding ABCDEF1234"
        };
        Container card4 = new()
        {
            Padding = new Thickness(10),
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            Child = symbol4
        };
        column4.Add(card4);
        TextBlock text17 = new()
        {
            Text = "Structured",
            FontName = StandardFonts.HelveticaBold,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        column4.Add(text17);
        TextBlock text18 = new()
        {
            Text = "X12 / EDIFACT",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center
        };
        column4.Add(text18);
        TextBlock text19 = new()
        {
            Text = "ABCDEF1234",
            FontSize = 8,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        };
        column4.Add(text19);
        row2.Add(column4);
        VerticalStack column5 = new();
        DataMatrixSymbol symbol5 = new()
        {
            Data = "café · €uro",
            Size = 95,
            AltText = "Data Matrix encoding café · €uro"
        };
        Container card5 = new()
        {
            Padding = new Thickness(10),
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            Child = symbol5
        };
        column5.Add(card5);
        TextBlock text20 = new()
        {
            Text = "Unicode",
            FontName = StandardFonts.HelveticaBold,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        column5.Add(text20);
        TextBlock text21 = new()
        {
            Text = "Base-256 (UTF-8)",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center
        };
        column5.Add(text21);
        TextBlock text22 = new()
        {
            Text = "café · €uro",
            FontSize = 8,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        };
        column5.Add(text22);
        row2.Add(column5);
        doc.Add(row2);
        Line divider2 = new()
        {
            Color = ColorPalette.Slate[200],
            Margin = new Thickness(0, 20, 0, 20)
        };
        doc.Add(divider2);
        VerticalStack column6 = new()
        {
            BreakAllowed = false
        };
        TextBlock text23 = new()
        {
            Text = "Rectangular Symbols",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            Color = ColorPalette.Indigo[600],
            Margin = new Thickness(0, 0, 0, 10)
        };
        column6.Add(text23);
        TextBlock text24 = new()
        {
            Text = "Unlike QR codes, Data Matrix supports rectangular symbols - handy for marking long, narrow parts and labels. Set `rectangular = true` and a short message will use the most compact rectangular size.",
            Margin = new Thickness(0, 0, 0, 15)
        };
        column6.Add(text24);
        HorizontalStack row3 = new()
        {
            Spacing = 20,
            WidthDistribution = WidthDistribution.Equal
        };
        VerticalStack column7 = new();
        DataMatrixSymbol symbol6 = new()
        {
            Data = "12345",
            Size = 140,
            AltText = "Rectangular Data Matrix encoding 12345",
            Rectangular = true
        };
        Container card6 = new()
        {
            Padding = new Thickness(10),
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            Child = symbol6
        };
        column7.Add(card6);
        TextBlock text25 = new()
        {
            Text = "18 × 8",
            FontName = StandardFonts.HelveticaBold,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        column7.Add(text25);
        TextBlock text26 = new()
        {
            Text = "12345",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center
        };
        column7.Add(text26);
        row3.Add(column7);
        VerticalStack column8 = new();
        DataMatrixSymbol symbol7 = new()
        {
            Data = "PART-99A",
            Size = 140,
            AltText = "Rectangular Data Matrix encoding PART-99A",
            Rectangular = true
        };
        Container card7 = new()
        {
            Padding = new Thickness(10),
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            Child = symbol7
        };
        column8.Add(card7);
        TextBlock text27 = new()
        {
            Text = "32 × 8",
            FontName = StandardFonts.HelveticaBold,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        column8.Add(text27);
        TextBlock text28 = new()
        {
            Text = "PART-99A",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center
        };
        column8.Add(text28);
        row3.Add(column8);
        VerticalStack column9 = new();
        DataMatrixSymbol symbol8 = new()
        {
            Data = "LOT 2026-06",
            Size = 140,
            AltText = "Rectangular Data Matrix encoding LOT 2026-06",
            Rectangular = true
        };
        Container card8 = new()
        {
            Padding = new Thickness(10),
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            Child = symbol8
        };
        column9.Add(card8);
        TextBlock text29 = new()
        {
            Text = "26 × 12",
            FontName = StandardFonts.HelveticaBold,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        column9.Add(text29);
        TextBlock text30 = new()
        {
            Text = "LOT 2026-06",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center
        };
        column9.Add(text30);
        row3.Add(column9);
        column6.Add(row3);
        doc.Add(column6);
        Line divider3 = new()
        {
            Color = ColorPalette.Slate[200],
            Margin = new Thickness(0, 20, 0, 20)
        };
        doc.Add(divider3);
        VerticalStack column10 = new()
        {
            BreakAllowed = false
        };
        TextBlock text31 = new()
        {
            Text = "Colours & Backgrounds",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            Color = ColorPalette.Indigo[600],
            Margin = new Thickness(0, 0, 0, 10)
        };
        column10.Add(text31);
        HorizontalStack row4 = new()
        {
            Spacing = 20,
            WidthDistribution = WidthDistribution.Equal
        };
        VerticalStack column11 = new();
        DataMatrixSymbol symbol9 = new()
        {
            Data = "ID-7782",
            Size = 120,
            AltText = "Data Matrix encoding ID-7782",
            ForegroundColor = ColorPalette.Blue[700]
        };
        Container card9 = new()
        {
            Padding = new Thickness(10),
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Blue[200]),
            Child = symbol9
        };
        column11.Add(card9);
        TextBlock text32 = new()
        {
            Text = "Custom Foreground",
            FontName = StandardFonts.HelveticaBold,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        column11.Add(text32);
        TextBlock text33 = new()
        {
            Text = "ID-7782",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center
        };
        column11.Add(text33);
        row4.Add(column11);
        VerticalStack column12 = new();
        DataMatrixSymbol symbol10 = new()
        {
            BackgroundColor = ColorPalette.Rose[50],
            Data = "BATCH-44",
            Size = 120,
            AltText = "Data Matrix encoding BATCH-44",
            ForegroundColor = ColorPalette.Rose[900]
        };
        Container card10 = new()
        {
            Padding = new Thickness(10),
            BackgroundColor = ColorPalette.Rose[50],
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Rose[200]),
            Child = symbol10
        };
        column12.Add(card10);
        TextBlock text34 = new()
        {
            Text = "Custom Background",
            FontName = StandardFonts.HelveticaBold,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        column12.Add(text34);
        TextBlock text35 = new()
        {
            Text = "BATCH-44",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center
        };
        column12.Add(text35);
        row4.Add(column12);
        VerticalStack column13 = new();
        DataMatrixSymbol symbol11 = new()
        {
            BackgroundColor = ColorPalette.Amber[100],
            Data = "A-1029",
            Size = 120,
            AltText = "Data Matrix encoding A-1029",
            ForegroundColor = ColorPalette.Slate[900]
        };
        Container card11 = new()
        {
            Padding = new Thickness(10),
            BackgroundColor = ColorPalette.Amber[100],
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Amber[300]),
            Child = symbol11
        };
        column13.Add(card11);
        TextBlock text36 = new()
        {
            Text = "High Contrast",
            FontName = StandardFonts.HelveticaBold,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        column13.Add(text36);
        TextBlock text37 = new()
        {
            Text = "A-1029",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center
        };
        column13.Add(text37);
        row4.Add(column13);
        column10.Add(row4);
        doc.Add(column10);
        return doc;
    }
}
