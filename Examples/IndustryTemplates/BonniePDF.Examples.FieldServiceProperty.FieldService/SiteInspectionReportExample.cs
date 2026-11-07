using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.FieldServiceProperty.FieldService;

/// <summary>
/// The register a construction, surveying, or property platform issues after a
/// site walk: every defect found, where it is, who owns it, how serious it is,
/// and when it must be closed. Main contractors issue these weekly; the same
/// document shape serves construction snagging, facilities condition surveys,
/// and lettings schedules of condition.
///
/// The interesting part for a document engine is the register itself. It is long,
/// it must carry its header onto every page it flows across, and the reader is
/// frequently working from a printed copy on site - so the pagination has to be
/// deterministic rather than whatever the viewport produced.
///
/// Evidence is referenced by plate number rather than embedded here. That is how
/// a large register is issued in practice (the photographs travel as a separate
/// annexe), and it keeps this example free of stock photography.
///
/// Sample project, sites, people, and defects throughout.
/// </summary>
[System.ComponentModel.DisplayName("Site inspection report")]
[System.ComponentModel.Category("Field Service & Property")]
[System.ComponentModel.Description(
    "A snagging register that flows across pages with a repeating header, with severity tiles, an items-by-trade chart, and close-out signatures. PDF/A-2a + PDF/UA-1.")]
public static class SiteInspectionReportExample
{
    public static IReadOnlyList<string> RequiredAssets { get; } =
    [
        "images/inspection/s-002-cable-tray.jpg",
        "images/inspection/s-003-penetration.jpg",
        "images/inspection/s-008-emergency-light.jpg",
    ];

    private const string Reference = "SIR-2026-0231";
    private const string Project = "Sample Project — Phase 2 fit-out";
    private const string Site = "Sample Site B — Building 4";
    private const string Client = "Sample Client Ltd";
    private const string Contractor = "Sample Contractor Ltd";
    private const string Inspector = "Sample Inspector — Site Manager";
    private const string Weather = "Dry, 14°C, light wind";

    private static readonly DateTime Inspected = new(2026, 9, 8);
    private static readonly DateTime Issued = new(2026, 9, 9);
    private static readonly DateTime Reinspection = new(2026, 9, 22);

    private sealed record Defect(
        string Reference,
        string Location,
        string Trade,
        string Description,
        string Severity,
        string Owner,
        string Due,
        string Evidence);

