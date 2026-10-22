using BonniePDF;

namespace BonniePDF.Examples.BarcodesQrCodes.QRCodes;

/// <summary>Native vector QR codes with readable explanations and accessible descriptions.</summary>
[System.ComponentModel.DisplayName("QR Codes")]
[System.ComponentModel.Category("Barcodes & QR Codes")]
[System.ComponentModel.Description("Create vector symbols with sample data, readable labels, and alt text.")]
public static class QRCodesExample
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
        doc.Metadata.Title = "QR Codes";
        doc.Metadata.Author = "BonniePDF";
        TextBlock title = new()
        {
            Text = "QR Code Generator",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 20)
        };
        doc.Add(title);
        TextBlock introduction = new()
        {
            Text = "BonniePDF provides a native, high-performance, vector-based QR Code element that implements a full ISO/IEC 18004 compliant encoder. Because it produces vector shapes rather than bitmaps, the generated codes are razor-sharp at any zoom level and keep the PDF file size extremely small.",
            Margin = new Thickness(0, 0, 0, 20)
        };
        doc.Add(introduction);
        TextBlock basicQrHeading = new()
        {
            Text = "Basic QR Code",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            Color = ColorPalette.Indigo[600],
            Margin = new Thickness(0, 0, 0, 10)
        };
        doc.Add(basicQrHeading);
        HorizontalStack row1 = new()
        {
            Spacing = 20,
            WidthDistribution = WidthDistribution.Weighted
        };
        QRCode code1 = new()
        {
            Data = "https://bonniepdf.com",
            Size = 130,
            QuietZone = 2,
            AltText = "QR code linking to bonniepdf.com"
        };
        Container card1 = new()
        {
            Padding = new Thickness(10),
            Child = code1
        };
        row1.Add(card1, 1);
        VerticalStack column1 = new();
        TextBlock text4 = new()
        {
            Text = "A simple QR Code",
            FontName = StandardFonts.HelveticaBold,
            Margin = new Thickness(0, 0, 0, 8)
        };
        column1.Add(text4);
        TextBlock text5 = new()
        {
            Text = "Size: 130pt",
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
            Text = "Error Correction: Medium (default)",
            Margin = new Thickness(0, 0, 0, 4)
        };
        column1.Add(text7);
        TextBlock text8 = new()
        {
            Text = "Data: https://bonniepdf.com"
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
            Text = "Error Correction Levels",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            Color = ColorPalette.Indigo[600],
            Margin = new Thickness(0, 0, 0, 10)
        };
        doc.Add(text9);
        TextBlock text10 = new()
        {
            Text = "Higher error correction levels provide more redundancy at the cost of denser modules and reduced data capacity. They are useful if the code might be damaged or if you intend to overlay a logo.",
            Margin = new Thickness(0, 0, 0, 15)
        };
        doc.Add(text10);
        HorizontalStack row2 = new()
        {
            Spacing = 15,
            WidthDistribution = WidthDistribution.Equal
        };
        VerticalStack column2 = new();
        QRCode code2 = new()
        {
            Data = "https://github.com/bonniepdf",
            QuietZone = 1,
            AltText = "QR code linking to github.com/bonniepdf ( error correction)",
            ErrorCorrection = QrErrorCorrection.Low
        };
        Container card2 = new()
        {
            Padding = new Thickness(10),
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            Child = code2
        };
        column2.Add(card2);
        TextBlock text11 = new()
        {
            Text = "Low",
            FontName = StandardFonts.HelveticaBold,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        column2.Add(text11);
        TextBlock text12 = new()
        {
            Text = "~7% restoration",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center
        };
        column2.Add(text12);
        TextBlock text13 = new()
        {
            Text = "https://github.com/bonniepdf",
            FontSize = 8,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        };
        column2.Add(text13);
        row2.Add(column2);
        VerticalStack column3 = new();
        QRCode code3 = new()
        {
            Data = "https://github.com/bonniepdf",
            QuietZone = 1,
            AltText = "QR code linking to github.com/bonniepdf ( error correction)"
        };
        Container card3 = new()
        {
            Padding = new Thickness(10),
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            Child = code3
        };
        column3.Add(card3);
        TextBlock text14 = new()
        {
            Text = "Medium",
            FontName = StandardFonts.HelveticaBold,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        column3.Add(text14);
        TextBlock text15 = new()
        {
            Text = "~15% restoration",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center
        };
        column3.Add(text15);
        TextBlock text16 = new()
        {
            Text = "https://github.com/bonniepdf",
            FontSize = 8,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        };
        column3.Add(text16);
        row2.Add(column3);
        VerticalStack column4 = new();
        QRCode code4 = new()
        {
            Data = "https://github.com/bonniepdf",
            QuietZone = 1,
            AltText = "QR code linking to github.com/bonniepdf ( error correction)",
            ErrorCorrection = QrErrorCorrection.Quartile
        };
        Container card4 = new()
        {
            Padding = new Thickness(10),
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            Child = code4
        };
        column4.Add(card4);
        TextBlock text17 = new()
        {
            Text = "Quartile",
            FontName = StandardFonts.HelveticaBold,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        column4.Add(text17);
        TextBlock text18 = new()
        {
            Text = "~25% restoration",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center
        };
        column4.Add(text18);
        TextBlock text19 = new()
        {
            Text = "https://github.com/bonniepdf",
            FontSize = 8,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        };
        column4.Add(text19);
        row2.Add(column4);
        VerticalStack column5 = new();
        QRCode code5 = new()
        {
            Data = "https://github.com/bonniepdf",
            QuietZone = 1,
            AltText = "QR code linking to github.com/bonniepdf ( error correction)",
            ErrorCorrection = QrErrorCorrection.High
        };
        Container card5 = new()
        {
            Padding = new Thickness(10),
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            Child = code5
        };
        column5.Add(card5);
        TextBlock text20 = new()
        {
            Text = "High",
            FontName = StandardFonts.HelveticaBold,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        column5.Add(text20);
        TextBlock text21 = new()
        {
            Text = "~30% restoration",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center
        };
        column5.Add(text21);
        TextBlock text22 = new()
        {
            Text = "https://github.com/bonniepdf",
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
            Text = "Colors & Backgrounds",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            Color = ColorPalette.Indigo[600],
            Margin = new Thickness(0, 0, 0, 10)
        };
        column6.Add(text23);
        HorizontalStack row3 = new()
        {
            Spacing = 20,
            WidthDistribution = WidthDistribution.Equal
        };
        VerticalStack column7 = new();
        TextBlock text24 = new()
        {
            Text = "Custom Foreground",
            FontName = StandardFonts.HelveticaBold,
            Margin = new Thickness(0, 0, 0, 8)
        };
        column7.Add(text24);
        QRCode code6 = new()
        {
            Data = "tel:+1234567890",
            Size = 120,
            QuietZone = 2,
            AltText = "QR code that dials the phone number +1 234 567 890",
            ForegroundColor = ColorPalette.Blue[700]
        };
        Container card6 = new()
        {
            Padding = new Thickness(10),
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Blue[200]),
            Child = code6
        };
        column7.Add(card6);
        TextBlock text25 = new()
        {
            Text = "tel:+1234567890",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        column7.Add(text25);
        row3.Add(column7);
        VerticalStack column8 = new();
        TextBlock text26 = new()
        {
            Text = "Custom Background",
            FontName = StandardFonts.HelveticaBold,
            Margin = new Thickness(0, 0, 0, 8)
        };
        column8.Add(text26);
        QRCode code7 = new()
        {
            BackgroundColor = ColorPalette.Rose[50],
            Data = "mailto:hello@example.com",
            Size = 120,
            QuietZone = 2,
            AltText = "QR code that composes an email to hello@example.com",
            ForegroundColor = ColorPalette.Rose[900]
        };
        Container card7 = new()
        {
            Padding = new Thickness(10),
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Rose[200]),
            Child = code7
        };
        column8.Add(card7);
        TextBlock text27 = new()
        {
            Text = "mailto:hello@example.com",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        column8.Add(text27);
        row3.Add(column8);
        VerticalStack column9 = new();
        TextBlock text28 = new()
        {
            Text = "Transparent Background",
            FontName = StandardFonts.HelveticaBold,
            Margin = new Thickness(0, 0, 0, 8)
        };
        column9.Add(text28);
        QRCode code8 = new()
        {
            Data = "WIFI:S:MyNetwork;T:WPA;P:password;;",
            Size = 120,
            QuietZone = 2,
            AltText = "QR code to join the MyNetwork Wi-Fi network",
            ForegroundColor = ColorPalette.Amber[900],
            DrawBackground = false
        };
        Container card8 = new()
        {
            Padding = new Thickness(10),
            BackgroundColor = ColorPalette.Amber[100],
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Amber[300]),
            Child = code8
        };
        column9.Add(card8);
        TextBlock text29 = new()
        {
            Text = "WIFI:S:MyNetwork;...",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        column9.Add(text29);
        row3.Add(column9);
        column6.Add(row3);
        doc.Add(column6);
        return doc;
    }
}
