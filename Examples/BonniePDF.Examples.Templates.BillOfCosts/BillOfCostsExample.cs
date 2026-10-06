using System.Globalization;
using BonniePDF;
using static BonniePDF.Examples.Shared.TradeKit;

namespace BonniePDF.Examples.Templates.BillOfCosts;

/// <summary>
/// What a legal-costs or practice-management platform produces at the end of a
/// matter: every unit of chargeable time, who did it and at what grade, the
/// disbursements paid out, and the summary that tells a paying party what is
/// being claimed and why.
///
/// It is the long-table document of the legal sector. Hundreds of time entries
/// flow across pages with a repeating header, grouped by phase, each group
/// carrying its own subtotal - and the phase subtotals must add to the grand
/// total or the bill is not one anybody will pay. Everything below is computed
/// from the entries rather than written beside them, for exactly that reason.
///
/// Sample matter, parties, fee earners, and figures throughout. Nothing here is
/// legal advice, a costs template, or a statement of any court's requirements.
/// </summary>
[System.ComponentModel.DisplayName("Bill of costs")]
[System.ComponentModel.Category("Legal")]
[System.ComponentModel.Description("A bill of costs whose phase subtotals, hours and VAT are all computed from the time entries, with one table per phase and a summary that reconciles to them. PDF/A-2a + PDF/UA-1.")]
public static class BillOfCostsExample
{
    private const string Reference = "BOC-2026-0318";
    private const string Matter = "Sample Claimant v Sample Defendant";
    private const string CaseNumber = "Sample case reference SC-2026-000417";
    private const string Court = "Sample County Court";
    private const string ClaimingParty = "The Claimant";
    private const string PayingParty = "The Defendant";
    private const string Firm = "Sample Solicitors LLP";
    private const string PreparedBy = "R. Oyelaran — Costs Draftsman";
    private const decimal VatRate = 0.2m;
    private const bool VatRegistered = true;

    private static readonly DateTime PeriodFrom = new(2025, 12, 1);
    private static readonly DateTime PeriodTo = new(2026, 8, 31);
    private static readonly DateTime Served = new(2026, 9, 14);

    /// <summary>Fee-earner grades and the hourly rate claimed for each.</summary>
    private sealed record GradeInfo(string Label, decimal Rate);

    private static readonly (string Key, GradeInfo Info)[] Grades =
    [
        ("A", new("Grade A — Solicitor, 8+ years", 282m)),
        ("B", new("Grade B — Solicitor, 4+ years", 232m)),
        ("C", new("Grade C — Other solicitor or legal executive", 185m)),
        ("D", new("Grade D — Trainee or paralegal", 129m)),
    ];

    private static GradeInfo GradeFor(string key) => Grades.First(g => string.Equals(g.Key, key, StringComparison.Ordinal)).Info;

    /// <param name="Units">Chargeable units of six minutes, as time is recorded in practice.</param>
    private sealed record TimeEntry(
        string Date,
        string Phase,
        string FeeEarner,
        string Grade,
        string Description,
        int Units);

