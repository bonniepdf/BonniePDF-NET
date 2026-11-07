using System.Globalization;
using System.Text;
using BonniePDF;

namespace BonniePDF.Examples.Sustainability;

/// <summary>
/// A greenhouse gas emissions statement: headline figures, a donut of emissions by scope with the total
/// in the centre, every source against last year, progress towards a 2030 target, the method behind the
/// numbers, and the figures attached as a CSV file inside the PDF.
/// </summary>
[System.ComponentModel.DisplayName("Greenhouse Gas Emissions Statement")]
[System.ComponentModel.Category("Sustainability")]
[System.ComponentModel.Description("A greenhouse gas emissions statement: headline figures, a donut of emissions by scope with the total in the centre, every source against last year, progress towards a 2030 target, the method behind the numbers, and the figures attached as a CSV file inside the PDF.")]
public static class CarbonEmissionsReportExample
{
    private static readonly Color Ink = ColorPalette.Stone[900];
    private static readonly Color Body = ColorPalette.Stone[700];
    private static readonly Color Mute = ColorPalette.Stone[600];
    private static readonly Color Green = ColorPalette.Green[800];

    /// <summary>Emissions by source, in tonnes of CO2 equivalent: scope, source, this year and last year.</summary>
    private static readonly (int Scope, string Source, int Now, int Before)[] Sources =
    [
        (1, "Fleet diesel", 3640, 4150),
        (1, "Natural gas for heating", 470, 520),
        (1, "Refrigerant leaks", 100, 110),
        (2, "Electricity (market-based)", 640, 1120),
        (3, "Purchased goods and services", 6200, 6650),
        (3, "Subcontracted transport", 3900, 4120),
        (3, "Business travel", 410, 380),
        (3, "Employee commuting", 1240, 1300),
        (3, "Waste", 120, 150),
        (3, "Use and disposal of sold products", 1700, 1880),
    ];

    /// <summary>Total emissions by reporting year, in tonnes of CO2 equivalent. 2019 is the baseline.</summary>
    private static readonly (string Year, int Value)[] History =
    [
        ("2019", 26100), ("2021", 23900), ("2022", 22700), ("2023", 21800), ("2024", 21050), ("2025", 20380), ("2026", 18420),
    ];

    /// <summary>The 2030 target: half the 2019 baseline.</summary>
    private const int Target = 13050;

    /// <summary>The plan to 2030: initiative, the scope it cuts, the saving it brings by 2030 in tonnes, and its status.</summary>
    private static readonly (string Initiative, int Scope, int Saving, string Status)[] Plan =
    [
        ("Electric vans for urban delivery (120 by 2028)", 1, 2400, "In progress"),
        ("Heat pumps at the four largest depots", 1, 420, "Planned"),
        ("Renewable power purchase agreement", 2, 640, "Contracted"),
        ("Supplier emissions programme, top 50 suppliers", 3, 3100, "In progress"),
        ("Move long-distance trunking to rail", 3, 900, "Planned"),
    ];

    /// <summary>The three scopes, in reporting order.</summary>
    private static readonly int[] Scopes = [1, 2, 3];

