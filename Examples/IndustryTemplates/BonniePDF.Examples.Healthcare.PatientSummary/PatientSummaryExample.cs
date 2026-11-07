using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.Healthcare.PatientSummary;

/// <summary>
/// A hospital patient summary on one A4 page: an identity banner with a wristband barcode, allergy
/// alerts, observations with six-reading trend lines and an early-warning score, medicines, a drawn INR
/// chart against its target range, problems, the care team and next steps.
/// Teal carries the trust and the clinical record; rose is kept for allergies and readings outside their
/// range, so red always means a warning. Entirely fictional.
/// </summary>
[System.ComponentModel.DisplayName("Patient Summary")]
[System.ComponentModel.Category("Healthcare")]
[System.ComponentModel.Description(
    "A hospital patient summary on one A4 page: an identity banner with a wristband barcode, allergy alerts, observations with trend lines and an early-warning score, medicines, a drawn INR chart against its target range, problems, the care team and next steps.")]
public static class PatientSummaryExample
{
    private static readonly Color Ink = ColorPalette.Slate[900];
    private static readonly Color Body = ColorPalette.Slate[700];
    private static readonly Color Mute = ColorPalette.Slate[500];
    private static readonly Color Trust = ColorPalette.Teal[800];
    private static readonly Color Alert = ColorPalette.Rose[700];

    /// <summary>Observations at 09:14: name, latest value, unit, NEWS2 points, and the last six readings, oldest first.</summary>
    private static readonly (string Name, string Value, string Unit, int Points, float[] Trend)[] Observations =
    [
        ("Respiration", "18", "per min", 0, [22, 20, 20, 18, 18, 18]),
        ("Oxygen", "95", "% on air", 1, [93, 94, 94, 95, 96, 95]),
        ("BP", "128/82", "mmHg", 0, [142, 136, 132, 130, 126, 128]),
        ("Pulse", "92", "bpm", 1, [104, 98, 96, 94, 90, 92]),
        ("Temperature", "37.1", "°C", 0, [37.8f, 37.6f, 37.4f, 37.2f, 37.0f, 37.1f]),
    ];

    /// <summary>Current medicines: name, dose, route, when, and what it is for.</summary>
    private static readonly (string Name, string Dose, string Route, string When, string Why)[] Medicines =
    [
        ("Warfarin", "5 mg", "Oral", "Evening", "Anticoagulation"),
        ("Bisoprolol", "2.5 mg", "Oral", "Morning", "Heart rate control"),
        ("Ramipril", "5 mg", "Oral", "Morning", "Blood pressure"),
        ("Atorvastatin", "40 mg", "Oral", "Night", "Cholesterol"),
        ("Lansoprazole", "15 mg", "Oral", "Breakfast", "Stomach protection"),
    ];

    /// <summary>INR results: the date of each weekly check and its value. Target range 2.0 to 3.0.</summary>
    private static readonly (string Date, float Value)[] Inr =
    [
        ("30 Apr", 1.8f), ("7 May", 2.1f), ("14 May", 2.6f), ("21 May", 3.4f),
        ("28 May", 2.9f), ("4 Jun", 2.4f), ("11 Jun", 2.2f), ("18 Jun", 2.5f),
    ];

    /// <summary>Problems: the condition, the year it was diagnosed, and whether it is still active.</summary>
    private static readonly (string Condition, string Since, bool Active)[] Problems =
    [
        ("Atrial fibrillation", "2021", true),
        ("Hypertension", "2018", true),
        ("Hypercholesterolaemia", "2019", true),
        ("Appendicectomy", "1998", false),
    ];

    /// <summary>Blood tests taken on 17 June: test, result, units, reference range, and whether the result is outside it.</summary>
    private static readonly (string Test, string Result, string Units, string Range, bool Outside)[] Bloods =
    [
        ("Haemoglobin", "128", "g/L", "115 to 165", false),
        ("Sodium", "139", "mmol/L", "133 to 146", false),
        ("Potassium", "5.4", "mmol/L", "3.5 to 5.3", true),
        ("Creatinine", "84", "µmol/L", "45 to 84", false),
        ("eGFR", "68", "mL/min/1.73m²", "above 60", false),
        ("C-reactive protein", "12", "mg/L", "below 5", true),
    ];

