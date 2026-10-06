using System.Globalization;
using BonniePDF;
using static BonniePDF.Examples.Shared.TradeKit;

namespace BonniePDF.Examples.Templates.Timesheet;

/// <summary>
/// The document professional-services software produces every week: what a
/// consultant worked on, against which project and rate, what is billable and
/// what is not, and the approval that turns it into an invoice line.
///
/// It is the natural companion to the quotation at one end of the engagement and
/// the invoice at the other, and it is where the arithmetic is most visible to
/// the person signing it: daily totals across the bottom, per-project totals down
/// the side, and both adding to the same figure. That cross-footing is the whole
/// point - a timesheet whose row totals and column totals disagree is one nobody
/// will approve.
///
/// Sample people, projects, clients, and rates throughout.
/// </summary>
[System.ComponentModel.DisplayName("Timesheet")]
[System.ComponentModel.Category("HR")]
[System.ComponentModel.Description("A weekly timesheet that cross-foots: hours by project and day whose row and column totals agree, with billable value, utilisation against target, expenses, and the approval that releases it for invoicing. PDF/A-2a + PDF/UA-1.")]
public static class TimesheetExample
{
    private const string Reference = "TS-2026-W37-0114";
    private const string Consultant = "Sample Consultant — Senior Consultant";
    private const string EmployeeId = "EMP-0114";
    private const string Approver = "Sample Manager — Engagement Manager";
    private const decimal ContractedHours = 37.5m;
    private const decimal UtilisationTarget = 0.8m;

    private static readonly DateTime WeekCommencing = new(2026, 9, 7);
    private static readonly DateTime WeekEnding = new(2026, 9, 13);
    private static readonly DateTime Submitted = new(2026, 9, 14);

    private static readonly string[] Days = ["Mon 07", "Tue 08", "Wed 09", "Thu 10", "Fri 11", "Sat 12", "Sun 13"];

    /// <param name="Rate">Hourly rate; zero where the line is not billable.</param>
    /// <param name="Hours">Hours per day, in the order of <see cref="Days"/>.</param>
    private sealed record ProjectRow(
        string Code,
        string Client,
        string Task,
        bool Billable,
        decimal Rate,
        decimal[] Hours);

    private static readonly ProjectRow[] Rows =
    [
        new("PRJ-2210", "Sample Client Ltd", "Data migration — phase 1", true, 145m,
            [7.5m, 6.0m, 0m, 4.0m, 3.5m, 0m, 0m]),
        new("PRJ-2210", "Sample Client Ltd", "Migration workshop — onsite", true, 165m,
            [0m, 0m, 7.5m, 0m, 0m, 0m, 0m]),
        new("PRJ-2244", "Sample Customer Ltd", "Integration support — retainer", true, 145m,
            [0m, 1.5m, 0m, 3.0m, 2.0m, 0m, 0m]),
        new("PRJ-2251", "Sample Buyer Ltd", "Discovery — scoping call and notes", true, 145m,
            [0m, 0m, 0m, 0m, 1.5m, 0m, 0m]),
        new("INT-0001", "Internal", "Practice meeting and pipeline review", false, 0m,
            [0m, 0m, 0m, 0.5m, 0m, 0m, 0m]),
        new("INT-0004", "Internal", "Accredited training — certification renewal", false, 0m,
            [0m, 0m, 0m, 0m, 1.5m, 0m, 0m]),
    ];

    private sealed record Expense(string Date, string Project, string Description, decimal Amount, bool Rechargeable);

    private static readonly Expense[] Expenses =
    [
        new("09 Sep 2026", "PRJ-2210", "Rail fare — onsite workshop, return", 84.60m, true),
        new("09 Sep 2026", "PRJ-2210", "Subsistence — onsite day", 18.50m, true),
        new("11 Sep 2026", "INT-0004", "Certification examination fee", 125.00m, false),
    ];

    private static string F1(decimal value) => value.ToString("F1", CultureInfo.InvariantCulture);

