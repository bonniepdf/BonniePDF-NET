using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.FinanceReporting.BankStatement;

/// <summary>
/// What a bank sends every month, and the document most people have actually
/// read closely: a balance at each end, every movement in between, and a
/// running balance that has to add up row by row.
///
/// The detail that makes it a statement rather than a table: nothing in the
/// money columns is typed in. Every amount is held in pence, the running balance
/// is calculated from the rows in order, and the opening balance plus money in
/// less money out is checked against the closing balance before the page is
/// drawn. A transaction list of 34 rows runs across pages with its header
/// repeated, and the spending chart is summed from the same rows.
///
/// Sample organisations and numbers throughout.
/// </summary>
[System.ComponentModel.DisplayName("Bank statement")]
[System.ComponentModel.Category("Finance & Reporting")]
[System.ComponentModel.Description(
    "A current-account statement: opening and closing balances, money in and out, spending by category, and a 34-row transaction list with a running balance whose header repeats across pages. PDF/A-2a + PDF/UA-1.")]
public static class BankStatementExample
{
    private static readonly string[] Holder = ["Ms A. Sample", "14 Example Gardens", "Sample Town", "ST4 7XY"];

    private static readonly string[] Bank = ["Sample Bank plc", "Customer Services", "PO Box 1000", "Sample City", "SC1 1AA"];

    private const string AccountNumber = "00482193";
    private const string SortCode = "99-00-42";
    private const string Iban = "GB00 SMPL 9900 4200 4821 93";
    private const string StatementNumber = "2026/09";
    private const int OpeningBalance = 241_867;

    private static readonly DateTime From = new(2026, 9, 1);
    private static readonly DateTime To = new(2026, 9, 30);

    /// <summary>Amounts are pence: positive money in, negative money out.</summary>
    private sealed record Transaction(int Day, string Description, string Category, int Amount);

    private sealed record CategorySpend(string Category, int Pence);

    private static readonly Transaction[] Transactions =
    [
        new(1, "Salary — Sample Employer Ltd", "Income", 312_450),
        new(1, "Standing order — Rent, Sample Lettings", "Housing", -125_000),
        new(2, "Card — Sample Supermarket", "Groceries", -6_482),
        new(3, "Direct debit — Sample Energy", "Utilities", -8_900),
        new(3, "Direct debit — Sample Broadband", "Utilities", -3_499),
        new(4, "Card — Sample Coffee Co", "Eating out", -385),
        new(5, "Card — Sample Pharmacy", "Health", -1_275),
        new(5, "Transfer in — J Sample", "Transfers", 5_000),
        new(6, "Card — Sample Fuel", "Transport", -5_410),
        new(7, "Card — Sample Supermarket", "Groceries", -4_127),
        new(8, "Direct debit — Sample Insurance", "Insurance", -2_650),
        new(8, "Card — Sample Bookshop", "Shopping", -2_199),
        new(9, "Card — Sample Train Co", "Transport", -3_850),
        new(10, "Card — Sample Restaurant", "Eating out", -4_620),
        new(11, "Cash withdrawal — Sample ATM", "Cash", -4_000),
        new(12, "Card — Sample Supermarket", "Groceries", -5_873),
        new(12, "Card — Sample Streaming", "Entertainment", -1_099),
        new(13, "Refund — Sample Retailer", "Shopping", 3_499),
        new(14, "Card — Sample Hardware", "Home", -7_845),
        new(15, "Card — Sample Coffee Co", "Eating out", -410),
        new(15, "Direct debit — Sample Gym", "Health", -3_500),
        new(16, "Card — Sample Supermarket", "Groceries", -3_918),
        new(17, "Card — Sample Taxi", "Transport", -1_860),
        new(18, "Card — Sample Cinema", "Entertainment", -2_400),
        new(19, "Card — Sample Restaurant", "Eating out", -5_180),
        new(20, "Faster payment — Sample Plumbing", "Home", -18_500),
        new(21, "Card — Sample Supermarket", "Groceries", -6_204),
        new(22, "Transfer out — Savings pot", "Transfers", -20_000),
        new(23, "Card — Sample Fuel", "Transport", -4_985),
        new(24, "Card — Sample Garden Centre", "Home", -3_295),
        new(25, "Direct debit — Sample Council Tax", "Housing", -16_400),
        new(26, "Card — Sample Supermarket", "Groceries", -4_456),
        new(27, "Card — Sample Coffee Co", "Eating out", -395),
        new(28, "Card — Sample Online Store", "Shopping", -5_999),
        new(29, "Interest paid", "Interest", 214),
        new(30, "Card — Sample Supermarket", "Groceries", -3_842),
    ];

