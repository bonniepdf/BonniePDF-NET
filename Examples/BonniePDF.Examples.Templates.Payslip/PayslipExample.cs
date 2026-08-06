using System.Globalization;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.Payslip;

/// <summary>
/// A UK payslip in the familiar statutory layout, built top to bottom from six
/// small sections: masthead, employee strip, the side-by-side Payments |
/// Deductions tables, the headline net-pay band, a year-to-date summary, and a
/// disclaimer. Every total is derived from the line items, never hand-typed, so
/// the document always reconciles. Output is AES-256 encrypted and PDF/UA-1
/// tagged (not PDF/A — archival output cannot be encrypted); the demo password
/// is printed on the slip so a developer can open the downloaded file.
/// </summary>
[System.ComponentModel.DisplayName("Payslip")]
[System.ComponentModel.Category("HR")]
[System.ComponentModel.Description("An encrypted UK payslip: earnings, deductions, taxes, year-to-date totals, masked identifiers, and AES-256 encryption. Privacy-sensitive and client-side-generated. PDF/UA-1 tagged.")]
public static class PayslipExample
{
    // ── Design tokens ─────────────────────────────────────────────────────────
    private static readonly Color Ink = ColorPalette.Slate[800];
    private static readonly Color Muted = ColorPalette.Slate[500];
    private static readonly Color Faint = ColorPalette.Slate[400];
    private static readonly Color Line = ColorPalette.Slate[200];
    private static readonly Color Panel = ColorPalette.Slate[100];
    private static readonly Color Brand = ColorPalette.Blue[600];
    private static readonly Color NetPanel = ColorPalette.Blue[50];
    private static readonly Color NetAmount = ColorPalette.Blue[700];

    // The demo password is printed on the slip (see Disclaimer) so a developer
    // who downloads the encrypted PDF can open it. Real payslips would not.
    private const string DemoPassword = "demo123";

    // ── Deterministic sample fixture ────────────────────────────────────────────
    private static readonly PayslipData Sample = new(
        new Employer("Sample Company Ltd", "12 Harbour Square, Bristol BS1 4QA", "123/AB456"),
        new Employee("Sample Employee", "NW-EMP-002384", "AB123456C", "A", "1257L", "Monthly", "BACS · Acct •••• 4521"),
        new PayPeriod("31 July 2026", 4, "2026/27"),
        Payments:
        [
            new MoneyLine("Basic salary", 4166.67m, 16666.68m),
            new MoneyLine("London weighting", 350.00m, 1400.00m),
            new MoneyLine("Overtime", 184.62m, 738.48m, Units: 8, Rate: 23.08m),
        ],
        Deductions:
        [
            new MoneyLine("Income Tax (PAYE)", 730.76m, 2923.04m, Kind: DeductionKind.Tax),
            new MoneyLine("National Insurance", 261.53m, 1046.12m, Kind: DeductionKind.Ni),
            new MoneyLine("Workplace pension (5%)", 208.33m, 833.32m, Kind: DeductionKind.Pension),
            new MoneyLine("Healthcare", 42.00m, 168.00m, Kind: DeductionKind.Other),
        ],
        Locale: "en-GB",
        Currency: "GBP");

    public static Document Generate()
    {
        DocumentSettings settings = new(PdfSize.A5, PageOrientation.Landscape, 26)
        {
            Conformance = PdfAConformance.None,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            FooterTemplate = pageNumber => new TextBlock
            {
                Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken}",
                FontName = StandardFonts.Helvetica,
                FontSize = 6.5f,
                TextColor = Faint,
                Alignment = TextAlignment.Center,
            },
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(8.5f).WithColor(Ink);

        // PDF/A is intentionally not claimed — archival PDF/A cannot be encrypted —
        // but PDF/UA-1 tagging works alongside AES-256 encryption.
        settings.WithEncryptionAes256(opts =>
            opts.WithUserPassword(DemoPassword).WithOwnerPassword(DemoPassword).AllowPrinting().AllowAccessibility());

        return BuildDocument(Sample, settings);
    }

