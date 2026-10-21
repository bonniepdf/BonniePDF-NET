using BonniePDF;

namespace BonniePDF.Examples.Templates.StatutoryComplianceReport;

/// <summary>
/// A quarterly facilities compliance report: KPI tiles, a status donut and category chart, a key-findings
/// summary, an inspection register, a priority action plan, and a sign-off block across two pages.
/// </summary>
[System.ComponentModel.DisplayName("Statutory compliance report")]
[System.ComponentModel.Category("Compliance")]
[System.ComponentModel.Description(
    "A two-page statutory compliance and inspection report for a managed site, with KPI tiles, charts, an inspection register, an action plan and sign-off. PDF/A-2a + PDF/UA-1.")]
public static class StatutoryComplianceReportExample
{
    // ── Palette ──────────────────────────────────────────────────────────────
    private static readonly Color Teal = Color.FromHex("#0e7c86");
    private static readonly Color Navy = Color.FromHex("#0f2f3d");
    private static readonly Color Ink = Color.FromHex("#33424d");
    private static readonly Color Muted = Color.FromHex("#5b6b76");
    private static readonly Color Rule = Color.FromHex("#d5dde2");
    private static readonly Color Red = Color.FromHex("#b42318");
    private static readonly Color Amber = Color.FromHex("#b45309");
    private static readonly Color Green = Color.FromHex("#1a7f37");
    private static readonly Color SignLine = Color.FromHex("#8a99a3");
    private static readonly float[] RegisterColumns = [2.1f, 2.6f, 1f, 1f, 0.95f];
    private static readonly float[] ActionColumns = [0.55f, 3.2f, 0.8f, 1.6f, 1.1f];

    private enum Status { Compliant, DueSoon, Overdue }

    private enum Risk { High, Medium, Low }

    private static Color StatusColor(Status s) => s switch
    {
        Status.Compliant => Green,
        Status.DueSoon => Amber,
        _ => Red,
    };

    private static string StatusText(Status s) => s == Status.DueSoon ? "Due soon" : s.ToString();

    private static Color RiskColor(Risk r) => r switch
    {
        Risk.High => Red,
        Risk.Medium => Amber,
        _ => Green,
    };

    // ── Sample data ──────────────────────────────────────────────────────────
    private sealed record RegisterRow(string Asset, string Requirement, string Last, string Next, Status Status);

    private sealed record ActionRow(string Ref, string Action, Risk Risk, string Owner, string Target);

    private static readonly RegisterRow[] Register =
    [
        new("Fire alarm system", "BS 5839-1, six-monthly service", "12 Aug 2026", "12 Feb 2027", Status.Compliant),
        new("Fire extinguishers (86)", "BS 5306-3, annual service", "03 Mar 2026", "03 Mar 2027", Status.Compliant),
        new("Fire doors", "Quarterly check, Fire Safety Order 2005", "28 Jun 2026", "28 Sep 2026", Status.Overdue),
        new("Emergency lighting", "BS 5266-1, annual full-duration test", "18 Nov 2025", "18 Nov 2026", Status.DueSoon),
        new("Legionella risk assessment", "L8 ACoP, two-yearly review", "14 Jan 2025", "14 Jan 2027", Status.Compliant),
        new("Water temperature monitoring", "HSG274, monthly", "30 Sep 2026", "31 Oct 2026", Status.DueSoon),
        new("Calorifier clean and disinfection", "HSG274, annual", "22 Oct 2025", "22 Oct 2026", Status.DueSoon),
        new("Fixed electrical installation", "BS 7671 EICR, five-yearly", "09 Jun 2023", "09 Jun 2028", Status.Compliant),
        new("Portable appliance testing", "IET Code of Practice, annual", "15 Sep 2025", "15 Sep 2026", Status.Overdue),
        new("Gas appliances and pipework", "Gas Safety Regulations 1998, annual", "05 Sep 2026", "05 Sep 2027", Status.Compliant),
        new("Passenger lifts (3)", "LOLER 1998, six-monthly examination", "02 Jul 2026", "02 Jan 2027", Status.Compliant),
        new("Goods lift", "LOLER 1998, six-monthly examination", "19 Mar 2026", "19 Sep 2026", Status.Overdue),
        new("F-gas leak checks", "F-gas Regulations, annual", "10 Dec 2025", "10 Dec 2026", Status.Compliant),
        new("Asbestos management survey", "CAR 2012, annual re-inspection", "25 Aug 2026", "25 Aug 2027", Status.Compliant),
    ];

