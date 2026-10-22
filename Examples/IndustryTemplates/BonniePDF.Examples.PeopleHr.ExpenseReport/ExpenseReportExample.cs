using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.PeopleHr.ExpenseReport;

/// <summary>
/// The form finance asks for after every trip, and the one that is most often
/// sent back. Reports are returned for the same three reasons: a foreign amount
/// with no stated rate, a receipt missing above the limit, and a total that does
/// not match the lines.
///
/// So none of those can happen here. Foreign-currency lines carry their original
/// amount, the rate, and the converted pounds, converted in whole-number
/// arithmetic so the result is the same every time. The policy is applied to
/// the data rather than described next to it: a meal over its limit and an
/// unreceipted line above £25 are flagged on the row itself and counted in the
/// tiles. Totals by category are summed from the lines.
///
/// Sample people, projects and amounts throughout.
/// </summary>
[System.ComponentModel.DisplayName("Expense report")]
[System.ComponentModel.Category("People & HR")]
[System.ComponentModel.Description(
    "An employee expense claim: foreign-currency lines converted at a stated rate, policy checks that flag over-limit meals and missing receipts, totals by category, and approval slots for the employee, manager, and finance. PDF/A-2a + PDF/UA-1.")]
public static class ExpenseReportExample
{
    private const string Number = "ER-2026-0417";
    private const string Employee = "Alex Sample";
    private const string EmployeeId = "EMP-20418";
    private const string Department = "Sales";
    private const string CostCentre = "CC-4410";
    private const string Project = "Customer visits — Lisbon";
    private const string Manager = "Jordan Reviewer";

    /// <summary>Pence per euro, in ten-thousandths: 0.8615.</summary>
    private const int EurRate = 8_615;

    private const int MealLimit = 3_500;
    private const int ReceiptLimit = 2_500;

    private static readonly DateTime From = new(2026, 10, 10);
    private static readonly DateTime To = new(2026, 10, 17);
    private static readonly DateTime Submitted = new(2026, 10, 19);

    private static readonly string[] Categories = ["Travel", "Accommodation", "Meals", "Client entertainment", "Office"];

    /// <summary><c>Minor</c> is pence or cents in the line's own currency.</summary>
    private sealed record ExpenseLine(int Day, string Description, string Category, string Currency, int Minor, bool Receipt);

    private sealed record Row(ExpenseLine Line, int Pence, string Flag);

    private static readonly ExpenseLine[] Lines =
    [
        new(10, "Train to Sample City Airport", "Travel", "GBP", 1_850, true),
        new(10, "Lunch at the airport", "Meals", "GBP", 1_495, true),
        new(10, "Taxi, Lisbon airport to hotel", "Travel", "EUR", 2_400, true),
        new(10, "Dinner, Restaurante Exemplo", "Meals", "EUR", 5_280, true),
        new(11, "Hotel Wi-Fi supplement", "Accommodation", "EUR", 900, true),
        new(11, "Coffee and pastry", "Meals", "EUR", 640, false),
        new(12, "Metro day pass", "Travel", "EUR", 650, true),
        new(12, "Team lunch", "Meals", "EUR", 3_120, true),
        new(13, "Client gift, Sample Lda", "Client entertainment", "GBP", 4_500, true),
        new(14, "Printing, proposal packs", "Office", "GBP", 2_310, true),
        new(14, "Dinner", "Meals", "EUR", 3_890, false),
        new(15, "Taxi to client site", "Travel", "EUR", 1_850, true),
        new(16, "Lunch", "Meals", "EUR", 2_180, true),
        new(17, "Taxi to Lisbon airport", "Travel", "EUR", 2_650, true),
        new(17, "Train home from the airport", "Travel", "GBP", 1_850, true),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        Row[] rows = [.. Lines.Select(line =>
        {
            int pence = ToPence(line);
            return new Row(line, pence, PolicyFlag(line, pence));
        })];
        int total = rows.Sum(r => r.Pence);
        Row[] flagged = [.. rows.Where(r => r.Flag != string.Empty)];
        int flaggedTotal = flagged.Sum(r => r.Pence);
        int receipts = rows.Count(r => r.Line.Receipt);
        (string Category, int Count, int Pence)[] byCategory =
        [
            .. Categories.Select(category => (
                category,
                rows.Count(r => r.Line.Category == category),
                rows.Where(r => r.Line.Category == category).Sum(r => r.Pence))),
        ];
        if (byCategory.Sum(c => c.Pence) != total)
        {
            throw new InvalidOperationException("Categories do not add up");
        }

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 46 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"Expense report {Number} · {Employee} · sample data",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Expense report {Number} — {Employee}";
        doc.Metadata.Author = Employee;
        doc.Metadata.Subject =
            "Employee expense report: foreign-currency conversion, policy flags, totals by category, and approval slots (sample data).";
        doc.Metadata.Keywords = "expense report, expenses, claim, receipts, policy, finance, hr, pdf/ua, pdf/a";

        doc.Add(AccentBar());
        doc.Add(Cell("EXPENSE CLAIM", size: 8.5f, bold: true, color: Accent, marginBottom: 4f));
        doc.Add(H1($"Expense report {Number}"));
        doc.Add(Cell($"{Project} · {GbDate(From)} to {GbDate(To)}", color: Muted, marginBottom: 14f));

        doc.Add(WeightedRow(
            [
                Field("Employee", $"{Employee} ({EmployeeId})"),
                Field("Department", Department),
                Field("Cost centre", CostCentre),
                Field("Approving manager", Manager),
            ],
            marginBottom: 6f));
        doc.Add(WeightedRow(
            [
                Field("Submitted", GbDate(Submitted)),
                Field("Currency of claim", "GBP"),
                Field("Euro rate used", $"1 EUR = £{(EurRate / 10_000m).ToString("F4", CultureInfo.InvariantCulture)}"),
                Field("Lines", rows.Length.ToString(CultureInfo.InvariantCulture)),
            ],
            marginBottom: 4f));

        doc.Add(WeightedRow(
            [
                Tile(Money(total), "Total claimed"),
                Tile(Money(total - flaggedTotal), "Within policy", Positive),
                Tile(flagged.Length.ToString(CultureInfo.InvariantCulture), "Lines to review", flagged.Length > 0 ? Negative : Positive),
                Tile($"{receipts} of {rows.Length}", "Receipts attached"),
            ],
            spacing: 10f,
            marginBottom: 4f));

        doc.Add(H2("Expenses"));
        Table table = new(1.25f, 2.65f, 1.45f, 0.8f, 1.2f, 1.05f, 1.55f)
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
            Th("Receipt"),
            Th("Original", TextAlignment.Right),
            Th("GBP", TextAlignment.Right),
            Th("Policy"));
        foreach (Row row in rows)
        {
            ExpenseLine line = row.Line;
            table.AddRow(
                Td(GbDate(new DateTime(2026, 10, line.Day)), color: Muted),
                Td(line.Description),
                Td(line.Category, color: Muted),
                Td(line.Receipt ? "Yes" : "No", bold: !line.Receipt, color: line.Receipt ? Positive : Negative),
                Td(line.Currency == "GBP" ? "—" : $"{line.Currency} {Amount(line.Minor)}", color: Muted, align: TextAlignment.Right),
                Td(Money(row.Pence), bold: true, align: TextAlignment.Right),
                Td(row.Flag == string.Empty ? "OK" : row.Flag, bold: row.Flag != string.Empty, color: row.Flag == string.Empty ? Muted : Negative));
        }

