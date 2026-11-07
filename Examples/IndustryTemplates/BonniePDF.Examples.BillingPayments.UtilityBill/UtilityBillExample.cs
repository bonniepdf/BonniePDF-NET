using System.Globalization;
using BonniePDF;
using BonniePDF.Drawing;

namespace BonniePDF.Examples.BillingPayments.UtilityBill;

/// <summary>
/// A domestic energy bill. The interesting problem is not the arithmetic — it
/// is that one page has to serve three different readers at once: someone
/// checking what they owe, someone querying how it was calculated, and a
/// payment system scanning a slip. So the page is built in three registers: a
/// headline amount-due band, a full tariff derivation from meter readings so
/// the total is auditable rather than asserted, a twelve-month consumption
/// chart putting this period in context, and a detachable payment slip below a
/// real dashed cut line. Output is PDF/A-2a + PDF/UA-1.
/// </summary>
[System.ComponentModel.DisplayName("Utility Bill")]
[System.ComponentModel.Category("Billing & Payments")]
[System.ComponentModel.Description(
    "A domestic energy bill that derives every figure from meter readings rather than asserting a total: a headline amount due, a full tariff breakdown, a twelve-month consumption chart, and a detachable payment slip below a dashed cut line carrying a scannable payment reference. PDF/A-2a + PDF/UA-1.")]
public static class UtilityBillExample
{
    // ── Design tokens ─────────────────────────────────────────────────────────
    private static readonly Color Ink = ColorPalette.Slate[800];

    private static readonly Color Muted = ColorPalette.Slate[500];

    // 600, not 400: 400 never clears 4.5:1 as text (2.51-2.63:1 on this
    // document's backgrounds).
    private static readonly Color Faint = ColorPalette.Slate[600];
    private static readonly Color Line = ColorPalette.Slate[200];
    private static readonly Color Panel = ColorPalette.Slate[50];
    private static readonly Color Accent = ColorPalette.Amber[600];
    private static readonly Color AccentSoft = ColorPalette.Amber[50];
    private static readonly Color AccentDeep = ColorPalette.Amber[700];

