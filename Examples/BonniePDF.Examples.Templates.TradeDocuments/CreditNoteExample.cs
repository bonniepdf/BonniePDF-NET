using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using static BonniePDF.Examples.Shared.TradeKit;

namespace BonniePDF.Examples.Templates.TradeDocuments;

/// <summary>
/// The correcting document: what was over-charged on an earlier invoice, why, and
/// how the credit is applied. Every billing system issues them, few example
/// catalogues show one, and the details that matter are the ones an auditor looks
/// for - the original invoice reference, the reason code, the VAT treatment of
/// the reversal, and whether the money is refunded or offset.
///
/// The visual grammar is the invoice's, deliberately: a credit note that does not
/// look like the invoice it corrects is harder to reconcile. The signal that this
/// is a reversal is the colour of the totals and the sign on every amount.
///
/// Sample organisations, references, and amounts throughout.
/// </summary>
[System.ComponentModel.DisplayName("Credit note")]
[System.ComponentModel.Category("Invoices")]
[System.ComponentModel.Description("A credit note against an earlier invoice: corrected lines with their reasons, the VAT reversal, and how the credit is applied to the account. PDF/A-2a + PDF/UA-1.")]
public static class CreditNoteExample
{
    private const decimal VatRate = 0.2m;

    private static readonly DateTime Issued = new(2026, 8, 11);
    private static readonly DateTime InvoiceDated = new(2026, 8, 7);

    private const string Number = "CRN-2026-0118";
    private const string AgainstInvoice = "INV-2026-3340";
    private const string Account = "ACC-004182";
    private const string ReasonCode = "CR-04 — Billed in error";
    private const string Treatment = "Offset against the account balance";
    private const string ApprovedBy = "P. Ahmadi — Credit Control";

    private static readonly string[] Supplier =
    [
        "Sample Supplier Ltd", "12 Sample Street", "Sample City", "SC2 9BD", "VAT GB 000 0000 00",
    ];

    private static readonly string[] Customer =
    [
        "Sample Customer Ltd", "Unit 7, Sample Business Park", "Sample Town", "SA1 4TP",
    ];

    private sealed record CreditLine(
        string OriginalLine,
        string Description,
        string Reason,
        int Quantity,
        decimal UnitPrice);

    private static readonly CreditLine[] Lines =
    [
        new(
            "Line 3",
            "Collaboration module — annual licence",
            "Duplicated on the original invoice; the same licence appears on line 2",
            1,
            280.00m),
        new(
            "Line 6",
            "Additional storage — 500GB",
            "Billed at the list rate; the contracted rate under schedule 2 applies",
            1,
            70.00m),
    ];

    public static Document Generate()
    {
        decimal net = Lines.Sum(l => l.Quantity * l.UnitPrice);
        decimal vat = net * VatRate;
        decimal total = net + vat;

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 46 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"Credit note {Number} against invoice {AgainstInvoice}",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Credit note {Number} — against invoice {AgainstInvoice}";
        doc.Metadata.Author = "Sample Supplier Ltd";
        doc.Metadata.Subject =
            "Credit note against an earlier invoice: corrected lines with reasons, VAT reversal, and how the credit is applied (sample data).";
        doc.Metadata.Keywords = "credit note, credit memo, billing correction, vat, accounts receivable, pdf/ua, pdf/a";

        // The accent is the reversal colour, not the house indigo: a credit note
        // read at a glance must not be mistaken for another invoice.
        doc.Add(AccentBar(Negative));
        doc.Add(Cell("CREDIT NOTE", size: 8.5f, bold: true, color: Negative, marginBottom: 4f));
        doc.Add(H1($"Credit note {Number}"));
        doc.Add(Cell(
            $"Against invoice {AgainstInvoice} dated {GbDate(InvoiceDated)}",
            color: Muted,
            marginBottom: 14f));

        doc.Add(WeightedRow(
            [
                Field("Credit note date", GbDate(Issued)),
                Field("Account", Account),
                Field("Original invoice", AgainstInvoice),
                Field("Reason code", ReasonCode),
            ],
            marginBottom: 6f));

        doc.Add(WeightedRow(
            [PartyBlock("From", Supplier), PartyBlock("Credit to", Customer)],
            spacing: 20f));

        doc.Add(H2("Lines credited"));
        doc.Add(Cell(
            "Each line references the line of the original invoice it corrects, and states why.",
            size: 9f,
            color: Muted,
            marginBottom: 8f));

        Table table = new(1f, 2.6f, 4f, 0.7f, 1.2f, 1.2f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Negative,
            HeaderTextColor = Color.White,
        };
        table.AddHeader(
            Th("Original"),
            Th("Description"),
            Th("Reason for credit"),
            Th("Qty"),
            Th("Unit price", TextAlignment.Right),
            Th("Credit", TextAlignment.Right));
        foreach (CreditLine line in Lines)
        {
            table.AddRow(
                Td(line.OriginalLine, bold: true),
                Td(line.Description),
                Td(line.Reason, color: Muted),
                Td(line.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                Td(Money(line.UnitPrice), align: TextAlignment.Right),
                Td(Money(-(line.Quantity * line.UnitPrice)), bold: true, color: Negative, align: TextAlignment.Right));
        }

        doc.Add(table);

        doc.Add(TotalsPanel(
        [
            TotalLine("Net credit", Money(-net)),
            TotalLine($"VAT at {VatRate * 100:0}% reversed", Money(-vat)),
            Divider(),
            TotalLine("Total credited", Money(-total), bold: true, size: 11f, color: Negative),
        ]));

        doc.Add(H2("How this credit is applied"));
        doc.Add(Panel(
            Treatment,
            [
                Cell(
                    $"The credit of {Money(total)} has been applied to account {Account} and appears on the next "
                        + "statement. No refund is due; the balance payable is reduced accordingly.",
                    size: 9f,
                    marginBottom: 3f),
                Cell(
                    "If the original invoice has already been paid in full, the credit may instead be refunded on "
                        + "request to the credit control contact shown below.",
                    size: 9f),
            ],
            Negative));

        doc.Add(H2("VAT"));
        doc.Add(Cell(
            $"This credit note reverses output tax of {Money(vat)} previously accounted for on invoice "
                + $"{AgainstInvoice}. The customer should adjust input tax recovered on that invoice by the same "
                + "amount in the period in which this credit note is received.",
            size: 9f,
            color: Muted,
            marginBottom: 10f));

        doc.Add(Cell(
            $"Approved by {ApprovedBy} on {GbDate(Issued)}. Queries: quote {Number}.",
            size: 8.5f,
            color: Muted));

        return doc;
    }
}