    private static readonly ActionRow[] Actions =
    [
        new("A-01", "Complete the quarterly fire door inspection and repair the two failed leaves on the Level 2 stairwell.", Risk.High, "Site FM lead", "30 Oct 2026"),
        new("A-02", "Complete overdue portable appliance testing for the 38 outstanding items.", Risk.Medium, "Electrical contractor", "27 Oct 2026"),
        new("A-03", "Book the LOLER thorough examination of the goods lift and keep it out of service until complete.", Risk.High, "Lift contractor", "23 Oct 2026"),
        new("A-04", "Complete the overdue roof fall-arrest anchor test.", Risk.High, "Roofing specialist", "06 Nov 2026"),
        new("A-05", "Upload the missing weekly sprinkler test records for the last three weeks.", Risk.Low, "Site engineer", "23 Oct 2026"),
        new("A-06", "Carry out the annual calorifier clean and disinfection (booked).", Risk.Medium, "Water hygiene contractor", "22 Oct 2026"),
        new("A-07", "Carry out the annual emergency lighting full-duration test (booked).", Risk.Medium, "Electrical contractor", "18 Nov 2026"),
    ];

    private static readonly (string Label, float Value)[] CategoryCompliance =
    [
        ("Fire safety", 96),
        ("Water hygiene", 88),
        ("Electrical", 93),
        ("Gas safety", 100),
        ("Lifts and lifting", 85),
        ("Emergency lighting", 92),
        ("F-gas and HVAC", 90),
        ("Asbestos", 100),
    ];

    // ── Document ─────────────────────────────────────────────────────────────
    public static Document Generate()
    {
        DocumentSettings settings = new(PdfSize.A4, 44)
        {
            PageColor = Color.White,
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            // Running header (skipped on the first page) and footer with page numbers.
            HeaderTemplate = pageNumber => pageNumber == 1
                ? null
                : BandRow(
                    "Northgate Retail Park  |  Compliance and Statutory Inspection Report  |  CR-2026-Q3-011",
                    "MERIDIAN FACILITIES SERVICES", Teal, 3, 1.4f),
            FooterTemplate = pageNumber => BandRow(
                "Confidential - prepared for Northgate Retail Park Ltd. Fictional sample data.",
                $"Page {pageNumber} of {TextElementBase.TotalPagesToken}", Muted, 3, 1),
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);

        Document doc = new(settings);
        doc.Metadata.Title = "Compliance and Statutory Inspection Report CR-2026-Q3-011";
        doc.Metadata.Author = "Meridian Facilities Services";
        doc.Metadata.Subject = "Quarterly statutory compliance and inspection report (fictional sample).";

        AddSummaryPage(doc);
        doc.AddPage();
        AddRegisterAndActions(doc);
        return doc;
    }

    // ── Page 1: summary ──────────────────────────────────────────────────────
    private static void AddSummaryPage(Document doc)
    {
        doc.Add(new Container { BackgroundColor = Teal, Height = 6 });
        doc.Add(Text("STATUTORY COMPLIANCE REPORT  |  QUARTER 3 2026", bold: true, size: 9, color: Teal, mt: 14, mb: 4));
        doc.Add(new Heading("Compliance and Statutory Inspection Report")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = Navy,
            Margin = new Thickness(0, 0, 0, 5),
        });
        doc.Add(Text("Northgate Retail Park, Fenwick Road, York", size: 10, color: Muted, mb: 14));

