using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.Templates.TradeDocuments;

/// <summary>
/// The other half of the invoice, and the half almost no PDF library shows: the
/// document a buyer issues to commit to a purchase. Procurement systems generate
/// these in volume, they carry the reference every subsequent invoice must quote,
/// and they are the artefact an approval workflow produces - so they carry an
/// authorisation block rather than a payment block.
///
/// Sample organisations, references, and amounts throughout.
/// </summary>
[System.ComponentModel.DisplayName("Purchase order")]
[System.ComponentModel.Category("Invoices")]
[System.ComponentModel.Description(
    "A purchase order with a line schedule, delivery instructions, conditions of purchase, and an authorisation block in place of a payment block. PDF/A-2a + PDF/UA-1.")]
public static class PurchaseOrderExample
{
    private const decimal VatRate = 0.2m;

    private const string Number = "PO-2026-004871";
    private const string Revision = "Rev. 0 — original issue";
    private const string Currency = "GBP";
    private const string Terms = "30 days net from date of valid invoice";
    private const string Incoterms = "DAP — Sample Distribution Centre";
    private const string Requisition = "REQ-2026-1188";
    private const string CostCentre = "CC-4420 — Facilities";
    private const string RaisedBy = "Sample Buyer — Buyer";
    private const string ApprovedBy = "Sample Manager — Head of Procurement";

    private static readonly DateTime Raised = new(2026, 9, 2);
    private static readonly DateTime Required = new(2026, 9, 23);

    private static readonly string[] Buyer =
    [
        "Sample Buyer Ltd", "Procurement Department", "5 Sample Avenue", "Sample City", "SC1 3RG",
    ];

    private static readonly string[] Supplier =
    [
        "Sample Supplier Ltd", "12 Sample Street", "Sample City", "SC2 9BD",
    ];

    private static readonly string[] DeliverTo =
    [
        "Sample Buyer Ltd", "Goods Inwards, Gate 3", "Sample Distribution Centre", "Sample Town", "ST9 2LD",
    ];

    private sealed record OrderLine(
        int Line,
        string PartCode,
        string Description,
        int Quantity,
        string Unit,
        decimal UnitPrice,
        string Required);

