using System.Globalization;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using static BonniePDF.Examples.Shared.TradeKit;

namespace BonniePDF.Examples.Templates.ExhibitBundleIndex;

/// <summary>
/// What a litigation-support or legal-practice platform generates around a
/// bundle: the cover sheet, the hyperlinked index, and the separator sheet that
/// introduces each section, all carrying continuous pagination across the whole
/// bundle.
///
/// <b>Scope, read this before evaluating.</b> BonniePDF generates documents. It
/// is not a PDF parser and not a merger, and it never will be. It therefore does
/// NOT assemble a bundle out of existing PDFs.
///
/// What this example produces is the generated apparatus of a bundle - the
/// cover, the index with live internal links and resolved page numbers, and the
/// section separators. Combining that apparatus with the exhibits themselves is
/// a merge step, and you would do it with a tool built for merging.
///
/// That boundary is stated here rather than buried, because the apparatus is the
/// part that is genuinely hard to produce - continuous pagination and an index
/// whose page references stay correct as content reflows - and the part a
/// templating engine handles worst.
///
/// Sample parties, references, and exhibits throughout. Nothing here is legal
/// advice or a procedural template.
/// </summary>
[System.ComponentModel.DisplayName("Exhibit bundle index")]
[System.ComponentModel.Category("Legal")]
[System.ComponentModel.Description("The generated apparatus of a litigation bundle: a cover sheet, an index whose links and page references resolve after layout, and a separator sheet per section, under continuous pagination. PDF/A-2a + PDF/UA-1.")]
public static class ExhibitBundleIndexExample
{
    private const string Reference = "Bundle A — Claimant";
    private const string Matter = "Sample Claimant v Sample Defendant";
    private const string CaseNumber = "Sample case reference SC-2026-000417";
    private const string Court = "Sample County Court";
    private const string HearingType = "Case management conference";
    private const string PreparedBy = "Sample Solicitors LLP, solicitors for the Claimant";
    private const string Volume = "Volume 1 of 1";

    private static readonly DateTime Hearing = new(2026, 10, 14);
    private static readonly DateTime PreparedOn = new(2026, 9, 25);

    private sealed record BundleItem(string Reference, string Description, string Dated, int Pages);

    private sealed record BundleSection(
        string Id,
        string Tab,
        string Title,
        string Description,
        BundleItem[] Items);

    private static readonly BundleSection[] Sections =
    [
        new("section-a", "A", "Statements of case",
            "The pleadings as filed, in the order in which they were served.",
            [
                new("A1", "Claim form", "04 Mar 2026", 3),
                new("A2", "Particulars of claim", "04 Mar 2026", 11),
                new("A3", "Defence", "29 Apr 2026", 9),
                new("A4", "Reply to defence", "20 May 2026", 4),
            ]),
        new("section-b", "B", "Orders and directions",
            "Orders of the court in date order, most recent last.",
            [
                new("B1", "Order of the court — allocation", "11 Jun 2026", 2),
                new("B2", "Order of the court — directions", "30 Jul 2026", 3),
            ]),
        new("section-c", "C", "Witness statements",
            "Witness statements with their exhibits following each statement.",
            [
                new("C1", "Witness statement of the Claimant", "14 Aug 2026", 16),
                new("C2", "Exhibit to the witness statement of the Claimant", "14 Aug 2026", 24),
                new("C3", "Witness statement of the Defendant", "02 Sep 2026", 12),
            ]),
        new("section-d", "D", "Correspondence",
            "Inter-party correspondence relied upon, in date order.",
            [
                new("D1", "Letter before claim", "18 Dec 2025", 5),
                new("D2", "Response to letter before claim", "22 Jan 2026", 6),
                new("D3", "Correspondence bundle — 14 items", "Various", 31),
            ]),
    ];

    public static Document Generate()
    {
        int totalItems = Sections.Sum(s => s.Items.Length);
        int totalPages = Sections.Sum(s => s.Items.Sum(i => i.Pages));

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 50 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10).WithColor(Ink);
        // Continuous pagination across the whole bundle, including the cover. A
        // bundle whose page numbers restart per section is a bundle nobody can
        // navigate from a transcript.
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"{Matter} · {Reference}",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"{Reference} — index and separators — {Matter}";
        doc.Metadata.Author = PreparedBy;
        doc.Metadata.Subject =
            "Generated bundle apparatus: cover sheet, hyperlinked index with resolved page references, and section separator sheets (sample data). Does not merge existing PDFs.";
        doc.Metadata.Keywords =
            "bundle, index, exhibits, pagination, litigation support, cross-references, pdf/ua, pdf/a";

        // ── Cover sheet ───────────────────────────────────────────────────────

        doc.Add(new Section("cover"));
        doc.Add(AccentBar());
        doc.Add(Cell(Court.ToUpperInvariant(), size: 9f, bold: true, color: Accent, marginBottom: 16f));
        doc.Add(Cell(CaseNumber, size: 10f, color: Muted, marginBottom: 20f));

