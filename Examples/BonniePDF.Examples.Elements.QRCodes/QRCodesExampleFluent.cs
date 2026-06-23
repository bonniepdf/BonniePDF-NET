using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Shapes;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Descriptors;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Elements.QRCodes;

/// <summary>
/// Demonstrates generating standard and customized QR Codes 
/// using the Fluent API.
/// </summary>
[System.ComponentModel.DisplayName("QR Codes (Fluent API)")]
[System.ComponentModel.Category("Elements")]
[System.ComponentModel.Description("Generating ISO/IEC 18004 compliant QR Codes with customized colors, quiet zones, and error correction levels using the Fluent API.")]
public static class QRCodesExampleFluent
{
    public static Document Generate()
    {
        return Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title = "QR Codes (Fluent API)";
                metadata.Author = "BonniePDF";
            });

            doc.WithSettings(settings =>
            {
                settings.PageSize = PdfSize.A4;
                settings.Margin = 50;
            });

            doc.WithDefaultTextStyle(style => style
                .WithFamily(StandardFonts.Helvetica)
                .WithSize(11)
                .WithColor(ColorPalette.Slate[600]));

            doc.Page(page =>
            {
                page.Content().Column(col =>
                {
                    // ── Title ──────────────────────────────────────────────────
                    col.Item()
                        .Text("QR Code Generator (Fluent API)")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(24)
                        .TextColor(ColorPalette.Slate[800])
                        .Margin(0, 0, 0, 20);

                    col.Item()
                        .Text("BonniePDF provides a native, high-performance, vector-based QR Code element " +
                              "that implements a full ISO/IEC 18004 compliant encoder. Because it produces vector shapes " +
                              "rather than bitmaps, the generated codes are razor-sharp at any zoom level and keep " +
                              "the PDF file size extremely small.")
                        .Margin(0, 0, 0, 20);

                    // ── Section 1: Basic QR Code ───────────────────────────────
                    SectionHeader(col, "Basic QR Code");

                    col.Item().Row(row =>
                    {
                        row.Spacing(20);

                        row.ConstantItem(150)
                            .Padding(10)
                            .QRCode("https://bonniepdf.com", size: 130)
                            .QuietZone(2)
                            .AltText("QR code linking to https://bonniepdf.com");

                        row.RelativeItem()
                            .Column(details =>
                            {
                                details.Item().Text("A simple QR Code").Font(StandardFonts.HelveticaBold).Margin(0, 0, 0, 8);
                                details.Item().Text("Size: 130pt").Margin(0, 0, 0, 4);
                                details.Item().Text("Quiet Zone: 2 modules").Margin(0, 0, 0, 4);
                                details.Item().Text("Error Correction: Medium (Default)").Margin(0, 0, 0, 4);
                                details.Item().Text("Data: https://bonniepdf.com");
                            });
                    });
                    
                    col.Item().Line(1, ColorPalette.Slate[200]).Margin(0, 20, 0, 20);

                    // ── Section 2: Error Correction Levels ─────────────────────
                    SectionHeader(col, "Error Correction Levels");
                    
                    col.Item().Text("Higher error correction levels provide more redundancy at the cost of " +
                                  "denser modules and reduced data capacity. They are useful if the code " +
                                  "might be damaged or if you intend to overlay a logo.").Margin(0, 0, 0, 15);

                    col.Item().Row(row =>
                    {
                        row.Spacing(15);
                        
                        string urlData = "https://github.com/bonniepdf";

                        // Low (7%)
                        row.RelativeItem().Column(card =>
                        {
                            card.Item().Background(ColorPalette.Slate[50]).Border(1, ColorPalette.Slate[200]).Padding(10).AlignCenter()
                                .QRCode(urlData, size: 100)
                                .ErrorCorrection(QrErrorCorrection.Low)
                                .QuietZone(1)
                                .AltText("QR code linking to https://github.com/bonniepdf with low error correction");

                            card.Item().AlignCenter().Margin(0, 8, 0, 0).Text("Low").Font(StandardFonts.HelveticaBold);
                            card.Item().AlignCenter().Text("~7% restoration").FontSize(9).TextColor(ColorPalette.Slate[500]);
                            card.Item().AlignCenter().Text(urlData).FontSize(8).TextColor(ColorPalette.Slate[400]).Margin(0, 4, 0, 0);
                        });

                        // Medium (15%)
                        row.RelativeItem().Column(card =>
                        {
                            card.Item().Background(ColorPalette.Slate[50]).Border(1, ColorPalette.Slate[200]).Padding(10).AlignCenter()
                                .QRCode(urlData, size: 100)
                                .ErrorCorrection(QrErrorCorrection.Medium)
                                .QuietZone(1)
                                .AltText("QR code linking to https://github.com/bonniepdf with medium error correction");

                            card.Item().AlignCenter().Margin(0, 8, 0, 0).Text("Medium").Font(StandardFonts.HelveticaBold);
                            card.Item().AlignCenter().Text("~15% restoration").FontSize(9).TextColor(ColorPalette.Slate[500]);
                            card.Item().AlignCenter().Text(urlData).FontSize(8).TextColor(ColorPalette.Slate[400]).Margin(0, 4, 0, 0);
                        });

                        // Quartile (25%)
                        row.RelativeItem().Column(card =>
                        {
                            card.Item().Background(ColorPalette.Slate[50]).Border(1, ColorPalette.Slate[200]).Padding(10).AlignCenter()
                                .QRCode(urlData, size: 100)
                                .ErrorCorrection(QrErrorCorrection.Quartile)
                                .QuietZone(1)
                                .AltText("QR code linking to https://github.com/bonniepdf with quartile error correction");

                            card.Item().AlignCenter().Margin(0, 8, 0, 0).Text("Quartile").Font(StandardFonts.HelveticaBold);
                            card.Item().AlignCenter().Text("~25% restoration").FontSize(9).TextColor(ColorPalette.Slate[500]);
                            card.Item().AlignCenter().Text(urlData).FontSize(8).TextColor(ColorPalette.Slate[400]).Margin(0, 4, 0, 0);
                        });

                        // High (30%)
                        row.RelativeItem().Column(card =>
                        {
                            card.Item().Background(ColorPalette.Slate[50]).Border(1, ColorPalette.Slate[200]).Padding(10).AlignCenter()
                                .QRCode(urlData, size: 100)
                                .ErrorCorrection(QrErrorCorrection.High)
                                .QuietZone(1)
                                .AltText("QR code linking to https://github.com/bonniepdf with high error correction");

                            card.Item().AlignCenter().Margin(0, 8, 0, 0).Text("High").Font(StandardFonts.HelveticaBold);
                            card.Item().AlignCenter().Text("~30% restoration").FontSize(9).TextColor(ColorPalette.Slate[500]);
                            card.Item().AlignCenter().Text(urlData).FontSize(8).TextColor(ColorPalette.Slate[400]).Margin(0, 4, 0, 0);
                        });
                    });
                    
                    col.Item().Line(1, ColorPalette.Slate[200]).Margin(0, 20, 0, 20);

                    // ── Section 3: Colors and Styling ──────────────────────────
                    SectionHeader(col, "Colors & Backgrounds");
                    
                    col.Item().Row(row =>
                    {
                        row.Spacing(20);

                        // Custom Foreground
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Custom Foreground").Margin(0, 0, 0, 8).Font(StandardFonts.HelveticaBold);
                            c.Item().Border(1, ColorPalette.Blue[200]).Padding(10).AlignCenter()
                                .QRCode("tel:+1234567890", size: 120)
                                .ForegroundColor(ColorPalette.Blue[700])
                                .QuietZone(2)
                                .AltText("QR code encoding telephone number +1234567890");
                            c.Item().AlignCenter().Margin(0, 8, 0, 0).Text("tel:+1234567890").FontSize(9).TextColor(ColorPalette.Slate[500]);
                        });

                        // Custom Colors
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Custom Background").Margin(0, 0, 0, 8).Font(StandardFonts.HelveticaBold);
                            c.Item().Border(1, ColorPalette.Rose[200]).Padding(10).AlignCenter()
                                .QRCode("mailto:hello@example.com", size: 120)
                                .ForegroundColor(ColorPalette.Rose[900])
                                .BackgroundColor(ColorPalette.Rose[50])
                                .QuietZone(2)
                                .AltText("QR code encoding email address mailto:hello@example.com");
                            c.Item().AlignCenter().Margin(0, 8, 0, 0).Text("mailto:hello@example.com").FontSize(9).TextColor(ColorPalette.Slate[500]);
                        });

                        // Transparent Background
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Transparent Background").Margin(0, 0, 0, 8).Font(StandardFonts.HelveticaBold);
                            c.Item()
                                .Background(ColorPalette.Amber[100])
                                .Border(1, ColorPalette.Amber[300])
                                .Padding(10)
                                .AlignCenter()
                                .QRCode("WIFI:S:MyNetwork;T:WPA;P:password;;", size: 120)
                                .ForegroundColor(ColorPalette.Amber[900])
                                .DrawBackground(false) // Let the container's background show through
                                .QuietZone(2)
                                .AltText("QR code encoding Wi-Fi network credentials for MyNetwork");
                            c.Item().AlignCenter().Margin(0, 8, 0, 0).Text("WIFI:S:MyNetwork;...").FontSize(9).TextColor(ColorPalette.Slate[500]);
                        });
                    });
                });
            });

        }).Build();

    }

    private static void SectionHeader(ColumnDescriptor col, string title)
    {
        col.Item()
            .Text(title)
            .Font(StandardFonts.HelveticaBold)
            .FontSize(16)
            .TextColor(ColorPalette.Indigo[600])
            .Margin(0, 0, 0, 10);
    }
}