    /// <summary>Fixed commitments and moves between your own accounts are not everyday spending.</summary>
    private static readonly HashSet<string> NotEveryday = ["Housing", "Transfers"];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        int moneyIn = Transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        int moneyOut = Transactions.Where(t => t.Amount < 0).Sum(t => -t.Amount);
        int closing = OpeningBalance + moneyIn - moneyOut;

        // The running balance is the sum of the rows, so check it against the totals
        // before drawing anything: a statement that does not add up is worse than none.
        int running = OpeningBalance;
        int[] balances = [.. Transactions.Select(t => running += t.Amount)];
        if (running != closing)
        {
            throw new InvalidOperationException("Statement does not reconcile");
        }

        CategorySpend[] spending = SpendingByCategory();

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 46 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"Statement {StatementNumber} · account ending {AccountNumber[^4..]} · sample data, not a real account",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Current account statement {StatementNumber}";
        doc.Metadata.Author = "Sample Bank plc";
        doc.Metadata.Subject =
            "Current account statement: opening and closing balances, money in and out, spending by category, and a transaction list with a running balance (sample data).";
        doc.Metadata.Keywords = "bank statement, current account, transactions, running balance, finance, pdf/ua, pdf/a";

        doc.Add(AccentBar());
        doc.Add(Cell("CURRENT ACCOUNT STATEMENT", size: 8.5f, bold: true, color: Accent, marginBottom: 4f));
        doc.Add(H1("Your statement"));
        doc.Add(Cell($"{GbDate(From)} to {GbDate(To)} · statement {StatementNumber}", color: Muted, marginBottom: 14f));

        doc.Add(WeightedRow(
            [PartyBlock("Account holder", Holder), PartyBlock("Your bank", Bank)],
            spacing: 20f,
            marginBottom: 4f));
        doc.Add(WeightedRow(
            [
                Field("Account number", AccountNumber),
                Field("Sort code", SortCode),
                Field("IBAN", Iban),
                Field("Account type", "Everyday current account"),
            ],
            marginBottom: 6f));

        doc.Add(WeightedRow(
            [
                Tile(Money(OpeningBalance), "Opening balance"),
                Tile(Money(moneyIn), "Money in", Positive),
                Tile(Money(moneyOut), "Money out", Negative),
                Tile(Money(closing), "Closing balance"),
            ],
            spacing: 10f,
            marginBottom: 4f));

        doc.Add(H2("Where your everyday money went"));
        doc.Add(new BarChart
        {
            Data = new ChartData
            {
                Series =
                [
                    new ChartSeries
                    {
                        Color = Accent,
                        DataPoints =
                        [
                            .. spending.Select(s => new ChartDataPoint
                            {
                                Label = s.Category,
                                Value = (decimal)Math.Round(s.Pence / 100.0, MidpointRounding.AwayFromZero),
                            }),
                        ],
                    },
                ],
                YAxis = new AxisOptions { MaxValue = 400, StepCount = 4, Prefix = "£" },
            },
            AltText =
                $"Everyday spending by category, in pounds. {spending[0].Category} is the largest at {Money(spending[0].Pence)}; "
                + $"{spending[^1].Category} is the smallest at {Money(spending[^1].Pence)}.",
            Height = 130,
            StretchToFit = true,
            BarColor = Accent,
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 7,
            BackgroundColor = Color.FromHex("#ffffff"),
        });
        doc.Add(Cell(
            "Money out only, rounded to the nearest pound. Rent, council tax and transfers are left out.",
            size: 8f,
            color: Muted,
            marginTop: 4f,
            marginBottom: 4f));