        table.AddRow(
            Td(string.Empty),
            Td("Total", bold: true),
            Td(string.Empty),
            Td(string.Empty),
            Td(string.Empty),
            Td(Money(total), bold: true, align: TextAlignment.Right),
            Td(string.Empty));
        doc.Add(table);

        doc.Add(H2("By category"));
        Table summary = new(3f, 1f, 1.6f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        summary.AddHeader(Th("Category"), Th("Lines", TextAlignment.Right), Th("Total", TextAlignment.Right));
        foreach ((string category, int count, int pence) in byCategory)
        {
            summary.AddRow(
                Td(category),
                Td(count.ToString(CultureInfo.InvariantCulture), align: TextAlignment.Right),
                Td(Money(pence), align: TextAlignment.Right));
        }

        summary.AddRow(
            Td("All categories", bold: true),
            Td(rows.Length.ToString(CultureInfo.InvariantCulture), bold: true, align: TextAlignment.Right),
            Td(Money(total), bold: true, align: TextAlignment.Right));
        doc.Add(summary);

        if (flagged.Length > 0)
        {
            List<IElement> notes = [];
            foreach (Row r in flagged)
            {
                notes.Add(Cell(
                    $"{GbDate(new DateTime(2026, 10, r.Line.Day))} · {r.Line.Description} ({Money(r.Pence)}) — {r.Flag}.",
                    size: 9f,
                    marginBottom: 3f));
            }

            notes.Add(Cell(
                $"Policy: meals up to {Money(MealLimit)} each, and a receipt for any line above {Money(ReceiptLimit)}. "
                + "Lines outside policy can still be approved with a note.",
                size: 9f,
                color: Muted));
            doc.Add(Panel("Lines for your manager to review", notes, Negative));
        }

        doc.Add(H2("Approvals"));
        doc.Add(Cell(
            "I confirm these expenses were incurred wholly for business purposes, that the receipts attached are the originals "
            + "or true copies, and that nothing here has been claimed before.",
            size: 9f,
            color: Muted,
            marginBottom: 8f));
        doc.Add(WeightedRow(
            [
                Slot("Employee", Employee),
                Slot("Manager approval", Manager),
                Slot("Finance", "Reference and date"),
            ],
            spacing: 22f,
            marginBottom: 6f));

        return doc;
    }

    /// <summary>The line in pence. Euros convert at the claim rate, rounding half up, in whole numbers.</summary>
    private static int ToPence(ExpenseLine line) =>
        line.Currency == "GBP" ? line.Minor : (line.Minor * EurRate + 5_000) / 10_000;

    /// <summary>Empty when the line is within policy; otherwise the reason it needs a second look.</summary>
    private static string PolicyFlag(ExpenseLine line, int pence)
    {
        if (line.Category == "Meals" && pence > MealLimit)
        {
            return "Over meal limit";
        }

        if (!line.Receipt && pence > ReceiptLimit)
        {
            return "Receipt needed";
        }

        return string.Empty;
    }

    /// <summary>Minor units as a decimal amount: 1,234.56.</summary>
    private static string Amount(int minor)
    {
        string whole = (minor / 100).ToString("N0", CultureInfo.InvariantCulture);
        string rest = (minor % 100).ToString("D2", CultureInfo.InvariantCulture);
        return $"{whole}.{rest}";
    }

    /// <summary>Pence as pounds: £1,234.56.</summary>
    private static string Money(int pence) => $"£{Amount(pence)}";

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

    private static readonly Color Accent = ColorPalette.Violet[700];

    private static readonly Color Rule = ColorPalette.Slate[200];

    private static readonly Color Positive = ColorPalette.Emerald[700];

    private static readonly Color Negative = ColorPalette.Rose[700];

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
}
