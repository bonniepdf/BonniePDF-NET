using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.FinanceReporting.FinancialDashboard;

/// <summary>A print-ready landscape annual review with KPI cards, gradient bars, a doughnut and a product table.</summary>
[System.ComponentModel.DisplayName("Financial Dashboard")]
[System.ComponentModel.Category("Finance & Reporting")]
[System.ComponentModel.Description("A print-ready landscape annual review with four KPIs, rounded gradient bars, a revenue-mix doughnut and a product revenue table.")]
public static class FinancialDashboardExample
{
    private static readonly Color Navy = Color.FromHex("#0f172a");
    private static readonly Color Blue = Color.FromHex("#2563eb");
    private static readonly Color Teal = Color.FromHex("#0d9488");
    private static readonly Color Violet = Color.FromHex("#7c3aed");
    private static readonly Color Amber = Color.FromHex("#d97706");
    private static readonly Color Muted = Color.FromHex("#475569");
    private static readonly Color Rule = Color.FromHex("#e2e8f0");
    private static readonly Color HeaderFill = Color.FromHex("#f8fafc");
    private static readonly Color Positive = Color.FromHex("#047857");

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        int year = DateTime.Now.Year - 1;
        string[] months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
        double[] revenue = [125, 146, 140, 175, 163, 190, 197, 182, 211, 226, 240, 233];
        Product[] products = [new("Enterprise", 910, 71, Blue), new("Professional", 650, 64, Teal), new("Starter", 446, 58, Violet), new("Services", 222, 82, Amber)];
        double total = revenue.Sum();
        double grossProfit = products.Sum(p => p.Revenue * p.Margin / 100);
        string totalLabel = "£" + F(total / 1000, "F2") + "M";
        double secondHalfGrowth = (revenue.Skip(6).Sum() / revenue.Take(6).Sum() - 1) * 100;
        // A white page with a printable margin: no full-bleed fills for a printer to waste ink on or clip.
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4, Orientation = PageOrientation.Landscape, Margin = 24,
            Conformance = PdfAConformance.PdfA2a, Accessibility = AccessibilityMode.PdfUa1, Language = "en-GB",
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(Muted)
        });
        doc.Metadata.Title = "Sample Company Ltd - FY " + year + " Financial Dashboard";
        doc.Metadata.Author = "Sample Company Ltd";
        doc.Metadata.Subject = "Annual performance review with revenue trends and product mix. Sample data.";

        Grid header = LayoutGrid([2, 1], 0);
        header.AddRow(
            Stack(Text("SAMPLE COMPANY LTD  /  FINANCE", 8, Blue, true),
                Text("Performance at a glance", 24, Navy, true, top: 6),
                Text("Annual review", 9, Muted, top: 5)),
            Stack(Text("FY " + year, 20, Navy, true, TextAlignment.Right),
                Text("REVENUE  +18.3% YoY", 8, Positive, true, TextAlignment.Right, 7),
                Text("Board & investor overview", 8, Muted, false, TextAlignment.Right, 5)));
        doc.Add(new Container { Padding = new Thickness(4, 0, 4, 8), Child = header });
        doc.Add(new Container { Height = 2, BackgroundColor = Blue, Margin = new Thickness(4, 0, 4, 6) });
        Grid metrics = LayoutGrid([1, 1, 1, 1], 4);
        metrics.AddRow(Kpi(totalLabel, "TOTAL REVENUE", "+18.3% vs prior year", Blue),
            Kpi("£" + F(grossProfit / 1000, "F2") + "M", "GROSS PROFIT", "+£272K vs prior year", Teal),
            Kpi(F(grossProfit / total * 100, "F1") + "%", "GROSS MARGIN", "+2.1 percentage points", Violet),
            Kpi("3,814", "ACTIVE CUSTOMERS", "+24.1% vs prior year", Amber));

        BarChart bars = new()
        {
            Theme = "modern", Height = 142, StretchToFit = true, ShowValueLabels = false, FontFamily = StandardFonts.Helvetica, LabelFontSize = 8,
            BarStyle = new ChartBarStyle { CornerRadius = 4, Gradient = new ChartLinearGradient { StartColor = Color.FromHex("#60a5fa"), EndColor = Blue, Scope = "bar" } },
            Data = new ChartData
            {
                Series = [new ChartSeries { Name = "Revenue", Color = Blue, DataPoints = months.Select((label, i) => new ChartDataPoint { Label = label, Value = (decimal)revenue[i] }).ToList() }],
                YAxis = new AxisOptions { Prefix = "£", MaxValue = 300, StepCount = 6 }
            },
            AltText = "Monthly revenue in thousands of pounds: 125, 146, 140, 175, 163, 190, 197, 182, 211, 226, 240, 233. Annual total £2,228,000."
        };
        PieChart donut = new()
        {
            Theme = "modern", Height = 132, StretchToFit = true, IsDonut = true, ShowLegend = false, ShowTotal = false,
            Colors = ["#2563eb", "#0d9488", "#7c3aed", "#d97706"],
            Data = new ChartData { Series = [new ChartSeries { DataPoints = products.Select(p => new ChartDataPoint { Label = p.Name, Value = (decimal)p.Revenue }).ToList() }] },
            AltText = "Revenue mix: Enterprise £910,000; Professional £650,000; Starter £446,000; Services £222,000. Total £2,228,000."
        };
        // A white circular foreground and centred labels demonstrate native vector layering.
        Layers donutLayers = new() { PrimaryLayer = donut };
        donutLayers.AddForeground(new Container
        {
            Height = 132, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            Child = new Container
            {
                Width = 64, Height = 64, CornerRadius = 32, BackgroundColor = Color.White, VerticalAlignment = VerticalAlignment.Center,
                Child = Stack(Text(totalLabel, 12, Navy, true, TextAlignment.Center), Text("TOTAL REVENUE", 5.5f, Muted, true, TextAlignment.Center, 4))
            }
        });
        Grid legend = LayoutGrid([1, 1], 1);
        IElement[] legendItems = products.Select(p => LegendItem(p.Name, F(Math.Round(p.Revenue / total * 100), "F0") + "%", p.Color)).ToArray();
        legend.AddRow(legendItems[0], legendItems[1]);
        legend.AddRow(legendItems[2], legendItems[3]);
        Grid charts = LayoutGrid([1.9f, 1], 4);
        charts.AddRow(Panel("Revenue momentum", "Monthly revenue (£000)  |  H2 +" + F(secondHalfGrowth, "F1") + "% vs H1", bars),
            Panel("Revenue mix", "A diversified product portfolio", donutLayers, legend));
        // A rule under each row only: no lines above the headings, at the edges or between columns.
        Table table = new(2.4f, 1.3f, 1, 1)
        {
            Border = new Border(0, 0, 0, 0.5f), BorderColors = new BorderColors(Rule), CellPadding = 4,
            HeaderBackgroundColor = HeaderFill, HeaderTextColor = Navy
        };
        table.AddHeader(Text("PRODUCT", 7, Muted, true), Text("REVENUE", 7, Muted, true, TextAlignment.Right), Text("SHARE", 7, Muted, true, TextAlignment.Right), Text("MARGIN", 7, Muted, true, TextAlignment.Right));
        foreach (Product p in products)
            table.AddRow(Text(p.Name, 8, Navy, true), Text("£" + F(p.Revenue * 1000, "N0"), 8, Navy, true, TextAlignment.Right),
                Text(F(Math.Round(p.Revenue / total * 100), "F0") + "%", 8, Muted, false, TextAlignment.Right), Text(p.Margin + "%", 8, Positive, true, TextAlignment.Right));
        Container bottom = Panel("Revenue by product", "", new Container { Margin = new Thickness(0, 7, 0, 0), Child = table });
        doc.Add(Stack(metrics, charts, new Container { Padding = new Thickness(4), Child = bottom }));
        Grid footer = LayoutGrid([2, 1], 0);
        footer.AddRow(Text("SAMPLE COMPANY LTD  /  FY " + year + "  /  Sample data in GBP", 7, Muted), Text("CONFIDENTIAL  /  01", 7, Muted, true, TextAlignment.Right));
        doc.Add(new Container { Padding = new Thickness(4, 4, 4, 0), Child = footer });
        return doc;
    }

    private static TextBlock Text(string value, float size, Color color, bool bold = false, TextAlignment align = TextAlignment.Left, float top = 0) =>
        new(value) { FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica, FontSize = size, Color = color, Alignment = align, Margin = new Thickness(0, top, 0, 0) };
    private static VerticalStack Stack(params IElement[] children)
    {
        VerticalStack stack = new();
        foreach (IElement child in children) stack.Add(child);
        return stack;
    }
    private static Grid LayoutGrid(float[] columns, float padding) => new(columns) { Border = Border.None, CellPadding = padding };
    // Cards and panels are outlined rather than filled, so they keep their shape on a white page.
    private static Container Kpi(string value, string label, string delta, Color accent) => new()
    {
        Border = new Border(0.75f), BorderColors = new BorderColors(Rule), CornerRadius = 6, Padding = new Thickness(14, 8),
        Child = Stack(new Container { BackgroundColor = accent, Width = 24, Height = 3, CornerRadius = 1.5f },
            Text(label, 7, Muted, true, top: 6), Text(value, 22, Navy, true, top: 4), Text(delta, 7, Positive, true, top: 3))
    };
    private static Container Panel(string title, string subtitle, params IElement[] children)
    {
        VerticalStack content = Stack(Text(title, 12, Navy, true));
        if (subtitle.Length > 0) content.Add(Text(subtitle, 7.5f, Muted, top: 4));
        foreach (IElement child in children) content.Add(child);
        return new Container { Border = new Border(0.75f), BorderColors = new BorderColors(Rule), CornerRadius = 6, Padding = new Thickness(12), Child = content };
    }
    private static Container LegendItem(string name, string share, Color color)
    {
        Grid row = LayoutGrid([9, 100], 0);
        row.AddRow(new Container { Width = 5, Height = 5, Margin = new Thickness(0, 2, 0, 0), CornerRadius = 2.5f, BackgroundColor = color }, Text(name + "  " + share, 7, Muted));
        return new Container { Padding = new Thickness(0, 3), Child = row };
    }
    private static string F(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
    private sealed record Product(string Name, double Revenue, int Margin, Color Color);
}
