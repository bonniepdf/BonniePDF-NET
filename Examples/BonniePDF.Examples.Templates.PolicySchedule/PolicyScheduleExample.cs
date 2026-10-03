using System.Globalization;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.PolicySchedule;

/// <summary>
/// Insurance policy schedule and coverage summary.
///
/// A regulated-document flagship for the insurance vertical: policyholder and
/// insured-property details, coverage limits and deductibles, a premium
/// breakdown, endorsements and exclusions, claims contacts, a renewal date, a QR
/// link to a self-service portal, accessible long tables, and PDF/A-2a archival
/// output. A good fit for BonniePDF's model of generating structured documents
/// from application data — it is not dependent on form filling.
///
/// Fixture-driven: <see cref="BuildDocument"/> is pure over a typed domain model,
/// with <see cref="Generate"/> as a thin adapter feeding in deterministic sample
/// data. Conformance: PDF/A-2a + PDF/UA-1, en-GB. Entirely fictional, and fully
/// self-contained — no shared kit.
/// </summary>
[System.ComponentModel.DisplayName("Policy Schedule")]
[System.ComponentModel.Category("Insurance")]
[System.ComponentModel.Description("An insurance policy schedule: property details, coverage limits and deductibles, a premium breakdown, endorsements, and a QR link to a self-service portal.")]
public static class PolicyScheduleExample
{
    // ── Brand tokens ──────────────────────────────────────────────────────────
    private static readonly Color Ink = ColorPalette.Slate[800];
    private static readonly Color Muted = ColorPalette.Slate[500];
    // 600, not 400: 400 never clears 4.5:1 as text (2.63:1 on this document's backgrounds).
    private static readonly Color Faint = ColorPalette.Slate[600];
    private static readonly Color HeaderBg = ColorPalette.Slate[100];
    private static readonly Color Rule = ColorPalette.Slate[200];
    private static readonly Color Brand = ColorPalette.Teal[700];
    private static readonly Color BrandSoft = ColorPalette.Teal[50];
    // 700, not 600: this table cell text on white needs 4.5:1 and 600 falls to 3.20:1.
    private static readonly Color Accent = ColorPalette.Amber[700];
    private static readonly Color Danger = ColorPalette.Rose[700];

    // ── Domain model ──────────────────────────────────────────────────────────

    /// <summary>One line of the schedule of cover.</summary>
    public sealed record Coverage(string Peril, string Section, decimal Limit, decimal Deductible);

    /// <summary>One line of the premium breakdown.</summary>
    public sealed record PremiumLine(string Label, decimal Amount);

    /// <summary>An endorsement or an exclusion.</summary>
    public sealed record Endorsement(string Code, string Title, string Detail, bool IsExclusion);

    public sealed record Insurer(string Name, string Address, string AuthCode);

    public sealed record Policy(string Number, string Product, string Inception, string Renewal, string Status);

    public sealed record Policyholder(string Name, string Address, string Email);

    public sealed record InsuredRisk(string Type, string Address, int YearBuilt, decimal Rebuild);

    public sealed record Claims(string Phone, string Email, string Hours);

    public sealed record PolicyScheduleData(
        Insurer Insurer,
        Policy Policy,
        Policyholder Policyholder,
        InsuredRisk InsuredRisk,
        Coverage[] Coverages,
        PremiumLine[] Premiums,
        Endorsement[] Endorsements,
        Claims Claims,
        string PortalUrl);

    // ── Sample fixture ────────────────────────────────────────────────────────