        doc.Add(H2("Your transactions"));

        Table table = new(1.25f, 3.3f, 1.3f, 1.05f, 1.05f, 1.15f)
        {
            CellPadding = 5,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        table.AddHeader(
            Th("Date"),
            Th("Description"),
            Th("Category"),
            Th("Money out", TextAlignment.Right),
            Th("Money in", TextAlignment.Right),
            Th("Balance", TextAlignment.Right));
        table.AddRow(
            Td(GbDate(From), color: Muted),
            Td("Opening balance", bold: true),
            Td(string.Empty),
            Td(string.Empty),
            Td(string.Empty),
            Td(Money(OpeningBalance), bold: true, align: TextAlignment.Right));
        for (int i = 0; i < Transactions.Length; i++)
        {
            Transaction t = Transactions[i];
            table.AddRow(
                Td(GbDate(new DateTime(2026, 9, t.Day)), color: Muted),
                Td(t.Description),
                Td(t.Category, color: Muted),
                Td(t.Amount < 0 ? Money(-t.Amount) : string.Empty, align: TextAlignment.Right),
                Td(t.Amount > 0 ? Money(t.Amount) : string.Empty, color: Positive, align: TextAlignment.Right),
                Td(Money(balances[i]), bold: true, align: TextAlignment.Right));
        }

        table.AddRow(
            Td(GbDate(To), color: Muted),
            Td("Closing balance", bold: true),
            Td(string.Empty),
            Td(Money(moneyOut), bold: true, align: TextAlignment.Right),
            Td(Money(moneyIn), bold: true, color: Positive, align: TextAlignment.Right),
            Td(Money(closing), bold: true, align: TextAlignment.Right));
        doc.Add(table);

        doc.Add(Panel("Interest and charges this month",
        [
            Cell($"Interest paid: {Money(214)} (0.10% AER on the average daily balance).", size: 9f, marginBottom: 3f),
            Cell("Charges: none. You did not use an overdraft this month.", size: 9f),
        ]));
        doc.Add(Panel("Check your statement",
        [
            Cell(
                "If you see a payment you do not recognise, contact us straight away on the number on the back of your card. "
                + "You are protected against unauthorised payments, and the sooner you tell us the sooner we can act.",
                size: 9f),
        ]));

        return doc;
    }

    /// <summary>Everyday money out summed by category, largest first (ties broken by name).</summary>
    private static CategorySpend[] SpendingByCategory() =>
    [
        .. Transactions
            .Where(t => t.Amount < 0 && !NotEveryday.Contains(t.Category))
            .GroupBy(t => t.Category)
            .Select(g => new CategorySpend(g.Key, g.Sum(t => -t.Amount)))
            .OrderByDescending(s => s.Pence)
            .ThenBy(s => s.Category, StringComparer.Ordinal)
            .Take(7),
    ];

    /// <summary>Pence as pounds: £1,234.56, with a leading minus when negative.</summary>
    private static string Money(int pence) => NumberFormatter.Format(pence / 100m, "N2", "£");

    // Local helpers and sample data for this document.
    private static readonly Color Ink = ColorPalette.Slate[700];

    /// <summary>600, not 400: 400 never clears 4.5:1 as body text on white.</summary>
    private static readonly Color Muted = ColorPalette.Slate[600];

    private static readonly Color Heading = ColorPalette.Slate[800];

    private static readonly Color Accent = ColorPalette.Teal[700];

    private static readonly Color Rule = ColorPalette.Slate[200];

    private static readonly Color Positive = ColorPalette.Emerald[700];

    private static readonly Color Negative = ColorPalette.Rose[700];

    /// <summary>A styled text cell with statement defaults.</summary>
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

    /// <summary>A summary tile.</summary>
    private static Container Tile(string value, string label, Color? accent = null)
    {
        VerticalStack body = new();
        body.Add(Cell(value, size: 14f, bold: true, color: accent ?? Accent, marginBottom: 2f));
        body.Add(Cell(label, size: 8f, color: Muted));
        return new Container
        {
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 6,
            Padding = new Thickness(11, 10),
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
