using System.Globalization;
using BonniePDF;
using static BonniePDF.Examples.Shared.TradeKit;

namespace BonniePDF.Examples.Templates.TradeDocuments;

/// <summary>
/// The collections letter, generated per overdue account on a schedule. It is
/// the point where the invoice cluster meets the correspondence cluster: a
/// letter in tone and structure, but carrying a schedule of overdue items and an
/// arithmetic that has to be right, because it is asserting a debt.
///
/// Two things make it worth generating rather than mail-merging. The schedule is
/// computed - days overdue derive from the due dates against the letter date, so
/// a letter sent a week later says something different without anyone editing
/// it. And the escalation stage drives the tone, the deadline, and the
/// consequences paragraph from one value, so a first reminder and a final notice
/// are the same document with a different stage.
///
/// Sample organisations, references, and amounts throughout. Nothing here is a
/// statutory notice or legal advice, and the interest and compensation figures
/// are illustrative.
/// </summary>
[System.ComponentModel.DisplayName("Dunning letter")]
[System.ComponentModel.Category("Invoices")]
[System.ComponentModel.Description("A collections letter whose overdue schedule, deadline, tone and consequences all derive from the escalation stage and the due dates, rather than being written out by hand. PDF/A-2a + PDF/UA-1.")]
public static class DunningLetterExample
{
    private sealed record Stage(string Name, int Days);

    /// <summary>The escalation ladder. The stage drives tone, deadline, and consequences.</summary>
    private static readonly Dictionary<int, Stage> Stages = new()
    {
        [1] = new("First reminder", 14),
        [2] = new("Second reminder", 10),
        [3] = new("Final notice before escalation", 7),
    };

    private const int StageNumber = 3;
    private const string Reference = "COL-2026-002281";
    private const string Account = "ACC-004182";
    private const string Supplier = "Sample Supplier Ltd";
    private const string CreditControl = "P. Ahmadi — Credit Control";
    private const string Contact = "credit.control@sample-supplier.example";
    private const string Phone = "01632 960 000";
    private const string Terms = "30 days net";

    /// <summary>Illustrative only; the real figures depend on the contract and jurisdiction.</summary>
    private const decimal InterestRate = 0.08m;
    private const decimal CompensationPerInvoice = 40m;

    private static readonly DateTime LetterDate = new(2026, 9, 15);

    private static readonly string[] CustomerAddress =
    [
        "Sample Customer Ltd", "Unit 7, Sample Business Park", "Sample Town", "SA1 4TP",
    ];

    private static readonly string[] SupplierAddress =
    [
        "Sample Supplier Ltd", "12 Sample Street", "Sample City", "SC2 9BD",
    ];

    private sealed record OverdueItem(string Reference, DateTime Issued, DateTime Due, decimal Amount);

    private static readonly OverdueItem[] Overdue =
    [
        new("INV-2026-3180", new DateTime(2026, 6, 15), new DateTime(2026, 7, 15), 1460.00m),
        new("INV-2026-3312", new DateTime(2026, 8, 3), new DateTime(2026, 9, 2), 1450.00m),
        new("INV-2026-3381", new DateTime(2026, 8, 14), new DateTime(2026, 9, 13), 1360.00m),
    ];

    private static int DaysOverdue(OverdueItem item, DateTime asAt)
        => Math.Max(0, (int)(asAt - item.Due).TotalDays);

    public static Document Generate()
    {
        Stage stage = Stages[StageNumber];

        // Days overdue are derived, so the same letter sent a week later says
        // something different without anyone editing it.
        (OverdueItem Item, int Days)[] rows = [.. Overdue.Select(item => (item, DaysOverdue(item, LetterDate)))];
        decimal principal = Overdue.Sum(item => item.Amount);

        // Illustrative statutory-style interest, accrued per item on a daily basis.
        decimal interest = rows.Sum(r => r.Item.Amount * InterestRate * r.Days / 365m);
        decimal compensation = Overdue.Length * CompensationPerInvoice;
        decimal totalClaimed = principal + interest + compensation;

        // A final notice is a different document from a first reminder, and it
        // should not look like one. Everything the eye lands on first - the rule,
        // the stage line, the table head, the total, the headings - takes the
        // same tone.
        Color tone = StageNumber == 3 ? Negative : Accent;

        DateTime deadline = LetterDate.AddDays(stage.Days);
        int oldest = rows.Max(r => r.Days);

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 50 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10).WithColor(Ink);
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"{Reference} · account {Account} · {stage.Name}",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"{stage.Name} — account {Account}";
        doc.Metadata.Author = Supplier;
        doc.Metadata.Subject =
            "Collections letter: overdue schedule with derived days overdue, illustrative interest and compensation, a payment deadline, and the consequences of non-payment (sample data).";
        doc.Metadata.Keywords = "dunning, collections, overdue, credit control, late payment, reminder, pdf/ua, pdf/a";

        // ── Letterhead ────────────────────────────────────────────────────────

        doc.Add(AccentBar(tone));
        doc.Add(Cell(stage.Name.ToUpperInvariant(), size: 8.5f, bold: true, color: tone, marginBottom: 12f));

