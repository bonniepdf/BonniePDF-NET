using System.Globalization;
using BonniePDF;
using static BonniePDF.Examples.Shared.TradeKit;

namespace BonniePDF.Examples.Templates.TradeDocuments;

/// <summary>
/// What a payer sends when they pay: which invoices this transfer settles, which
/// credit notes were applied against them, and what the net payment therefore
/// is. Accounts-receivable teams live or die by these - an unallocated payment
/// is a debt that stays open on somebody's ledger until a human works out what
/// it was for.
///
/// The whole document is one reconciliation: the allocated amounts must add to
/// the payment made, and it says so on its face if they do not. That single
/// property is what makes it worth generating rather than typing.
///
/// Short by design. It is a one-page document that travels attached to a payment
/// notification, and padding it out would be working against its job.
///
/// Sample organisations, references, and amounts throughout.
/// </summary>
[System.ComponentModel.DisplayName("Remittance advice")]
[System.ComponentModel.Category("Invoices")]
[System.ComponentModel.Description("A remittance advice that reconciles on its face: the invoices a payment settles, the credits applied against them, and the tie-back to the amount that actually left the bank. PDF/A-2a + PDF/UA-1.")]
public static class RemittanceAdviceExample
{
    private const string Reference = "REM-2026-009142";
    private const string Method = "Bank transfer (BACS)";
    private const string BankReference = "SAMPLEBUYER 004182";
    private const string Account = "ACC-004182";
    private const string Contact = "accounts.payable@sample-buyer.example";

    private static readonly DateTime PaidOn = new(2026, 9, 12);

    private static readonly string[] Payer =
    [
        "Sample Buyer Ltd", "Accounts Payable", "5 Sample Avenue", "Sample City", "SC1 3RG",
    ];

    private static readonly string[] Payee =
    [
        "Sample Supplier Ltd", "12 Sample Street", "Sample City", "SC2 9BD",
    ];

    private sealed record AllocatedItem(
        string Kind,
        string Reference,
        string Dated,
        string Due,
        /// <summary>Positive settles a debt; negative is a credit applied against it.</summary>
        decimal Amount,
        string Note);

    private static readonly AllocatedItem[] Items =
    [
        new("Invoice", "INV-2026-3312", "03 Aug 2026", "02 Sep 2026", 1450.00m, "Paid in full"),
        new("Invoice", "INV-2026-3381", "14 Aug 2026", "13 Sep 2026", 1360.00m, "Paid in full"),
        new("Invoice", "INV-2026-3428", "24 Aug 2026", "23 Sep 2026", 2240.00m, "Paid in full"),
        new("Credit note", "CRN-2026-0126", "26 Aug 2026", "—", -280.00m, "Applied against INV-2026-3428"),
        new("Invoice", "INV-2026-3402", "19 Aug 2026", "18 Sep 2026", 2700.00m, "Part payment — £2,700.00 of £5,400.00"),
        new("Adjustment", "ADJ-2026-0041", "21 Aug 2026", "20 Sep 2026", -62.40m, "Late payment charge disputed — see note below"),
    ];

    /// <summary>What actually left the bank. Compared against the allocations, not derived from them.</summary>
    private const decimal PaymentMade = 7407.60m;

    private static Color KindColour(string kind) => kind switch
    {
        "Credit note" => Positive,
        "Adjustment" => Negative,
        _ => Ink,
    };

