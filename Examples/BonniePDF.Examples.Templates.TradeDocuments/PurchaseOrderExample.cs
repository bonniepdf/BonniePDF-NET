using System.Globalization;
using BonniePDF;
using static BonniePDF.Examples.Shared.TradeKit;

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
[System.ComponentModel.Description("A purchase order with a line schedule, delivery instructions, conditions of purchase, and an authorisation block in place of a payment block. PDF/A-2a + PDF/UA-1.")]
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
}
