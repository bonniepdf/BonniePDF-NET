using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.FieldServiceProperty.FieldService;

/// <summary>
/// The document a field-service, facilities-management, or maintenance platform
/// produces at the end of every visit: what was wrong, what the engineer did,
/// how long it took, what parts went in, what is still outstanding, and the
/// customer's signature accepting the work.
///
/// It is the most-generated document in that whole sector, and it is a poor fit
/// for HTML-to-PDF: it must paginate predictably (an engineer prints it or emails
/// it straight from a van), it carries a ruled signature block that has to land
/// on the page rather than wherever the flow leaves it, and it is increasingly
/// required to be accessible because the customer copy goes to a building
/// occupier.
///
/// All organisations, people, sites, and asset numbers are samples.
/// </summary>
[System.ComponentModel.DisplayName("Job sheet")]
[System.ComponentModel.Category("Field Service & Property")]
[System.ComponentModel.Description(
    "A field-service job sheet: labour and parts with computed totals, a register of outstanding defects by severity, and a ruled customer sign-off block. PDF/A-2a + PDF/UA-1.")]
public static class JobSheetExample
{
    public static IReadOnlyList<string> RequiredAssets { get; } =
    [
        "images/field-service/ahu.jpg",
    ];

    private const string JobNumber = "JOB-2026-04817";
    private const string OrderReference = "PO 55-2026-0912";
    private const string Priority = "P2 — Same working day";
    private const string Engineer = "Sample Engineer (Eng. 4471)";
    private const string Client = "Sample Client Ltd";
    private const string Site = "Sample Site B — Building 4, Level 2";
    private const string Asset = "AHU-04 — Air handling unit, plant room 2";
    private const string AssetTag = "SC-B4-AHU-04";
    private const string Contract = "Planned maintenance — schedule 3";

    private static readonly DateTime Raised = new(2026, 9, 9, 7, 42, 0);
    private static readonly DateTime Attended = new(2026, 9, 9, 11, 5, 0);
    private static readonly DateTime Completed = new(2026, 9, 9, 14, 20, 0);

    private sealed record LabourLine(string Description, string Operative, decimal Hours, decimal Rate);

    private static readonly LabourLine[] Labour =
    [
        new("Attendance and fault diagnosis", "Sample Engineer", 1.5m, 68m),
        new("Bearing replacement — supply fan", "Sample Engineer", 2.25m, 68m),
        new("Belt tension and alignment check", "Sample Engineer", 0.5m, 68m),
        new("Run-up test and handover", "Sample Engineer", 0.75m, 68m),
    ];

    private sealed record PartLine(string Code, string Description, int Quantity, decimal UnitPrice, bool Fitted);

    private static readonly PartLine[] Parts =
    [
        new("BRG-6204-2RS", "Sealed deep-groove bearing, 20mm bore", 2, 18.40m, true),
        new("BLT-SPZ-1287", "Drive belt, SPZ section, 1287mm", 1, 22.90m, true),
        new("FLT-G4-592", "Panel filter G4, 592 × 592 × 48", 4, 11.25m, true),
        new("SEAL-AHU-12", "Access door seal, 12mm closed cell", 1, 9.60m, false),
    ];

    private sealed record Outstanding(string Reference, string Description, string Severity, string Action);

    private static readonly Outstanding[] OutstandingItems =
    [
        new(
            "D-01",
            "Corrosion to condensate tray, approx. 15% of surface",
            "Medium",
            "Quote raised for tray replacement — see QT-2026-1180"),
        new(
            "D-02",
            "Access door seal perished; part carried but door frame out of true",
            "Low",
            "Return visit required with fabrication support"),
        new(
            "D-03",
            "No isolator label at local switch",
            "High",
            "Labelling to be completed before next statutory inspection"),
    ];

