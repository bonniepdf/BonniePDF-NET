using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.Hospitality;

/// <summary>
/// A hotel guest folio that doubles as the VAT invoice: nightly room charges, restaurant and bar
/// postings, Edinburgh's visitor levy, the deposit and payment, VAT by rate, a signature line and a QR
/// code for a copy.
/// </summary>
[System.ComponentModel.DisplayName("Hotel Guest Folio")]
[System.ComponentModel.Category("Hospitality")]
[System.ComponentModel.Description("A hotel guest folio that doubles as the VAT invoice: nightly room charges, restaurant and bar postings, Edinburgh’s visitor levy, the deposit and payment, VAT by rate, a signature line and a QR code for a copy.")]
public static class GuestFolioExample
{
    private static readonly Color Hotel = ColorPalette.Rose[900];
    private static readonly Color Ink = ColorPalette.Stone[900];
    private static readonly Color Muted = ColorPalette.Stone[500];

    /// <summary>The folio's lines: date, description, VAT rate (percent, or null when outside the scope of VAT), charge and credit in pence.</summary>
    private static readonly (string Date, string Description, int? Rate, long Charge, long Credit)[] Lines =
    [
        ("1 Oct", "Deposit, card ending 4417", null, 0, 16500),
        ("2 Nov", "Room 214, superior double, bed and breakfast", 20, 16500, 0),
        ("2 Nov", "Edinburgh visitor levy, 5% of the room rate", null, 825, 0),
        ("2 Nov", "The Terrace restaurant, dinner, table 6", 20, 7840, 0),
        ("3 Nov", "Room 214, superior double, bed and breakfast", 20, 16500, 0),
        ("3 Nov", "Edinburgh visitor levy, 5% of the room rate", null, 825, 0),
        ("3 Nov", "Library bar, cheque 2207", 20, 2460, 0),
        ("4 Nov", "Room 214, superior double, bed and breakfast", 20, 16500, 0),
        ("4 Nov", "Edinburgh visitor levy, 5% of the room rate", null, 825, 0),
        ("4 Nov", "The Terrace restaurant, dinner, table 3", 20, 6420, 0),
        ("4 Nov", "Laundry, two shirts", 20, 1200, 0),
        ("4 Nov", "Newspaper delivery", 0, 280, 0),
        ("5 Nov", "Minibar", 20, 950, 0),
        ("5 Nov", "Payment, Visa ending 4417", null, 0, 54625),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Maintainability", "MA0051:Method is too long",
        Justification = "The example intentionally keeps generation inline.")]
    public static Document Generate()
    {
        Document doc = new()
        {
            Settings = new DocumentSettings
            {
                Margin = 40,
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(ColorPalette.Stone[700]),
                FooterTemplate = PageTemplate.FromElements(@default: new TextBlock(
                    "Ardmore House Hotel Ltd  ·  Registered in Scotland SC712204  ·  VAT GB 284 6619 03  ·  This folio is your VAT invoice")
                {
                    FontSize = 7.5f,
                    Color = Muted,
                    Alignment = TextAlignment.Center,
                }),
            },
            Metadata = new DocumentMetadata
            {
                Title = "Guest folio AH-26-04812",
                Author = "Ardmore House Hotel",
                Subject = "Guest folio and VAT invoice",
            },
        };

        // ── Masthead ──
        HorizontalStack masthead = new() { WidthDistribution = WidthDistribution.Equal, Margin = new Thickness(0, 0, 0, 14) };
        VerticalStack hotel = new() { Spacing = 3 };
        hotel.Add(new TextBlock("ARDMORE HOUSE") { FontName = StandardFonts.HelveticaBold, FontSize = 15, LetterSpacing = 3, Color = Hotel });
        hotel.Add(new TextBlock("18 Royal Terrace, Edinburgh EH7 5AB  ·  0131 496 0742") { FontSize = 8, Color = Muted });
        masthead.Add(hotel);
        VerticalStack title = new() { Spacing = 3 };
        title.Add(new Heading("Guest folio")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 20,
            Color = Ink,
            Alignment = TextAlignment.Right,
            Margin = new Thickness(0),
        });
        title.Add(new TextBlock("No. AH-26-04812") { Color = Muted, Alignment = TextAlignment.Right });
        masthead.Add(title);
        doc.Add(new Container
        {
            Border = new Border(0, 0, 0, 1.5f),
            BorderColors = new BorderColors(Hotel),
            Padding = new Thickness(0, 0, 0, 10),
            Margin = new Thickness(0, 0, 0, 14),
            Child = masthead,
        });

        // ── Guest, stay and folio details ──
        HorizontalStack details = new() { WidthDistribution = WidthDistribution.Equal, Spacing = 10, Margin = new Thickness(0, 0, 0, 18) };
        details.Add(Panel("Guest", ["Ms Amelia Hart", "14 Orchard Row", "Bath BA1 5JT"]));
        details.Add(Panel("Stay", ["Room 214, superior double", "2 to 5 November 2026", "3 nights, 2 adults"]));
        details.Add(Panel("Account", ["Booking BK7Q2M", "Rate: bed and breakfast", "Cashier: T. Murray"]));
        doc.Add(details);

        // ── Charges and payments ──
        doc.Add(new Heading("Charges and payments")
        {
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 12,
            Color = Ink,
            Margin = new Thickness(0, 0, 0, 6),
        });
        Table ledger = new(0.8f, 3.6f, 0.7f, 1, 1)
        {
            CellPadding = 5,
            Border = new Border(0, 0, 0, 0.5f),
            BorderColors = new BorderColors(ColorPalette.Stone[200]),
            HeaderBackgroundColor = Hotel,
            HeaderTextColor = Color.White,
            Margin = new Thickness(0, 0, 0, 16),
        };
        ledger.AddHeader(Th("Date"), Th("Description"), Th("VAT", TextAlignment.Center), Th("Charges", TextAlignment.Right), Th("Credits", TextAlignment.Right));
        foreach (var (date, description, rate, charge, credit) in Lines)
        {
            TableRow row = new()
            {
                Cells =
                [
                    Cell(date, TextAlignment.Left, Muted),
                    Cell(description),
                    Cell(rate is null ? "O/S" : $"{rate}%", TextAlignment.Center, Muted),
                    Cell(charge > 0 ? Money(charge) : "", TextAlignment.Right),
                    Cell(credit > 0 ? Money(credit) : "", TextAlignment.Right, ColorPalette.Emerald[800]),
                ],
            };
            if (credit > 0) row.BackgroundColor = ColorPalette.Emerald[50];
            ledger.AddRow(row);
        }
        long charges = Lines.Sum(line => line.Charge);
        long credits = Lines.Sum(line => line.Credit);
        ledger.AddFooter(Th("Totals"), Th(""), Th(""), Th(Money(charges), TextAlignment.Right), Th(Money(credits), TextAlignment.Right));
        doc.Add(ledger);

        // ── VAT by rate, and the balance ──
        Table vat = new(1.6f, 1, 1, 1)
        {
            CellPadding = 4,
            Border = new Border(0, 0, 0, 0.5f),
            BorderColors = new BorderColors(ColorPalette.Stone[200]),
            HeaderBackgroundColor = ColorPalette.Stone[100],
            HeaderTextColor = Ink,
        };
        vat.AddHeader(Th("VAT rate"), Th("Net", TextAlignment.Right), Th("VAT", TextAlignment.Right), Th("Gross", TextAlignment.Right));
        foreach (int? rate in new int?[] { 20, 0, null })
        {
            long gross = Lines.Where(line => line.Rate == rate).Sum(line => line.Charge);
            long net = rate is null ? gross : (long)Math.Round(gross * 100m / (100 + rate.Value), MidpointRounding.AwayFromZero);
            vat.AddRow(new TableRow
            {
                Cells =
                [
                    Cell(rate is null ? "Outside the scope" : $"{rate}%"),
                    Cell(Money(net), TextAlignment.Right),
                    Cell(rate is null ? "" : Money(gross - net), TextAlignment.Right),
                    Cell(Money(gross), TextAlignment.Right),
                ],
            });
        }
        VerticalStack balanceText = new() { Spacing = 3 };
        balanceText.Add(new TextBlock("BALANCE") { FontName = StandardFonts.HelveticaBold, FontSize = 7, LetterSpacing = 1.5f, Color = Hotel });
        balanceText.Add(new TextBlock(Money(charges - credits)) { FontName = StandardFonts.HelveticaBold, FontSize = 22, Color = Ink });
        balanceText.Add(new TextBlock("Paid in full by Visa ending 4417 on 5 November 2026. Thank you for staying with us.") { FontSize = 8 });
        Container balance = new()
        {
            BackgroundColor = ColorPalette.Rose[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(ColorPalette.Rose[200]),
            CornerRadius = 6,
            Padding = new Thickness(12),
            Child = balanceText,
        };
        HorizontalStack summary = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 18, Margin = new Thickness(0, 0, 0, 26) };
        summary.Add(vat, 1.5f);
        summary.Add(balance, 1);
        doc.Add(summary);

        // ── Signature and a code for a copy ──
        VerticalStack sign = new() { Spacing = 4 };
        sign.Add(new Container
        {
            Height = 34,
            Border = new Border(0, 0, 0, 0.75f),
            BorderColors = new BorderColors(ColorPalette.Stone[400]),
        });
        sign.Add(new TextBlock("Guest signature: I agree that I am responsible for this account.") { FontSize = 8, Color = Muted });
        HorizontalStack copy = new() { WidthDistribution = WidthDistribution.Auto, Spacing = 8 };
        copy.Add(new QRCode
        {
            Data = "https://ardmorehouse.example/folio/AH-26-04812",
            Size = 58,
            QuietZone = 1,
            ForegroundColor = Ink,
            AltText = "QR code: download a copy of this folio or ask for a company invoice",
        });
        copy.Add(new Container
        {
            Width = 110,
            Child = new TextBlock("Scan for a copy of this folio, or to ask for an invoice in your company’s name.") { FontSize = 8, Color = Muted },
        });
        HorizontalStack close = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 30 };
        close.Add(sign, 1);
        close.Add(copy);
        doc.Add(close);
        return doc;
    }

    // Local helpers used by this document.

    /// <summary>Pence as pounds: 54625 becomes £546.25.</summary>
    private static string Money(long pence)
    {
        long abs = Math.Abs(pence);
        string pounds = (abs / 100).ToString("#,0", CultureInfo.InvariantCulture);
        string cents = (abs % 100).ToString("00", CultureInfo.InvariantCulture);
        return $"{(pence < 0 ? "−" : "")}£{pounds}.{cents}";
    }

    /// <summary>A labelled block of details on a light panel.</summary>
    private static Container Panel(string label, string[] lines)
    {
        VerticalStack stack = new() { Spacing = 2 };
        stack.Add(new TextBlock(label.ToUpperInvariant())
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 7,
            LetterSpacing = 1,
            Color = Muted,
            Margin = new Thickness(0, 0, 0, 2),
        });
        for (int i = 0; i < lines.Length; i++)
            stack.Add(new TextBlock(lines[i]) { FontSize = 9.5f, Color = i == 0 ? Ink : ColorPalette.Stone[700] });
        return new Container
        {
            BackgroundColor = ColorPalette.Stone[50],
            CornerRadius = 4,
            Padding = new Thickness(10),
            Child = stack,
        };
    }

    /// <summary>A header or footer cell: bold, aligned as its column.</summary>
    private static TextBlock Th(string text, TextAlignment alignment = TextAlignment.Left) =>
        new(text) { FontName = StandardFonts.HelveticaBold, FontSize = 8.5f, Alignment = alignment };

    /// <summary>A body cell.</summary>
    private static TableCell Cell(string text, TextAlignment alignment = TextAlignment.Left, Color? color = null) =>
        new() { Content = new TextBlock(text) { Color = color ?? ColorPalette.Stone[800], Alignment = alignment } };
}