    private static readonly Defect[] Defects =
    [
        new("S-001", "Level 2 — Room 2.04", "Mechanical",
            "Supply diffuser not aligned to ceiling grid; 18mm offset to grid line", "Minor",
            "Mechanical subcontractor", "15 Sep 2026", "P-014"),
        new("S-002", "Level 2 — Riser 2B", "Electrical", "Cable tray earth bonding not continuous across the coupling",
            "Critical", "Electrical subcontractor", "10 Sep 2026", "P-015, P-016"),
        new("S-003", "Level 2 — Corridor C2", "Fire stopping",
            "Service penetration through compartment wall not sealed; three cables passing", "Critical",
            "Fire stopping subcontractor", "10 Sep 2026", "P-017"),
        new("S-004", "Level 2 — Room 2.07", "Decoration",
            "Paint finish to reveal shows roller lap marks over approx. 1.2m", "Minor", "Finishes subcontractor",
            "19 Sep 2026", "P-018"),
        new("S-005", "Level 2 — Room 2.09", "Joinery",
            "Door leaf binding on frame at the head; closing force above specification", "Major",
            "Joinery subcontractor", "12 Sep 2026", "P-019"),
        new("S-006", "Level 2 — Plant room 2", "Mechanical",
            "Pipework insulation incomplete at valve set; approx. 600mm exposed", "Major", "Mechanical subcontractor",
            "12 Sep 2026", "P-020"),
        new("S-007", "Level 2 — Room 2.11", "Flooring", "Vinyl welded seam lifting at the threshold over approx. 300mm",
            "Major", "Flooring subcontractor", "12 Sep 2026", "P-021"),
        new("S-008", "Level 2 — Corridor C2", "Electrical",
            "Emergency luminaire EM-2-14 not illuminating on test key operation", "Critical",
            "Electrical subcontractor", "10 Sep 2026", "P-022"),
        new("S-009", "Level 2 — Room 2.02", "Ceilings", "Two ceiling tiles cut short; visible gap to perimeter trim",
            "Minor", "Ceilings subcontractor", "19 Sep 2026", "P-023"),
        new("S-010", "Level 2 — Room 2.04", "Electrical",
            "Socket outlet centre line 40mm below the datum set out on the drawing", "Minor",
            "Electrical subcontractor", "19 Sep 2026", "P-024"),
        new("S-011", "Level 2 — Stair core B", "Fire stopping",
            "Linear gap seal to head of wall missing at two positions", "Major", "Fire stopping subcontractor",
            "12 Sep 2026", "P-025, P-026"),
        new("S-012", "Level 2 — Room 2.09", "Mechanical",
            "Radiator bracket fixed into plasterboard without pattress reinforcement", "Major",
            "Mechanical subcontractor", "12 Sep 2026", "P-027"),
        new("S-013", "Level 2 — Corridor C2", "Signage",
            "Escape route sign fitted at 2.35m; above the specified mounting height", "Observation",
            "Principal contractor", "26 Sep 2026", "P-028"),
        new("S-014", "Level 2 — Room 2.11", "Decoration",
            "Sealant bead to skirting inconsistent along the north elevation", "Minor", "Finishes subcontractor",
            "19 Sep 2026", "P-029"),
        new("S-015", "Level 2 — Plant room 2", "Electrical",
            "Distribution board schedule not updated to reflect the final circuit list", "Major",
            "Electrical subcontractor", "15 Sep 2026", "P-030"),
    ];

    private static readonly string[] SeverityOrder = ["Critical", "Major", "Minor", "Observation"];

    private static Color SeverityColour(string severity) => severity switch
    {
        "Critical" => StatusCritical,
        "Major" => StatusHigh,
        "Minor" => StatusMedium,
        _ => StatusLow,
    };

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate() => Generate(LoadDefaultAssets());

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        Dictionary<string, int> counts = CountBy(Defects, d => d.Severity);
        Dictionary<string, int> byTrade = CountBy(Defects, d => d.Trade);

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 44 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.HeaderTemplate = pageNumber =>
            pageNumber == 1 ? null : RunningBand(Reference, Project);
        doc.Settings.FooterTemplate = pageNumber => RunningBand(
            $"Issued {ShortDate(Issued)} · re-inspection {ShortDate(Reinspection)}",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Site inspection report {Reference} — {Project}";
        doc.Metadata.Author = Contractor;
        doc.Metadata.Subject =
            "Site inspection and snagging register with severity, ownership, target dates, and evidence references (sample data).";
        doc.Metadata.Keywords =
            "site inspection, snagging, defects, construction, surveying, register, pdf/ua, pdf/a";

        // ── Title block ───────────────────────────────────────────────────────

        doc.Add(AccentBar());
        doc.Add(Eyebrow("SITE INSPECTION REPORT · SNAGGING REGISTER"));
        doc.Add(H1("Level 2 fit-out — weekly inspection"));
        doc.Add(Body($"{Project} · {Site}", color: Muted, marginBottom: 14f));

        doc.Add(FieldRow(
            [
                Field("Report reference", Reference),
                Field("Inspected", ShortDate(Inspected)),
                Field("Issued", ShortDate(Issued)),
                Field("Re-inspection due", ShortDate(Reinspection)),
            ],
            marginBottom: 6f));

        doc.Add(FieldRow(
        [
            Field("Client", Client),
            Field("Principal contractor", Contractor),
            Field("Inspected by", Inspector),
            Field("Conditions", Weather),
        ]));

        // ── Severity tiles ────────────────────────────────────────────────────

        HorizontalStack tiles = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 10,
            Margin = new Thickness(0, 0, 0, 6),
        };
        tiles.Add(Tile(Defects.Length.ToString(CultureInfo.InvariantCulture), "Items raised", Accent), 1);
        foreach (string severity in SeverityOrder)
        {
            tiles.Add(
                Tile(
                    counts.GetValueOrDefault(severity).ToString(CultureInfo.InvariantCulture),
                    severity,
                    SeverityColour(severity)),
                1);
        }

