using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.FinancialStatement;

/// <summary>
/// Fictional UK/EU statutory financial statements in an IFRS-style format:
/// statement of comprehensive income and statement of financial position, with
/// prior-year comparatives, note references, bracketed negatives, and subtotal
/// rules. Amounts are shown in GBP thousands.
/// </summary>
[System.ComponentModel.DisplayName("IFRS Financial Statements")]
[System.ComponentModel.Category("Reports")]
[System.ComponentModel.Description("UK/EU statutory financial statements in an IFRS-style format: income statement and financial position with comparatives, note references, bracketed negatives, and subtotal rules.")]
public static class FinancialStatementExample
{
    private static readonly Color Navy = ColorPalette.Slate[800];
    private static readonly Color Ink = ColorPalette.Slate[700];
    private static readonly Color Muted = ColorPalette.Slate[500];
    private static readonly Color Rule = ColorPalette.Slate[300];

    private static readonly StatementLine[] Income =
    [
        Line("Revenue", "2", 48210, 42880),
        Line("Cost of sales", cur: -29140, prior: -26050),
        Line("Gross profit", cur: 19070, prior: 16830, kind: Kind.Subtotal),
        Line("Administrative expenses", cur: -11240, prior: -10110),
        Line("Other operating income", "3", 640, 410),
        Line("Operating profit", cur: 8470, prior: 7130, kind: Kind.Subtotal),
        Line("Finance income", cur: 120, prior: 90),
        Line("Finance costs", cur: -515, prior: -560),
        Line("Profit before taxation", cur: 8075, prior: 6660, kind: Kind.Subtotal),
        Line("Taxation", "6", -1940, -1600),
        Line("Profit for the financial year", cur: 6135, prior: 5060, kind: Kind.Total),
    ];

    private static readonly StatementLine[] Position =
    [
        Section("Non-current assets"),
        Line("Property, plant and equipment", "9", 14820, 13110, indent: true),
        Line("Intangible assets", "10", 6240, 5980, indent: true),
        Line("Right-of-use assets", cur: 3115, prior: 3480, indent: true),
        Line("", cur: 24175, prior: 22570, kind: Kind.Subtotal),
        Section("Current assets"),
        Line("Inventories", cur: 4460, prior: 3990, indent: true),
        Line("Trade and other receivables", "12", 8730, 7620, indent: true),
        Line("Cash and cash equivalents", cur: 9905, prior: 6840, indent: true),
        Line("", cur: 23095, prior: 18450, kind: Kind.Subtotal),
        Line("Total assets", cur: 47270, prior: 41020, kind: Kind.Total),
        Section("Current liabilities"),
        Line("Trade and other payables", cur: -7180, prior: -6410, indent: true),
        Line("Lease liabilities", cur: -640, prior: -600, indent: true),
        Line("Current tax", cur: -1890, prior: -1520, indent: true),
        Line("", cur: -9710, prior: -8530, kind: Kind.Subtotal),
        Section("Non-current liabilities"),
        Line("Borrowings", "14", -8200, -9000, indent: true),
        Line("Deferred tax", cur: -1225, prior: -1090, indent: true),
        Line("", cur: -9425, prior: -10090, kind: Kind.Subtotal),
        Line("Net assets", cur: 28135, prior: 22400, kind: Kind.Total),
        Section("Equity"),
        Line("Share capital", cur: 2000, prior: 2000, indent: true),
        Line("Share premium", cur: 5400, prior: 5400, indent: true),
        Line("Retained earnings", cur: 20735, prior: 15000, indent: true),
        Line("Total equity", cur: 28135, prior: 22400, kind: Kind.Total),
    ];

    public static Document Generate()
    {
        DocumentSettings settings = new(PdfSize.A4, 54)
        {
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            FooterTemplate = pageNumber => new TextBlock
            {
                Text = $"Sample Company Ltd  -  Company no. 07741122  -  Page {pageNumber} of {TextElementBase.TotalPagesToken}",
                FontName = StandardFonts.Helvetica,
                FontSize = 8,
                TextColor = Muted,
            },
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10).WithColor(Ink);

        Document doc = new(settings);
        doc.Metadata.Title = "Financial Statements - Sample Company Ltd";
        doc.Metadata.Author = "Sample Company Ltd";
        doc.Metadata.Subject = "Statutory financial statements (IFRS) for the year ended 31 March 2026 (fictional).";

        doc.Add(Company("Sample Company Ltd"));
        doc.Add(H1("Annual Report and Financial Statements"));
        doc.Add(Sub("For the year ended 31 March 2026"));
        doc.Add(Sub("Company registration number 07741122 (England & Wales)  -  Prepared under UK-adopted IFRS"));
        doc.Add(Gap(18));

        doc.Add(H2("Statement of Comprehensive Income", "comprehensive-income"));
        doc.Add(ColumnHeads());
        doc.Add(RuleLine(Rule, 0.6f));
        foreach (StatementLine line in Income) doc.Add(RenderLine(line));
        doc.Add(Gap(8));
        doc.Add(Sub("The notes on pages 8 to 21 form part of these financial statements."));

        doc.AddPage();
        doc.Add(H2("Statement of Financial Position", "financial-position"));
        doc.Add(Sub("As at 31 March 2026"));
        doc.Add(Gap(6));
        doc.Add(ColumnHeads());
        doc.Add(RuleLine(Rule, 0.6f));
        foreach (StatementLine line in Position) doc.Add(RenderLine(line));
        doc.Add(Gap(12));
        doc.Add(Sub("These financial statements were approved by the Board of Directors and authorised for issue on 20 June 2026, and are signed on its behalf by A. Whitfield, Director."));

        return doc;
    }

