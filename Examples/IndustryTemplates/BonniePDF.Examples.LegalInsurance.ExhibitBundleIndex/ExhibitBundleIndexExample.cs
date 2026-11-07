using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.LegalInsurance.ExhibitBundleIndex;

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
[System.ComponentModel.Category("Legal & Insurance")]
[System.ComponentModel.Description(
    "The generated apparatus of a litigation bundle: a cover sheet, an index whose links and page references resolve after layout, and a separator sheet per section, under continuous pagination. PDF/A-2a + PDF/UA-1.")]
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

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
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

    // Local helpers and sample data for this document.

    // Local helpers and sample data for this document.
    private static readonly Color Ink = ColorPalette.Slate[700];

    /// <summary>600, not 400: 400 never clears 4.5:1 as body text on white.</summary>
    private static readonly Color Muted = ColorPalette.Slate[600];

    private static readonly Color Heading = ColorPalette.Slate[800];

    private static readonly Color Accent = ColorPalette.Rose[800];

    private static readonly Color Rule = ColorPalette.Slate[200];

    /// <summary>A styled text cell with trade-document defaults.</summary>
    private static TextBlock Cell(string text, float size = 9f, bool bold = false, Color? color = null, TextAlignment align = TextAlignment.Left, float marginTop = 0f, float marginBottom = 0f) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = size,
        Color = color ?? Ink,
        Alignment = align,
        Margin = new Thickness(0, marginTop, 0, marginBottom),
    };

    /// <summary>Table header cell - white on the accent band.</summary>
    private static TextBlock Th(string text, TextAlignment align = TextAlignment.Left) => Cell(text, size: 8.5f, bold: true, color: Color.White, align: align);

    /// <summary>Table body cell.</summary>
    private static TextBlock Td(string text, float size = 8.5f, bool bold = false, Color? color = null, TextAlignment align = TextAlignment.Left) => Cell(text, size: size, bold: bold, color: color, align: align);

    private static Heading H1(string text) => new(text)
    {
        Level = 1,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 21,
        Color = Heading,
        Margin = new Thickness(0, 0, 0, 4),
    };

    /// <summary>The full-width accent rule at the top of each document.</summary>
    private static Container AccentBar(Color? color = null, float height = 4f) => new()
    {
        BackgroundColor = color ?? Accent,
        Height = height,
        Margin = new Thickness(0, 0, 0, 14),
    };

    /// <summary>One label/value pair for the reference grid.</summary>
    private static VerticalStack Field(string label, string value)
    {
        VerticalStack stack = new();
        stack.Add(Cell(label.ToUpperInvariant(), size: 6.5f, bold: true, color: Muted, marginBottom: 2f));
        stack.Add(Cell(value, size: 9.5f));
        return stack;
    }

    /// <summary>A row of evenly weighted elements.</summary>
    private static HorizontalStack WeightedRow(IEnumerable<IElement> items, float spacing = 14f, float marginTop = 0f, float marginBottom = 14f)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = spacing,
            Margin = new Thickness(0, marginTop, 0, marginBottom),
        };
        foreach (IElement item in items)
        {
            row.Add(item, 1);
        }

        return row;
    }

    /// <summary>A bordered panel with a heading - terms, notes, declarations.</summary>
    private static Container Panel(string title, IEnumerable<IElement> children, Color? accent = null)
    {
        VerticalStack body = new()
        {
            Spacing = 4
        };
        body.Add(Cell(title, size: 9.5f, bold: true, color: accent ?? Accent, marginBottom: 4f));
        foreach (IElement child in children)
        {
            body.Add(child);
        }

        return new Container
        {
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 6,
            Padding = new Thickness(12),
            Margin = new Thickness(0, 4, 0, 12),
            Child = body,
        };
    }

    /// <summary>The running header / footer band.</summary>
    private static HorizontalStack Band(string left, string right)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted
        };
        row.Add(Cell(left, size: 7.5f, color: Muted), 2.5f);
        row.Add(Cell(right, size: 7.5f, color: Muted, align: TextAlignment.Right), 2);
        return row;
    }

    private static string GbDate(DateTime date) => date.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("en-GB"));
}
