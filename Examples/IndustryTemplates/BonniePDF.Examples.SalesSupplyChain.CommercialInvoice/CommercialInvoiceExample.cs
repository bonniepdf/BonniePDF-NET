using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.SalesSupplyChain.CommercialInvoice;

/// <summary>
/// The invoice a customs officer reads, not the one a finance team files. It has
/// to answer a short list of questions without being asked: who is selling, who
/// is buying, where the goods are going, on what delivery terms, what each line
/// is by tariff code, where it was made, how much it weighs, and what it is
/// worth. A missing tariff code or origin is the usual reason a consignment is
/// held at the border.
///
/// So every line carries a tariff (HS) code and a country of origin, the totals
/// are summed from the lines, and the weights are added up in grams before they
/// are shown in kilograms. The air waybill number is printed as a barcode for the
/// handler's scanner, and the document ends with a signed declaration.
///
/// Sample companies, codes and goods throughout.
/// </summary>
[System.ComponentModel.DisplayName("Commercial invoice")]
[System.ComponentModel.Category("Sales & Supply Chain")]
[System.ComponentModel.Description(
    "An export commercial invoice for customs: exporter, consignee, and delivery details, Incoterms, goods by tariff code and country of origin, weights, totals that add up, an air waybill barcode, and a signed declaration. PDF/A-2a + PDF/UA-1.")]
public static class CommercialInvoiceExample
{
    private const string Number = "CI-2026-00871";
    private const string CustomerReference = "PO-DE-558204";
    private const string Incoterm = "DAP Hamburg (Incoterms 2020)";
    private const string Currency = "EUR";
    private const string ReasonForExport = "Sale";
    private const string OriginCountry = "United Kingdom";
    private const string DestinationCountry = "Germany";
    private const string Carrier = "Sample Air Cargo";
    private const string Waybill = "12548392017";
    private const string Packages = "6 packages (4 crates, 2 pallets)";
    private const string PaymentTerms = "30 days from invoice date";

    /// <summary>Cents.</summary>
    private const int Freight = 41_200;

    private const int Insurance = 5_840;

    /// <summary>Packaging adds this many percent to the net weight.</summary>
    private const int PackagingPercent = 14;

    private static readonly DateTime InvoiceDate = new(2026, 10, 12);

    private static readonly string[] Exporter =
    [
        "Sample Pumps Ltd",
        "8 Foundry Lane",
        "Sample City SC3 8RT",
        "United Kingdom",
        "EORI GB123456789000",
        "VAT GB 123 4567 89",
    ];

    private static readonly string[] Consignee =
    [
        "Beispiel Anlagenbau GmbH",
        "Musterstraße 14",
        "20095 Hamburg",
        "Germany",
        "EORI DE1234567",
        "VAT DE 123 456 789",
    ];

    private static readonly string[] DeliverTo =
    [
        "Beispiel Anlagenbau GmbH",
        "Goods Inwards",
        "Hafenstraße 3",
        "21079 Hamburg",
        "Germany",
    ];

    /// <summary><c>Hs</c> is the Harmonised System tariff code; <c>UnitCents</c> is cents per unit; <c>NetGrams</c> is grams per unit.</summary>
    private sealed record Goods(string Description, string Hs, string Origin, int Quantity, string Unit, int UnitCents, int NetGrams);

    private static readonly Goods[] GoodsLines =
    [
        new("Stainless steel pump housing, 40 mm", "8413.91", "GB", 120, "each", 1_840, 620),
        new("Replacement impeller, bronze", "8413.91", "GB", 240, "each", 785, 180),
        new("Mechanical seal kit", "8484.20", "GB", 300, "kit", 420, 90),
        new("Control valve, 25 mm", "8481.80", "GB", 60, "each", 4_600, 1_350),
        new("Pressure gauge, 0–10 bar", "9026.20", "GB", 150, "each", 960, 210),
        new("Installation guide, printed", "4901.99", "GB", 120, "each", 115, 150),
        new("O-ring set, assorted", "4016.93", "GB", 500, "set", 190, 30),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        int goodsTotal = GoodsLines.Sum(g => g.Quantity * g.UnitCents);
        int invoiceTotal = goodsTotal + Freight + Insurance;
        int netGrams = GoodsLines.Sum(g => g.Quantity * g.NetGrams);
        int grossGrams = netGrams + netGrams * PackagingPercent / 100;
        int units = GoodsLines.Sum(g => g.Quantity);

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 46 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"Commercial invoice {Number} · sample data, not a customs declaration",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Commercial invoice {Number} — {OriginCountry} to {DestinationCountry}";
        doc.Metadata.Author = "Sample Pumps Ltd";
        doc.Metadata.Subject =
            "Export commercial invoice for customs: parties, Incoterms, goods by tariff code and origin, weights, totals, an air waybill barcode, and a declaration (sample data).";
        doc.Metadata.Keywords = "commercial invoice, export, customs, hs code, incoterms, country of origin, air waybill, pdf/ua, pdf/a";

        doc.Add(AccentBar());

        VerticalStack heading = new();
        heading.Add(Cell("COMMERCIAL INVOICE · EXPORT", size: 8.5f, bold: true, color: Accent, marginBottom: 4f));
        heading.Add(H1($"Invoice {Number}"));
        heading.Add(Cell("For customs purposes. Goods are for sale and are not gifts or samples.", color: Muted));

        VerticalStack waybill = new();
        waybill.Add(Cell("AIR WAYBILL", size: 6.5f, bold: true, color: Muted, align: TextAlignment.Right, marginBottom: 2f));
        waybill.Add(new Barcode
        {
            Value = Waybill,
            Symbology = BarcodeSymbology.Code128,
            Height = 34,
            AltText = $"Air waybill barcode (Code 128): {Waybill}",
        });

        HorizontalStack top = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 20,
            Margin = new Thickness(0, 0, 0, 14),
        };
        top.Add(heading, 3);
        top.Add(waybill, 2);
        doc.Add(top);

