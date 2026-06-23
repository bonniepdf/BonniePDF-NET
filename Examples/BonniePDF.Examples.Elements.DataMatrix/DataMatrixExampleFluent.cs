using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Descriptors;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Elements.DataMatrix;

/// <summary>
/// Demonstrates generating square and rectangular Data Matrix (ECC200) symbols
/// using the Fluent API.
/// </summary>
[System.ComponentModel.DisplayName("Data Matrix (Fluent API)")]
[System.ComponentModel.Category("Elements")]
[System.ComponentModel.Description("Generating ISO/IEC 16022 compliant Data Matrix symbols with automatic encodation, rectangular sizes, quiet zones, and custom colours using the Fluent API.")]
public static class DataMatrixExampleFluent
{
    public static Document Generate()
    {
        return Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title = "Data Matrix (Fluent API)";
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
                        .Text("Data Matrix Generator (Fluent API)")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(24)
                        .TextColor(ColorPalette.Slate[800])
                        .Margin(0, 0, 0, 20);

                    col.Item()
                        .Text("BonniePDF includes a native, high-performance, vector-based Data Matrix (ECC200) " +
                              "element implementing a full ISO/IEC 16022 encoder: automatic encodation across ASCII, C40, " +
                              "Text, X12, EDIFACT and Base-256 schemes, interleaved Reed-Solomon error correction, and " +
                              "both square and rectangular symbols. Because it produces vector shapes rather than bitmaps, " +
                              "every symbol is razor-sharp at any zoom level and keeps the PDF file size extremely small.")
                        .Margin(0, 0, 0, 20);

                    // ── Section 1: Basic symbol ────────────────────────────────
                    SectionHeader(col, "Basic Data Matrix");

                    col.Item().Row(row =>
                    {
                        row.Spacing(20);

                        row.ConstantItem(140)
                            .Padding(10)
                            .DataMatrix("https://www.bonniepdf.dev", size: 120)
                            .QuietZone(2)
                            .AltText("Data matrix encoding https://www.bonniepdf.dev");

                        row.RelativeItem()
                            .Column(details =>
                            {
                                details.Item().Text("A simple square symbol").Font(StandardFonts.HelveticaBold).Margin(0, 0, 0, 8);
                                details.Item().Text("Size: 120pt").Margin(0, 0, 0, 4);
                                details.Item().Text("Quiet Zone: 2 modules").Margin(0, 0, 0, 4);
                                details.Item().Text("Encodation: chosen automatically").Margin(0, 0, 0, 4);
                                details.Item().Text("Data: https://www.bonniepdf.dev");
                            });
                    });

                    col.Item().Line(1, ColorPalette.Slate[200]).Margin(0, 20, 0, 20);

                    // ── Section 2: Automatic encodation ────────────────────────
                    SectionHeader(col, "Automatic Encodation");

                    col.Item().Text("The encoder analyses the message and selects the most compact scheme " +
                                  "automatically - numeric, alphanumeric, structured, or raw bytes for Unicode - " +
                                  "so dense data still fits a small symbol.").Margin(0, 0, 0, 15);

                    col.Item().Row(row =>
                    {
                        row.Spacing(15);

                        EncodationCard(row, "0123456789012345", "Numeric", "Digit pairs");
                        EncodationCard(row, "SHIP-LABEL-A7", "Alphanumeric", "Text scheme");
                        EncodationCard(row, "ABCDEF1234", "Structured", "X12 / EDIFACT");
                        EncodationCard(row, "café · €uro", "Unicode", "Base-256 (UTF-8)");
                    });

                    col.Item().Line(1, ColorPalette.Slate[200]).Margin(0, 20, 0, 20);

                    // ── Section 3: Rectangular symbols ─────────────────────────
                    SectionHeader(col, "Rectangular Symbols");

                    col.Item().Text("Unlike QR codes, Data Matrix supports rectangular symbols - handy for marking " +
                                  "long, narrow parts and labels. Set Rectangular(true) and a short message will use " +
                                  "the most compact rectangular size.").Margin(0, 0, 0, 15);

                    col.Item().Row(row =>
                    {
                        row.Spacing(20);

                        RectangularCard(row, "12345", "18 x 8");
                        RectangularCard(row, "PART-99A", "32 x 8");
                        RectangularCard(row, "LOT 2026-06", "26 x 12");
                    });

                    col.Item().Line(1, ColorPalette.Slate[200]).Margin(0, 20, 0, 20);

                    // ── Section 4: Colours ─────────────────────────────────────
                    SectionHeader(col, "Colours & Backgrounds");

                    col.Item().Row(row =>
                    {
                        row.Spacing(20);

                        // Custom Foreground
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Custom Foreground").Margin(0, 0, 0, 8).Font(StandardFonts.HelveticaBold);
                            c.Item().Border(1, ColorPalette.Blue[200]).Padding(10).AlignCenter()
                                .DataMatrix("ID-7782", size: 120)
                                .ForegroundColor(ColorPalette.Blue[700])
                                .QuietZone(2)
                                .AltText("Data matrix encoding ID-7782");
                            c.Item().AlignCenter().Margin(0, 8, 0, 0).Text("ID-7782").FontSize(9).TextColor(ColorPalette.Slate[500]);
                        });

                        // Custom Background
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Custom Background").Margin(0, 0, 0, 8).Font(StandardFonts.HelveticaBold);
                            c.Item().Border(1, ColorPalette.Rose[200]).Padding(10).AlignCenter()
                                .DataMatrix("BATCH-44", size: 120)
                                .ForegroundColor(ColorPalette.Rose[900])
                                .BackgroundColor(ColorPalette.Rose[50])
                                .QuietZone(2)
                                .AltText("Data matrix encoding BATCH-44");
                            c.Item().AlignCenter().Margin(0, 8, 0, 0).Text("BATCH-44").FontSize(9).TextColor(ColorPalette.Slate[500]);
                        });

                        // High Contrast (transparent, container background shows through)
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("High Contrast").Margin(0, 0, 0, 8).Font(StandardFonts.HelveticaBold);
                            c.Item()
                                .Background(ColorPalette.Amber[100])
                                .Border(1, ColorPalette.Amber[300])
                                .Padding(10)
                                .AlignCenter()
                                .DataMatrix("A-1029", size: 120)
                                .ForegroundColor(ColorPalette.Slate[900])
                                .DrawBackground(false)
                                .QuietZone(2)
                                .AltText("Data matrix encoding A-1029");
                            c.Item().AlignCenter().Margin(0, 8, 0, 0).Text("A-1029").FontSize(9).TextColor(ColorPalette.Slate[500]);
                        });
                    });
                });
            });

        }).Build();
    }

    private static void EncodationCard(RowDescriptor row, string data, string label, string detail)
    {
        row.RelativeItem().Column(card =>
        {
            card.Item().Background(ColorPalette.Slate[50]).Border(1, ColorPalette.Slate[200]).Padding(10).AlignCenter()
                .DataMatrix(data, size: 95)
                .QuietZone(2)
                .AltText($"Data matrix encoding {data}");

            card.Item().AlignCenter().Margin(0, 8, 0, 0).Text(label).Font(StandardFonts.HelveticaBold);
            card.Item().AlignCenter().Text(detail).FontSize(9).TextColor(ColorPalette.Slate[500]);
            card.Item().AlignCenter().Text(data).FontSize(8).TextColor(ColorPalette.Slate[400]).Margin(0, 4, 0, 0);
        });
    }

    private static void RectangularCard(RowDescriptor row, string data, string label)
    {
        row.RelativeItem().Column(card =>
        {
            card.Item().Border(1, ColorPalette.Slate[200]).Padding(10).AlignCenter()
                .DataMatrix(data, size: 140)
                .Rectangular(true)
                .QuietZone(2)
                .AltText($"Rectangular data matrix encoding {data}");

            card.Item().AlignCenter().Margin(0, 8, 0, 0).Text(label).Font(StandardFonts.HelveticaBold);
            card.Item().AlignCenter().Text(data).FontSize(9).TextColor(ColorPalette.Slate[500]);
        });
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
