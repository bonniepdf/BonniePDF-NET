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
/// The controlled form that authorises high-risk work on a site: what is being
/// done, what could go wrong, what controls are in place, who tested the
/// atmosphere, who isolated what, and the three signatures that make it valid -
/// issue, acceptance, and cancellation.
///
/// A permit is the most form-like document in the field set and the least
/// forgiving: it is printed and displayed at the work location, it has a hard
/// validity window, and the signature panels must be present and complete or the
/// permit is void. Nothing about it may reflow unpredictably.
///
/// The counterpoint to the job sheet in the same family: where that document
/// records what happened, this one authorises what is about to.
///
/// Sample site, people, and references throughout; the controls listed are
/// illustrative and are not a substitute for a real risk assessment.
/// </summary>
[System.ComponentModel.DisplayName("Permit to work")]
[System.ComponentModel.Category("Field service")]
[System.ComponentModel.Description("A hot works permit: hazards and controls, an atmospheric test record, an isolation register, a tick-box precautions list, and the three signature panels that make it valid. PDF/A-2a + PDF/UA-1.")]
public static class PermitToWorkExample
{
    private const string Number = "PTW-2026-0451";
    private const string PermitType = "Hot works — grinding and welding";
    private const string Site = "Sample Site B — Building 4, Plant room 2";
    private const string Project = "Sample Project — Phase 2 fit-out";
    private const string Contractor = "Sample Contractor Ltd";
    private const string IssuedBy = "Sample Issuer — Authorised Person (AP-118)";
    private const string AcceptedBy = "Sample Engineer — Supervising Engineer (Eng. 4471)";
    private const string RiskAssessment = "RA-2026-0117 rev. C";
    private const string MethodStatement = "MS-2026-0088 rev. B";

    private static readonly DateTime ValidFrom = new(2026, 9, 10, 8, 0, 0);
    private static readonly DateTime ValidTo = new(2026, 9, 10, 16, 0, 0);

    private sealed record Hazard(string Reference, string Description, string Who, string Rating, string Controls);

    private static readonly Hazard[] Hazards =
    [
        new(
            "H-1",
            "Ignition of combustible material by sparks or hot slag",
            "Operatives, building occupants",
            "High",
            "Combustibles removed to 10m or protected with fire blankets. Two 6-litre extinguishers at the work position. Fire watch maintained throughout and for 60 minutes after work ceases."),
        new(
            "H-2",
            "Activation of the detection system causing an unnecessary evacuation",
            "Building occupants",
            "Medium",
            "Local detector heads isolated by the responsible person before work begins and reinstated on cancellation. Isolation recorded below and confirmed to the building manager."),
        new(
            "H-3",
            "Exposure to welding fume in a confined plant room",
            "Operatives",
            "High",
            "Local exhaust ventilation at the arc, doors held open on the corridor side, and RPE to the standard named in the method statement. Atmospheric test recorded below."),
        new(
            "H-4",
            "Contact with live services in the work area",
            "Operatives",
            "High",
            "Circuits isolated and locked off at DB-2B; keys retained by the supervising engineer. Isolation certificate ISO-2026-0288 attached."),
        new(
            "H-5",
            "Arc eye and burns to persons passing the work area",
            "Other trades",
            "Medium",
            "Welding screens erected on the corridor side; signage at both plant room doors for the permit duration."),
        new(
            "H-6",
            "Manual handling of the welding set on a stair core",
            "Operatives",
            "Low",
            "Two-person lift, set transported on a sack truck, route surveyed before the shift."),
    ];

    private static Color RatingColour(string rating) => rating switch
    {
        "High" => StatusCritical,
        "Medium" => StatusHigh,
        _ => StatusMedium,
    };

    private sealed record Isolation(string Reference, string System, string Point, string Method, string By);

    private static readonly Isolation[] Isolations =
    [
        new(
            "I-1",
            "Electrical — lighting and power, plant room 2",
            "DB-2B ways 4, 6, 8",
            "Locked off, personal padlock, caution notice applied",
            "Sample Engineer"),
        new(
            "I-2",
            "Fire detection — zone 14",
            "Panel FP-1, zone isolate",
            "Zone isolated at panel, building manager informed",
            "Sample Issuer"),
        new(
            "I-3",
            "Heating — LTHW flow and return to AHU-04",
            "Valves V-204 / V-205",
            "Valves closed, chained, and tagged",
            "Sample Engineer"),
    ];

    private static readonly string[] Precautions =
    [
        "Risk assessment and method statement read and understood by every operative on the permit.",
        "Work area inspected and combustible materials removed or protected before ignition sources are introduced.",
        "Fire extinguishers checked in date and positioned at the work location.",
        "Fire watch nominated, briefed, and present for the duration and for 60 minutes after work ceases.",
        "Atmospheric test completed and within limits before work starts, and repeated after any break longer than 30 minutes.",
        "Detection isolation confirmed with the building manager, and reinstatement confirmed on cancellation.",
        "Permit displayed at the work location for its full duration.",
    ];