    private static readonly PolicyScheduleData Sample = new(
        Insurer: new Insurer(
            "Sample Home Insurance Ltd",
            "20 Sample Street\nLondon EC3M 8AF\nAuthorised by the FCA (sample)",
            "SHIL-FC-0099"),
        Policy: new Policy("HOM-2026-8842139", "HomeShield Buildings & Contents", "01 Sep 2026", "01 Sep 2027", "Active"),
        Policyholder: new Policyholder("Sample Policyholder", "47 Sample Walk\nSample City SC6 1QR", "policyholder@sample.example"),
        InsuredRisk: new InsuredRisk("Semi-detached house", "47 Sample Walk, Sample City SC6 1QR", 1936, 412000m),
        Coverages:
        [
            new Coverage("Fire, smoke, lightning", "Buildings", 412000m, 250m),
            new Coverage("Subsidence / heave", "Buildings", 412000m, 1000m),
            new Coverage("Escape of water", "Buildings", 412000m, 350m),
            new Coverage("Storm & flood", "Buildings", 412000m, 250m),
            new Coverage("Accidental damage", "Buildings", 412000m, 250m),
            new Coverage("Contents (replacement)", "Contents", 65000m, 150m),
            new Coverage("Valuable possessions", "Contents", 15000m, 250m),
            new Coverage("Personal legal liability", "Liability", 2000000m, 0m),
            new Coverage("Alternative accommodation", "Buildings", 30000m, 0m),
            new Coverage("Pedal cycles (away from home)", "Contents", 2000m, 100m),
        ],
        Premiums:
        [
            new PremiumLine("Base premium (Buildings)", 312.40m),
            new PremiumLine("Base premium (Contents)", 148.60m),
            new PremiumLine("Accidental damage extension", 58.00m),
            new PremiumLine("Personal possessions cover", 34.50m),
            new PremiumLine("Broker fee", 25.00m),
            new PremiumLine("Insurance Premium Tax (12%)", 69.42m),
        ],
        Endorsements:
        [
            new Endorsement("EN-01", "High-value items schedule", "Items over £2,000 listed in Appendix A.", false),
            new Endorsement("EN-04", "Home business extension", "Covers incidental office use up to £15,000.", false),
            new Endorsement("EX-12", "Wear and tear / gradual deterioration", "Excluded in all sections.", true),
            new Endorsement("EX-22", "Acts of terrorism", "Excluded unless Pool Re extension purchased.", true),
            new Endorsement("EX-27", "Unoccupied property beyond 60 days", "Theft and water damage excluded if unoccupied > 60 consecutive days.", true),
            new Endorsement("EN-09", "Smart-water property marking", "£50 excess reduction on approved theft claims.", false),
        ],
        Claims: new Claims("0800 555 0142", "claims@sample-home-insurance.example", "24 hours / 7 days"),
        PortalUrl: "https://portal.sample-home-insurance.example/p/HOM-2026-8842139");

    public static Document Generate() => BuildDocument(Sample);

    // ── Local helpers ─────────────────────────────────────────────────────────