    private static IElement RenderLine(StatementLine line)
    {
        if (line.Kind == Kind.Section)
        {
            return new TextBlock
            {
                Text = line.Label,
                FontName = StandardFonts.HelveticaBold,
                FontSize = 10,
                TextColor = Navy,
                Margin = new Thickness(0, 8, 0, 4),
            };
        }

        bool bold = line.Kind is Kind.Subtotal or Kind.Total;
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Margin = new Thickness(0, 2, 0, 2),
        };

        TextBlock label = new()
        {
            Text = line.Label,
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = 10,
            TextColor = bold ? Navy : Ink,
            Margin = line.Indent ? new Thickness(14, 0, 0, 0) : new Thickness(),
        };

        row.Add(label, 6);
        row.Add(NumCell(line.Note ?? string.Empty, false, Muted, TextAlignment.Center), 1);
        row.Add(NumCell(Format(line.Current), bold, bold ? Navy : Ink, TextAlignment.Right), 2);
        row.Add(NumCell(Format(line.Prior), bold, Muted, TextAlignment.Right), 2);

        if (line.Kind is not (Kind.Subtotal or Kind.Total)) return row;

        VerticalStack stack = new();
        stack.Add(RuleLine(line.Kind == Kind.Total ? Navy : Rule, line.Kind == Kind.Total ? 0.8f : 0.5f));
        stack.Add(row);
        if (line.Kind == Kind.Total) stack.Add(RuleLine(Navy, 0.5f));
        return stack;
    }

    private static HorizontalStack ColumnHeads()
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Margin = new Thickness(0, 2, 0, 2),
        };
        row.Add(NumCell("", false, Muted, TextAlignment.Left), 6);
        row.Add(NumCell("Note", false, Muted, TextAlignment.Center), 1);
        row.Add(NumCell("2026", true, Navy, TextAlignment.Right), 2);
        row.Add(NumCell("2025", true, Muted, TextAlignment.Right), 2);
        return row;
    }

    private static TextBlock NumCell(string text, bool bold, Color color, TextAlignment align) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = 10,
        TextColor = color,
        Alignment = align,
    };

    private static string Format(int? value)
    {
        if (!value.HasValue) return string.Empty;
        string number = Math.Abs(value.Value).ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
        return value.Value < 0 ? $"({number})" : number;
    }

    private static TextBlock Company(string text) => new()
    {
        Text = text,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 11,
        TextColor = Muted,
        Margin = new Thickness(0, 0, 0, 4),
    };

    private static Heading H1(string text) => new(text)
    {
        TocIdentifier = "title",
        Level = 1,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 22,
        TextColor = Navy,
        Margin = new Thickness(0, 0, 0, 4),
    };

    private static Heading H2(string text, string tocId) => new(text)
    {
        TocIdentifier = tocId,
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 14,
        TextColor = Navy,
        Margin = new Thickness(0, 4, 0, 8),
    };

    private static TextBlock Sub(string text) => new()
    {
        Text = text,
        FontName = StandardFonts.Helvetica,
        FontSize = 9,
        TextColor = Muted,
        Margin = new Thickness(0, 0, 0, 3),
    };

    private static Container RuleLine(Color color, float thickness) => new()
    {
        BackgroundColor = color,
        Padding = new Thickness(0, thickness / 2, 0, thickness / 2),
        Margin = new Thickness(0, 2, 0, 2),
        Child = new TextBlock { Text = " ", FontSize = 1 },
    };

    private static Container Gap(float height) => new()
    {
        Padding = new Thickness(0, height / 2, 0, height / 2),
        Child = new TextBlock { Text = " ", FontSize = 1 },
    };

    private static StatementLine Section(string label) => new(label, null, null, null, Kind.Section, false);

    private static StatementLine Line(
        string label,
        string? note = null,
        int? cur = null,
        int? prior = null,
        Kind kind = Kind.Line,
        bool indent = false) => new(label, note, cur, prior, kind, indent);

    private sealed record StatementLine(string Label, string? Note, int? Current, int? Prior, Kind Kind, bool Indent);

    private enum Kind
    {
        Line,
        Section,
        Subtotal,
        Total,
    }
}