        doc.Add(tiles);

        // ── Summary ───────────────────────────────────────────────────────────

        doc.Add(H2("Summary"));
        doc.Add(Body(
            $"{Defects.Length} items were raised during the level 2 walk. Three are Critical and relate to "
            + "life-safety systems — an incomplete fire-stopping penetration, a discontinuous earth bond, and "
            + "an emergency luminaire that failed on test. These are to be closed and evidenced before the "
            + "re-inspection; all other items follow the standard fourteen-day close-out. No item on this "
            + "register is accepted as complete until it has been re-inspected and signed off below."));

        doc.Add(Panel(
            "Critical items — close before re-inspection",
            [
                .. Defects
                    .Where(d => string.Equals(d.Severity, "Critical", StringComparison.Ordinal))
                    .Select(d => Body(
                        $"{d.Reference} · {d.Location} — {d.Description}",
                        marginBottom: 3f,
                        size: 9f)),
            ],
            StatusCritical));

        HorizontalStack evidenceStrip = new()
        {
            WidthDistribution = WidthDistribution.Equal,
            Spacing = 10,
            Margin = new Thickness(0, 0, 0, 6),
        };
        evidenceStrip.Add(EvidenceCard(
            Asset(assets, "images/inspection/s-002-cable-tray.jpg"),
            "S-002 · Riser cable tray earth strap",
            "Inspection photograph for S-002 showing cable tray coupling in Riser 2B with disconnected earth strap."));
        evidenceStrip.Add(EvidenceCard(
            Asset(assets, "images/inspection/s-003-penetration.jpg"),
            "S-003 · Unsealed fire penetration",
            "Inspection photograph for S-003 showing three cables passing through an unsealed compartment wall penetration."));
        evidenceStrip.Add(EvidenceCard(
            Asset(assets, "images/inspection/s-008-emergency-light.jpg"),
            "S-008 · Corridor emergency bulkhead",
            "Inspection photograph for S-008 showing corridor emergency bulkhead luminaire EM-2-14 unlit during key test."));
        doc.Add(evidenceStrip);

        // ── Items by trade ────────────────────────────────────────────────────

        doc.Add(H2("Items by trade"));
        doc.Add(TradeChart(byTrade));

        // ── The register ──────────────────────────────────────────────────────

        doc.Add(KeepWithNext(H2("Defect register"), 150f));
        doc.Add(Body(
            "Every item carries a reference, an owner, a target date, and an evidence plate number. "
            + "Photographic evidence is issued as a separate annexe under the same references.",
            color: Muted));