    public static Document Generate()
    {
        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 44 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.HeaderTemplate = pageNumber => pageNumber == 1
            ? null
            : RunningBand($"Permit to work {Number} — {PermitType}", Site, StatusCritical);
        doc.Settings.FooterTemplate = pageNumber => RunningBand(
            "This permit is void unless all three panels are signed. Display at the work location.",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}",
            Muted);

        doc.Metadata.Title = $"Permit to work {Number} — {PermitType}";
        doc.Metadata.Author = Contractor;
        doc.Metadata.Subject =
            "Hot works permit to work: hazards and controls, atmospheric test, isolation record, precautions, and issue/acceptance/cancellation signatures (sample data).";
        doc.Metadata.Keywords = "permit to work, hot works, construction, safety, isolation, RAMS, pdf/ua, pdf/a";

        // ── Title block ───────────────────────────────────────────────────────

        doc.Add(AccentBar(StatusCritical));
        doc.Add(Eyebrow("PERMIT TO WORK · HOT WORKS · DISPLAY AT THE WORK LOCATION", StatusCritical));
        doc.Add(H1(PermitType));
        doc.Add(Body($"{Project} · {Site}", color: Muted, marginBottom: 12f));

        doc.Add(FieldRow(
            [
                Field("Permit number", Number),
                Field("Valid from", $"{ShortDate(ValidFrom)} {Clock(ValidFrom)}"),
                Field("Valid until", $"{ShortDate(ValidTo)} {Clock(ValidTo)}"),
                Field("Contractor", Contractor),
            ],
            marginBottom: 6f));

        doc.Add(FieldRow(
        [
            Field("Risk assessment", RiskAssessment),
            Field("Method statement", MethodStatement),
            Field("Issued by", IssuedBy),
            Field("Accepted by", AcceptedBy),
        ]));

        doc.Add(Panel(
            "Validity",
            [
                Body(
                    "This permit authorises only the work described below, only at the location named, and only "
                        + "between the times shown. It lapses automatically at the expiry time and must be cancelled "
                        + "and reissued if the work is not complete.",
                    marginBottom: 0f,
                    size: 9f),
            ],
            StatusCritical));

        // ── Description of work ───────────────────────────────────────────────

        doc.Add(H2("Description of work"));
        doc.Add(Body(
            "Cutting and removal of redundant LTHW pipework brackets in plant room 2, followed by MIG welding of "
                + "three replacement bracket plates to the existing steel frame. Angle grinder and portable welding "
                + "set in use. Work is at low level; no working at height or confined-space entry is authorised "
                + "under this permit."));

        // ── Hazards and controls ──────────────────────────────────────────────

        doc.Add(H2("Hazards and controls"));