    private static readonly OrderLine[] Lines =
    [
        new(1, "FLT-G4-592", "Panel filter G4, 592 × 592 × 48mm", 48, "each", 11.25m, "23 Sep 2026"),
        new(2, "BRG-6204-2RS", "Sealed deep-groove bearing, 20mm bore", 24, "each", 18.40m, "23 Sep 2026"),
        new(3, "BLT-SPZ-1287", "Drive belt, SPZ section, 1287mm", 12, "each", 22.90m, "23 Sep 2026"),
        new(4, "SEAL-AHU-12", "Access door seal, 12mm closed cell, per metre", 60, "metre", 9.60m, "30 Sep 2026"),
        new(5, "LUB-SYN-5L", "Synthetic bearing lubricant, 5 litre", 4, "each", 64.00m, "30 Sep 2026"),
        new(6, "PPE-GLV-CR5", "Cut-resistant gloves, level 5, size L", 40, "pair", 7.85m, "23 Sep 2026"),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        decimal net = Lines.Sum(l => l.Quantity * l.UnitPrice);
        decimal vat = net * VatRate;

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 46 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.HeaderTemplate = pageNumber =>
            pageNumber == 1 ? null : Band($"Purchase order {Number}", Revision);
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"Quote {Number} on all invoices, delivery notes, and correspondence",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Purchase order {Number}";
        doc.Metadata.Author = "Sample Buyer Ltd";
        doc.Metadata.Subject =
            "Purchase order with line schedule, delivery instructions, order terms, and authorisation (sample data).";
        doc.Metadata.Keywords = "purchase order, procurement, buying, supply chain, pdf/ua, pdf/a";

        doc.Add(AccentBar());
        doc.Add(Cell("PURCHASE ORDER", size: 8.5f, bold: true, color: Accent, marginBottom: 4f));
        doc.Add(H1($"Order {Number}"));
        doc.Add(Cell(Revision, color: Muted, marginBottom: 14f));

        doc.Add(WeightedRow(
            [
                Field("Order date", GbDate(Raised)),
                Field("Required by", GbDate(Required)),
                Field("Requisition", Requisition),
                Field("Cost centre", CostCentre),
            ],
            marginBottom: 6f));
        doc.Add(WeightedRow(
        [
            Field("Currency", Currency),
            Field("Payment terms", Terms),
            Field("Delivery terms", Incoterms),
            Field("Raised by", RaisedBy),
        ]));

        doc.Add(WeightedRow(
            [
                PartyBlock("Order to", Supplier),
                PartyBlock("Invoice to", Buyer),
                PartyBlock("Deliver to", DeliverTo),
            ],
            spacing: 20f,
            marginBottom: 4f));

        doc.Add(H2("Order lines"));

        Table table = new(0.5f, 1.5f, 3.6f, 0.8f, 0.8f, 1.1f, 1.2f, 1.2f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        table.AddHeader(
            Th("#"),
            Th("Part code"),
            Th("Description"),
            Th("Qty"),
            Th("Unit"),
            Th("Unit price", TextAlignment.Right),
            Th("Line total", TextAlignment.Right),
            Th("Required"));
        foreach (OrderLine line in Lines)
        {
            table.AddRow(
                Td(line.Line.ToString(CultureInfo.InvariantCulture)),
                Td(line.PartCode),
                Td(line.Description),
                Td(line.Quantity.ToString(CultureInfo.InvariantCulture)),
                Td(line.Unit, color: Muted),
                Td(Money(line.UnitPrice), align: TextAlignment.Right),
                Td(Money(line.Quantity * line.UnitPrice), bold: true, align: TextAlignment.Right),
                Td(line.Required, color: Muted));
        }

        doc.Add(table);

        doc.Add(TotalsPanel(
        [
            TotalLine("Order net", Money(net)),
            TotalLine($"VAT at {VatRate * 100:0}%", Money(vat)),
            Divider(),
            TotalLine("Order total", Money(net + vat), bold: true, size: 11f, color: Accent),
        ]));

        // The heading stays with the terms panel it introduces.
        VerticalStack terms = new() { BreakAllowed = false };
        terms.Add(H2("Order terms"));
        terms.Add(Panel("Conditions of purchase",
        [
            Cell(
                "This order is placed on the buyer’s standard conditions of purchase, which prevail over any terms "
                + "the supplier proposes unless varied in writing by the buyer’s procurement department.",
                size: 9f,
                marginBottom: 3f),
            Cell(
                "Deliveries are accepted only at the address shown, between 07:00 and 15:00 on working days, and "
                + "must be accompanied by a delivery note quoting this order number and the relevant line numbers.",
                size: 9f,
                marginBottom: 3f),
            Cell(
                "Part deliveries are accepted. Invoices that do not quote this order number will be returned unpaid. "
                + "Quantities delivered in excess of those ordered may be returned at the supplier’s cost.",
                size: 9f),
        ]));
        doc.Add(terms);

        VerticalStack authorisation = new() { BreakAllowed = false };
        authorisation.Add(H2("Authorisation"));
        authorisation.Add(Cell(
            "Raised and approved within the delegated authority for this cost centre.",
            size: 9f,
            color: Muted,
            marginBottom: 8f));
        authorisation.Add(WeightedRow(
            [
                AuthSlot("Raised by", RaisedBy, GbDate(Raised)),
                AuthSlot("Approved by", ApprovedBy, GbDate(Raised)),
                AuthSlot("Supplier acknowledgement", "Name, signature and date", string.Empty),
            ],
            spacing: 22f,
            marginBottom: 0f));
        doc.Add(authorisation);

        return doc;
    }

    private static VerticalStack AuthSlot(string role, string name, string dated)
    {
        VerticalStack stack = new();
        stack.Add(Cell(role.ToUpperInvariant(), size: 6.5f, bold: true, color: Muted, marginBottom: 18f));
        stack.Add(new Container
        {
            BackgroundColor = Rule,
            Height = 0.75f,
            Margin = new Thickness(0, 0, 0, 5),
        });
        stack.Add(Cell(name, size: 9f));
        stack.Add(Cell(
            dated.Length > 0 ? $"Date: {dated}" : "Date:",
            size: 8f,
            color: Muted,
            marginTop: 3f));
        return stack;
    }

    // Local helpers and sample data for this document.

    // Local helpers and sample data for this document.
    private static readonly Color Ink = ColorPalette.Slate[700];

    /// <summary>600, not 400: 400 never clears 4.5:1 as body text on white.</summary>
    private static readonly Color Muted = ColorPalette.Slate[600];

    private static readonly Color Heading = ColorPalette.Slate[800];

    private static readonly Color Accent = ColorPalette.Indigo[700];

    private static readonly Color Rule = ColorPalette.Slate[200];

    /// <summary>A styled text cell with trade-document defaults.</summary>
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

    /// <summary>A "label ....... value" totals line, weighted so the value hugs the right.</summary>
    private static HorizontalStack TotalLine(string label, string value, bool bold = false, float size = 9f, Color? color = null)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Margin = new Thickness(0, 3, 0, 3),
        };
        row.Add(Cell(label, size: size, bold: bold, color: color ?? Muted), 3);
        row.Add(Cell(value, size: size, bold: bold, color: color ?? Ink, align: TextAlignment.Right), 2);
        return row;
    }

    /// <summary>A right-hand totals panel, with the body already assembled.</summary>
    private static HorizontalStack TotalsPanel(IEnumerable<IElement> children, float weight = 1.6f)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Margin = new Thickness(0, 10, 0, 4),
        };
        row.Add(new Container(), 2);
        VerticalStack body = new();
        foreach (IElement child in children)
        {
            body.Add(child);
        }

        row.Add(new Container { BackgroundColor = ColorPalette.Indigo[50], CornerRadius = 6, Padding = new Thickness(12), Child = body, }, weight);
        return row;
    }

    /// <summary>A bordered panel with a heading - terms, notes, declarations.</summary>
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

    /// <summary>A thin divider, used above a grand total.</summary>
    private static Container Divider() => new()
    {
        BackgroundColor = Rule,
        Height = 0.75f,
        Margin = new Thickness(0, 5, 0, 5),
    };

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

    /// <summary>
    /// Sterling, with the sign outside the symbol.
    ///
    /// On a statement or a credit note, where the sign is the most important
    /// character in the cell, it belongs in front of the symbol rather than after
    /// it - "-£420.00", not "£-420.00".
    /// </summary>
    private static string Money(decimal value, string symbol = "£", string culture = "en-GB") => (value < 0 ? "-" : string.Empty) + symbol + Math.Abs(value).ToString("N2", CultureInfo.GetCultureInfo(culture));

    private static string GbDate(DateTime date) => date.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("en-GB"));

    private static TextBlock AddressLines(IEnumerable<string> items, float size = 9f) => Cell(string.Join('\n', items), size: size);
}