        doc.Add(WeightedRow(
            [
                Lines(CustomerAddress, size: 10f),
                Lines(
                    [Supplier, .. SupplierAddress.Skip(1), string.Empty, Phone, Contact],
                    size: 9f,
                    color: Muted,
                    align: TextAlignment.Right),
            ],
            spacing: 20f,
            marginBottom: 18f));

        doc.Add(Cell(GbDate(LetterDate), size: 10f, color: Muted, marginBottom: 14f));

        doc.Add(H1($"Overdue account — {Money(principal)} outstanding"));
        doc.Add(Cell($"Our reference {Reference} · your account {Account}", color: Muted, marginBottom: 14f));

        // ── The body ──────────────────────────────────────────────────────────

        doc.Add(Cell("Dear Sir or Madam", size: 10f, marginBottom: 10f));

        doc.Add(Cell(
            $"Our records show that {Money(principal)} remains outstanding on your account, across "
                + $"{Overdue.Length} invoices. The oldest is now {oldest} days past its due date. Our agreed "
                + $"payment terms are {Terms}.",
            size: 10f,
            marginBottom: 10f));

        doc.Add(StageNumber == 3
            ? Cell(
                "We have written to you twice about this and have not received payment or a response. This letter "
                    + "is a final notice before the account is escalated.",
                size: 10f,
                marginBottom: 10f)
            : Cell(
                "This may be an oversight. If payment has been sent in the last few days, please accept our thanks "
                    + "and disregard this letter.",
                size: 10f,
                marginBottom: 10f));

        // ── The schedule ──────────────────────────────────────────────────────

        doc.Add(H2("Invoices outstanding", tone));

        Table table = new(1.8f, 1.4f, 1.4f, 1.3f, 1.5f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = tone,
            HeaderTextColor = Color.White,
        };
        table.AddHeader(
            Th("Invoice"),
            Th("Issued"),
            Th("Due"),
            Th("Days overdue", TextAlignment.Right),
            Th("Amount", TextAlignment.Right));
        foreach ((OverdueItem item, int days) in rows)
        {
            table.AddRow(
                Td(item.Reference, bold: true),
                Td(GbDate(item.Issued), color: Muted),
                Td(GbDate(item.Due), color: Muted),
                Td(
                    days.ToString(CultureInfo.InvariantCulture),
                    bold: true,
                    color: days > 60 ? Negative : Ink,
                    align: TextAlignment.Right),
                Td(Money(item.Amount), bold: true, align: TextAlignment.Right));
        }

        doc.Add(table);

        doc.Add(TotalsPanel(
        [
            TotalLine("Principal outstanding", Money(principal)),
            TotalLine($"Interest at {InterestRate * 100:0}% per annum", Money(interest)),
            TotalLine($"Compensation, {Money(CompensationPerInvoice)} per invoice", Money(compensation)),
            Divider(),
            TotalLine("Total now claimed", Money(totalClaimed), bold: true, size: 12f, color: tone),
        ]));

        doc.Add(Cell(
            "Interest is calculated on each invoice from its due date to the date of this letter. Interest and "
                + "compensation figures are illustrative of the basis on which they would be claimed; the amounts "
                + "actually recoverable depend on the contract and the applicable law.",
            size: 8.5f,
            color: Muted,
            marginTop: 6f,
            marginBottom: 10f));

        // ── What we are asking for ────────────────────────────────────────────

        // The debt is stated in full by this point. What follows is the response
        // the letter asks for, and it reads as one block or not at all - a page
        // break through the middle of it is how a payer misses the deadline.
        doc.AddPage();
        doc.Add(H2("What we are asking you to do", tone));
        doc.Add(Panel(
            $"Pay {Money(principal)} by {GbDate(deadline)}",
            [
                Cell(
                    $"Payment by bank transfer to the account shown on your invoices, quoting {Account}. If you pay "
                        + $"the principal in full by {GbDate(deadline)} we will not pursue the interest or "
                        + "compensation shown above.",
                    size: 9.5f,
                    marginBottom: 3f),
                Cell(
                    "If any of these invoices is disputed, tell us which one and on what ground, and we will hold "
                        + "that item while we investigate. A dispute over one invoice does not hold the others.",
                    size: 9.5f,
                    marginBottom: 3f),
                Cell(
                    "If you are unable to pay in full, contact us before the date above. We would rather agree a "
                        + "payment plan than escalate.",
                    size: 9.5f),
            ],
            tone));

        if (StageNumber == 3)
        {
            doc.Add(H2("If we do not hear from you", tone));
            doc.Add(Cell(
                $"If payment is not received and you have not contacted us by {GbDate(deadline)}, the account will "
                    + "be placed on stop, further orders will not be processed, and the debt will be referred for "
                    + "collection. Costs and interest may then be added.",
                size: 10f,
                marginBottom: 10f));
            doc.Add(Cell(
                "We would prefer to resolve this directly. A telephone call today is enough to stop the escalation.",
                size: 10f,
                marginBottom: 12f));
        }

        doc.Add(Cell("Yours faithfully", size: 10f, marginBottom: 24f));
        doc.Add(Cell(CreditControl, size: 10f, bold: true, marginBottom: 2f));
        doc.Add(Cell(Supplier, size: 10f, color: Muted, marginBottom: 2f));
        doc.Add(Cell($"{Phone} · {Contact}", size: 9f, color: Muted));

        return doc;
    }
}