        HorizontalStack meta = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Margin = new Thickness(0, 0, 0, 16),
        };
        meta.Add(MetaItem("Report ref", "CR-2026-Q3-011"), 1);
        meta.Add(MetaItem("Reporting period", "1 Jul - 30 Sep 2026"), 1);
        meta.Add(MetaItem("Issued", "20 October 2026"), 1);
        meta.Add(MetaItem("Prepared by", "Meridian Facilities Services"), 1.3f);
        doc.Add(meta);

        doc.Add(H2("Executive summary", mb: 6));
        doc.Add(Text(
            "Of the 128 statutory assets and systems in scope, 117 (91.4%) are compliant, 6 fall due within the next 30 " +
            "days and 5 are overdue. The overdue items are concentrated in fire door checks, portable appliance testing, " +
            "goods lift examination, roof fall-arrest testing and sprinkler record-keeping. No incidents arose from any " +
            "of them. The goods lift is to be removed from service until its LOLER examination is complete (action A-03). " +
            "Seven actions remain open, each with an owner and a target date, and two are already booked.",
            align: TextAlignment.Justify, mb: 12));

        // KPI tiles
        HorizontalStack kpis = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 10,
            Margin = new Thickness(0, 0, 0, 16),
        };
        kpis.Add(Tile("91.4%", "Overall compliance", "#e8f4f5", Teal), 1);
        kpis.Add(Tile("128", "Assets and systems in scope", "#e8f4f5", Navy), 1);
        kpis.Add(Tile("5", "Overdue inspections", "#fdeceb", Red), 1);
        kpis.Add(Tile("7", "Open actions", "#fdf3e3", Amber), 1);
        doc.Add(kpis);

        // Charts
        HorizontalStack charts = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 12 };
        charts.Add(Card("Inspection status (128 assets)", StatusDonut()), 1);
        charts.Add(Card("Compliance by category (%)", CategoryBars()), 1.2f);
        doc.Add(charts);

        // Key findings
        VerticalStack findings = new() { Spacing = 8, Margin = new Thickness(0, 16, 0, 0) };
        findings.Add(H2("Key findings", mb: 2));
        findings.Add(Callout(
            "Needs attention: five inspections are overdue, three of them high risk (fire doors, goods lift and roof " +
            "fall-arrest anchors). All have a named owner and a date before 6 November.",
            "#fdeceb", Red));
        findings.Add(Callout(
            "Coming up: six items fall due within 30 days, led by water hygiene work (calorifier clean and monthly " +
            "temperature checks) and the annual emergency lighting test.",
            "#fdf3e3", Amber));
        findings.Add(Callout(
            "On track: gas safety and asbestos management are fully compliant, and fire alarm, passenger lift and " +
            "F-gas inspections are all in date.",
            "#e9f6ed", Green));
        doc.Add(findings);
    }

    // ── Page 2: register, actions, sign-off ──────────────────────────────────
    private static void AddRegisterAndActions(Document doc)
    {
        doc.Add(H2("Inspection register", mt: 4, mb: 4));
        doc.Add(Text(
            "Key statutory items, grouped by discipline. The full register of 128 assets is held in the CAFM system. " +
            "Status is stated in words as well as colour.",
            color: Muted, mb: 8));

        Table register = DataTable(RegisterColumns,
            "Asset or system", "Requirement", "Last done", "Next due", "Status");
        foreach (RegisterRow r in Register)
        {
            register.AddRow(
                Cell(r.Asset),
                Cell(r.Requirement),
                Cell(r.Last),
                Cell(r.Next),
                Cell(StatusText(r.Status), bold: true, color: StatusColor(r.Status)));
        }

        doc.Add(register);

        doc.Add(H2("Priority action plan", mb: 4));
        doc.Add(Text(
            "Seven open actions. A-01 to A-05 close the overdue items; A-06 and A-07 are booked ahead of their due dates.",
            color: Muted, mb: 8));

        Table actions = DataTable(ActionColumns,
            "Ref", "Action required", "Risk", "Owner", "Target date");
        foreach (ActionRow a in Actions)
        {
            actions.AddRow(
                Cell(a.Ref, bold: true),
                Cell(a.Action),
                Cell(a.Risk.ToString(), bold: true, color: RiskColor(a.Risk)),
                Cell(a.Owner),
                Cell(a.Target));
        }

        doc.Add(actions);

        // Keep the sign-off block together: move to a new page if < 100pt remain.
        VerticalStack signOff = new();
        signOff.Add(H2("Review and sign-off", mb: 8));
        signOff.Add(SignRow(
            ("PREPARED BY", "Compliance manager, Meridian"),
            ("REVIEWED BY", "Account director, Meridian"),
            ("ACCEPTED BY CLIENT", "Name: ____________________")));
        doc.Add(new EnsureSpace(100, signOff));
    }

    // ── Charts ───────────────────────────────────────────────────────────────
    private static VerticalStack StatusDonut()
    {
        PieChart chart = new(new ChartData
        {
            Series =
            [
                new ChartSeries
                {
                    Name = "Status",
                    DataPoints =
                    [
                        new ChartDataPoint { Label = "Compliant", Value = 117 },
                        new ChartDataPoint { Label = "Due within 30 days", Value = 6 },
                        new ChartDataPoint { Label = "Overdue", Value = 5 },
                    ],
                },
            ],
        })
        {
            AltText = "Donut chart of inspection status for 128 assets: 117 compliant, 6 due within 30 days, 5 overdue.",
            IsDonut = true,
            ShowLegend = false, // the legend below states counts in words, in matching colours
            ShowTotal = true,
            TotalLabel = "Assets",
            Width = 195,
            Height = 150,
            Colors = ["#1a7f37", "#b45309", "#b42318"],
        };

        VerticalStack stack = new();
        stack.Add(chart);
        stack.Add(Text("Compliant: 117", bold: true, size: 8.5f, color: Green, mt: 4));
        stack.Add(Text("Due within 30 days: 6", bold: true, size: 8.5f, color: Amber, mt: 2));
        stack.Add(Text("Overdue: 5", bold: true, size: 8.5f, color: Red, mt: 2));
        return stack;
    }

    private static BarChart CategoryBars()
    {
        ChartSeries series = new() { Name = "Compliance", Color = Teal };
        foreach ((string label, float value) in CategoryCompliance)
        {
            series.DataPoints.Add(new ChartDataPoint { Label = label, Value = (decimal)value });
        }

        return new BarChart(new ChartData
        {
            Series = [series],
            // In horizontal mode the value axis is still the Y axis.
            YAxis = new AxisOptions { MinValue = 0, MaxValue = 100, StepCount = 4, Suffix = "%" },
        })
        {
            AltText =
                "Bar chart of percentage compliance by category: fire safety 96, water hygiene 88, electrical 93, gas " +
                "safety 100, lifts and lifting 85, emergency lighting 92, F-gas and HVAC 90, asbestos management 100.",
            Orientation = ChartOrientation.Horizontal,
            BarColor = Teal,
            ShowValueLabels = true,
            Width = 245,
            Height = 180,
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static TextBlock Text(
        string value,
        bool bold = false,
        float size = 9.5f,
        Color? color = null,
        TextAlignment? align = null,
        float mt = 0,
        float mb = 0)
    {
        TextBlock block = new()
        {
            Text = value,
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            Color = color ?? Ink,
            Margin = new Thickness(0, mt, 0, mb),
        };
        if (align is not null)
        {
            block.Alignment = align.Value;
        }

        return block;
    }

    private static Heading H2(string value, float mt = 0, float mb = 0) => new(value)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 14,
        Color = Teal,
        Margin = new Thickness(0, mt, 0, mb),
    };

    private static VerticalStack MetaItem(string label, string value)
    {
        VerticalStack stack = new();
        stack.Add(Text(label.ToUpperInvariant(), bold: true, size: 7, color: Muted, mb: 2));
        stack.Add(Text(value));
        return stack;
    }

    private static Container Tile(string value, string label, string fillHex, Color accent)
    {
        VerticalStack stack = new();
        stack.Add(Text(value, bold: true, size: 20, color: accent, mb: 3));
        stack.Add(Text(label, size: 8.5f, color: Color.FromHex("#45556c")));
        return new Container
        {
            BackgroundColor = Color.FromHex(fillHex),
            CornerRadius = 8,
            Padding = new Thickness(13, 11, 13, 11),
            Child = stack,
        };
    }

    private static Container Card(string title, IElement content)
    {
        VerticalStack stack = new();
        stack.Add(Text(title, bold: true, size: 10.5f, color: Navy, mb: 6));
        stack.Add(content);
        return new Container
        {
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 8,
            Padding = new Thickness(12, 12, 12, 12),
            Child = stack,
        };
    }

    /// <summary>A tinted box with a coloured left rule.</summary>
    private static Container Callout(string body, string fillHex, Color accent) => new()
    {
        BackgroundColor = Color.FromHex(fillHex),
        Padding = new Thickness(12, 9, 12, 9),
        Border = Border.LeftOnly(3),
        BorderColors = BorderColors.LeftOnly(accent),
        Child = Text(body),
    };

    private static Table DataTable(float[] columns, params string[] headers)
    {
        Table table = new(columns)
        {
            CellPadding = 5,
            Margin = new Thickness(0, 0, 0, 10),
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Navy,
            HeaderTextColor = Color.White,
            RepeatHeadersOnNewPage = true,
        };
        table.AddHeader(headers.Select(h => (IElement)Text(h, bold: true, size: 8.5f, color: Color.White)).ToArray());
        return table;
    }

    private static TextBlock Cell(string value, bool bold = false, Color? color = null) =>
        Text(value, bold: bold, size: 8.5f, color: color);

    private static HorizontalStack SignRow(params (string Title, string NameLine)[] blocks)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 16 };
        foreach ((string title, string nameLine) in blocks)
        {
            VerticalStack stack = new();
            stack.Add(Text(title, bold: true, size: 8, color: Muted));
            stack.Add(Text(nameLine, mt: 2));
            stack.Add(new Container
            {
                Height = 30,
                Border = Border.BottomOnly(0.75f),
                BorderColors = BorderColors.BottomOnly(SignLine),
            });
            stack.Add(Text("Signature and date", size: 7.5f, color: Muted, mt: 2));
            row.Add(stack, 1);
        }

        return row;
    }

    private static HorizontalStack BandRow(string left, string right, Color rightColor, float lw, float rw)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.Add(Text(left, size: 7.5f, color: Muted), lw);
        row.Add(Text(right, bold: true, size: 7.5f, color: rightColor, align: TextAlignment.Right), rw);
        return row;
    }
}