    /// <summary>The care team and what happens next.</summary>
    private static readonly (string Role, string Detail)[] NextSteps =
    [
        ("Consultant", "Dr R. Okafor, cardiology"),
        ("Anticoagulation nurse", "S. Price, extension 4471"),
        ("Next INR check", "25 June 2026, anticoagulation clinic"),
        ("Echocardiogram", "Booked for 2 July 2026"),
    ];

    /// <summary>Asset paths required by this example, relative to the shared <c>Examples/assets</c> directory.</summary>
    public static IReadOnlyList<string> RequiredAssets { get; } =
    [
        "images/svg/sample-healthcare.svg",
    ];

    /// <summary>Builds the document using the default shared assets. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate() => Generate(LoadDefaultAssets());

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Maintainability", "MA0051:Method is too long",
        Justification = "The example intentionally keeps generation inline.")]
    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);
        string markSvg = System.Text.Encoding.UTF8.GetString(GetAsset(assets, "images/svg/sample-healthcare.svg"));

        Document doc = new()
        {
            Settings = new DocumentSettings
            {
                PageSize = PdfSize.A4,
                Margin = 0,
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(8.5f).WithColor(Body),
            },
            Metadata = new DocumentMetadata
            {
                Title = "Patient summary: Priya Shah",
                Author = "Sample Healthcare Trust",
                Subject = "Inpatient summary: identity, alerts, observations, medicines, results and plan",
            },
        };

        // ── Trust masthead ──
        VerticalStack trust = new() { Spacing = 2 };
        trust.Add(Tb("Sample Healthcare Trust", 12, Color.White, bold: true));
        trust.Add(Tb("Cardiology, ward 4B", 8, ColorPalette.Teal[100]));
        VerticalStack printed = new() { Spacing = 2 };
        printed.Add(new Heading("Patient summary")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            Color = Color.White,
            Alignment = TextAlignment.Right,
            Margin = new Thickness(0),
        });
        printed.Add(Tb("Printed 18 June 2026, 09:30", 8, ColorPalette.Teal[100], align: TextAlignment.Right));
        HorizontalStack masthead = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 10 };
        masthead.Add(new Svg { SvgData = markSvg, Width = 24, Height = 24, IsDecorative = true });
        masthead.Add(trust, 1);
        masthead.Add(printed, 1);
        doc.Add(new Container { BackgroundColor = Trust, Padding = new Thickness(32, 16, 32, 16), Child = masthead });

        // ── Identity banner ──
        VerticalStack name = new() { Spacing = 3 };
        name.Add(Tb("SHAH, Priya (Ms)", 16, Ink, bold: true));
        name.Add(Tb("Born 14 March 1979 (47)  ·  Female  ·  Patient ID 999 482 9310", 9, Body));
        HorizontalStack identity = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 16 };
        identity.Add(name, 1);
        identity.Add(new Container
        {
            HorizontalAlignment = HorizontalAlignment.End,
            Child = new Barcode
            {
                Value = "H0482931",
                Symbology = BarcodeSymbology.Code128,
                Height = 24,
                ModuleWidth = 0.9f,
                ForegroundColor = Ink,
                BackgroundColor = Color.White,
                TextLabelFontSize = 6.5f,
                AltText = "Hospital number barcode: H0482931",
            },
        });
        Grid facts = new(1, 1, 1, 1) { Border = Border.None, CellPadding = 0, Margin = new Thickness(0, 10, 0, 0) };
        facts.AddRow(
            Fact("Hospital number", "H0482931"),
            Fact("Address", "12 Sample Road, Sample Town ST4 2QR"),
            Fact("GP", "Dr A. Sample, Sample Road Surgery"),
            Fact("Next of kin", "Arjun Shah (husband), 07700 900483"));
        VerticalStack banner = new();
        banner.Add(identity);
        banner.Add(facts);
        doc.Add(BodyInset(new Container
        {
            Border = new Border(4, 0, 0, 0),
            BorderColors = new BorderColors(Trust),
            Padding = new Thickness(12, 0, 0, 0),
            Margin = new Thickness(0, 16, 0, 12),
            Child = banner,
        }));

        // ── Alerts ──
        VerticalStack alertLines = new() { Spacing = 2 };
        alertLines.Add(Tb("Penicillin: anaphylaxis (2014). Avoid all beta-lactam antibiotics.", 8.5f, ColorPalette.Rose[900], bold: true));
        alertLines.Add(Tb("Latex: contact urticaria, use nitrile gloves.  ·  Anticoagulated: warfarin, INR target 2.0 to 3.0.", 8.5f, ColorPalette.Rose[900]));
        HorizontalStack alerts = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 10 };
        alerts.Add(new Container
        {
            BackgroundColor = Alert,
            CornerRadius = 3,
            Padding = new Thickness(6, 3, 6, 3),
            Child = Tb("ALERTS", 7.5f, Color.White, bold: true),
        });
        alerts.Add(alertLines, 1);
        doc.Add(BodyInset(new Container
        {
            BackgroundColor = ColorPalette.Rose[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(ColorPalette.Rose[200]),
            CornerRadius = 6,
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 14),
            Child = alerts,
        }));

        // ── Observations, with the early warning score ──
        doc.Add(BodyInset(Section("Observations  ·  18 June 2026, 09:14")));
        HorizontalStack observations = new() { WidthDistribution = WidthDistribution.Equal, Spacing = 8, Margin = new Thickness(0, 0, 0, 14) };
        foreach (var (label, value, unit, points, trend) in Observations) observations.Add(Observation(label, value, unit, points, trend));
        int score = Observations.Sum(row => row.Points);
        VerticalStack scoreText = new() { Spacing = 2 };
        scoreText.Add(Tb("NEWS2 SCORE", 6.5f, ColorPalette.Teal[100], bold: true));
        scoreText.Add(Tb(score.ToString(CultureInfo.InvariantCulture), 22, Color.White, bold: true));
        scoreText.Add(Tb("Low risk", 8, ColorPalette.Teal[50], bold: true));
        scoreText.Add(Tb("Observe every 4 to 6 hours", 7, ColorPalette.Teal[50]));
        observations.Add(new Container { BackgroundColor = Trust, CornerRadius = 6, Padding = new Thickness(8), Child = scoreText });
        doc.Add(BodyInset(observations));

        // ── Medicines beside the INR chart ──
        Table medicines = new(1.2f, 0.7f, 0.6f, 1.1f, 2)
        {
            CellPadding = 4,
            Border = new Border(0, 0, 0, 0.5f),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            HeaderBackgroundColor = ColorPalette.Teal[50],
            HeaderTextColor = ColorPalette.Teal[900],
        };
        medicines.AddHeader(Th("Medicine"), Th("Dose"), Th("Route"), Th("When"), Th("For"));
        foreach (var (medicine, dose, route, when, why) in Medicines)
            medicines.AddRow(Tb(medicine, 8.5f, Ink, bold: true), Tb(dose, 8.5f, Body), Tb(route, 8.5f, Body), Tb(when, 8.5f, Body), Tb(why, 8.5f, Mute));

        VerticalStack chart = new() { Spacing = 4 };
        chart.Add(new LineChart(new ChartData
        {
            Series = [new ChartSeries
            {
                Color = ColorPalette.Teal[700],
                DataPoints = Inr.Select(reading => new ChartDataPoint { Label = reading.Date, Value = (decimal)reading.Value }).ToList(),
            }],
            YAxis = new AxisOptions { MinValue = 1, MaxValue = 4, StepCount = 3 },
        })
        {
            Height = 112,
            StretchToFit = true,
            Theme = "modern",
            ShowValueLabels = false,
            References = [new ChartReference { Value = 2, EndValue = 3, Color = ColorPalette.Teal[50] }],
            AltText = "INR results for the last eight weeks against the target range of 2.0 to 3.0: 1.8, 2.1, 2.6, 3.4, 2.9, 2.4, 2.2 and 2.5. Two results fell outside the range, on 30 April and 21 May.",
        });
        chart.Add(Tb("Weekly INR. Two readings fell outside the shaded target range.", 7, Mute));
        VerticalStack medicinesColumn = new();
        medicinesColumn.Add(Section("Current medicines"));
        medicinesColumn.Add(medicines);
        VerticalStack chartColumn = new();
        chartColumn.Add(Section("INR results"));
        chartColumn.Add(chart);
        HorizontalStack clinical = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 18, Margin = new Thickness(0, 0, 0, 14) };
        clinical.Add(medicinesColumn, 1.45f);
        clinical.Add(chartColumn, 1);
        doc.Add(BodyInset(clinical));

        // ── Problems beside the care team and next steps ──
        Table problems = new(2.2f, 0.8f, 1)
        {
            CellPadding = 4,
            Border = new Border(0, 0, 0, 0.5f),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
        };
        problems.AddHeader(Th("Condition"), Th("Since"), Th("Status"));
        foreach (var (condition, since, active) in Problems)
        {
            problems.AddRow(new TableRow
            {
                Cells =
                [
                    new TableCell { Content = Tb(condition, 8.5f, active ? Ink : Mute, bold: active) },
                    new TableCell { Content = Tb(since, 8.5f, Mute) },
                    new TableCell { Content = Pill(active ? "Active" : "Past", active) },
                ],
            });
        }

        Grid plan = new(1, 1.8f)
        {
            Border = new Border(0, 0, 0, 0.5f),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            CellPadding = 4,
        };
        foreach (var (role, detail) in NextSteps) plan.AddRow(Tb(role, 8.5f, Mute), Tb(detail, 8.5f, Ink));

        VerticalStack problemsColumn = new();
        problemsColumn.Add(Section("Problems"));
        problemsColumn.Add(problems);
        VerticalStack planColumn = new();
        planColumn.Add(Section("Care team and next steps"));
        planColumn.Add(plan);
        HorizontalStack lower = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 18, Margin = new Thickness(0, 0, 0, 14) };
        lower.Add(problemsColumn, 1);
        lower.Add(planColumn, 1.2f);
        doc.Add(BodyInset(lower));

        // ── Recent blood tests, with results outside their range flagged ──
        Table bloods = new(1.6f, 0.8f, 1.2f, 1.2f, 0.8f)
        {
            CellPadding = 4,
            Border = new Border(0, 0, 0, 0.5f),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            HeaderBackgroundColor = ColorPalette.Teal[50],
            HeaderTextColor = ColorPalette.Teal[900],
            Margin = new Thickness(0, 0, 0, 16),
        };
        bloods.AddHeader(Th("Test"), Th("Result"), Th("Units"), Th("Reference range"), Th("Flag"));
        foreach (var (test, result, units, range, outside) in Bloods)
        {
            TableRow row = new()
            {
                Cells =
                [
                    new TableCell { Content = Tb(test, 8.5f, Ink) },
                    new TableCell { Content = Tb(result, 8.5f, outside ? Alert : Ink, bold: true) },
                    new TableCell { Content = Tb(units, 8.5f, outside ? Body : Mute) },
                    new TableCell { Content = Tb(range, 8.5f, outside ? Body : Mute) },
                    new TableCell { Content = Tb(outside ? "High" : "Normal", 8.5f, outside ? Alert : ColorPalette.Teal[800], bold: outside) },
                ],
            };
            if (outside) row.BackgroundColor = ColorPalette.Rose[50];
            bloods.AddRow(row);
        }
        VerticalStack bloodsColumn = new();
        bloodsColumn.Add(Section("Blood tests  ·  17 June 2026"));
        bloodsColumn.Add(bloods);
        doc.Add(BodyInset(bloodsColumn));

        doc.Add(BodyInset(new Container
        {
            Border = new Border(0, 0.5f, 0, 0),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            Padding = new Thickness(0, 6, 0, 0),
            Child = Tb("Confidential: this summary contains personal health information. Check identity against the wristband before acting on it.  ·  Page 1 of 1", 7, Mute),
        }));
        return doc;
    }

    // Local helpers used by this document.

    /// <summary>Text in Helvetica or Helvetica Bold.</summary>
    private static TextBlock Tb(string text, float size, Color color, bool bold = false, TextAlignment? align = null)
    {
        TextBlock block = new(text)
        {
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            Color = color,
        };
        if (align.HasValue) block.Alignment = align.Value;
        return block;
    }

    /// <summary>The page body, inset from the full-bleed masthead.</summary>
    private static Container BodyInset(IElement child) => new() { Padding = new Thickness(32, 0, 32, 0), Child = child };

    /// <summary>A section heading.</summary>
    private static Heading Section(string text) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 10,
        Color = Trust,
        Margin = new Thickness(0, 0, 0, 6),
    };

    /// <summary>A table header cell.</summary>
    private static TextBlock Th(string text) => Tb(text, 7.5f, ColorPalette.Teal[900], bold: true);

    /// <summary>A labelled fact in the identity banner.</summary>
    private static VerticalStack Fact(string label, string value)
    {
        VerticalStack stack = new() { Spacing = 2, Margin = new Thickness(0, 0, 10, 0) };
        stack.Add(Tb(label.ToUpperInvariant(), 6.5f, Mute, bold: true));
        stack.Add(Tb(value, 8.5f, Ink));
        return stack;
    }

    /// <summary>A status pill: teal for an active problem, grey for a past one.</summary>
    private static Container Pill(string text, bool active) => new()
    {
        FitContent = true,
        BackgroundColor = active ? ColorPalette.Teal[50] : ColorPalette.Slate[100],
        CornerRadius = 6,
        Padding = new Thickness(6, 1.5f, 6, 1.5f),
        Child = Tb(text, 7, active ? ColorPalette.Teal[800] : ColorPalette.Slate[600], bold: true),
    };

    /// <summary>One observation: its latest value, a six-reading trend line, and its NEWS2 points.</summary>
    private static Container Observation(string name, string value, string unit, int points, float[] trend)
    {
        bool flagged = points > 0;
        decimal low = (decimal)trend.Min();
        decimal high = (decimal)trend.Max();
        VerticalStack stack = new() { Spacing = 2 };
        stack.Add(Tb(name.ToUpperInvariant(), 6.5f, Mute, bold: true));
        stack.Add(Tb(value, 16, Ink, bold: true));
        stack.Add(Tb(unit, 7, Mute));
        stack.Add(new LineChart(new ChartData
        {
            Series = [new ChartSeries
            {
                Color = ColorPalette.Teal[600],
                DataPoints = trend.Select(value => new ChartDataPoint { Value = (decimal)value }).ToList(),
            }],
            YAxis = new AxisOptions { MinValue = low, MaxValue = high > low ? high : low + 1 },
        })
        {
            Sparkline = true,
            BackgroundColor = flagged ? ColorPalette.Amber[50] : ColorPalette.Slate[50],
            Height = 16,
            StretchToFit = true,
            DefaultLineWidth = 1,
            AltText = $"{name}, last six readings: {string.Join(", ", trend.Select(v => v.ToString(CultureInfo.InvariantCulture)))}",
        });
        stack.Add(Tb($"NEWS2 {points}", 7, flagged ? ColorPalette.Amber[800] : ColorPalette.Teal[800], bold: true));
        return new Container
        {
            BackgroundColor = flagged ? ColorPalette.Amber[50] : ColorPalette.Slate[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(flagged ? ColorPalette.Amber[300] : ColorPalette.Slate[200]),
            CornerRadius = 6,
            Padding = new Thickness(8),
            Child = stack,
        };
    }

    private static Dictionary<string, byte[]> LoadDefaultAssets()
    {
        string assetsDir = FindAssetsDir();
        Dictionary<string, byte[]> dict = new(StringComparer.Ordinal);
        foreach (string key in RequiredAssets)
        {
            string fullPath = Path.Combine(assetsDir, key.Replace('/', Path.DirectorySeparatorChar));
            dict[key] = File.ReadAllBytes(fullPath);
        }

        return dict;
    }

    private static byte[] GetAsset(IReadOnlyDictionary<string, byte[]> assets, string key)
    {
        if (assets.TryGetValue(key, out byte[]? bytes))
            return bytes;
        string fullPath = Path.Combine(FindAssetsDir(), key.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllBytes(fullPath);
    }

    private static string FindAssetsDir()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            string candidate = Path.Combine(dir, "Examples", "assets");
            if (Directory.Exists(candidate))
                return candidate;
            string direct = Path.Combine(dir, "assets");
            if (Directory.Exists(direct))
                return direct;
            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate Examples/assets directory.");
    }
}