    // ── Deterministic sample fixture ──────────────────────────────────────────
    private static readonly UtilityBillData Sample = new(
        new Supplier("Sample Energy", "PO Box 4120, Cardiff CF10 3RT", "0800 000 0000",
            "Supply licence held with the energy regulator"),
        new Customer("Sample Customer", "48 Sample Road, Bristol BS6 6DR", "SE-8842-01773"),
        new BillingPeriod("1 May 2026", "31 July 2026", "5 August 2026", "19 August 2026"),
        Charges:
        [
            new MeterCharge("Electricity", "E19K-442081", 41822, 42615, 24.50m, 60.10m, 92),
            new MeterCharge("Gas", "G07T-118254", 8801, 9184, 6.24m, 31.43m, 92),
        ],
        BroughtForward: 0m,
        VatRatePercent: 5m,
        History:
        [
            new MonthUsage("Aug", 902), new MonthUsage("Sep", 874), new MonthUsage("Oct", 1015),
            new MonthUsage("Nov", 1288), new MonthUsage("Dec", 1502), new MonthUsage("Jan", 1611),
            new MonthUsage("Feb", 1436), new MonthUsage("Mar", 1204), new MonthUsage("Apr", 1002),
            new MonthUsage("May", 861), new MonthUsage("Jun", 798), new MonthUsage("Jul", 810),
        ],
        Payment: new PaymentDetails("SE884201773", "20-00-00", "41827734"),
        Locale: "en-GB",
        Currency: "GBP");

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        DocumentSettings settings = new(PdfSize.A4, PageOrientation.Portrait, 38)
        {
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9f).WithColor(Ink);
        return BuildDocument(Sample, settings);
    }

    /// <summary>Pure builder: the same data always produces the same document.</summary>
    public static Document BuildDocument(UtilityBillData data, DocumentSettings settings)
    {
        Document doc = new(settings);
        doc.Metadata.Title = $"Energy bill — {data.Customer.AccountNumber} — {data.Period.To}";
        doc.Metadata.Author = data.Supplier.Name;
        doc.Metadata.Subject = "Domestic energy bill with a detachable payment slip (fictional sample data).";

        doc.Add(Masthead(data));
        doc.Add(AccountStrip(data));
        doc.Add(AmountDueBand(data));
        doc.Add(TariffTable(data));
        doc.Add(ConsumptionChart(data));
        doc.Add(CutLine());
        doc.Add(RemittanceSlip(data));
        return doc;
    }

    // ── Derivation — every figure on the bill comes from here ─────────────────

    /// <summary>Units consumed on a meter: the difference between the two readings.</summary>
    public static int UnitsUsed(MeterCharge charge) => charge.CurrentReading - charge.PreviousReading;

    public static decimal EnergyCost(MeterCharge charge) => Round2(UnitsUsed(charge) * charge.UnitRatePence / 100m);

    public static decimal StandingCost(MeterCharge charge) => Round2(charge.Days * charge.StandingChargePence / 100m);

    public static decimal ChargeSubtotal(MeterCharge charge) => Round2(EnergyCost(charge) + StandingCost(charge));

    public static decimal NetTotal(UtilityBillData data) => Round2(data.Charges.Sum(ChargeSubtotal));

    public static decimal VatAmount(UtilityBillData data) => Round2(NetTotal(data) * data.VatRatePercent / 100m);

    public static decimal AmountDue(UtilityBillData data) =>
        Round2(NetTotal(data) + VatAmount(data) + data.BroughtForward);

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    // ── Section builders (top to bottom) ──────────────────────────────────────

    private static VerticalStack Masthead(UtilityBillData data)
    {
        Heading title = new(data.Supplier.Name)
        {
            Level = 1,
            TocIdentifier = "bill",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16f,
            Color = AccentDeep,
        };

        VerticalStack identity = new() { Spacing = 2 };
        identity.Add(title);
        identity.Add(Text(data.Supplier.Address, size: 7.5f, color: Muted));
        identity.Add(Text($"Enquiries {data.Supplier.Phone}", size: 7.5f, color: Faint));

        VerticalStack marker = new() { Spacing = 2 };
        marker.Add(Text("YOUR ENERGY BILL", bold: true, size: 11, align: TextAlignment.Right));
        marker.Add(Text($"{data.Period.From} to {data.Period.To}", size: 8, color: Muted, align: TextAlignment.Right));
        marker.Add(Text($"Issued {data.Period.Issued}", size: 7.5f, color: Muted, align: TextAlignment.Right));

        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.Add(identity, 1);
        row.Add(marker, 1);

        VerticalStack stack = new();
        stack.Add(row);
        stack.Add(new Container { Height = 2, BackgroundColor = Accent, Margin = new Thickness(0, 8, 0, 12) });
        return stack;
    }

    private static Container AccountStrip(UtilityBillData data)
    {
        IElement[] fields =
        [
            Field("Account name", data.Customer.Name),
            Field("Supply address", data.Customer.Address),
            Field("Account number", data.Customer.AccountNumber),
            Field("Payment due by", data.Period.DueBy),
        ];

        float[] columns = new float[fields.Length];
        Array.Fill(columns, 1f);
        Grid grid = new(columns) { CellPadding = 5, Border = Border.None };
        grid.AddRow(fields);

        return new Container
        {
            BackgroundColor = Panel,
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(0, 0, 0, 12),
            Child = grid,
        };
    }

    /// <summary>The headline. A bill is read for this number first.</summary>
    private static Container AmountDueBand(UtilityBillData data)
    {
        VerticalStack label = new() { Spacing = 1 };
        label.Add(Text("TOTAL AMOUNT DUE", bold: true, size: 9));
        label.Add(Text($"Please pay by {data.Period.DueBy}", size: 8, color: Muted));

        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.Add(label, 1);
        row.Add(Text(Money(AmountDue(data), data), bold: true, size: 20, color: AccentDeep, align: TextAlignment.Right),
            1);

        return new Container
        {
            BackgroundColor = AccentSoft,
            Border = Border.LeftOnly(3),
            BorderColors = BorderColors.LeftOnly(Accent),
            Padding = new Thickness(10, 12, 10, 12),
            Margin = new Thickness(0, 0, 0, 14),
            Child = row,
        };
    }

    /// <summary>
    /// The derivation. Meter readings in, money out, one row per step — so a
    /// customer querying the total can follow it without a calculator, and
    /// nothing on the bill is an unexplained assertion.
    /// </summary>
    private static VerticalStack TariffTable(UtilityBillData data)
    {
        Table table = new(1.5f, 1.5f, 0.8f, 0.9f, 1f)
        {
            CellPadding = 5,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Line),
            HeaderBackgroundColor = Panel,
        };
        table.AddHeader(
            ColumnHead("Charge"),
            ColumnHead("Meter / basis"),
            ColumnHead("Units", TextAlignment.Right),
            ColumnHead("Rate", TextAlignment.Right),
            ColumnHead("Amount", TextAlignment.Right));

        foreach (MeterCharge charge in data.Charges)
        {
            table.AddRow(
                Text($"{charge.Fuel} — energy", bold: true),
                Text($"{charge.MeterSerial}  ·  {charge.PreviousReading} → {charge.CurrentReading}", size: 8,
                    color: Muted),
                Text(UnitsUsed(charge).ToString(CultureInfo.InvariantCulture), align: TextAlignment.Right),
                Text($"{charge.UnitRatePence.ToString("F2", CultureInfo.InvariantCulture)}p",
                    align: TextAlignment.Right),
                Text(Money(EnergyCost(charge), data), align: TextAlignment.Right));
            table.AddRow(
                Text($"{charge.Fuel} — standing charge"),
                Text($"{charge.Days} days", size: 8, color: Muted),
                Text(charge.Days.ToString(CultureInfo.InvariantCulture), align: TextAlignment.Right),
                Text($"{charge.StandingChargePence.ToString("F2", CultureInfo.InvariantCulture)}p",
                    align: TextAlignment.Right),
                Text(Money(StandingCost(charge), data), align: TextAlignment.Right));
        }

        void TotalRow(string label, string value, bool bold = false) => table.AddRow(
            Text(label, bold: bold),
            Text(""),
            Text(""),
            Text(""),
            Text(value, bold: bold, align: TextAlignment.Right));

        TotalRow("Total before VAT", Money(NetTotal(data), data));
        TotalRow($"VAT at {data.VatRatePercent}%", Money(VatAmount(data), data));
        if (data.BroughtForward != 0m)
        {
            TotalRow("Balance brought forward", Money(data.BroughtForward, data));
        }

        TotalRow("Total amount due", Money(AmountDue(data), data), bold: true);

        VerticalStack stack = new() { Margin = new Thickness(0, 0, 0, 14) };
        stack.Add(SectionHeading("How this bill is calculated", "calculation"));
        stack.Add(table);
        return stack;
    }

    /// <summary>This period against the last twelve — context makes a bill useful.</summary>
    private static VerticalStack ConsumptionChart(UtilityBillData data)
    {
        ChartData chartData = new()
        {
            Series =
            [
                new ChartSeries
                {
                    DataPoints = [.. data.History.Select(h => new ChartDataPoint { Label = h.Month, Value = h.Units })]
                }
            ],
            // Fit the axis to the data: the top of the scale is the busiest month rounded up to 500 units.
            YAxis = { MaxValue = (int)Math.Ceiling(data.History.Max(h => h.Units) / 500.0) * 500, StepCount = 4 },
        };

        int min = data.History.Min(h => h.Units);
        int max = data.History.Max(h => h.Units);

        BarChart chart = new()
        {
            Data = chartData,
            AltText =
                $"Monthly energy use over the last twelve months, from {data.History[0].Month} to {data.History[^1].Month}, ranging from {min} to {max} units. Use peaks in winter and falls through the summer.",
            Theme = "modern",
            Height = 150,
            StretchToFit = true,
            BarColor = Accent,
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 7,
            BackgroundColor = Color.FromHex("#ffffff"),
        };

        TextBlock caption =
            Text("Units per month, including both fuels. Winter peaks are normal for a home of this size.", size: 8,
                color: Muted);
        caption.Margin = new Thickness(0, 4, 0, 0);

        VerticalStack stack = new() { Margin = new Thickness(0, 0, 0, 14) };
        stack.Add(SectionHeading("Your energy use over the last year", "usage"));
        stack.Add(chart);
        stack.Add(caption);
        return stack;
    }

    /// <summary>
    /// A real dashed rule, drawn through the canvas path API with a dash
    /// pattern, rather than a row of hyphens pretending to be one. Marking it
    /// decorative keeps it out of the reading order — it is a cutting guide.
    /// </summary>
    private static CanvasElement CutLine() => new((canvas, width, height) =>
    {
        canvas.BeginPath();
        canvas.MoveTo(0, height / 2);
        canvas.LineTo(width, height / 2);
        canvas.StrokeCurrentPath(new StrokeStyle
            { Paint = Color.FromHex("#94a3b8"), Width = 0.75f, DashPattern = [3, 3] });
    })
    {
        DrawHeight = 10,
        IsDecorative = true,
        Margin = new Thickness(0, 6, 0, 6),
    };

    /// <summary>The detachable half: what a payments team keys in, or a scanner reads.</summary>
    private static Container RemittanceSlip(UtilityBillData data)
    {
        VerticalStack details = new() { Spacing = 5 };
        details.Add(Field("Account number", data.Customer.AccountNumber, boldValue: true));
        details.Add(Field("Payment reference", data.Payment.Reference, boldValue: true));
        details.Add(Field("Sort code", data.Payment.SortCode, boldValue: true));
        details.Add(Field("Bank account", data.Payment.AccountNumber, boldValue: true));

        Barcode barcode = new()
        {
            Value = data.Payment.Reference,
            Symbology = BarcodeSymbology.Code128,
            Height = 34,
            ModuleWidth = 0.9f,
            IncludeTextLabel = true,
            TextLabelFontSize = 6.5f,
            AltText = $"Payment reference barcode: {data.Payment.Reference}",
            Margin = new Thickness(0, 6, 0, 0),
        };

        VerticalStack payable = new() { Spacing = 2 };
        payable.Add(Text("AMOUNT PAYABLE", bold: true, size: 6.5f, color: Faint));
        payable.Add(Text(Money(AmountDue(data), data), bold: true, size: 17, color: AccentDeep));
        payable.Add(Text($"Due by {data.Period.DueBy}", size: 7.5f, color: Muted));
        payable.Add(barcode);

        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 18 };
        row.Add(details, 1.1f);
        row.Add(payable, 1f);

        TextBlock title = Text("Payment slip — detach and return with your payment", bold: true, size: 9);
        title.Margin = new Thickness(0, 0, 0, 8);

        VerticalStack body = new();
        body.Add(title);
        body.Add(row);

        return new Container
        {
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Line),
            Padding = new Thickness(12, 12, 12, 12),
            Child = body,
        };
    }

    // ── Small helpers ─────────────────────────────────────────────────────────

    private static Heading SectionHeading(string text, string id) => new(text)
    {
        Level = 2,
        TocIdentifier = id,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 11f,
        Color = Ink,
        Margin = new Thickness(0, 0, 0, 6),
    };

    private static TextBlock ColumnHead(string text, TextAlignment align = TextAlignment.Left) =>
        Text(text, bold: true, size: 7.5f, color: Muted, align: align);

    /// <summary>A label-over-value cell, the unit the strip and slip are built from.</summary>
    private static VerticalStack Field(string label, string value, bool boldValue = false)
    {
        VerticalStack stack = new() { Spacing = 1 };
        stack.Add(Text(label.ToUpperInvariant(), bold: true, size: 6.5f, color: Faint));
        stack.Add(Text(value, bold: boldValue));
        return stack;
    }

    private static TextBlock Text(string text, bool bold = false, float size = 9f, Color? color = null,
        TextAlignment align = TextAlignment.Left) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = size,
        Color = color ?? Ink,
        Alignment = align,
    };

    private static string Money(decimal value, UtilityBillData data) =>
        value.ToString("C2", CultureInfo.GetCultureInfo(data.Locale));

    // ── Domain model (the data-to-document boundary) ──────────────────────────

    public sealed record Supplier(string Name, string Address, string Phone, string Regulator);

    public sealed record Customer(string Name, string Address, string AccountNumber);

    public sealed record BillingPeriod(string From, string To, string Issued, string DueBy);

    /// <summary>One metered fuel, with the readings its charge derives from.</summary>
    public sealed record MeterCharge(
        string Fuel,
        string MeterSerial,
        int PreviousReading,
        int CurrentReading,
        decimal UnitRatePence,
        decimal StandingChargePence,
        int Days);

    public sealed record MonthUsage(string Month, int Units);

    public sealed record PaymentDetails(string Reference, string SortCode, string AccountNumber);

    public sealed record UtilityBillData(
        Supplier Supplier,
        Customer Customer,
        BillingPeriod Period,
        MeterCharge[] Charges,
        decimal BroughtForward,
        decimal VatRatePercent,
        MonthUsage[] History,
        PaymentDetails Payment,
        string Locale,
        string Currency);
}