    /// <summary>One colour per scope, in slice order.</summary>
    private static readonly string[] ScopeColors = ["#166534", "#65a30d", "#94a3b8"];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Maintainability", "MA0051:Method is too long",
        Justification = "The example intentionally keeps generation inline.")]
    public static Document Generate()
    {
        Document doc = new()
        {
            Settings = new DocumentSettings
            {
                PageSize = PdfSize.A4,
                Margin = 42,
                Conformance = PdfAConformance.PdfA3a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(Body),
                FooterTemplate = PageTemplate.FromElements(@default: Footer()),
            },
            Metadata = new DocumentMetadata
            {
                Title = "Greenhouse gas emissions statement 2026",
                Author = "Sample Logistics Group plc",
                Subject = "Scope 1, 2 and 3 emissions for the year to 31 March 2026",
            },
        };

        int total = Sources.Sum(row => row.Now);
        int lastYear = Sources.Sum(row => row.Before);
        int[] byScope = [.. Scopes.Select(scope => Sources.Where(row => row.Scope == scope).Sum(row => row.Now))];

        // ── Title ──
        doc.Add(Tb("SAMPLE LOGISTICS GROUP PLC  ·  SUSTAINABILITY", 7.5f, Green, bold: true));
        doc.Add(new Heading("Greenhouse gas emissions statement")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = Ink,
            Margin = new Thickness(0, 3, 0, 2),
        });
        doc.Add(Tb("Year to 31 March 2026, in tonnes of carbon dioxide equivalent (tCO₂e)", 9.5f, Mute, bottom: 14));

        // ── Headline figures ──
        double progress = (double)(History[0].Value - total) / (History[0].Value - Target);
        HorizontalStack headlines = new() { WidthDistribution = WidthDistribution.Equal, Spacing = 8, Margin = new Thickness(0, 0, 0, 16) };
        headlines.Add(Kpi("Total emissions", Thousands(total), $"{Percent((double)total / lastYear - 1)} on last year"));
        headlines.Add(Kpi("Intensity", "41.2", "tCO₂e per £m revenue, −12%"));
        headlines.Add(Kpi("Renewable electricity", "86%", "of electricity used, up from 61%"));
        headlines.Add(Kpi("2030 target", $"{Round(progress * 100)}%", "of the way to halving 2019 emissions"));
        doc.Add(headlines);

        // ── Emissions by scope: donut beside a plain-language reading of each scope ──
        string[] scopeLabels = ["Scope 1: direct", "Scope 2: electricity", "Scope 3: value chain"];
        PieChart donut = new(new ChartData
        {
            Series =
            [
                new ChartSeries
                {
                    DataPoints = [.. scopeLabels.Select((label, i) => new ChartDataPoint { Label = label, Value = byScope[i], LabelFormat = "N0" })],
                },
            ],
        })
        {
            AltText = $"Donut chart of emissions by scope: scope 1 {Thousands(byScope[0])}, scope 2 {Thousands(byScope[1])}, scope 3 {Thousands(byScope[2])} tonnes. Total {Thousands(total)} tonnes.",
            Height = 220,
            StretchToFit = true,
            IsDonut = true,
            TotalLabel = "Total tCO₂e",
            TotalPosition = "center",
            TotalValueFormat = "N0",
            ShowLegend = false,
            Colors = ScopeColors,
            BackgroundColor = Color.White,
        };

        Table sources = new(2.6f, 1, 1, 0.8f)
        {
            CellPadding = 3.5f,
            Border = new Border(0, 0, 0, 0.5f),
            BorderColors = new BorderColors(ColorPalette.Stone[200]),
            HeaderBackgroundColor = Green,
            HeaderTextColor = Color.White,
            Margin = new Thickness(0, 0, 0, 10),
        };
        sources.AddHeader(Th("Source"), Th("2026", TextAlignment.Right), Th("2025", TextAlignment.Right), Th("Change", TextAlignment.Right));
        foreach (int scope in Scopes)
        {
            sources.AddRow(new TableRow
            {
                BackgroundColor = ColorPalette.Green[50],
                Cells =
                [
                    new TableCell { Content = Tb($"Scope {scope}", 8.5f, ColorPalette.Green[900], bold: true) },
                    new TableCell { Content = Tb(Thousands(byScope[scope - 1]), 8.5f, ColorPalette.Green[900], bold: true, align: TextAlignment.Right) },
                    new TableCell { Content = Tb(Thousands(Sources.Where(row => row.Scope == scope).Sum(row => row.Before)), 8.5f, ColorPalette.Green[900], align: TextAlignment.Right) },
                    new TableCell { Content = Tb("", 8.5f, Ink) },
                ],
            });
            foreach (var (_, source, now, before) in Sources.Where(row => row.Scope == scope))
            {
                sources.AddRow(
                    Tb(source, 8.5f, Body),
                    Tb(Thousands(now), 8.5f, Ink, align: TextAlignment.Right),
                    Tb(Thousands(before), 8.5f, Mute, align: TextAlignment.Right),
                    Tb(Percent((double)now / before - 1), 8.5f, now > before ? ColorPalette.Rose[700] : Green, align: TextAlignment.Right));
            }
        }
        sources.AddFooter(
            Tb("Total", 8.5f, Ink, bold: true),
            Tb(Thousands(total), 8.5f, Ink, bold: true, align: TextAlignment.Right),
            Tb(Thousands(lastYear), 8.5f, Ink, bold: true, align: TextAlignment.Right),
            Tb(Percent((double)total / lastYear - 1), 8.5f, Green, bold: true, align: TextAlignment.Right));

        // The donut beside a plain-language reading of each scope, then every source in full.
        VerticalStack reading = new() { Spacing = 8 };
        reading.Add(ScopeNote(Color.FromHex(ScopeColors[0]), "Scope 1: direct", "Fuel burnt in our own vehicles and buildings, and refrigerant that escapes."));
        reading.Add(ScopeNote(Color.FromHex(ScopeColors[1]), "Scope 2: electricity", "Electricity we buy. Renewable supply contracts cut it by 43% this year."));
        reading.Add(ScopeNote(Color.FromHex(ScopeColors[2]), "Scope 3: value chain", "Goods we buy, transport we subcontract, travel, commuting, waste and the products we sell."));
        HorizontalStack scopes = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 20, Margin = new Thickness(0, 0, 0, 14) };
        scopes.Add(donut, 1.4f);
        scopes.Add(reading, 1);
        doc.Add(Section("Emissions by scope"));
        doc.Add(scopes);
        doc.Add(Section("Emissions by source"));
        doc.Add(sources);

        // ── Page 2 ──
        doc.Add(new PageBreak());

        // ── Progress towards the 2030 target ──
        doc.Add(Section("Progress towards the 2030 target"));
        ChartSeries series = new() { Color = ColorPalette.Green[700] };
        foreach (var (year, value) in History)
            series.DataPoints.Add(new ChartDataPoint { Label = year, Value = value / 1000m, LabelFormat = "N1" });
        BarChart history = new(new ChartData { Series = [series], YAxis = new AxisOptions { MaxValue = 30, StepCount = 6 } })
        {
            Theme = "modern",
            References = [new ChartReference { Value = Target / 1000m, Color = ColorPalette.Green[900], Thickness = 1.5f, DashPattern = [5, 3] }],
            AltText = $"Bar chart of total emissions in thousands of tonnes: {string.Join(", ", History.Select(h => $"{h.Year} {(h.Value / 1000.0).ToString("F1", CultureInfo.InvariantCulture)}"))}. The 2030 target is {(Target / 1000.0).ToString("F2", CultureInfo.InvariantCulture)} thousand tonnes.",
            Height = 230,
            StretchToFit = true,
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 8,
            BackgroundColor = Color.White,
        };
        VerticalStack targetNote = new() { Spacing = 6 };
        targetNote.Add(Tb("Thousand tCO₂e, all scopes", 8, Mute));
        targetNote.Add(ProgressBar(progress));
        targetNote.Add(Tb($"From the 2019 baseline of {Thousands(History[0].Value)} tonnes to {Thousands(total)} this year: {Round(progress * 100)}% of the reduction needed to reach {Thousands(Target)} tonnes by 2030.", 8.5f, Body));
        targetNote.Add(Tb("The target is validated as consistent with limiting warming to 1.5 °C. Fleet electrification and supplier engagement deliver most of the remaining cut.", 8.5f, Body));
        HorizontalStack target = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 20, Margin = new Thickness(0, 0, 0, 16) };
        target.Add(history, 1.6f);
        target.Add(targetNote, 1);
        doc.Add(target);

        // ── The plan to 2030 ──
        doc.Add(Section("How we will close the gap"));
        Table plan = new(3, 0.7f, 1.1f, 1)
        {
            CellPadding = 4,
            Border = new Border(0, 0, 0, 0.5f),
            BorderColors = new BorderColors(ColorPalette.Stone[200]),
            HeaderBackgroundColor = Green,
            HeaderTextColor = Color.White,
            Margin = new Thickness(0, 0, 0, 6),
        };
        plan.AddHeader(Th("Initiative"), Th("Scope"), Th("Saving by 2030", TextAlignment.Right), Th("Status"));
        foreach (var (initiative, scope, saving, status) in Plan)
        {
            plan.AddRow(
                Tb(initiative, 8.5f, Ink),
                Tb(scope.ToString(CultureInfo.InvariantCulture), 8.5f, Body),
                Tb($"{Thousands(saving)} t", 8.5f, Ink, align: TextAlignment.Right),
                Tb(status, 8.5f, string.Equals(status, "Planned", StringComparison.Ordinal) ? Mute : Green, bold: !string.Equals(status, "Planned", StringComparison.Ordinal)));
        }
        int planned = Plan.Sum(row => row.Saving);
        plan.AddFooter(
            Tb("Total planned savings", 8.5f, Ink, bold: true),
            Tb("", 8.5f, Ink),
            Tb($"{Thousands(planned)} t", 8.5f, Ink, bold: true, align: TextAlignment.Right),
            Tb("", 8.5f, Ink));
        doc.Add(plan);
        doc.Add(Tb($"The gap to the 2030 target is {Thousands(total - Target)} tonnes. The plan saves {Thousands(planned)} tonnes, leaving headroom if growth adds emissions.", 8.5f, Body, bottom: 16));

        // ── Method, assurance and the attached data ──
        doc.Add(Section("How these figures were prepared"));
        doc.Add(Tb("Calculated under the GHG Protocol Corporate Standard on an operational control basis, using the UK government conversion factors for 2026. Scope 2 is reported market-based; location-based scope 2 was 2,980 tonnes. Scope 3 covers the six categories material to the group. An independent assurer gave limited assurance over scopes 1 and 2 under ISAE 3410.", 8.5f, Body, bottom: 10));
        HorizontalStack attached = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 10 };
        attached.Add(new Container
        {
            BackgroundColor = ColorPalette.Green[100],
            CornerRadius = 4,
            Padding = new Thickness(6, 3, 6, 3),
            Child = Tb("ATTACHED", 7.5f, ColorPalette.Green[900], bold: true),
        });
        attached.Add(Tb($"emissions-2026.csv: {Sources.Length} sources with scope, this year and last year. Open it from your PDF viewer's attachments panel.", 8.5f, Ink), 1);
        doc.Add(new Container
        {
            BackgroundColor = ColorPalette.Stone[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(ColorPalette.Stone[200]),
            CornerRadius = 6,
            Padding = new Thickness(12, 8, 12, 8),
            Child = attached,
        });

        // The figures behind the report travel inside it, so an analyst can reuse them without re-keying.
        doc.Attach(new FileAttachment
        {
            FileName = "emissions-2026.csv",
            Content = BuildCsv(),
            Relationship = AttachmentRelationship.Source,
            MediaType = "text/csv",
            Description = "Greenhouse gas emissions by source, year to 31 March 2026 and prior year",
        });
        return doc;
    }

    // Local helpers used by this document.

    /// <summary>Text in Helvetica or Helvetica Bold.</summary>
    private static TextBlock Tb(string text, float size, Color color, bool bold = false, TextAlignment? align = null, float bottom = 0)
    {
        TextBlock block = new(text)
        {
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            Color = color,
        };
        if (align.HasValue) block.Alignment = align.Value;
        if (bottom > 0) block.Margin = new Thickness(0, 0, 0, bottom);
        return block;
    }

    /// <summary>A section heading.</summary>
    private static Heading Section(string text) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 11,
        Color = Green,
        Margin = new Thickness(0, 0, 0, 6),
    };

    /// <summary>A table header cell.</summary>
    private static TextBlock Th(string text, TextAlignment? align = null) => Tb(text, 7.5f, Color.White, bold: true, align: align);

    /// <summary>A headline figure on a tinted tile.</summary>
    private static Container Kpi(string label, string value, string note)
    {
        VerticalStack stack = new() { Spacing = 2 };
        stack.Add(Tb(label.ToUpperInvariant(), 6.5f, ColorPalette.Green[900], bold: true));
        stack.Add(Tb(value, 18, Ink, bold: true));
        stack.Add(Tb(note, 7.5f, Mute));
        return new Container { BackgroundColor = ColorPalette.Green[50], CornerRadius = 6, Padding = new Thickness(10), Child = stack };
    }

    /// <summary>A scope's colour key, name and what it covers.</summary>
    private static HorizontalStack ScopeNote(Color color, string name, string text)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 8 };
        row.Add(new Container { BackgroundColor = color, CornerRadius = 2, Width = 10, Height = 10, Margin = new Thickness(0, 2, 0, 0) });
        VerticalStack words = new() { Spacing = 2 };
        words.Add(Tb(name, 9, Ink, bold: true));
        words.Add(Tb(text, 8.5f, Body));
        row.Add(words, 1);
        return row;
    }

    /// <summary>The share of the reduction achieved so far, as a bar with the baseline and target at its ends.</summary>
    private static CanvasElement ProgressBar(double share) => new((canvas, width, _) =>
    {
        canvas.FillRoundedRect(0, 4, width, 10, 5, ColorPalette.Stone[200]);
        canvas.FillRoundedRect(0, 4, (float)(width * share), 10, 5, ColorPalette.Green[700]);
        canvas.DrawText(0, 18, "2019 baseline", ColorPalette.Stone[600], 7, StandardFonts.Helvetica, TextAlignment.Left);
        const string label = "2030 target";
        float labelWidth = canvas.MeasureText(label, 7, StandardFonts.Helvetica).Width;
        canvas.DrawText(width - labelWidth, 18, label, ColorPalette.Stone[600], 7, StandardFonts.Helvetica, TextAlignment.Left);
    })
    {
        DrawWidth = 0,
        DrawHeight = 28,
        AltText = $"Progress bar: {Round(share * 100)}% of the way from the 2019 baseline to the 2030 target.",
    };

    /// <summary>The footer: the report and its page number.</summary>
    private static HorizontalStack Footer()
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.Add(Tb("Sample Logistics Group plc  ·  Greenhouse gas emissions statement 2026", 7, Mute), 3);
        row.Add(Tb($"Page {TextElementBase.CurrentPageToken} of {TextElementBase.TotalPagesToken}", 7, Mute, align: TextAlignment.Right), 1);
        return row;
    }

    /// <summary>Rounds half up, as a whole number.</summary>
    private static long Round(double value) => (long)Math.Floor(value + 0.5);

    /// <summary>Tonnes with a thousands separator: 18420 becomes 18,420.</summary>
    private static string Thousands(long value) => NumberFormatter.Format(value, "N0");

    /// <summary>A change as a signed percentage to one decimal place: −0.096 becomes −9.6%.</summary>
    private static string Percent(double change)
    {
        double rounded = Round(change * 1000) / 10.0;
        string sign = rounded > 0 ? "+" : rounded < 0 ? "−" : "";
        return $"{sign}{Math.Abs(rounded).ToString("F1", CultureInfo.InvariantCulture)}%";
    }

    /// <summary>The figures as CSV: scope, source, this year and last year.</summary>
    private static byte[] BuildCsv()
    {
        StringBuilder csv = new("scope,source,tco2e_2026,tco2e_2025\n");
        foreach (var (scope, source, now, before) in Sources)
            csv.Append(CultureInfo.InvariantCulture, $"{scope},\"{source}\",{now},{before}\n");
        return Encoding.UTF8.GetBytes(csv.ToString());
    }
}