        doc.Add(H1(Matter));
        doc.Add(Cell(Reference, size: 14f, bold: true, color: Accent, marginTop: 4f, marginBottom: 24f));

        doc.Add(WeightedRow(
            [
                Field("Hearing", $"{HearingType}, {GbDate(Hearing)}"),
                Field("Volume", Volume),
                Field("Sections", Sections.Length.ToString(CultureInfo.InvariantCulture)),
                Field("Items", totalItems.ToString(CultureInfo.InvariantCulture)),
            ],
            marginBottom: 8f));
        doc.Add(WeightedRow(
        [
            Field("Prepared by", PreparedBy),
            Field("Prepared on", GbDate(PreparedOn)),
            Field("Exhibit pages", totalPages.ToString(CultureInfo.InvariantCulture)),
            Field("Index", "Follows this sheet"),
        ]));

        doc.Add(Panel("What this document is",
        [
            Cell(
                "This is the generated apparatus of the bundle: this cover sheet, the index that follows, and a "
                    + "separator sheet introducing each section. The exhibits themselves are separate documents and "
                    + "are combined with this apparatus at assembly.",
                size: 9f),
        ]));

        // ── Index ─────────────────────────────────────────────────────────────

        doc.Add(new PageBreak());
        doc.Add(new Section("index"));
        doc.Add(H1("Index"));
        doc.Add(Cell(
            "Each section below links to its separator sheet, and the page reference resolves after layout — so it "
                + "stays correct when the bundle is repaginated.",
            color: Muted,
            marginBottom: 14f));

        foreach (BundleSection section in Sections)
        {
            HorizontalStack heading = new()
            {
                WidthDistribution = WidthDistribution.Weighted,
                Margin = new Thickness(0, 10, 0, 4),
            };
            heading.Add(
                new SectionLink($"Tab {section.Tab} — {section.Title}", section.Id)
                {
                    FontName = StandardFonts.HelveticaBold,
                    FontSize = 12,
                    Color = Accent,
                },
                4);

            HorizontalStack pageRow = new() { Spacing = 4 };
            TextBlock label = Cell("separator at page", size: 9f, color: Muted);
            pageRow.Add(label, label.GetPreferredWidth());
            pageRow.Add(new PageNumberOf(section.Id)
            {
                FontName = StandardFonts.HelveticaBold,
                FontSize = 9,
                Color = Ink,
            });
            heading.Add(pageRow, 1.4f);
            doc.Add(heading);

            Table table = new(0.8f, 5.2f, 1.6f, 1f)
            {
                CellPadding = 5,
                Border = new Border(0.5f),
                BorderColors = new BorderColors(Rule),
                HeaderBackgroundColor = Accent,
                HeaderTextColor = Color.White,
            };
            table.AddHeader(Th("Ref"), Th("Document"), Th("Dated"), Th("Pages", TextAlignment.Right));
            foreach (BundleItem item in section.Items)
            {
                table.AddRow(
                    Td(item.Reference, bold: true),
                    Td(item.Description),
                    Td(item.Dated, color: Muted),
                    Td(item.Pages.ToString(CultureInfo.InvariantCulture), align: TextAlignment.Right));
            }

            doc.Add(table);
        }

        // ── Separator sheets ──────────────────────────────────────────────────

        foreach (BundleSection section in Sections)
        {
            doc.Add(new PageBreak());
            doc.Add(new Section(section.Id));

            doc.Add(new Container
            {
                BackgroundColor = Accent,
                Height = 6,
                Margin = new Thickness(0, 0, 0, 40),
            });

            doc.Add(Cell(
                $"TAB {section.Tab}",
                size: 44f,
                bold: true,
                color: Accent,
                align: TextAlignment.Center,
                marginBottom: 8f));
            doc.Add(Cell(section.Title, size: 20f, bold: true, align: TextAlignment.Center, marginBottom: 10f));
            doc.Add(Cell(
                section.Description,
                size: 10f,
                color: Muted,
                align: TextAlignment.Center,
                marginBottom: 26f));

            VerticalStack contents = new() { Spacing = 4 };
            contents.Add(Cell(
                $"This section contains {section.Items.Length} item(s):",
                size: 9.5f,
                bold: true,
                marginBottom: 4f));
            foreach (BundleItem item in section.Items)
            {
                contents.Add(Cell($"{item.Reference} · {item.Description} ({item.Dated})", size: 9.5f));
            }

            doc.Add(new Container
            {
                Border = new Border(0.75f),
                BorderColors = new BorderColors(Rule),
                CornerRadius = 6,
                Padding = new Thickness(14),
                Child = contents,
            });

            doc.Add(new SectionLink("Return to the index", "index")
            {
                FontName = StandardFonts.Helvetica,
                FontSize = 9,
                Color = Accent,
                Margin = new Thickness(0, 18, 0, 0),
            });
        }

        return doc;
    }
}