    /// <summary>Pure builder: the same data always produces the same document.</summary>
    public static Document BuildDocument(PayslipData data, DocumentSettings settings)
    {
        Document doc = new(settings);
        doc.Metadata.Title = $"Payslip — {data.Employee.Name}";
        doc.Metadata.Author = data.Employer.Name;
        doc.Metadata.Subject = "Encrypted payslip / earnings statement (fictional sample data).";

        doc.Add(Masthead(data));
        doc.Add(EmployeeStrip(data));
        doc.Add(EarningsBlock(data));
        doc.Add(NetPayBand(data));
        doc.Add(YearToDate(data));
        doc.Add(Disclaimer());
        return doc;
    }

    // ── Section builders (top to bottom) ────────────────────────────────────────

    private static VerticalStack Masthead(PayslipData data)
    {
        VerticalStack identity = new() { Spacing = 2 };
        identity.Add(Text(data.Employer.Name, bold: true, size: 15, color: Brand));
        identity.Add(Text(data.Employer.Address, size: 7.5f, color: Muted));

        VerticalStack period = new() { Spacing = 2 };
        period.Add(Text("PAYSLIP", bold: true, size: 11, align: TextAlignment.Right));
        period.Add(Text($"Pay date   {data.PayPeriod.PayDate}", size: 8, color: Muted, align: TextAlignment.Right));
        period.Add(Text($"Tax month {data.PayPeriod.TaxMonth}  ·  {data.PayPeriod.TaxYear}", size: 8, color: Muted, align: TextAlignment.Right));

        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.Add(identity, 1);
        row.Add(period, 1);

        VerticalStack stack = new();
        stack.Add(row);
        stack.Add(new Container { Height = 2, BackgroundColor = Brand, Margin = new Thickness(0, 8, 0, 12) });
        return stack;
    }

    private static Container EmployeeStrip(PayslipData data) => Strip(
    [
        Field("Employee", data.Employee.Name),
        Field("Payroll no.", data.Employee.PayrollId),
        Field("NI number", MaskId(data.Employee.NiNumber)),
        Field("NI category", data.Employee.NiCategory),
        Field("Tax code", data.Employee.TaxCode),
        Field("Pay frequency", data.Employee.PayFrequency),
    ]);

    private static HorizontalStack EarningsBlock(PayslipData data)
    {
        Table payments = new(2f, 0.7f, 0.9f, 1.1f)
        {
            CellPadding = 5,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Line),
            HeaderBackgroundColor = Panel,
        };
        payments.AddHeader(ColumnHead("Payments"), ColumnHead("Units", TextAlignment.Right), ColumnHead("Rate", TextAlignment.Right), ColumnHead("Amount", TextAlignment.Right));
        foreach (MoneyLine line in data.Payments)
        {
            payments.AddRow(
                Text(line.Label),
                Text(line.Units is { } units ? units.ToString(CultureInfo.InvariantCulture) : "", align: TextAlignment.Right),
                Text(line.Rate is { } rate ? Money(rate, data) : "", color: Muted, align: TextAlignment.Right),
                Text(Money(line.Amount, data), align: TextAlignment.Right));
        }
        payments.AddRow(Text("Gross pay", bold: true), Text(""), Text(""), Text(Money(Sum(data.Payments, l => l.Amount), data), bold: true, align: TextAlignment.Right));

