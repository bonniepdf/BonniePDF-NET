using System.Globalization;
using BonniePDF;
using static BonniePDF.Examples.Shared.TradeKit;

namespace BonniePDF.Examples.Templates.TradeDocuments;

/// <summary>
/// The second-most-generated document in business software after the invoice,
/// and the one most often missing from a PDF library's examples: a period
/// statement showing the opening balance, every invoice, payment and credit that
/// moved it, the running balance after each, the closing balance, and an ageing
/// analysis of what is overdue.
///
/// Two things make it a real test rather than a table demo. The running balance
/// has to be arithmetically correct and tie back to the closing figure - a
/// statement that does not reconcile is worse than no statement. And the ledger
/// is long, so it flows across pages with a repeating header, with the ageing
/// summary and remittance advice kept whole at the end.
///
/// Sample organisations, references, and amounts throughout.
/// </summary>
[System.ComponentModel.DisplayName("Customer account statement")]
[System.ComponentModel.Category("Invoices")]
[System.ComponentModel.Description("A period statement with a computed running balance, a closing figure, and an ageing analysis derived from the open items and reconciled against that closing figure. PDF/A-2a + PDF/UA-1.")]
public static class CustomerAccountStatementExample
{
    private static readonly Color Overdue = ColorPalette.Rose[700];
    private static readonly Color Credit = ColorPalette.Emerald[700];

    private const string Number = "ACC-004182";
    private const string Customer = "Sample Customer Ltd";
    private const string Supplier = "Sample Supplier Ltd";
    private const string Terms = "30 days net";
    private const decimal CreditLimit = 25000m;
    private const decimal OpeningBalance = 6280.50m;

    private static readonly DateTime PeriodFrom = new(2026, 8, 1);
    private static readonly DateTime PeriodTo = new(2026, 8, 31);
    private static readonly DateTime StatementDate = new(2026, 9, 1);

    private static readonly string[] Address =
    [
        "Sample Customer Ltd", "Unit 7, Sample Business Park", "Sample Town", "SA1 4TP", "United Kingdom",
    ];

    private static readonly string[] SupplierAddress =
    [
        "Sample Supplier Ltd", "12 Sample Street", "Sample City", "SC2 9BD", "United Kingdom",
    ];

    /// <param name="Amount">Positive increases what the customer owes; negative reduces it.</param>
    private sealed record Entry(string Date, string Kind, string Reference, string Detail, decimal Amount);

    private static readonly Entry[] Entries =
    [
        new("03 Aug 2026", "Invoice", "INV-2026-3312", "Monthly subscription — August", 1450.00m),
        new("05 Aug 2026", "Payment", "BACS-88214", "Payment received, thank you — settles INV-2026-3245", -4820.50m),
        new("07 Aug 2026", "Invoice", "INV-2026-3340", "Professional services — July", 3280.00m),
        new("11 Aug 2026", "Credit note", "CRN-2026-0118", "Credit — duplicated licence line on INV-2026-3340", -420.00m),
        new("12 Aug 2026", "Invoice", "INV-2026-3366", "Additional user seats (14)", 1876.00m),
        new("14 Aug 2026", "Invoice", "INV-2026-3381", "Onsite support visit — 2 days", 1360.00m),
        new("18 Aug 2026", "Payment", "BACS-88903", "Payment received, thank you", -3280.00m),
        new("19 Aug 2026", "Invoice", "INV-2026-3402", "Data migration — phase 1", 5400.00m),
        new("21 Aug 2026", "Adjustment", "ADJ-2026-0041", "Late payment charge — INV-2026-3180", 62.40m),
        new("24 Aug 2026", "Invoice", "INV-2026-3428", "Training workshop — 8 attendees", 2240.00m),
        new("26 Aug 2026", "Credit note", "CRN-2026-0126", "Credit — training workshop, one attendee withdrew", -280.00m),
        new("27 Aug 2026", "Payment", "BACS-89417", "Payment received, thank you", -1456.00m),
        new("28 Aug 2026", "Invoice", "INV-2026-3455", "Monthly subscription — September", 1450.00m),
        new("31 Aug 2026", "Invoice", "INV-2026-3470", "Integration support — retainer", 980.00m),
    ];