    public static Document Generate()
    {
        // Everything is computed. The cross-footing below - row totals and column
        // totals adding to the same figure - is what an approver actually checks.
        decimal[] rowTotals = [.. Rows.Select(row => row.Hours.Sum())];
        decimal[] dayTotals = [.. Days.Select((_, dayIndex) => Rows.Sum(row => row.Hours[dayIndex]))];

        decimal totalFromRows = rowTotals.Sum();
        decimal totalFromDays = dayTotals.Sum();
        bool crossFoots = Math.Abs(totalFromRows - totalFromDays) < 0.005m;

        decimal billableHours = Rows.Where(r => r.Billable).Sum(r => r.Hours.Sum());
        decimal nonBillableHours = totalFromRows - billableHours;
        decimal billableValue = Rows.Where(r => r.Billable).Sum(r => r.Hours.Sum() * r.Rate);
        decimal utilisation = totalFromRows > 0 ? billableHours / totalFromRows : 0m;

        decimal rechargeable = Expenses.Where(e => e.Rechargeable).Sum(e => e.Amount);
        decimal nonRechargeable = Expenses.Where(e => !e.Rechargeable).Sum(e => e.Amount);

        // Landscape, because seven day columns plus project, client, task, rate
        // and totals do not fit a portrait page at a readable size.
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Orientation = PageOrientation.Landscape,
            Margin = 40,
        });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(Ink);
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"{Reference} · {Consultant} · week ending {GbDate(WeekEnding)}",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Timesheet {Reference} — week ending {GbDate(WeekEnding)}";
        doc.Metadata.Author = "Sample Consultancy Ltd";
        doc.Metadata.Subject =
            "Weekly timesheet: hours by project and day, billable value, utilisation, expenses, and the approval that releases it for invoicing (sample data).";
        doc.Metadata.Keywords =
            "timesheet, billable hours, utilisation, professional services, approval, pdf/ua, pdf/a";

        doc.Add(AccentBar());
        doc.Add(Cell("TIMESHEET · WEEKLY", size: 8.5f, bold: true, color: Accent, marginBottom: 4f));
        doc.Add(H1($"Week ending {GbDate(WeekEnding)}"));
        doc.Add(Cell(Consultant, color: Muted, marginBottom: 12f));

        doc.Add(WeightedRow(
            [
                Field("Timesheet", Reference),
                Field("Employee", EmployeeId),
                Field("Week commencing", GbDate(WeekCommencing)),
                Field("Submitted", GbDate(Submitted)),
                Field("Approver", Approver),
            ],
            marginBottom: 6f));

        doc.Add(WeightedRow(
            [
                Tile(F1(totalFromRows), "Hours recorded"),
                Tile(F1(billableHours), "Billable", Positive),
                Tile(F1(nonBillableHours), "Non-billable", Muted),
                Tile(
                    $"{(utilisation * 100).ToString("F0", CultureInfo.InvariantCulture)}%",
                    $"Utilisation (target {(UtilisationTarget * 100).ToString("F0", CultureInfo.InvariantCulture)}%)",
                    utilisation >= UtilisationTarget ? Positive : Negative),
                Tile(Money(billableValue), "Billable value", Accent),
            ],
            spacing: 8f,
            marginBottom: 4f));

        // ── The grid ──────────────────────────────────────────────────────────

        doc.Add(H2("Hours by project and day"));

        Table grid = new(1.1f, 1.6f, 3.2f, 0.9f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.85f, 1.05f)
        {
            CellPadding = 5,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        grid.AddHeader(
        [
            Th("Project"),
            Th("Client"),
            Th("Task"),
            Th("Rate", TextAlignment.Right),
            .. Days.Select(day => Th(day, TextAlignment.Right)),
            Th("Hours", TextAlignment.Right),
            Th("Value", TextAlignment.Right),
        ]);

        for (int index = 0; index < Rows.Length; index++)
        {
            ProjectRow row = Rows[index];
            decimal hours = rowTotals[index];
            grid.AddRow(
            [
                Td(row.Code, bold: true),
                Td(row.Client, color: Muted),
                Td(row.Task),
                Td(row.Billable ? Money(row.Rate) : "—", color: row.Billable ? Ink : Muted, align: TextAlignment.Right),
                .. row.Hours.Select(value => Td(
                    value == 0 ? "·" : F1(value),
                    color: value == 0 ? Muted : Ink,
                    align: TextAlignment.Right)),
                Td(F1(hours), bold: true, align: TextAlignment.Right),
                Td(
                    row.Billable ? Money(hours * row.Rate) : "Non-billable",
                    bold: row.Billable,
                    color: row.Billable ? Ink : Muted,
                    align: TextAlignment.Right),
            ]);
        }

        // The cross-footing row: day totals along the bottom, against the row total.
        grid.AddRow(
        [
            Td("Daily total", bold: true, color: Accent),
            Td(string.Empty),
            Td(string.Empty),
            Td(string.Empty),
            .. dayTotals.Select(value => Td(F1(value), bold: true, color: Accent, align: TextAlignment.Right)),
            Td(F1(totalFromDays), bold: true, color: Accent, align: TextAlignment.Right),
            Td(Money(billableValue), bold: true, color: Accent, align: TextAlignment.Right),
        ]);
        doc.Add(grid);

        doc.Add(crossFoots
            ? Cell(
                $"Row totals and daily totals both come to {F1(totalFromRows)} hours against "
                    + $"{F1(ContractedHours)} contracted.",
                size: 8.5f,
                color: Muted,
                marginTop: 6f,
                marginBottom: 4f)
            : Cell(
                $"Row totals come to {F1(totalFromRows)} hours but daily totals come to {F1(totalFromDays)}. "
                    + "This timesheet does not cross-foot and must not be approved.",
                size: 9.5f,
                bold: true,
                color: Negative,
                marginTop: 6f,
                marginBottom: 4f));

        // ── Expenses ──────────────────────────────────────────────────────────

        // Each section is kept whole, so a heading never sits apart from its table and a table is never left with its totals on another page.
        VerticalStack expensesSection = new() { BreakAllowed = false };
        expensesSection.Add(H2("Expenses"));

        Table expenses = new(1.2f, 1.2f, 5.4f, 1.6f, 1.2f)
        {
            CellPadding = 5,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        expenses.AddHeader(
            Th("Date"),
            Th("Project"),
            Th("Description"),
            Th("Recharge"),
            Th("Amount", TextAlignment.Right));
        foreach (Expense expense in Expenses)
        {
            expenses.AddRow(
                Td(expense.Date),
                Td(expense.Project),
                Td(expense.Description),
                Td(expense.Rechargeable ? "Rechargeable" : "Absorbed", color: expense.Rechargeable ? Positive : Muted),
                Td(Money(expense.Amount), bold: true, align: TextAlignment.Right));
        }

        expensesSection.Add(expenses);

        expensesSection.Add(TotalsPanel(
        [
            TotalLine("Billable time", Money(billableValue)),
            TotalLine("Rechargeable expenses", Money(rechargeable)),
            TotalLine("Absorbed expenses", Money(nonRechargeable), color: Muted),
            Divider(),
            TotalLine("Invoiceable this week", Money(billableValue + rechargeable), bold: true, size: 11f, color: Accent),
        ]));
        doc.Add(expensesSection);

        // ── Approval ──────────────────────────────────────────────────────────

        VerticalStack approval = new() { BreakAllowed = false };
        approval.Add(H2("Declaration and approval"));
        approval.Add(Panel("Consultant declaration",
        [
            Cell(
                "I confirm that the hours recorded above were worked on the projects shown, that the expenses "
                    + "claimed were incurred in performing them, and that receipts have been submitted for every "
                    + "rechargeable item.",
                size: 9f),
        ]));
        approval.Add(Panel(
            "Approval releases this for invoicing",
            [
                Cell(
                    "Approving this timesheet releases the billable time and rechargeable expenses to the next "
                        + "invoice run for each project. Query any line before approving; a line queried after "
                        + "approval has to be credited.",
                    size: 9f),
            ],
            Negative));

        approval.Add(WeightedRow(
            [
                Field("Submitted by", $"{Consultant} — {GbDate(Submitted)}"),
                Field("Approved by", $"{Approver} — date"),
                Field("Released to billing", "Reference"),
            ],
            spacing: 22f,
            marginBottom: 0f));
        doc.Add(approval);

        return doc;
    }
}