    private static TextBlock Cell(string text, float size = 9f, bool bold = false, Color? color = null, TextAlignment align = TextAlignment.Left) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = size,
        Color = color ?? Ink,
        Alignment = align,
    };

    /// <summary>Sterling with up to two decimals and none when the amount is whole.</summary>
    private static string Money(decimal value)
        => "£" + value.ToString("#,##0.##", CultureInfo.GetCultureInfo("en-GB"));

    private static TextBlock Eyebrow(string text, Color? color = null)
        => Cell(text.ToUpperInvariant(), size: 8f, bold: true, color: color ?? Brand);

    /// <summary>
    /// A labelled group of facts. Each key sits above its value rather than beside it, so a long value
    /// (an email address does not wrap) gets the whole column instead of a fraction of it.
    /// </summary>
    private static VerticalStack KeyValueBlock(string label, (string Key, string Value)[] rows)
    {
        VerticalStack stack = new();
        stack.Add(Cell(label, size: 8f, bold: true, color: Brand));
        foreach ((string key, string value) in rows)
        {
            VerticalStack pair = new() { Margin = new Thickness(0, 6, 0, 0) };
            pair.Add(Cell(key, size: 7.5f, color: Faint));
            pair.Add(Cell(value));
            stack.Add(pair);
        }

        return stack;
    }

    // ── BuildDocument ─────────────────────────────────────────────────────────

    public static Document BuildDocument(PolicyScheduleData data)
    {
        decimal premiumTotal = data.Premiums.Sum(p => p.Amount);

        DocumentSettings settings = new(PdfSize.A4, 50)
        {
            PageColor = Color.White,
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9f).WithColor(Ink),
            HeaderTemplate = pageNumber => new TextBlock
            {
                Text = $"{data.Insurer.Name} · Policy {data.Policy.Number} · Page {pageNumber} of {TextElementBase.TotalPagesToken}",
                FontName = StandardFonts.Helvetica,
                FontSize = 7.5f,
                Color = Faint,
                Alignment = TextAlignment.Left,
            },
            FooterTemplate = pageNumber => new TextBlock
            {
                Text = $"{data.Policy.Product} · Renewal {data.Policy.Renewal} · Page {pageNumber} of {TextElementBase.TotalPagesToken}",
                FontName = StandardFonts.Helvetica,
                FontSize = 7.5f,
                Color = Faint,
                Alignment = TextAlignment.Center,
            },
        };

        Document doc = new(settings);
        doc.Metadata.Title = $"Policy Schedule — {data.Policy.Product} — {data.Policy.Number}";
        doc.Metadata.Author = data.Insurer.Name;
        doc.Metadata.Subject = "Insurance policy schedule and coverage summary (fictional sample).";

        // ── Masthead ──────────────────────────────────────────────────────────
        Grid masthead = new(1.6f, 1f) { CellPadding = 0, Border = Border.None, Margin = new Thickness(0, 0, 0, 10) };

        VerticalStack status = new();
        status.Add(Cell("POLICY SCHEDULE", size: 11f, bold: true, color: Brand));
        status.Add(Cell($"{data.Policy.Status.ToUpperInvariant()} · renews {data.Policy.Renewal}", size: 8f, color: Muted));
        Container statusBox = new()
        {
            BackgroundColor = BrandSoft,
            Padding = new Thickness(8, 10),
            Border = Border.LeftOnly(3),
            BorderColors = BorderColors.LeftOnly(Brand),
            Child = status,
        };

        VerticalStack identity = new();
        identity.Add(new Heading(data.Insurer.Name)
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 18f,
            Color = Brand,
        });
        identity.Add(Cell(data.Insurer.Address, size: 8f, color: Muted));
        masthead.AddRow(identity, statusBox);
        doc.Add(masthead);
        doc.Add(new Container { BackgroundColor = Brand, Height = 1.5f });

        // ── Policy + policyholder + risk (three-column block) ─────────────────
        Grid riskGrid = new(1f, 1f, 1f) { CellPadding = 12, Border = Border.None };
        riskGrid.AddRow(
            KeyValueBlock("POLICY",
            [
                ("Policy number", data.Policy.Number),
                ("Product", data.Policy.Product),
                ("Inception", data.Policy.Inception),
                ("Renewal", data.Policy.Renewal),
            ]),
            KeyValueBlock("POLICYHOLDER",
            [
                ("Name", data.Policyholder.Name),
                ("Address", data.Policyholder.Address),
                ("Email", data.Policyholder.Email),
            ]),
            KeyValueBlock("INSURED RISK",
            [
                ("Property type", data.InsuredRisk.Type),
                ("Risk address", data.InsuredRisk.Address),
                ("Year built", data.InsuredRisk.YearBuilt.ToString(CultureInfo.InvariantCulture)),
                ("Rebuild sum insured", Money(data.InsuredRisk.Rebuild)),
            ]));
        doc.Add(new Container
        {
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            Margin = new Thickness(0, 14, 0, 16),
            Child = riskGrid,
        });

        // ── Coverage schedule (the long table — repeats headers across pages) ─
        doc.Add(Eyebrow("Schedule of cover"));
        Table covTable = new(2.6f, 1.2f, 1.2f, 1f, 0.8f)
        {
            CellPadding = 7,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = HeaderBg,
            HeaderTextColor = Ink,
            RepeatHeadersOnNewPage = true,
            Margin = new Thickness(0, 4, 0, 14),
        };
        covTable.AddHeader(
            Cell("Peril / benefit", size: 8f, bold: true),
            Cell("Section", size: 8f, bold: true),
            Cell("Sum insured", size: 8f, bold: true, align: TextAlignment.Right),
            Cell("Deductible", size: 8f, bold: true, align: TextAlignment.Right),
            Cell("Status", size: 8f, bold: true, align: TextAlignment.Center));
        foreach (Coverage c in data.Coverages)
        {
            covTable.AddRow(
                Cell(c.Peril),
                Cell(c.Section, color: Muted),
                Cell(Money(c.Limit), align: TextAlignment.Right),
                Cell(c.Deductible > 0 ? Money(c.Deductible) : "Nil", color: Muted, align: TextAlignment.Right),
                Cell("Covered", color: Brand, align: TextAlignment.Center));
        }

        doc.Add(covTable);

        // ── Premium breakdown + QR portal (side by side) ──────────────────────
        Table premTable = new(3f, 1f)
        {
            CellPadding = 5,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = HeaderBg,
            HeaderTextColor = Ink,
            Margin = new Thickness(0, 4, 0, 0),
        };
        premTable.AddHeader(
            Cell("Premium breakdown", size: 8f, bold: true),
            Cell("Amount", size: 8f, bold: true, align: TextAlignment.Right));
        foreach (PremiumLine p in data.Premiums)
        {
            premTable.AddRow(Cell(p.Label), Cell(Money(p.Amount), align: TextAlignment.Right));
        }

        premTable.AddRow(
            Cell("Total annual premium", bold: true, color: Brand),
            Cell(Money(premiumTotal), bold: true, color: Brand, align: TextAlignment.Right));

        VerticalStack qrBody = new() { Spacing = 6 };
        qrBody.Add(Cell("Self-service portal", size: 8f, bold: true, color: Muted, align: TextAlignment.Center));
        qrBody.Add(new Container
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new QRCode
            {
                Data = data.PortalUrl,
                Size = 92,
                QuietZone = 1,
                ErrorCorrection = QrErrorCorrection.High,
                ForegroundColor = Ink,
                AltText = $"QR code linking to the policy portal for policy {data.Policy.Number}",
            },
        });
        qrBody.Add(Cell("Scan to view documents and report a claim", size: 7.5f, color: Faint, align: TextAlignment.Center));
        Container qrCard = new()
        {
            BackgroundColor = Color.White,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            Padding = new Thickness(10, 10),
            Child = qrBody,
        };

        Grid premQrGrid = new(1.4f, 1f) { CellPadding = 0, Border = Border.None, Margin = new Thickness(0, 4, 0, 0) };
        premQrGrid.AddRow(premTable, new Container { Padding = new Thickness(16, 0, 0, 0), Child = qrCard });
        doc.Add(premQrGrid);

        // ── Endorsements & exclusions ─────────────────────────────────────────
        // Heading and table stay together: a short register is never split across pages.
        VerticalStack endorsements = new() { BreakAllowed = false };
        endorsements.Add(Eyebrow("Endorsements & exclusions"));
        Table endTable = new(0.8f, 2f, 4f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = HeaderBg,
            HeaderTextColor = Ink,
            Margin = new Thickness(0, 4, 0, 14),
        };
        endTable.AddHeader(
            Cell("Code", size: 8f, bold: true),
            Cell("Title", size: 8f, bold: true),
            Cell("Detail", size: 8f, bold: true));
        foreach (Endorsement e in data.Endorsements)
        {
            endTable.AddRow(
                Cell(e.Code, bold: true, color: e.IsExclusion ? Danger : Accent),
                Cell(e.Title, color: e.IsExclusion ? Danger : Ink),
                Cell(e.Detail, color: Muted));
        }

        endorsements.Add(endTable);
        doc.Add(endorsements);

        // ── Claims & contacts ─────────────────────────────────────────────────
        // The middle column is wider: an email address cannot wrap, so it needs room.
        Grid claimsGrid = new(1f, 1.7f, 1f) { CellPadding = 0, Border = Border.None };

        VerticalStack line = new();
        line.Add(Cell("CLAIMS LINE", size: 8f, bold: true, color: Brand));
        line.Add(Cell(data.Claims.Phone, size: 13f, bold: true));
        VerticalStack email = new();
        email.Add(Cell("EMAIL", size: 8f, bold: true, color: Brand));
        email.Add(Cell(data.Claims.Email, size: 10f));
        VerticalStack hours = new();
        hours.Add(Cell("HOURS", size: 8f, bold: true, color: Brand));
        hours.Add(Cell(data.Claims.Hours, size: 10f));
        claimsGrid.AddRow(line, email, hours);

        doc.Add(new Container
        {
            BackgroundColor = BrandSoft,
            Border = Border.LeftOnly(3),
            BorderColors = BorderColors.LeftOnly(Brand),
            Padding = new Thickness(14, 16),
            Margin = new Thickness(0, 6, 0, 0),
            Child = claimsGrid,
        });

        // ── Renewal & notice ──────────────────────────────────────────────────
        doc.Add(new Container
        {
            Margin = new Thickness(0, 14, 0, 0),
            Child = Cell(
                $"This schedule is a summary; the full policy wording prevails. Cover continues until {data.Policy.Renewal} "
                    + "unless renewed or cancelled. This is a fictional document for demonstration only and is not insurance advice.",
                size: 7.5f,
                color: Faint),
        });

        return doc;
    }
}