        Table register = new(0.7f, 1.6f, 1.1f, 3.1f, 1.35f, 1.6f, 0.95f, 0.85f)
        {
            CellPadding = 5,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        register.AddHeader(
            Th("Ref"),
            Th("Location"),
            Th("Trade"),
            Th("Observation"),
            Th("Severity"),
            Th("Owner"),
            Th("Due"),
            Th("Plate"));
        foreach (Defect defect in Defects)
        {
            register.AddRow(
                Td(defect.Reference, bold: true),
                Td(defect.Location),
                Td(defect.Trade, color: Muted),
                Td(defect.Description),
                StatusCell(defect.Severity, SeverityColour(defect.Severity)),
                Td(defect.Owner, color: Muted),
                Td(defect.Due),
                Td(defect.Evidence, color: Muted));
        }

        doc.Add(register);

        // ── Close-out and distribution ────────────────────────────────────────

        // The close-out wording stays on the same page as the sign-off it leads to.
        VerticalStack closeOut = new() { BreakAllowed = false };
        closeOut.Add(H2("Close-out"));
        closeOut.Add(Body(
            "Each owner is to close their items by the target date and submit evidence against the item reference. "
            + "Items not closed by the re-inspection date are escalated to the weekly progress meeting and "
            + "remain open on the next register.",
            color: Muted,
            size: 9f));

        closeOut.Add(SignatureRow(
        [
            SignatureSlot("Inspected by", Inspector, ShortDate(Inspected)),
            SignatureSlot("Received — principal contractor", "Sample Manager — Project Manager"),
            SignatureSlot("Client representative", "Sample Manager — Facilities Manager"),
        ]));

        closeOut.Add(Body(
            $"{Reference} · issued to the client, the principal contractor, and each named subcontractor.",
            color: Muted,
            marginTop: 10f,
            marginBottom: 0f,
            size: 8f));
        doc.Add(closeOut);

        return doc;
    }

    // ── Chart ─────────────────────────────────────────────────────────────────

    private static BarChart TradeChart(Dictionary<string, int> byTrade)
    {
        KeyValuePair<string, int>[] entries =
        [
            .. byTrade
                .OrderByDescending(e => e.Value)
                .ThenBy(e => e.Key, StringComparer.Ordinal),
        ];

        int highest = entries.Max(e => e.Value);

        ChartSeries series = new()
        {
            Color = Accent,
            DataPoints = [.. entries.Select(e => new ChartDataPoint { Label = e.Key, Value = e.Value })],
        };

        ChartData data = new() { Series = [series] };
        // The value axis is explicit. Left to its default it scales for currency
        // and runs to five figures, which turns a count of "4" into an invisible
        // bar.
        data.YAxis.MaxValue = highest;
        data.YAxis.StepCount = highest;
        data.YAxis.ShowGridLines = true;
        data.YAxis.Legend = "Open items";
        data.XAxis.Legend = "Trade";

        return new BarChart
        {
            Data = data,
            AltText = "Horizontal bar chart of open items by trade: "
                      + string.Join(", ", entries.Select(e => $"{e.Key} {e.Value}"))
                      + ".",
            Height = 170,
            StretchToFit = true,
            Orientation = ChartOrientation.Horizontal,
            ShowLegend = false,
        };
    }

    // ── Local helpers ─────────────────────────────────────────────────────────

    private static Dictionary<string, int> CountBy(IEnumerable<Defect> items, Func<Defect, string> key)
    {
        Dictionary<string, int> outMap = new(StringComparer.Ordinal);
        foreach (Defect item in items)
        {
            string k = key(item);
            outMap[k] = outMap.GetValueOrDefault(k) + 1;
        }

        return outMap;
    }

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

    /// <summary>
    /// Keeps a heading with the start of whatever it introduces: the heading moves to the next
    /// page unless <paramref name = "minHeight"/> points of room remain below it.
    /// </summary>
    private static EnsureSpace KeepWithNext(Heading heading, float minHeight = 120f)
    {
        return new EnsureSpace(minHeight, heading);
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

    private static VerticalStack EvidenceCard(byte[] imageData, string caption, string altText)
    {
        VerticalStack card = new() { Spacing = 4 };
        card.Add(new Container
        {
            Height = 76,
            CornerRadius = 4,
            ClipContent = true,
            Child = new Image
            {
                ImageData = imageData,
                Height = 76,
                FitMode = ImageFitMode.Cover,
                AltText = altText,
            },
        });
        card.Add(new TextBlock
        {
            Text = caption,
            FontName = StandardFonts.Helvetica,
            FontSize = 8,
            Color = Muted,
        });
        return card;
    }

    private static byte[] Asset(IReadOnlyDictionary<string, byte[]> assets, string path) =>
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