    private static Color SeverityColour(string severity) => severity switch
    {
        "Critical" => StatusCritical,
        "High" => StatusHigh,
        "Medium" => StatusMedium,
        _ => StatusLow,
    };

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate() => Generate(LoadDefaultAssets());

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        decimal labourTotal = Labour.Sum(l => l.Hours * l.Rate);
        decimal partsTotal = Parts.Where(p => p.Fitted).Sum(p => p.Quantity * p.UnitPrice);
        decimal onSite = (decimal)(Completed - Attended).TotalHours;

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 46 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        // The running header repeats the job number, because these get printed,
        // separated, and filed a page at a time. Page one carries the full title
        // block instead, so it is suppressed there.
        doc.Settings.HeaderTemplate = pageNumber => pageNumber == 1
            ? null
            : RunningBand($"Job sheet {JobNumber}", $"{Client} — {Site}");
        doc.Settings.FooterTemplate = pageNumber => RunningBand(
            "Customer copy — retain for your records",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Job sheet {JobNumber} — {Asset}";
        doc.Metadata.Author = "Sample Contractor Ltd";
        doc.Metadata.Subject =
            "Field service job sheet with labour, parts, outstanding defects, and customer sign-off (sample data).";
        doc.Metadata.Keywords =
            "job sheet, work order, field service, facilities management, maintenance, pdf/ua, pdf/a";

        // ── Title block ───────────────────────────────────────────────────────

        doc.Add(AccentBar());
        doc.Add(Eyebrow("JOB SHEET · COMPLETED VISIT"));
        doc.Add(H1("Reactive maintenance — air handling unit"));
        doc.Add(Body($"{Client} · {Site}", color: Muted, marginBottom: 2f));
        doc.Add(Body("Issued by Sample Contractor Ltd", color: Muted, marginBottom: 14f));

        doc.Add(FieldRow(
            [
                Field("Job number", JobNumber),
                Field("Order reference", OrderReference),
                Field("Priority", Priority),
                Field("Contract", Contract),
            ],
            marginBottom: 6f));

        doc.Add(FieldRow(
        [
            Field("Asset", AssetTag),
            Field("Raised", $"{ShortDate(Raised)} {Clock(Raised)}"),
            Field("Attended", $"{ShortDate(Attended)} {Clock(Attended)}"),
            Field("Completed", $"{ShortDate(Completed)} {Clock(Completed)}"),
        ]));

        // ── Summary tiles ─────────────────────────────────────────────────────

        HorizontalStack tiles = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 10,
            Margin = new Thickness(0, 0, 0, 4),
        };
        tiles.Add(Tile(Hours(onSite), "Time on site", Accent), 1);
        tiles.Add(Tile(Hours(Labour.Sum(l => l.Hours)), "Labour booked", Accent), 1);
        tiles.Add(Tile(
            Parts.Count(p => p.Fitted).ToString(CultureInfo.InvariantCulture),
            "Parts fitted",
            Accent), 1);
        tiles.Add(Tile(
            OutstandingItems.Length.ToString(CultureInfo.InvariantCulture),
            "Outstanding items",
            StatusHigh), 1);
        doc.Add(tiles);

        // ── Reported fault and work done ──────────────────────────────────────

        VerticalStack faultText = new();
        faultText.Add(H2("Fault reported"));
        faultText.Add(Body(
            "Occupier reported intermittent noise and reduced airflow from the level 2 supply grilles. "
            + "Building management system showed the unit running but with supply air temperature drifting "
            + "above setpoint during the morning occupancy period."));

