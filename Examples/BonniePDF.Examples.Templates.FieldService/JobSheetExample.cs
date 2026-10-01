using System.Globalization;
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
[System.ComponentModel.Category("Field service")]
[System.ComponentModel.Description("A field-service job sheet: labour and parts with computed totals, a register of outstanding defects by severity, and a ruled customer sign-off block. PDF/A-2a + PDF/UA-1.")]
public static class JobSheetExample
{
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

    public static Document Generate()
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

        doc.Add(H2("Fault reported"));
        doc.Add(Body(
            "Occupier reported intermittent noise and reduced airflow from the level 2 supply grilles. "
                + "Building management system showed the unit running but with supply air temperature drifting "
                + "above setpoint during the morning occupancy period."));

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
                BackgroundColor = ColorPalette.Indigo[50],
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
}