    private static readonly TimeEntry[] Entries =
    [
        new("04 Dec 2025", "Pre-action", "J. Okonkwo", "A", "Initial conference with client; instructions taken", 14),
        new("08 Dec 2025", "Pre-action", "T. Hargreaves", "C", "Review of client documents and chronology preparation", 22),
        new("18 Dec 2025", "Pre-action", "J. Okonkwo", "A", "Drafting letter before claim", 18),
        new("19 Dec 2025", "Pre-action", "M. Farrell", "D", "Collating enclosures to letter before claim", 9),
        new("22 Jan 2026", "Pre-action", "J. Okonkwo", "A", "Considering response to letter before claim; advice to client", 11),
        new("18 Feb 2026", "Issue and statements of case", "T. Hargreaves", "C", "Drafting claim form and particulars of claim", 34),
        new("25 Feb 2026", "Issue and statements of case", "J. Okonkwo", "A", "Settling particulars of claim", 12),
        new("04 Mar 2026", "Issue and statements of case", "M. Farrell", "D", "Issuing claim; service on the Defendant", 7),
        new("05 May 2026", "Issue and statements of case", "T. Hargreaves", "C", "Considering Defence; note to file", 13),
        new("20 May 2026", "Issue and statements of case", "S. Beaumont", "B", "Drafting reply to defence", 16),
        new("11 Jun 2026", "Case management", "S. Beaumont", "B", "Preparing directions questionnaire and costs budget", 21),
        new("24 Jun 2026", "Case management", "J. Okonkwo", "A", "Attending case management conference", 15),
        new("30 Jul 2026", "Case management", "M. Farrell", "D", "Diarising directions; updating case plan", 5),
        new("06 Jul 2026", "Disclosure", "M. Farrell", "D", "Collating and paginating disclosure documents", 41),
        new("14 Jul 2026", "Disclosure", "T. Hargreaves", "C", "Review of disclosure for relevance and privilege", 38),
        new("21 Jul 2026", "Disclosure", "S. Beaumont", "B", "Preparing disclosure list and statement", 14),
        new("28 Jul 2026", "Disclosure", "T. Hargreaves", "C", "Inspection of the Defendant’s disclosure", 19),
        new("05 Aug 2026", "Witness statements", "S. Beaumont", "B", "Taking statement from the Claimant", 26),
        new("12 Aug 2026", "Witness statements", "T. Hargreaves", "C", "Drafting and revising witness statement", 23),
        new("14 Aug 2026", "Witness statements", "J. Okonkwo", "A", "Settling witness statement and exhibit", 9),
        new("26 Aug 2026", "Witness statements", "M. Farrell", "D", "Preparing exhibit bundle and index", 12),
        new("31 Aug 2026", "Costs management", "S. Beaumont", "B", "Updating costs budget and preparing this bill", 18),
    ];

    private sealed record Disbursement(string Date, string Description, decimal Amount, bool Vatable);

    private static readonly Disbursement[] Disbursements =
    [
        new("04 Mar 2026", "Court issue fee", 455.00m, false),
        new("11 Jun 2026", "Court fee — case management conference", 275.00m, false),
        new("18 Jul 2026", "Document hosting and processing", 340.00m, true),
        new("20 Aug 2026", "Counsel’s fee — advice on quantum", 1250.00m, true),
        new("26 Aug 2026", "Copying and bundle production", 186.40m, true),
    ];

    private const int MinutesPerUnit = 6;

    private static decimal EntryHours(TimeEntry entry) => entry.Units * MinutesPerUnit / 60m;

    private static decimal EntryValue(TimeEntry entry) => EntryHours(entry) * GradeFor(entry.Grade).Rate;