        Table hazards = new(0.6f, 2.6f, 1.5f, 0.9f, 4.4f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        hazards.AddHeader(Th("Ref"), Th("Hazard"), Th("Who is at risk"), Th("Rating"), Th("Controls in place"));
        foreach (Hazard hazard in Hazards)
        {
            hazards.AddRow(
                Td(hazard.Reference, bold: true),
                Td(hazard.Description),
                Td(hazard.Who, color: Muted),
                StatusCell(hazard.Rating, RatingColour(hazard.Rating)),
                Td(hazard.Controls));
        }

        doc.Add(hazards);

        // ── Atmospheric test ──────────────────────────────────────────────────

        doc.Add(H2("Atmospheric test record"));

        Table gas = new(2.2f, 1.2f, 1.4f, 1.2f, 1.6f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        gas.AddHeader(Th("Measurement"), Th("Reading"), Th("Acceptable range"), Th("Result"), Th("Tested by"));
        gas.AddRow(Td("Oxygen"), Td("20.9 %"), Td("19.5 – 23.5 %", color: Muted), Pass(), Td("Sample Issuer"));
        gas.AddRow(Td("Flammable gas (LEL)"), Td("0 %"), Td("< 10 %", color: Muted), Pass(), Td("Sample Issuer"));
        gas.AddRow(Td("Carbon monoxide"), Td("0 ppm"), Td("< 30 ppm", color: Muted), Pass(), Td("Sample Issuer"));
        doc.Add(gas);

        doc.Add(Body(
            $"Instrument SN-4471-B, calibration due 14 Nov 2026. Tested at {Clock(ValidFrom)} on "
                + $"{ShortDate(ValidFrom)}. Re-test required after any break in work longer than 30 minutes.",
            color: Muted,
            size: 8.5f));

        // ── Isolations ────────────────────────────────────────────────────────

        doc.Add(H2("Isolation record"));

        Table isolations = new(0.6f, 3.2f, 1.8f, 3.4f, 1.2f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        isolations.AddHeader(Th("Ref"), Th("System"), Th("Isolation point"), Th("Method"), Th("Isolated by"));
        foreach (Isolation isolation in Isolations)
        {
            isolations.AddRow(
                Td(isolation.Reference, bold: true),
                Td(isolation.System),
                Td(isolation.Point),
                Td(isolation.Method, color: Muted),
                Td(isolation.By));
        }

        doc.Add(isolations);

        doc.Add(Body(
            "All isolations above are to be reinstated on cancellation of this permit, and the reinstatement "
                + "confirmed in the cancellation panel.",
            color: Muted,
            size: 8.5f));

        // ── Precautions checklist ─────────────────────────────────────────────

        doc.Add(H2("Precautions — to be confirmed before work starts"));
        VerticalStack checklist = new() { Spacing = 5, Margin = new Thickness(0, 0, 0, 10) };
        foreach (string precaution in Precautions)
        {
            checklist.Add(CheckItem(precaution));
        }

        doc.Add(checklist);

        // ── Signature panels ──────────────────────────────────────────────────

        // The three panels start a page of their own. A permit that splits its
        // signature panels across a page break is a permit someone can sign half
        // of, and this document's own footer says it is void unless all three are
        // complete - so the layout has to guarantee what the wording claims.
        doc.AddPage();
        doc.Add(H2("Authorisation"));
        doc.Add(Body(
            "All three panels must be completed. The permit is not valid until panels 1 and 2 are signed, and the "
                + "work area may not be handed back until panel 3 is signed.",
            color: Muted,
            size: 9f));

        doc.Add(H3("1. Issue"));
        doc.Add(Body(
            "I have inspected the work location, I am satisfied that the controls above are in place, and I "
                + "authorise the work described for the period stated.",
            color: Muted,
            marginBottom: 4f,
            size: 9f));
        doc.Add(SignatureRow(
        [
            SignatureSlot("Authorised person", IssuedBy, $"{ShortDate(ValidFrom)} at {Clock(ValidFrom)}"),
        ]));

        doc.Add(H3("2. Acceptance"));
        doc.Add(Body(
            "I have read this permit and the associated risk assessment and method statement. I accept "
                + "responsibility for the work being carried out in accordance with them, and I have briefed every "
                + "operative on the permit.",
            color: Muted,
            marginBottom: 4f,
            size: 9f));
        doc.Add(SignatureRow(
        [
            SignatureSlot("Accepting person", AcceptedBy, ShortDate(ValidFrom)),
            SignatureSlot("Operatives briefed by", "Name and signature"),
        ]));
        doc.Add(Body(
            "3 operatives briefed — see the attendance register for this permit.",
            color: Muted,
            marginTop: 6f,
            size: 8.5f));

        doc.Add(H3("3. Cancellation"));
        doc.Add(Body(
            "Work is complete or suspended, the area has been inspected, the fire watch has been maintained for "
                + "the required period, all isolations have been reinstated, and this permit is cancelled.",
            color: Muted,
            marginBottom: 4f,
            size: 9f));
        doc.Add(SignatureRow(
        [
            SignatureSlot("Accepting person", AcceptedBy),
            SignatureSlot("Authorised person", IssuedBy),
            SignatureSlot("Isolations reinstated by", "Name and signature"),
        ]));

        doc.Add(Body(
            $"{Number} · white copy displayed at the work location, yellow copy retained by the issuing office.",
            color: Muted,
            marginTop: 10f,
            marginBottom: 0f,
            size: 8f));

        return doc;
    }

    // ── Local helpers ─────────────────────────────────────────────────────────

    private static Container Pass() => StatusCell("Pass", StatusDone);

    private static HorizontalStack CheckItem(string text)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 8 };

        // An empty bordered box: the permit is signed off against a printed copy,
        // so the operative ticks these by hand.
        row.Add(
            new Container
            {
                Border = new Border(0.75f),
                BorderColors = new BorderColors(ColorPalette.Slate[400]),
                CornerRadius = 2,
                Width = 11,
                Height = 11,
            },
            0.35f);
        row.Add(
            new TextBlock { Text = text, FontName = StandardFonts.Helvetica, FontSize = 9, Color = Ink },
            12);
        return row;
    }

    private static string Clock(DateTime d) => d.ToString("HH:mm", CultureInfo.InvariantCulture);

    private static HorizontalStack RunningBand(string left, string right, Color color)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.Add(
            new TextBlock { Text = left, FontName = StandardFonts.Helvetica, FontSize = 7.5f, Color = color },
            3);
        row.Add(
            new TextBlock
            {
                Text = right,
                FontName = StandardFonts.Helvetica,
                FontSize = 7.5f,
                Color = Muted,
                Alignment = TextAlignment.Right,
            },
            1.5f);
        return row;
    }
}