    public static Document Generate()
    {
        decimal allocated = Items.Sum(i => i.Amount);
        // The one property this document exists to assert.
        bool reconciles = Math.Abs(allocated - PaymentMade) < 0.005m;
        decimal difference = allocated - PaymentMade;

        int settled = Items.Count(i => i.Amount > 0);
        int credits = Items.Count(i => i.Amount < 0);

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 46 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"Remittance advice {Reference} · bank reference {BankReference}",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Remittance advice {Reference}";
        doc.Metadata.Author = "Sample Buyer Ltd";
        doc.Metadata.Subject =
            "Remittance advice: the invoices a payment settles, the credits applied against them, and the reconciliation to the amount actually paid (sample data).";
        doc.Metadata.Keywords =
            "remittance advice, payment allocation, accounts payable, accounts receivable, pdf/ua, pdf/a";

        doc.Add(AccentBar());
        doc.Add(Cell("REMITTANCE ADVICE", size: 8.5f, bold: true, color: Accent, marginBottom: 4f));
        doc.Add(H1($"Payment of {Money(PaymentMade)}"));
        doc.Add(Cell(
            $"Sent {GbDate(PaidOn)} by {Method}. This is not an invoice.",
            color: Muted,
            marginBottom: 10f));

        doc.Add(WeightedRow(
            [
                Field("Remittance reference", Reference),
                Field("Payment date", GbDate(PaidOn)),
                Field("Bank reference", BankReference),
                Field("Supplier account", Account),
            ],
            marginBottom: 6f));

        doc.Add(WeightedRow(
            [PartyBlock("Paid by", Payer), PartyBlock("Paid to", Payee)],
            spacing: 20f,
            marginBottom: 4f));

        doc.Add(H2("What this payment settles"));
        doc.Add(Cell(
            $"{settled} item(s) settled, {credits} credit(s) applied. Please allocate against the references below "
                + "rather than against the payment total.",
            size: 9f,
            color: Muted,
            marginBottom: 8f));

        Table table = new(1.3f, 1.7f, 1.2f, 1.2f, 3.4f, 1.4f)
        {
            CellPadding = 5,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        table.AddHeader(
            Th("Type"),
            Th("Reference"),
            Th("Dated"),
            Th("Due"),
            Th("Note"),
            Th("Allocated", TextAlignment.Right));
        foreach (AllocatedItem item in Items)
        {
            table.AddRow(
                Td(item.Kind, color: KindColour(item.Kind)),
                Td(item.Reference, bold: true),
                Td(item.Dated, color: Muted),
                Td(item.Due, color: Muted),
                Td(item.Note),
                Td(
                    Money(item.Amount),
                    bold: true,
                    color: item.Amount < 0 ? Positive : Ink,
                    align: TextAlignment.Right));
        }

        doc.Add(table);

        doc.Add(TotalsPanel(
        [
            TotalLine("Total allocated", Money(allocated)),
            TotalLine("Payment sent", Money(PaymentMade)),
            Divider(),
            TotalLine(
                reconciles ? "Difference" : "UNRECONCILED DIFFERENCE",
                Money(difference),
                bold: true,
                size: 11f,
                color: reconciles ? Positive : Negative),
        ]));

        doc.Add(reconciles
            ? Cell(
                "The allocated amounts agree with the payment sent. Nothing on this advice is unallocated.",
                size: 9f,
                color: Muted,
                marginTop: 4f)
            : Cell(
                $"The allocations total {Money(allocated)} but {Money(PaymentMade)} was sent. This advice does not "
                    + "reconcile and must not be issued until the difference is resolved.",
                size: 9.5f,
                bold: true,
                color: Negative,
                marginTop: 6f,
                marginBottom: 4f));

        doc.Add(H2("Notes on this payment"));

        // Side by side, not stacked: every line spent on the notes is a line this
        // document does not have. It travels attached to a payment notification
        // and is meant to be read in one screen, so it has to stay on one page.
        doc.Add(WeightedRow(
            [
                Panel("Part payment — INV-2026-3402",
                [
                    Cell(
                        "Half of this invoice is paid pending agreement of the milestone it covers. The balance "
                            + "will follow on the next payment run once the milestone is signed off.",
                        size: 9f),
                ]),
                Panel(
                    "Deduction — ADJ-2026-0041",
                    [
                        Cell(
                            "The late payment charge is not accepted and has been deducted. The original invoice "
                                + "was settled within terms; please raise a credit note or provide the calculation "
                                + "relied upon.",
                            size: 9f),
                    ],
                    Negative),
            ],
            spacing: 12f,
            marginBottom: 0f));

        doc.Add(Cell(
            $"Queries on this remittance: {Contact}, quoting {Reference}.",
            size: 8.5f,
            color: Muted,
            marginTop: 8f));

        return doc;
    }
}