    /// <summary>
    /// The items still outstanding at the statement date, with the date each fell
    /// due.
    ///
    /// The ageing analysis is computed from these, not written down beside them.
    /// Two of the sample rows are deliberately netted (an invoice less its credit
    /// note), because that is how an open item actually ages - the credit does
    /// not sit in its own bucket.
    ///
    /// The document asserts that these sum to the ledger's closing balance. A
    /// statement whose ageing does not tie to its own closing figure is worse
    /// than no statement, and it is the single defect this document must not
    /// have.
    /// </summary>
    private sealed record OpenItem(string Reference, DateTime Due, decimal Amount);

    private static readonly OpenItem[] OpenItems =
    [
        new("INV-2026-3180", new DateTime(2026, 7, 15), 1460.00m),
        new("INV-2026-3312", new DateTime(2026, 9, 2), 1450.00m),
        new("INV-2026-3381", new DateTime(2026, 9, 13), 1360.00m),
        new("INV-2026-3402", new DateTime(2026, 9, 18), 5400.00m),
        new("ADJ-2026-0041", new DateTime(2026, 9, 20), 62.40m),
        new("INV-2026-3428", new DateTime(2026, 9, 23), 1960.00m),
        new("INV-2026-3455", new DateTime(2026, 9, 27), 1450.00m),
        new("INV-2026-3470", new DateTime(2026, 9, 30), 980.00m),
    ];

    private static readonly string[] Buckets =
    [
        "Current", "1–30 days", "31–60 days", "61–90 days", "Over 90 days",
    ];

    /// <summary>Which ageing bucket an item falls into at the statement date.</summary>
    private static string BucketFor(OpenItem item, DateTime asAt)
    {
        int daysOverdue = (int)(asAt - item.Due).TotalDays;
        if (daysOverdue <= 0)
        {
            return "Current";
        }

        if (daysOverdue <= 30)
        {
            return "1–30 days";
        }

        if (daysOverdue <= 60)
        {
            return "31–60 days";
        }

        return daysOverdue <= 90 ? "61–90 days" : "Over 90 days";
    }

    private static Color KindColour(string kind) => kind switch
    {
        "Payment" or "Credit note" => Credit,
        "Adjustment" => Overdue,
        _ => Ink,
    };

    public static Document Generate()
    {
        // The running balance is computed, never transcribed. A statement whose
        // closing figure does not follow from its own rows is the one defect this
        // document cannot have.
        decimal running = OpeningBalance;
        List<(Entry Entry, decimal Balance)> rows = [];
        foreach (Entry entry in Entries)
        {
            running += entry.Amount;
            rows.Add((entry, running));
        }

        decimal closing = running;

        decimal invoiced = Entries.Where(e => e.Amount > 0).Sum(e => e.Amount);
        decimal paid = -Entries.Where(e => e.Amount < 0).Sum(e => e.Amount);

        // Ageing is derived from the open items, then reconciled against the
        // ledger's own closing balance. If a future edit to either list breaks
        // the tie, the document says so on its face rather than printing a
        // plausible lie.
        Dictionary<string, decimal> ageing = Buckets.ToDictionary(b => b, _ => 0m, StringComparer.Ordinal);
        foreach (OpenItem item in OpenItems)
        {
            ageing[BucketFor(item, StatementDate)] += item.Amount;
        }

        decimal ageingTotal = OpenItems.Sum(item => item.Amount);
        bool reconciles = Math.Abs(ageingTotal - closing) < 0.005m;
        decimal overdue = ageingTotal - ageing["Current"];

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 46 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.HeaderTemplate = pageNumber => pageNumber == 1
            ? null
            : Band($"Statement {Number} — {Customer}", $"As at {GbDate(StatementDate)}");
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"{Supplier} · payment terms {Terms}",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Account statement {Number} — {GbDate(PeriodFrom)} to {GbDate(PeriodTo)}";
        doc.Metadata.Author = Supplier;
        doc.Metadata.Subject =
            "Customer account statement: opening balance, ledger with running balance, closing balance, ageing analysis, and remittance advice (sample data).";
        doc.Metadata.Keywords = "account statement, ledger, running balance, ageing, accounts receivable, pdf/ua, pdf/a";

        // ── Title and parties ─────────────────────────────────────────────────

        doc.Add(new Container
        {
            BackgroundColor = Accent,
            Height = 4,
            Margin = new Thickness(0, 0, 0, 14),
        });

        HorizontalStack titleRow = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Margin = new Thickness(0, 0, 0, 16),
        };

