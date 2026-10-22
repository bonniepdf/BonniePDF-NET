using BonniePDF;
using BonniePDF.Fluent;

namespace BonniePDF.Examples.BarcodesQrCodes.Barcodes;

/// <summary>
/// Demonstrates the four native 1D barcode symbologies (Code 128, EAN-13,
/// UPC-A, ITF-14) using the Fluent API, with a description of what each
/// format is used for.
/// </summary>
[System.ComponentModel.DisplayName("Barcodes (Fluent API)")]
[System.ComponentModel.Category("Barcodes & QR Codes")]
[System.ComponentModel.Description(
    "Generating native, vector-based 1D barcodes (Code 128, EAN-13, UPC-A, ITF-14) with automatic check digits and human-readable labels using the Fluent API.")]
public static class BarcodesExampleFluent
{
    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        return Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title = "Barcodes (Fluent API)";
                metadata.Author = "BonniePDF";
            });

            doc.WithSettings(settings =>
            {
                settings.PageSize = PdfSize.A4;
                settings.Margin = 50;
                settings.Language = "en-GB";
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
                        .Text("1D Barcodes (Fluent API)")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(24)
                        .Color(ColorPalette.Slate[800])
                        .Margin(0, 0, 0, 16);

                    col.Item()
                        .Text("BonniePDF renders 1D barcodes as native vector graphics - sharp at any zoom, tiny in " +
                              "the PDF, and with zero external dependencies. The four built-in symbologies cover retail " +
                              "and logistics across the UK, EU, and US. Check digits and quiet zones are computed " +
                              "automatically; the human-readable value is printed beneath each symbol.")
                        .Margin(0, 0, 0, 20);

                    // ── Code 128 ───────────────────────────────────────────────
                    BarcodeSection(col,
                        "Code 128 - the universal logistics barcode",
                        "Encodes the full ASCII set (letters, digits, and symbols), so it carries alphanumeric " +
                        "references as well as numbers. It is the workhorse of shipping and courier labels - UPS, " +
                        "FedEx, and DHL tracking numbers, plus GS1-128 supply-chain labels - and of tickets, badges, " +
                        "and asset tags.",
                        "1Z9999W99999999999",
                        BarcodeSymbology.Code128);

                    // ── EAN-13 ─────────────────────────────────────────────────
                    BarcodeSection(col,
                        "EAN-13 - the EU / UK retail product barcode",
                        "The product barcode scanned at virtually every grocery and retail till outside North America. " +
                        "Twelve digits identify the product (a GS1 company prefix plus an item reference) and the " +
                        "thirteenth is a computed check digit. The leading digit is encoded implicitly through the bar " +
                        "parity of the left-hand half.",
                        "400638133393",
                        BarcodeSymbology.Ean13);

                    // ── UPC-A ──────────────────────────────────────────────────
                    BarcodeSection(col,
                        "UPC-A - the US / Canada retail product barcode",
                        "The retail product barcode of the United States and Canada. Eleven digits plus a check digit. " +
                        "Structurally it is an EAN-13 with an implicit leading zero, so European scanners read it " +
                        "natively and the two formats share an encoder.",
                        "03600029145",
                        BarcodeSymbology.UpcA);

                    // ── ITF-14 ─────────────────────────────────────────────────
                    BarcodeSection(col,
                        "ITF-14 - the outer-case / shipping barcode",
                        "Interleaved 2 of 5, fourteen digits. The GS1 standard for shipping cartons and outer cases " +
                        "(the box a retail unit ships in). It is printed directly on corrugated cardboard, so it is " +
                        "framed by heavy \"bearer bars\" that even out the print and stop a partial scan from being " +
                        "misread as a shorter code.",
                        "1061414100041",
                        BarcodeSymbology.Itf14,
                        isLast: true);
                });
            });
        }).Build();
    }

    private static void BarcodeSection(ColumnDescriptor col, string title, string description,
        string value, BarcodeSymbology symbology, bool isLast = false)
    {
        col.Item()
            .Text(title)
            .Font(StandardFonts.HelveticaBold)
            .FontSize(15)
            .Color(ColorPalette.Indigo[600])
            .Margin(0, 0, 0, 8);

        col.Item()
            .Text(description)
            .FontSize(10)
            .Color(ColorPalette.Slate[600])
            .Margin(0, 0, 0, 12);

        col.Item()
            .Background(Color.White)
            .Border(1, ColorPalette.Slate[200])
            .Padding(14)
            .AlignCenter()
            .Barcode(value, symbology)
            .Height(55)
            .AltText($"{title}: {value}");

        if (!isLast)
            col.Item().Line(1, ColorPalette.Slate[200]).Margin(0, 20, 0, 20);
    }
}