    public static Document Generate()
    {
        // Every total below is computed from the entries. A bill whose phase
        // subtotals do not add to its grand total is not one anybody will pay, so
        // nothing here is transcribed.
        string[] phases = [.. Entries.Select(e => e.Phase).Distinct(StringComparer.Ordinal)];
        (string Phase, TimeEntry[] Rows, decimal Hours, decimal Value)[] phaseTotals =
        [
            .. phases.Select(phase =>
            {
                TimeEntry[] rows = [.. Entries.Where(e => string.Equals(e.Phase, phase, StringComparison.Ordinal))];
                return (phase, rows, rows.Sum(EntryHours), rows.Sum(EntryValue));
            }),
        ];

        decimal profitCosts = phaseTotals.Sum(p => p.Value);
        decimal totalHours = phaseTotals.Sum(p => p.Hours);
        decimal vatOnCosts = profitCosts * VatRate;

        decimal disbursementsNet = Disbursements.Sum(d => d.Amount);
        decimal vatOnDisbursements = Disbursements.Where(d => d.Vatable).Sum(d => d.Amount * VatRate);

        decimal grandTotal = profitCosts + vatOnCosts + disbursementsNet + vatOnDisbursements;

        // The reconciliation the document asserts on its own face.
        decimal phaseSum = phaseTotals.Sum(p => p.Value);
        bool reconciles = Math.Abs(phaseSum - profitCosts) < 0.005m;

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 44 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.HeaderTemplate = pageNumber =>
            pageNumber == 1 ? null : Band($"{Matter} · {Reference}", CaseNumber);
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"Bill of costs served {GbDate(Served)} by {Firm}",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Bill of costs {Reference} — {Matter}";
        doc.Metadata.Author = Firm;
        doc.Metadata.Subject =
            "Bill of costs: chargeable time by phase and fee-earner grade, disbursements, VAT, and a summary that reconciles to the phase subtotals (sample data).";
        doc.Metadata.Keywords = "bill of costs, legal costs, time recording, disbursements, phases, vat, pdf/ua, pdf/a";

        // ── Title ─────────────────────────────────────────────────────────────

        doc.Add(AccentBar());
        doc.Add(Cell("BILL OF COSTS", size: 8.5f, bold: true, color: Accent, marginBottom: 4f));
        doc.Add(H1(Matter));
        doc.Add(Cell($"{Court} · {CaseNumber}", color: Muted, marginBottom: 14f));

        doc.Add(WeightedRow(
            [
                Field("Bill reference", Reference),
                Field("Claiming party", ClaimingParty),
                Field("Paying party", PayingParty),
                Field("Served", GbDate(Served)),
            ],
            marginBottom: 6f));
        doc.Add(WeightedRow(
        [
            Field("Period covered", $"{GbDate(PeriodFrom)} to {GbDate(PeriodTo)}"),
            Field("Prepared by", PreparedBy),
            Field("Firm", Firm),
            Field("VAT status", VatRegistered ? "Registered — VAT claimed" : "Not registered"),
        ]));

        doc.Add(WeightedRow(
            [
                Tile(totalHours.ToString("F1", CultureInfo.InvariantCulture), "Chargeable hours"),
                Tile(Entries.Length.ToString(CultureInfo.InvariantCulture), "Time entries"),
                Tile(Money(profitCosts), "Profit costs"),
                Tile(Money(grandTotal), "Total claimed", Accent),
            ],
            spacing: 10f,
            marginBottom: 4f));

        // ── Rates ─────────────────────────────────────────────────────────────

        // Each heading asks for room for the start of what it introduces, so it is never left alone at the foot of a page.
        doc.Add(KeepWithNext(H2("Hourly rates claimed")));
        Table rates = new(1f, 4.6f, 1.4f, 1.4f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        rates.AddHeader(
            Th("Grade"),
            Th("Description"),
            Th("Hourly rate", TextAlignment.Right),
            Th("Hours claimed", TextAlignment.Right));
        foreach ((string key, GradeInfo info) in Grades)
        {
            decimal hours = Entries
                .Where(e => string.Equals(e.Grade, key, StringComparison.Ordinal))
                .Sum(EntryHours);
            rates.AddRow(
                Td(key, bold: true),
                Td(info.Label),
                Td(Money(info.Rate), align: TextAlignment.Right),
                Td(hours.ToString("F1", CultureInfo.InvariantCulture), bold: true, align: TextAlignment.Right));
        }

        doc.Add(rates);

        doc.Add(Cell(
            $"Time is recorded in units of {MinutesPerUnit} minutes and claimed in hours at the grade rate. "
                + "Rates are those claimed by the receiving party and remain subject to assessment.",
            size: 8.5f,
            color: Muted,
            marginTop: 6f,
            marginBottom: 4f));

        // ── Schedule of work, by phase ────────────────────────────────────────

        doc.Add(KeepWithNext(H2("Schedule of work"), 170f));
        doc.Add(Cell(
            "Each phase carries its own subtotal. The phase subtotals are what the summary adds up.",
            size: 9f,
            color: Muted,
            marginBottom: 8f));

        // One table per phase, each with its own header and subtotal. A single
        // continuous table with an inline phase banner puts the phase name in the
        // narrowest column, where it wraps to four lines - and a phase that starts
        // mid-page then loses its label entirely when the table carries over.
        foreach ((string phase, TimeEntry[] rows, decimal hours, decimal value) in phaseTotals)
        {
            Heading heading = H3($"{phase} — {hours.ToString("F1", CultureInfo.InvariantCulture)} hours, {Money(value)}");

            Table schedule = new(1.1f, 1.5f, 0.6f, 5f, 0.85f, 1.15f, 1.25f)
            {
                CellPadding = 5,
                Border = new Border(0.5f),
                BorderColors = new BorderColors(Rule),
                HeaderBackgroundColor = Accent,
                HeaderTextColor = Color.White,
            };
            schedule.AddHeader(
                Th("Date"),
                Th("Fee earner"),
                Th("Gr."),
                Th("Work done"),
                Th("Units", TextAlignment.Right),
                Th("Hours", TextAlignment.Right),
                Th("Value", TextAlignment.Right));

            foreach (TimeEntry entry in rows)
            {
                schedule.AddRow(
                    Td(entry.Date),
                    Td(entry.FeeEarner, color: Muted),
                    Td(entry.Grade),
                    Td(entry.Description),
                    Td(entry.Units.ToString(CultureInfo.InvariantCulture), align: TextAlignment.Right),
                    Td(EntryHours(entry).ToString("F1", CultureInfo.InvariantCulture), align: TextAlignment.Right),
                    Td(Money(EntryValue(entry)), align: TextAlignment.Right));
            }

            schedule.AddRow(
                Td(string.Empty),
                Td(string.Empty),
                Td(string.Empty),
                Td($"Subtotal — {phase}", bold: true),
                Td(string.Empty),
                Td(hours.ToString("F1", CultureInfo.InvariantCulture), bold: true, align: TextAlignment.Right),
                Td(Money(value), bold: true, color: Accent, align: TextAlignment.Right));

            // A phase is short enough to keep whole: heading, rows and subtotal share a page.
            VerticalStack phaseBlock = new() { BreakAllowed = false };
            phaseBlock.Add(heading);
            phaseBlock.Add(schedule);
            doc.Add(phaseBlock);
        }

        // ── Disbursements ─────────────────────────────────────────────────────

        doc.Add(KeepWithNext(H2("Disbursements")));

        Table disbursements = new(1.2f, 5.4f, 1.2f, 1.3f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        disbursements.AddHeader(Th("Date"), Th("Description"), Th("VAT"), Th("Amount", TextAlignment.Right));
        foreach (Disbursement item in Disbursements)
        {
            disbursements.AddRow(
                Td(item.Date),
                Td(item.Description),
                Td(item.Vatable ? "Standard rate" : "Outside the scope", color: Muted),
                Td(Money(item.Amount), bold: true, align: TextAlignment.Right));
        }

        doc.Add(disbursements);

        doc.Add(Cell(
            "Court fees are outside the scope of VAT and no VAT is claimed on them. VAT is claimed on the "
                + "remaining disbursements at the standard rate.",
            size: 8.5f,
            color: Muted,
            marginTop: 6f,
            marginBottom: 4f));

        // ── Summary ───────────────────────────────────────────────────────────

        doc.Add(KeepWithNext(H2("Summary of costs claimed"), 150f));

        doc.Add(TotalsPanel(
            [
                TotalLine("Profit costs", Money(profitCosts)),
                TotalLine($"VAT on profit costs at {VatRate * 100:0}%", Money(vatOnCosts)),
                TotalLine("Disbursements", Money(disbursementsNet)),
                TotalLine("VAT on disbursements", Money(vatOnDisbursements)),
                Divider(),
                TotalLine("Total claimed", Money(grandTotal), bold: true, size: 12f, color: Accent),
            ],
            1.8f));

        doc.Add(reconciles
            ? Cell(
                $"The phase subtotals add to {Money(phaseSum)}, which agrees with the profit costs claimed above.",
                size: 8.5f,
                color: Muted,
                marginTop: 6f,
                marginBottom: 4f)
            : Cell(
                $"The phase subtotals total {Money(phaseSum)} but the profit costs are stated as "
                    + $"{Money(profitCosts)}. This bill does not reconcile and must not be served.",
                size: 9.5f,
                bold: true,
                color: Negative,
                marginTop: 8f,
                marginBottom: 4f));

        doc.Add(Panel("Points of dispute",
        [
            Cell(
                "A paying party wishing to dispute any item should identify it by its date and phase and state the "
                    + "ground of objection, so that the receiving party can reply against the same reference.",
                size: 9f),
        ]));

        doc.Add(Cell(
            $"Prepared by {PreparedBy} for {Firm} and served on {GbDate(Served)}. "
                + "Figures are stated to the nearest penny and are subject to assessment.",
            size: 8.5f,
            color: Muted,
            marginTop: 8f));

        return doc;
    }
}