        VerticalStack titleStack = new();
        titleStack.Add(Cell("STATEMENT OF ACCOUNT", size: 8.5f, bold: true, color: Accent, marginBottom: 4f));
        titleStack.Add(new Heading("Account statement")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 21,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 4),
        });
        titleStack.Add(Cell($"{GbDate(PeriodFrom)} to {GbDate(PeriodTo)}", color: Muted));
        titleRow.Add(titleStack, 3);

        VerticalStack refStack = new();
        refStack.Add(Cell(
            "Statement date",
            size: 7f,
            bold: true,
            color: Muted,
            align: TextAlignment.Right,
            marginBottom: 2f));
        refStack.Add(Cell(GbDate(StatementDate), align: TextAlignment.Right, marginBottom: 6f));
        refStack.Add(Cell(
            "Account number",
            size: 7f,
            bold: true,
            color: Muted,
            align: TextAlignment.Right,
            marginBottom: 2f));
        refStack.Add(Cell(Number, align: TextAlignment.Right));
        titleRow.Add(refStack, 2);
        doc.Add(titleRow);

        HorizontalStack parties = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 20,
            Margin = new Thickness(0, 0, 0, 16),
        };
        parties.Add(PartyBlock("Statement for", Address), 1);
        parties.Add(PartyBlock("From", SupplierAddress), 1);

        VerticalStack termsStack = new();
        termsStack.Add(Cell("TERMS", size: 7f, bold: true, color: Muted, marginBottom: 2f));
        termsStack.Add(Cell(Terms, marginBottom: 8f));
        termsStack.Add(Cell("CREDIT LIMIT", size: 7f, bold: true, color: Muted, marginBottom: 2f));
        termsStack.Add(Cell(Money(CreditLimit)));
        parties.Add(termsStack, 1);
        doc.Add(parties);

        // ── Balance summary ───────────────────────────────────────────────────

        HorizontalStack summary = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 10,
            Margin = new Thickness(0, 0, 0, 6),
        };
        summary.Add(Tile(Money(OpeningBalance), "Opening balance", Ink), 1);
        summary.Add(Tile(Money(invoiced), "Invoiced and charged", Ink), 1);
        summary.Add(Tile(Money(paid), "Paid and credited", Credit), 1);
        summary.Add(Tile(Money(closing), "Closing balance", Accent), 1);
        doc.Add(summary);

        // ── The ledger ────────────────────────────────────────────────────────

        doc.Add(H2("Account activity"));

        Table ledger = new(1.1f, 1.2f, 1.5f, 3.6f, 1.2f, 1.3f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        ledger.AddHeader(Th("Date"), Th("Type"), Th("Reference"), Th("Details"), Th("Amount"), Th("Balance"));

        ledger.AddRow(
            Cell(GbDate(PeriodFrom), size: 8.5f),
            Cell("Balance", size: 8.5f, bold: true, color: Muted),
            Cell("—", size: 8.5f, color: Muted),
            Cell("Opening balance brought forward", size: 8.5f, color: Muted),
            Cell(string.Empty, size: 8.5f),
            Cell(Money(OpeningBalance), size: 8.5f, bold: true, align: TextAlignment.Right));

        foreach ((Entry entry, decimal balance) in rows)
        {
            ledger.AddRow(
                Cell(entry.Date, size: 8.5f),
                Cell(entry.Kind, size: 8.5f, color: KindColour(entry.Kind)),
                Cell(entry.Reference, size: 8.5f),
                Cell(entry.Detail, size: 8.5f, color: Muted),
                Cell(
                    Money(entry.Amount),
                    size: 8.5f,
                    color: entry.Amount < 0 ? Credit : Ink,
                    align: TextAlignment.Right),
                Cell(Money(balance), size: 8.5f, align: TextAlignment.Right));
        }

        doc.Add(ledger);

        // ── Closing balance ───────────────────────────────────────────────────

        HorizontalStack closingRow = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Margin = new Thickness(0, 10, 0, 4),
        };
        closingRow.Add(new Container(), 2);

        VerticalStack closingBody = new();
        closingBody.Add(TotalLine("Opening balance", Money(OpeningBalance)));
        closingBody.Add(TotalLine("Invoiced and charged", Money(invoiced)));
        closingBody.Add(TotalLine("Paid and credited", Money(-paid)));
        closingBody.Add(new Container
        {
            BackgroundColor = Rule,
            Height = 0.75f,
            Margin = new Thickness(0, 5, 0, 5),
        });
        closingBody.Add(TotalLine("Balance now due", Money(closing), bold: true, size: 11f, color: Accent));

        closingRow.Add(
            new Container
            {
                BackgroundColor = ColorPalette.Indigo[50],
                CornerRadius = 6,
                Padding = new Thickness(12),
                Child = closingBody,
            },
            1.6f);
        doc.Add(closingRow);

        // ── Ageing analysis ───────────────────────────────────────────────────

        doc.Add(H2("Ageing of outstanding items"));
        doc.Add(Cell(
            "How the balance now due is distributed by age. Anything beyond the Current column is past its due date.",
            size: 9f,
            color: Muted,
            marginBottom: 8f));

        Table ageingTable = new([.. Buckets.Select(_ => 1f)])
        {
            CellPadding = 8,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        ageingTable.AddHeader([.. Buckets.Select(b => Th(b))]);
        ageingTable.AddRow(
        [
            .. Buckets.Select(bucket =>
            {
                decimal amount = ageing[bucket];
                return Cell(
                    Money(amount),
                    bold: true,
                    // Only the Current column is not yet late.
                    color: string.Equals(bucket, "Current", StringComparison.Ordinal) || amount == 0
                        ? Ink
                        : Overdue);
            }),
        ]);
        doc.Add(ageingTable);

        doc.Add(Cell(
            $"Total outstanding {Money(ageingTotal)}, of which {Money(overdue)} is overdue."
                + (reconciles ? " This agrees with the closing balance above." : string.Empty),
            size: 8.5f,
            color: Muted,
            marginTop: 6f,
            marginBottom: 4f));

        if (!reconciles)
        {
            doc.Add(Cell(
                $"The ageing analysis totals {Money(ageingTotal)} but the ledger closes at {Money(closing)}. "
                    + "This statement does not reconcile and must not be issued.",
                size: 9f,
                bold: true,
                color: Overdue,
                marginBottom: 4f));
        }

        // ── Remittance advice ─────────────────────────────────────────────────

        doc.Add(H2("Remittance advice"));

        VerticalStack slip = new() { Spacing = 6 };
        slip.Add(Cell(
            "Please detach and return with your payment, or quote the account number on your transfer.",
            size: 9f,
            color: Muted));
        slip.Add(RemittanceRow("Account number", Number));
        slip.Add(RemittanceRow("Statement date", GbDate(StatementDate)));
        slip.Add(RemittanceRow("Balance now due", Money(closing)));
        slip.Add(RemittanceRow("Amount enclosed", string.Empty));
        slip.Add(Cell(
            "Payment by bank transfer to the account shown on your invoices. "
                + "Queries: quote the account number and the reference of the item in question.",
            size: 8.5f,
            color: Muted,
            marginTop: 4f));

        doc.Add(new Container
        {
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 6,
            Padding = new Thickness(14),
            Child = slip,
        });

        return doc;
    }

    private static HorizontalStack RemittanceRow(string label, string value)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.Add(Cell(label, size: 9f, color: Muted), 2);

        VerticalStack valueStack = new();
        valueStack.Add(Cell(value, size: 9f, bold: value.Length > 0, align: TextAlignment.Right));

        // An empty value is a box the payer writes in.
        if (value.Length == 0)
        {
            valueStack.Add(new Container { BackgroundColor = ColorPalette.Slate[400], Height = 0.75f });
        }

        row.Add(valueStack, 1.5f);
        return row;
    }
}