        Table deductions = new(2f, 1.1f)
        {
            CellPadding = 5,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Line),
            HeaderBackgroundColor = Panel,
        };
        deductions.AddHeader(ColumnHead("Deductions"), ColumnHead("Amount", TextAlignment.Right));
        foreach (MoneyLine line in data.Deductions)
        {
            deductions.AddRow(Text(line.Label), Text(Money(line.Amount, data), align: TextAlignment.Right));
        }
        deductions.AddRow(Text("Total deductions", bold: true), Text(Money(Sum(data.Deductions, l => l.Amount), data), bold: true, align: TextAlignment.Right));

        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 14, Margin = new Thickness(0, 0, 0, 12) };
        row.Add(payments, 1.3f);
        row.Add(deductions, 1f);
        return row;
    }

    private static Container NetPayBand(PayslipData data)
    {
        decimal net = Sum(data.Payments, l => l.Amount) - Sum(data.Deductions, l => l.Amount);

        VerticalStack label = new() { Spacing = 1 };
        label.Add(Text("NET PAY", bold: true, size: 9, color: Ink));
        label.Add(Text($"Paid by {data.Employee.PaidTo}", size: 7.5f, color: Muted));

        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.Add(label, 1);
        row.Add(Text(Money(net, data), bold: true, size: 16, color: NetAmount, align: TextAlignment.Right), 1);

        return new Container
        {
            BackgroundColor = NetPanel,
            Border = Border.LeftOnly(3),
            BorderColors = BorderColors.LeftOnly(Brand),
            Padding = new Thickness(10, 12, 10, 12),
            Margin = new Thickness(0, 0, 0, 12),
            Child = row,
        };
    }

    private static Container YearToDate(PayslipData data)
    {
        decimal grossYtd = Sum(data.Payments, l => l.Ytd);
        decimal netYtd = grossYtd - Sum(data.Deductions, l => l.Ytd);
        decimal YtdOf(DeductionKind kind) => data.Deductions.FirstOrDefault(l => l.Kind == kind)?.Ytd ?? 0m;

        return Strip(
        [
            Field("Taxable gross (YTD)", Money(grossYtd, data)),
            Field("Income tax (YTD)", Money(YtdOf(DeductionKind.Tax), data)),
            Field("National Insurance (YTD)", Money(YtdOf(DeductionKind.Ni), data)),
            Field("Pension (YTD)", Money(YtdOf(DeductionKind.Pension), data)),
            Field("Net pay (YTD)", Money(netYtd, data)),
        ]);
    }

    private static TextBlock Disclaimer() => Text(
        $"Sample payslip · fictional data · encrypted PDF (open password: {DemoPassword})",
        bold: true, size: 10, color: Ink);

    // ── Small helpers ───────────────────────────────────────────────────────────

    private static TextBlock Text(string text, bool bold = false, float size = 8.5f, Color? color = null, TextAlignment align = TextAlignment.Left) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = size,
        TextColor = color ?? Ink,
        Alignment = align,
    };

    private static TextBlock ColumnHead(string text, TextAlignment align = TextAlignment.Left) =>
        Text(text, bold: true, size: 7, color: Muted, align: align);

    /// <summary>A label-over-value cell, the unit the metadata strips are built from.</summary>
    private static VerticalStack Field(string label, string value)
    {
        VerticalStack stack = new() { Spacing = 1 };
        stack.Add(Text(label.ToUpperInvariant(), bold: true, size: 6.5f, color: Faint));
        stack.Add(Text(value));
        return stack;
    }

    /// <summary>Wraps a row of fields in a light panel — used for both strips.</summary>
    private static Container Strip(IElement[] fields)
    {
        float[] columns = new float[fields.Length];
        Array.Fill(columns, 1f);
        Grid grid = new(columns) { CellPadding = 5, Border = Border.None };
        grid.AddRow(fields);
        return new Container { BackgroundColor = Panel, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(0, 0, 0, 12), Child = grid };
    }

    private static string Money(decimal value, PayslipData data) =>
        value.ToString("C2", CultureInfo.GetCultureInfo(data.Locale));

    private static string MaskId(string id, int keep = 4) =>
        id.Length <= keep ? id : "•••• " + id[^keep..];

    private static decimal Sum(MoneyLine[] lines, Func<MoneyLine, decimal> field) =>
        Math.Round(lines.Sum(field), 2);

    // ── Domain model (the data-to-document boundary) ─────────────────────────────

    public enum DeductionKind { None, Tax, Ni, Pension, Other }

    public sealed record MoneyLine(string Label, decimal Amount, decimal Ytd, int? Units = null, decimal? Rate = null, DeductionKind Kind = DeductionKind.None);

    public sealed record Employer(string Name, string Address, string PayeReference);

    public sealed record Employee(string Name, string PayrollId, string NiNumber, string NiCategory, string TaxCode, string PayFrequency, string PaidTo);

    public sealed record PayPeriod(string PayDate, int TaxMonth, string TaxYear);

    public sealed record PayslipData(Employer Employer, Employee Employee, PayPeriod PayPeriod, MoneyLine[] Payments, MoneyLine[] Deductions, string Locale, string Currency);
}