        HorizontalStack faultSummary = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 14,
        };
        faultSummary.Add(faultText, 3.4f);
        faultSummary.Add(
            new Container
            {
                Width = 110,
                Height = 82,
                CornerRadius = 6,
                ClipContent = true,
                Margin = new Thickness(0, 14, 0, 0),
                Child = new Image
                {
                    ImageData = GetAsset(assets, "images/field-service/ahu.jpg"),
                    Width = 110,
                    Height = 82,
                    FitMode = ImageFitMode.Cover,
                    AltText = "Rooftop air handling unit with access panel open showing belt-driven fan.",
                },
            },
            1f);
        doc.Add(faultSummary);

        doc.Add(H2("Work carried out"));
        doc.Add(Body(
            "Unit isolated and locked off. Supply fan assembly stripped and inspected: drive-end bearing found "
            + "dry and scored, with corresponding wear to the drive belt. Both bearings replaced as a pair and "
            + "the belt renewed. Filters replaced at the same visit as they were within one month of their "
            + "scheduled change. Alignment and tension checked against manufacturer figures, unit run up for "
            + "thirty minutes and supply air temperature confirmed stable at setpoint. Controls returned to "
            + "automatic."));

        doc.Add(Panel("Safety and isolation",
        [
            Body("Permit to work PTW-2026-0442 raised and closed on completion.", marginBottom: 3f, size: 9f),
            Body(
                "Lock-off applied at local isolator; keys retained by attending engineer throughout.",
                marginBottom: 3f,
                size: 9f),
            Body("Work area left clean, guarding refitted, and access doors secured.", marginBottom: 0f, size: 9f),
        ]));

        // ── Labour ────────────────────────────────────────────────────────────

        doc.Add(H2("Labour"));

        Table labour = new(3.4f, 1.4f, 0.8f, 0.9f, 1f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        labour.AddHeader(Th("Description"), Th("Operative"), Th("Hours"), Th("Rate"), Th("Amount"));
        foreach (LabourLine line in Labour)
        {
            labour.AddRow(
                Td(line.Description),
                Td(line.Operative, color: Muted),
                Td(Hours(line.Hours)),
                Td(Gbp(line.Rate)),
                Td(Gbp(line.Hours * line.Rate), bold: true));
        }

        doc.Add(labour);

        // ── Parts ─────────────────────────────────────────────────────────────

        Table parts = new(1.3f, 3.1f, 0.7f, 1f, 1f, 0.9f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        parts.AddHeader(Th("Code"), Th("Description"), Th("Qty"), Th("Unit"), Th("Amount"), Th("Status"));
        foreach (PartLine part in Parts)
        {
            parts.AddRow(
                Td(part.Code),
                Td(part.Description),
                Td(part.Quantity.ToString(CultureInfo.InvariantCulture)),
                Td(Gbp(part.UnitPrice)),
                Td(part.Fitted ? Gbp(part.Quantity * part.UnitPrice) : "—"),
                StatusCell(
                    part.Fitted ? "Fitted" : "Carried, not fitted",
                    part.Fitted ? StatusDone : StatusLow));
        }

        TextBlock partsNote = Body(
            "Parts carried but not fitted are returned to stock and are not charged. "
            + "Totals below exclude VAT and are provisional until invoiced.",
            color: Muted,
            marginBottom: 6f,
            size: 8.5f);

        VerticalStack partsStack = new() { BreakAllowed = false };
        partsStack.Add(H2("Parts and materials"));
        partsStack.Add(parts);
        partsStack.Add(partsNote);
        doc.Add(partsStack);

        // ── Totals ────────────────────────────────────────────────────────────

        HorizontalStack totals = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Margin = new Thickness(0, 2, 0, 4),
        };
        totals.Add(new Container(), 2);

        VerticalStack totalsBody = new() { Spacing = 5 };
        totalsBody.Add(TotalLine("Labour", Gbp(labourTotal)));
        totalsBody.Add(TotalLine("Parts fitted", Gbp(partsTotal)));
        totalsBody.Add(TotalLine("Total excluding VAT", Gbp(labourTotal + partsTotal), strong: true));

        totals.Add(
            new Container
            {
                BackgroundColor = ColorPalette.Cyan[50],
                CornerRadius = 6,
                Padding = new Thickness(12),
                Child = totalsBody,
            },
            1.4f);
        doc.Add(totals);

        // ── Outstanding work ──────────────────────────────────────────────────

        TextBlock defectsIntro = Body(
            "The following were identified during the visit but fall outside the scope of this job. "
            + "Each carries a reference so it can be quoted, scheduled, or accepted as a risk.",
            color: Muted);

        Table defects = new(0.7f, 3.4f, 1f, 3f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        defects.AddHeader(Th("Ref"), Th("Observation"), Th("Severity"), Th("Recommended action"));
        foreach (Outstanding item in OutstandingItems)
        {
            defects.AddRow(
                Td(item.Reference, bold: true),
                Td(item.Description),
                StatusCell(item.Severity, SeverityColour(item.Severity)),
                Td(item.Action, color: Muted));
        }

        VerticalStack defectsStack = new() { BreakAllowed = false };
        defectsStack.Add(H2("Outstanding items and recommendations"));
        defectsStack.Add(defectsIntro);
        defectsStack.Add(defects);
        doc.Add(defectsStack);

        doc.Add(Panel(
            "Next planned visit",
            [
                Body("Scheduled quarterly service — due 09 Dec 2026, schedule 3.", marginBottom: 3f, size: 9f),
                Body("Item D-03 to be closed before the next statutory inspection.", marginBottom: 0f, size: 9f),
            ],
            StatusHigh));

        // ── Sign-off ──────────────────────────────────────────────────────────

        doc.Add(H2("Acceptance and sign-off"));
        doc.Add(Body(
            "By signing, the customer representative confirms that the work described above was carried out, "
            + "that the work area was left in a safe condition, and that the outstanding items were explained. "
            + "Signing does not constitute acceptance of charges, which are confirmed on invoice.",
            color: Muted,
            size: 9f));

        doc.Add(SignatureRow(
        [
            SignatureSlot("Attending engineer", Engineer, ShortDate(Completed)),
            SignatureSlot("Customer representative", "Sample Manager — Facilities Manager"),
        ]));

        doc.Add(Body(
            $"Job {JobNumber} · {AssetTag} · Retain this sheet with the asset history file.",
            color: Muted,
            marginTop: 10f,
            marginBottom: 0f,
            size: 8f));

        return doc;
    }

    // ── Local helpers ─────────────────────────────────────────────────────────

    private static string Clock(DateTime d) => d.ToString("HH:mm", CultureInfo.InvariantCulture);

    private static HorizontalStack RunningBand(string left, string right)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.Add(
            new TextBlock { Text = left, FontName = StandardFonts.Helvetica, FontSize = 7.5f, Color = Muted },
            2);
        row.Add(
            new TextBlock
            {
                Text = right,
                FontName = StandardFonts.Helvetica,
                FontSize = 7.5f,
                Color = Muted,
                Alignment = TextAlignment.Right,
            },
            2);
        return row;
    }

    private static Container Tile(string value, string label, Color accent)
    {
        VerticalStack stack = new();
        stack.Add(new TextBlock
        {
            Text = value,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            Color = accent,
            Margin = new Thickness(0, 0, 0, 2),
        });
        stack.Add(new TextBlock
        {
            Text = label,
            FontName = StandardFonts.Helvetica,
            FontSize = 8,
            Color = Muted,
        });

        return new Container
        {
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 6,
            Padding = new Thickness(11, 10),
            Child = stack,
        };
    }

    private static HorizontalStack TotalLine(string label, string value, bool strong = false)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.Add(
            new TextBlock
            {
                Text = label,
                FontName = strong ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
                FontSize = strong ? 10 : 9,
                Color = strong ? Ink : Muted,
            },
            2);
        row.Add(
            new TextBlock
            {
                Text = value,
                FontName = StandardFonts.HelveticaBold,
                FontSize = strong ? 11 : 9,
                Color = strong ? Accent : Ink,
                Alignment = TextAlignment.Right,
            },
            1.2f);
        return row;
    }

    // Local helpers and sample data for this document.

    // Local helpers and sample data for this document.
    // ── Palette ───────────────────────────────────────────────────────────────
    // Slate ink with a cyan accent, per the shared example design tokens.
    private static readonly Color Ink = ColorPalette.Slate[700];

    /// <summary>600, not 400: 400 never clears 4.5:1 as body text on white.</summary>
    private static readonly Color Muted = ColorPalette.Slate[600];

    private static readonly Color HeadingColor = ColorPalette.Slate[800];

    private static readonly Color Accent = ColorPalette.Cyan[700];

    private static readonly Color Rule = ColorPalette.Slate[200];

    private static readonly Color PanelFill = ColorPalette.Slate[50];

    /// <summary>
    /// Status ramp shared by all of these documents.
    ///
    /// Deliberately not the traffic-light set a designer would reach for first:
    /// the amber is 700 and the emerald 700 because 500/600 fall below 4.5:1 as
    /// small text on white, and every one of these appears as a table cell.
    /// </summary>
    private static readonly Color StatusCritical = ColorPalette.Rose[700];

    private static readonly Color StatusHigh = ColorPalette.Amber[700];

    private static readonly Color StatusMedium = ColorPalette.Blue[700];

    private static readonly Color StatusLow = ColorPalette.Slate[600];

    private static readonly Color StatusDone = ColorPalette.Emerald[700];

    // ── Text ──────────────────────────────────────────────────────────────────
    /// <summary>Small caps-style label above a title - the document's own classification.</summary>
    private static TextBlock Eyebrow(string text, Color? color = null) => new()
    {
        Text = text,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 8.5f,
        Color = color ?? Accent,
        Margin = new Thickness(0, 0, 0, 4),
    };

    private static Heading H1(string text) => new(text)
    {
        Level = 1,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 21,
        Color = HeadingColor,
        Margin = new Thickness(0, 0, 0, 6),
    };

    private static Heading H2(string text) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 13,
        Color = Accent,
        Margin = new Thickness(0, 14, 0, 8),
    };

    private static TextBlock Body(string text, Color? color = null, float marginTop = 0f, float marginBottom = 8f, float size = 9.5f) => new()
    {
        Text = text,
        FontName = StandardFonts.Helvetica,
        FontSize = size,
        Color = color ?? Ink,
        Margin = new Thickness(0, marginTop, 0, marginBottom),
    };

    /// <summary>Table header cell - white on the accent header band.</summary>
    private static TextBlock Th(string text) => new()
    {
        Text = text,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 8.5f,
        Color = Color.White,
    };

    /// <summary>Table body cell.</summary>
    private static TextBlock Td(string text, Color? color = null, bool bold = false, float size = 8.5f) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = size,
        Color = color ?? Ink,
    };

    // ── Structure ─────────────────────────────────────────────────────────────
    /// <summary>A full-width accent rule; the visual top of every one of these documents.</summary>
    private static Container AccentBar(Color? color = null, float height = 3f) => new()
    {
        BackgroundColor = color ?? Accent,
        Height = height,
        Margin = new Thickness(0, 0, 0, 14),
    };

    /// <summary>One label/value pair in the metadata grid.</summary>
    private static VerticalStack Field(string label, string value)
    {
        VerticalStack stack = new();
        stack.Add(new TextBlock { Text = label.ToUpperInvariant(), FontName = StandardFonts.HelveticaBold, FontSize = 6.5f, Color = Muted, Margin = new Thickness(0, 0, 0, 2), });
        stack.Add(new TextBlock { Text = value, FontName = StandardFonts.Helvetica, FontSize = 9.5f, Color = Ink, });
        return stack;
    }

    /// <summary>
    /// A row of label/value fields, evenly weighted.
    ///
    /// Field documents are read by someone standing up holding a clipboard or a
    /// phone, so the reference data goes in a scannable grid at the top rather
    /// than in prose.
    /// </summary>
    private static HorizontalStack FieldRow(IEnumerable<IElement> fields, float marginBottom = 14f)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 14,
            Margin = new Thickness(0, 2, 0, marginBottom),
        };
        foreach (IElement f in fields)
        {
            row.Add(f, 1);
        }

        return row;
    }

    /// <summary>A bordered panel with a heading - used for notes, scope, and declarations.</summary>
    private static Container Panel(string title, IEnumerable<IElement> children, Color? accent = null)
    {
        VerticalStack stack = new()
        {
            Spacing = 4
        };
        stack.Add(new TextBlock { Text = title, FontName = StandardFonts.HelveticaBold, FontSize = 9.5f, Color = accent ?? Accent, Margin = new Thickness(0, 0, 0, 4), });
        foreach (IElement child in children)
        {
            stack.Add(child);
        }

        return new Container
        {
            BackgroundColor = PanelFill,
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 6,
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 12),
            Child = stack,
        };
    }

    /// <summary>A coloured status/severity pill for a table cell.</summary>
    private static Container StatusCell(string text, Color color) => new()
    {
        Child = new TextBlock
        {
            Text = text,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 8,
            Color = color,
        },
    };

    // ── Signature blocks ──────────────────────────────────────────────────────
    /// <summary>
    /// A single ruled signature slot: name, role, a rule to sign on, and a date.
    ///
    /// This is drawn, not cryptographic. A drawn signature block is what these
    /// documents carry in the field - the customer signs the engineer's tablet or
    /// the printed sheet. Digital signing is a separate, separately-licensed
    /// concern and nothing here should be read as claiming it.
    /// </summary>
    private static VerticalStack SignatureSlot(string role, string name, string? dated = null)
    {
        VerticalStack stack = new()
        {
            Spacing = 0
        };
        stack.Add(new TextBlock { Text = role.ToUpperInvariant(), FontName = StandardFonts.HelveticaBold, FontSize = 6.5f, Color = Muted, Margin = new Thickness(0, 0, 0, 18), });
        // The rule people actually sign on.
        stack.Add(new Container { BackgroundColor = ColorPalette.Slate[400], Height = 0.75f, Margin = new Thickness(0, 0, 0, 5), });
        stack.Add(new TextBlock { Text = name, FontName = StandardFonts.Helvetica, FontSize = 9, Color = Ink, });
        stack.Add(new TextBlock { Text = dated is not null ? $"Date: {dated}" : "Date:", FontName = StandardFonts.Helvetica, FontSize = 8, Color = Muted, Margin = new Thickness(0, 3, 0, 0), });
        return stack;
    }

    /// <summary>A row of signature slots, evenly weighted.</summary>
    private static HorizontalStack SignatureRow(IEnumerable<IElement> slots)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 22,
            Margin = new Thickness(0, 10, 0, 4),
            // A signature row is never split between pages.
            BreakAllowed = false,
        };
        foreach (IElement s in slots)
        {
            row.Add(s, 1);
        }

        return row;
    }

    // ── Formatting ────────────────────────────────────────────────────────────
    private static readonly CultureInfo GbCulture = CultureInfo.GetCultureInfo("en-GB");

    private static string ShortDate(DateTime d) => d.ToString("dd MMM yyyy", GbCulture);

    /// <summary>Sterling, to two places.</summary>
    private static string Gbp(decimal value) => value.ToString("C2", GbCulture);

    /// <summary>Decimal hours, as engineers record them: 1.5, 0.75.</summary>
    private static string Hours(decimal value) => $"{value.ToString("F2", CultureInfo.InvariantCulture)} h";

    private static byte[] GetAsset(IReadOnlyDictionary<string, byte[]> assets, string path) =>
        assets.TryGetValue(path, out byte[]? bytes)
            ? bytes
            : throw new InvalidOperationException($"Missing required asset: {path}");

    private static Dictionary<string, byte[]> LoadDefaultAssets()
    {
        string assetsDir = FindAssetsDirectory();
        Dictionary<string, byte[]> result = new(StringComparer.Ordinal);
        foreach (string key in RequiredAssets)
        {
            string fullPath = Path.Combine(assetsDir, key.Replace('/', Path.DirectorySeparatorChar));
            result[key] = File.ReadAllBytes(fullPath);
        }

        return result;
    }

    private static string FindAssetsDirectory()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, "Examples", "assets");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            string localCandidate = Path.Combine(dir, "assets");
            if (Directory.Exists(localCandidate))
            {
                return localCandidate;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
    }
}