        doc.Add(WeightedRow(
            [PartyBlock("Exporter", Exporter), PartyBlock("Consignee", Consignee), PartyBlock("Deliver to", DeliverTo)],
            spacing: 18f,
            marginBottom: 4f));
        doc.Add(WeightedRow(
            [
                Field("Invoice date", GbDate(InvoiceDate)),
                Field("Customer reference", CustomerReference),
                Field("Delivery terms", Incoterm),
                Field("Payment terms", PaymentTerms),
            ],
            marginBottom: 6f));
        doc.Add(WeightedRow(
            [
                Field("Country of origin", OriginCountry),
                Field("Destination", DestinationCountry),
                Field("Reason for export", ReasonForExport),
                Field("Carrier", Carrier),
            ],
            marginBottom: 6f));

        doc.Add(H2("Goods"));
        Table table = new(0.45f, 3.1f, 1.05f, 0.75f, 0.7f, 0.7f, 1.1f, 1.3f)
        {
            CellPadding = 5,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        table.AddHeader(
            Th("#"),
            Th("Description of goods"),
            Th("HS code"),
            Th("Origin"),
            Th("Qty", TextAlignment.Right),
            Th("Unit"),
            Th("Unit price", TextAlignment.Right),
            Th("Total", TextAlignment.Right));
        for (int i = 0; i < GoodsLines.Length; i++)
        {
            Goods g = GoodsLines[i];
            table.AddRow(
                Td((i + 1).ToString(CultureInfo.InvariantCulture), color: Muted),
                Td(g.Description),
                Td(g.Hs),
                Td(g.Origin, color: Muted),
                Td(g.Quantity.ToString(CultureInfo.InvariantCulture), align: TextAlignment.Right),
                Td(g.Unit, color: Muted),
                Td(Amount(g.UnitCents), align: TextAlignment.Right),
                Td(Amount(g.Quantity * g.UnitCents), bold: true, align: TextAlignment.Right));
        }

        doc.Add(table);
        doc.Add(Cell($"All amounts in {Currency}. Unit prices exclude freight and insurance.", size: 8f, color: Muted, marginTop: 4f, marginBottom: 8f));

        Table totals = new(3f, 1.4f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        totals.AddHeader(Th("Summary"), Th("Amount", TextAlignment.Right));
        totals.AddRow(Td("Total value of goods"), Td(Amount(goodsTotal), align: TextAlignment.Right));
        totals.AddRow(Td("Freight"), Td(Amount(Freight), align: TextAlignment.Right));
        totals.AddRow(Td("Insurance"), Td(Amount(Insurance), align: TextAlignment.Right));
        totals.AddRow(
            Td($"Invoice total ({Currency})", size: 9.5f, bold: true),
            Td(Amount(invoiceTotal), size: 9.5f, bold: true, align: TextAlignment.Right));
        HorizontalStack totalsRow = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 20,
            Margin = new Thickness(0, 0, 0, 10),
        };
        totalsRow.Add(
            Panel("Packing",
            [
                Cell(Packages, size: 9f, marginBottom: 3f),
                Cell($"{GoodsLines.Length} lines, {Thousands(units)} units", size: 9f, marginBottom: 3f),
                Cell($"Net weight {Kg(netGrams)} kg", size: 9f, marginBottom: 3f),
                Cell($"Gross weight {Kg(grossGrams)} kg", size: 9f),
            ]),
            1);
        totalsRow.Add(totals, 1);
        doc.Add(totalsRow);

        doc.Add(Panel("Declaration",
        [
            Cell(
                "I declare that the information in this invoice is true and correct, that the goods are of the origin stated, "
                + "and that, except where otherwise clearly indicated, they are of United Kingdom preferential origin.",
                size: 9f),
        ]));
        doc.Add(WeightedRow(
            [Slot("Name and position", "Authorised signatory"), Slot("Signature", string.Empty), Slot("Date", GbDate(InvoiceDate))],
            spacing: 22f,
            marginBottom: 6f));

        return doc;
    }

    /// <summary>A whole number with thousands separators: 1,490.</summary>
    private static string Thousands(int n) => NumberFormatter.Format(n, "N0");

    /// <summary>Cents as a decimal amount: 1,234.56.</summary>
    private static string Amount(int cents) =>
        $"{Thousands(cents / 100)}.{(cents % 100).ToString("D2", CultureInfo.InvariantCulture)}";

