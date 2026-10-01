using System.Globalization;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using static BonniePDF.Examples.Templates.FieldService.FieldKit;

namespace BonniePDF.Examples.Templates.FieldService;

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
[System.ComponentModel.Category("Field service")]
[System.ComponentModel.Description("A snagging register that flows across pages with a repeating header, with severity tiles, an items-by-trade chart, and close-out signatures. PDF/A-2a + PDF/UA-1.")]
public static class SiteInspectionReportExample
{
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
        new("S-001", "Level 2 — Room 2.04", "Mechanical", "Supply diffuser not aligned to ceiling grid; 18mm offset to grid line", "Minor", "Mechanical subcontractor", "15 Sep 2026", "P-014"),
        new("S-002", "Level 2 — Riser 2B", "Electrical", "Cable tray earth bonding not continuous across the coupling", "Critical", "Electrical subcontractor", "10 Sep 2026", "P-015, P-016"),
        new("S-003", "Level 2 — Corridor C2", "Fire stopping", "Service penetration through compartment wall not sealed; three cables passing", "Critical", "Fire stopping subcontractor", "10 Sep 2026", "P-017"),
        new("S-004", "Level 2 — Room 2.07", "Decoration", "Paint finish to reveal shows roller lap marks over approx. 1.2m", "Minor", "Finishes subcontractor", "19 Sep 2026", "P-018"),
        new("S-005", "Level 2 — Room 2.09", "Joinery", "Door leaf binding on frame at the head; closing force above specification", "Major", "Joinery subcontractor", "12 Sep 2026", "P-019"),
        new("S-006", "Level 2 — Plant room 2", "Mechanical", "Pipework insulation incomplete at valve set; approx. 600mm exposed", "Major", "Mechanical subcontractor", "12 Sep 2026", "P-020"),
        new("S-007", "Level 2 — Room 2.11", "Flooring", "Vinyl welded seam lifting at the threshold over approx. 300mm", "Major", "Flooring subcontractor", "12 Sep 2026", "P-021"),
        new("S-008", "Level 2 — Corridor C2", "Electrical", "Emergency luminaire EM-2-14 not illuminating on test key operation", "Critical", "Electrical subcontractor", "10 Sep 2026", "P-022"),
        new("S-009", "Level 2 — Room 2.02", "Ceilings", "Two ceiling tiles cut short; visible gap to perimeter trim", "Minor", "Ceilings subcontractor", "19 Sep 2026", "P-023"),
        new("S-010", "Level 2 — Room 2.04", "Electrical", "Socket outlet centre line 40mm below the datum set out on the drawing", "Minor", "Electrical subcontractor", "19 Sep 2026", "P-024"),
        new("S-011", "Level 2 — Stair core B", "Fire stopping", "Linear gap seal to head of wall missing at two positions", "Major", "Fire stopping subcontractor", "12 Sep 2026", "P-025, P-026"),
        new("S-012", "Level 2 — Room 2.09", "Mechanical", "Radiator bracket fixed into plasterboard without pattress reinforcement", "Major", "Mechanical subcontractor", "12 Sep 2026", "P-027"),
        new("S-013", "Level 2 — Corridor C2", "Signage", "Escape route sign fitted at 2.35m; above the specified mounting height", "Observation", "Principal contractor", "26 Sep 2026", "P-028"),
        new("S-014", "Level 2 — Room 2.11", "Decoration", "Sealant bead to skirting inconsistent along the north elevation", "Minor", "Finishes subcontractor", "19 Sep 2026", "P-029"),
        new("S-015", "Level 2 — Plant room 2", "Electrical", "Distribution board schedule not updated to reflect the final circuit list", "Major", "Electrical subcontractor", "15 Sep 2026", "P-030"),
    ];

    private static readonly string[] SeverityOrder = ["Critical", "Major", "Minor", "Observation"];

    private static Color SeverityColour(string severity) => severity switch
    {
        "Critical" => StatusCritical,
        "Major" => StatusHigh,
        "Minor" => StatusMedium,
        _ => StatusLow,
    };

    public static Document Generate()
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

        // ── Items by trade ────────────────────────────────────────────────────

        doc.Add(H2("Items by trade"));
        doc.Add(TradeChart(byTrade));

        // ── The register ──────────────────────────────────────────────────────

        doc.Add(H2("Defect register"));
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

        doc.Add(H2("Close-out"));
        doc.Add(Body(
            "Each owner is to close their items by the target date and submit evidence against the item reference. "
                + "Items not closed by the re-inspection date are escalated to the weekly progress meeting and "
                + "remain open on the next register.",
            color: Muted,
            size: 9f));

        doc.Add(SignatureRow(
        [
            SignatureSlot("Inspected by", Inspector, ShortDate(Inspected)),
            SignatureSlot("Received — principal contractor", "Sample Manager — Project Manager"),
            SignatureSlot("Client representative", "Sample Manager — Facilities Manager"),
        ]));

        doc.Add(Body(
            $"{Reference} · issued to the client, the principal contractor, and each named subcontractor.",
            color: Muted,
            marginTop: 10f,
            marginBottom: 0f,
            size: 8f));

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
}