    /// <summary>Grams as kilograms to one decimal place, rounding half up: 290.1.</summary>
    private static string Kg(int grams)
    {
        int tenths = (grams + 50) / 100;
        return $"{(tenths / 10).ToString(CultureInfo.InvariantCulture)}.{(tenths % 10).ToString(CultureInfo.InvariantCulture)}";
    }

    private static VerticalStack Slot(string role, string hint)
    {
        VerticalStack stack = new();
        stack.Add(Cell(role.ToUpperInvariant(), size: 6.5f, bold: true, color: Muted, marginBottom: 20f));
        stack.Add(new Container
        {
            BackgroundColor = Rule,
            Height = 0.75f,
            Margin = new Thickness(0, 0, 0, 5),
        });
        stack.Add(Cell(hint, size: 8f, color: Muted));
        return stack;
    }

    // Local helpers and sample data for this document.
    private static readonly Color Ink = ColorPalette.Slate[700];

    /// <summary>600, not 400: 400 never clears 4.5:1 as body text on white.</summary>
    private static readonly Color Muted = ColorPalette.Slate[600];

    private static readonly Color Heading = ColorPalette.Slate[800];

    private static readonly Color Accent = ColorPalette.Orange[700];

    private static readonly Color Rule = ColorPalette.Slate[200];

    /// <summary>A styled text cell with document defaults.</summary>
    private static TextBlock Cell(string text, float size = 9f, bool bold = false, Color? color = null, TextAlignment align = TextAlignment.Left, float marginTop = 0f, float marginBottom = 0f) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = size,
        Color = color ?? Ink,
        Alignment = align,
        Margin = new Thickness(0, marginTop, 0, marginBottom),
    };

    /// <summary>Table header cell - white on the accent band.</summary>
    private static TextBlock Th(string text, TextAlignment align = TextAlignment.Left) => Cell(text, size: 8.5f, bold: true, color: Color.White, align: align);

    /// <summary>Table body cell.</summary>
    private static TextBlock Td(string text, float size = 8.5f, bool bold = false, Color? color = null, TextAlignment align = TextAlignment.Left) => Cell(text, size: size, bold: bold, color: color, align: align);

    private static Heading H1(string text) => new(text)
    {
        Level = 1,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 21,
        Color = Heading,
        Margin = new Thickness(0, 0, 0, 4),
    };

    private static Heading H2(string text, Color? color = null) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 13,
        Color = color ?? Accent,
        Margin = new Thickness(0, 16, 0, 8),
    };

    /// <summary>The full-width accent rule at the top of each document.</summary>
    private static Container AccentBar(Color? color = null, float height = 4f) => new()
    {
        BackgroundColor = color ?? Accent,
        Height = height,
        Margin = new Thickness(0, 0, 0, 14),
    };

    /// <summary>A labelled address block.</summary>
    private static VerticalStack PartyBlock(string label, IEnumerable<string> address)
    {
        VerticalStack stack = new();
        stack.Add(Cell(label.ToUpperInvariant(), size: 7f, bold: true, color: Muted, marginBottom: 3f));
        stack.Add(AddressLines(address, size: 9f));
        return stack;
    }

    /// <summary>One label/value pair for the reference grid.</summary>
    private static VerticalStack Field(string label, string value)
    {
        VerticalStack stack = new();
        stack.Add(Cell(label.ToUpperInvariant(), size: 6.5f, bold: true, color: Muted, marginBottom: 2f));
        stack.Add(Cell(value, size: 9.5f));
        return stack;
    }

    /// <summary>A row of evenly weighted elements.</summary>
    private static HorizontalStack WeightedRow(IEnumerable<IElement> items, float spacing = 14f, float marginTop = 0f, float marginBottom = 14f)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = spacing,
            Margin = new Thickness(0, marginTop, 0, marginBottom),
        };
        foreach (IElement item in items)
        {
            row.Add(item, 1);
        }

        return row;
    }

    /// <summary>A bordered panel with a heading - notes and declarations.</summary>
    private static Container Panel(string title, IEnumerable<IElement> children, Color? accent = null)
    {
        VerticalStack body = new()
        {
            Spacing = 4
        };
        body.Add(Cell(title, size: 9.5f, bold: true, color: accent ?? Accent, marginBottom: 4f));
        foreach (IElement child in children)
        {
            body.Add(child);
        }

        return new Container
        {
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 6,
            Padding = new Thickness(12),
            Margin = new Thickness(0, 4, 0, 12),
            Child = body,
        };
    }

    /// <summary>The running header / footer band.</summary>
    private static HorizontalStack Band(string left, string right)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted
        };
        row.Add(Cell(left, size: 7.5f, color: Muted), 2.5f);
        row.Add(Cell(right, size: 7.5f, color: Muted, align: TextAlignment.Right), 2);
        return row;
    }

    private static string GbDate(DateTime date) => date.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("en-GB"));

    private static TextBlock AddressLines(IEnumerable<string> items, float size = 9f) => Cell(string.Join('\n', items), size: size);
}
