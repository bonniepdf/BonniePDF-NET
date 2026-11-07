using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.ComplianceAudit.BoardMinutes;

/// <summary>
/// Minutes are the one document a company is expected to be able to produce
/// years later, so this template is built for the archive rather than the
/// screen: it claims PDF/A-4, the archival profile for PDF 2.0, and nothing
/// about it depends on the viewer (the fonts are embedded, the colours are
/// device-independent, and the page numbers are part of the page).
///
/// The figures in the summary are derived from the record rather than typed
/// beside it. Quorum is a majority of the directors, a resolution passes when
/// more directors vote for it than against it, and the tiles count what the
/// tables below list, so they cannot disagree.
///
/// Every company, name, and figure in it is fictional.
/// </summary>
[System.ComponentModel.DisplayName("Board minutes")]
[System.ComponentModel.Category("Compliance & Audit")]
[System.ComponentModel.Description(
    "The minutes of a board meeting, built to be kept for years: attendance and quorum, numbered agenda items with their resolutions, a vote table whose outcomes are worked out from the votes, an action register, and sign-off by the Chair and the Secretary. PDF/A-4.")]
public static class BoardMinutesExample
{
    private const string Company = "Sample Holdings plc";
    private const string Number = "07/2026";
    private const string Time = "10:00 to 12:20";
    private const string Place = "1 Exemplar Street, Sample City (boardroom) and by video";

    private static readonly DateTime MeetingDate = new(2026, 10, 14);
    private static readonly DateTime Circulated = new(2026, 10, 21);

    private sealed record Director(string Name, string Role, bool Present);

    private sealed record Resolution(string Text, int For, int Against, int Abstain)
    {
        /// <summary>More directors for than against.</summary>
        public bool IsPassed => For > Against;
    }

    private sealed record AgendaItem(string Title, string Discussion, int? Resolution = null);

    private sealed record ActionItem(string Action, string Owner, DateTime Due);

    private static readonly Director[] Directors =
    [
        new("Morgan Example", "Chair", true),
        new("Riley Sample", "Chief Executive", true),
        new("Jordan Placeholder", "Finance Director", true),
        new("Casey Illustrative", "Non-executive Director", true),
        new("Taylor Demonstration", "Non-executive Director", true),
        new("Alex Specimen", "Non-executive Director", false),
        new("Sam Notional", "Non-executive Director", false),
    ];

    private static readonly Resolution[] Resolutions =
    [
        new("Approve the minutes of the meeting held on 9 September 2026.", 5, 0, 0),
        new("Approve the capital budget for the year to 30 June 2027 of £4.2m.", 4, 1, 0),
        new("Appoint Sample Audit LLP as external auditor for the year to 30 June 2027.", 5, 0, 0),
        new("Approve the lease of the Northern depot on the terms tabled.", 2, 2, 1),
    ];

    private static readonly AgendaItem[] Agenda =
    [
        new("Apologies, quorum, and conflicts of interest",
            "Apologies were received from Alex Specimen and Sam Notional. The Secretary confirmed that a quorum was present. No director declared an interest in any item of business."),
        new("Minutes of the previous meeting",
            "The minutes of the meeting held on 9 September 2026 were taken as read and agreed as a true record.", 0),
        new("Capital budget",
            "The Finance Director presented the capital budget, which is £0.3m above the current year, mainly for the warehouse automation project. One director asked for the payback period of each project to be reported at every meeting. The Chair noted the request and the budget was put to the vote.", 1),
        new("External auditor",
            "The Audit Committee recommended reappointing Sample Audit LLP. Fees are unchanged in real terms and the audit partner will rotate at the end of the year.", 2),
        new("Northern depot lease",
            "The Chief Executive tabled the proposed ten-year lease. Directors were divided on the break clause and on the rent review. The vote was tied, the Chair did not use a casting vote, and the matter was deferred to the next meeting with revised terms.", 3),
        new("Any other business", "There was no other business. The Chair closed the meeting at 12:20."),
    ];

    private static readonly ActionItem[] Actions =
    [
        new("Report the payback period of each capital project at every meeting.", "Jordan Placeholder", new(2026, 11, 18)),
        new("Return to the board with revised lease terms for the Northern depot.", "Riley Sample", new(2026, 11, 18)),
        new("Send the engagement letter to Sample Audit LLP.", "Company Secretary", new(2026, 10, 30)),
        new("Circulate the signed minutes to all directors.", "Company Secretary", new(2026, 10, 28)),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        int present = Directors.Count(d => d.Present);
        int quorum = Directors.Length / 2 + 1;
        int passed = Resolutions.Count(r => r.IsPassed);

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 46 });
        doc.Settings.Conformance = PdfAConformance.PdfA4;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"{Company} · Board meeting {Number} · minutes, circulated {GbDate(Circulated)}",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Minutes of board meeting {Number} — {Company}";
        doc.Metadata.Author = Company;
        doc.Metadata.Subject = $"Minutes of the board meeting held on {GbDate(MeetingDate)}: attendance, resolutions, votes, and actions.";
        doc.Metadata.Keywords = "board minutes, resolutions, governance, company records, archive, pdf/a-4";

        doc.Add(AccentBar());
        doc.Add(Cell("BOARD OF DIRECTORS", size: 8.5f, bold: true, color: Accent, marginBottom: 4f));
        doc.Add(H1($"Minutes of board meeting {Number}"));
        doc.Add(Cell($"{Company} · {GbDate(MeetingDate)} · {Time}", color: Muted, marginBottom: 2f));
        doc.Add(Cell(Place, color: Muted, marginBottom: 14f));

        doc.Add(WeightedRow(
            [
                Tile($"{present} of {Directors.Length}", "Directors present"),
                Tile(present >= quorum ? "Met" : "Not met", $"Quorum ({quorum} directors)"),
                Tile($"{passed} of {Resolutions.Length}", "Resolutions passed"),
                Tile(Actions.Length.ToString(CultureInfo.InvariantCulture), "Actions raised"),
            ],
            spacing: 10f,
            marginBottom: 4f));

        doc.Add(H2("Attendance"));
        Table attendance = ReportTable(2.4f, 2.4f, 1.6f);
        attendance.AddHeader(Th("Director"), Th("Role"), Th("Attendance"));
        foreach (Director d in Directors)
        {
            attendance.AddRow(Td(d.Name), Td(d.Role, color: Muted), Td(d.Present ? "Present" : "Apologies", bold: d.Present));
        }

        doc.Add(attendance);
        doc.Add(Cell("In attendance: the Company Secretary, who took the minutes.", size: 8.5f, color: Muted, marginTop: 6f));

        doc.Add(H2("Business"));
        for (int i = 0; i < Agenda.Length; i++)
        {
            AgendaItem item = Agenda[i];

            // Item 4 starts the second page so its resolution is not parted from its heading.
            if (i == 3)
            {
                doc.Add(new PageBreak());
            }

            doc.Add(AgendaHeading(i + 1, item.Title));
            doc.Add(Cell(item.Discussion, size: 9f, marginBottom: 4f));
            if (item.Resolution is int r)
            {
                doc.Add(ResolutionBox(r));
            }
        }

        doc.Add(H2("Votes"));
        Table votes = ReportTable(0.5f, 4.4f, 0.8f, 0.9f, 0.9f, 1.1f);
        votes.AddHeader(Th("No."), Th("Resolution"), Th("For", TextAlignment.Right), Th("Against", TextAlignment.Right), Th("Abstain", TextAlignment.Right), Th("Outcome"));
        for (int i = 0; i < Resolutions.Length; i++)
        {
            Resolution r = Resolutions[i];
            votes.AddRow(
                Td($"R{i + 1}", color: Muted),
                Td(r.Text),
                Td(r.For.ToString(CultureInfo.InvariantCulture), align: TextAlignment.Right),
                Td(r.Against.ToString(CultureInfo.InvariantCulture), align: TextAlignment.Right),
                Td(r.Abstain.ToString(CultureInfo.InvariantCulture), align: TextAlignment.Right),
                Td(r.IsPassed ? "Passed" : "Not passed", bold: true, color: r.IsPassed ? Passed : Failed));
        }

        doc.Add(votes);
        doc.Add(Cell("A resolution passes when more directors vote for it than against it. Abstentions are not counted either way.", size: 8.5f, color: Muted, marginTop: 6f));

        doc.Add(H2("Actions"));
        Table actions = ReportTable(0.5f, 4.4f, 1.9f, 1.3f);
        actions.AddHeader(Th("No."), Th("Action"), Th("Owner"), Th("Due"));
        for (int i = 0; i < Actions.Length; i++)
        {
            actions.AddRow(Td($"A{i + 1}", color: Muted), Td(Actions[i].Action), Td(Actions[i].Owner), Td(GbDate(Actions[i].Due)));
        }

        doc.Add(actions);

        doc.Add(H2("Approval"));
        doc.Add(Cell(
            "These minutes are a permanent record of the meeting. Once signed, the Company Secretary keeps them with the company records for at least ten years.",
            size: 9f,
            color: Muted,
            marginBottom: 8f));
        doc.Add(WeightedRow(
            [Slot("Chair", "Morgan Example"), Slot("Company Secretary", "Name"), Slot("Date", "Signed on")],
            spacing: 22f,
            marginBottom: 6f));

        return doc;
    }

    private static Table ReportTable(params float[] columns) => new(columns)
    {
        CellPadding = 6,
        Border = new Border(0.5f),
        BorderColors = new BorderColors(Rule),
        HeaderBackgroundColor = Accent,
        HeaderTextColor = Color.White,
    };

    private static Heading AgendaHeading(int number, string title) => new($"{number}. {title}")
    {
        Level = 3,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 10.5f,
        Color = Heading,
        Margin = new Thickness(0, 8, 0, 4),
    };

    /// <summary>The resolution that closed an item, set apart so it is easy to find later.</summary>
    private static Container ResolutionBox(int index)
    {
        Resolution r = Resolutions[index];
        string outcome = r.IsPassed ? "Passed" : "Not passed";
        VerticalStack body = new();
        body.Add(Cell(
            $"RESOLUTION R{index + 1} · {outcome.ToUpperInvariant()} ({r.For} for, {r.Against} against, {r.Abstain} abstained)",
            size: 7f,
            bold: true,
            color: r.IsPassed ? Passed : Failed,
            marginBottom: 3f));
        body.Add(Cell(r.Text, size: 9f));
        return new Container
        {
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 4,
            Padding = new Thickness(7, 8),
            Margin = new Thickness(0, 0, 0, 4),
            Child = body,
        };
    }

    private static VerticalStack Slot(string role, string hint)
    {
        VerticalStack stack = new();
        stack.Add(Cell(role.ToUpperInvariant(), size: 6.5f, bold: true, color: Muted, marginBottom: 20f));
        stack.Add(new Container
        {
            BackgroundColor = Rule,
            Height = 0.75f,
            Margin = new Thickness(0, 0, 0, 5),
        });
        stack.Add(Cell(hint, size: 8f, color: Muted));
        return stack;
    }

    // Local helpers and sample data for this document.
    private static readonly Color Ink = ColorPalette.Slate[700];

    /// <summary>600, not 400: 400 never clears 4.5:1 as body text on white.</summary>
    private static readonly Color Muted = ColorPalette.Slate[600];

    private static readonly Color Heading = ColorPalette.Slate[800];

    private static readonly Color Accent = ColorPalette.Slate[700];

    private static readonly Color Rule = ColorPalette.Slate[200];

    private static readonly Color Passed = ColorPalette.Green[700];

    private static readonly Color Failed = ColorPalette.Red[700];

    /// <summary>A styled text cell with document defaults.</summary>
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

    private static Heading H2(string text, Color? color = null) => new(text)
    {
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 13,
        Color = color ?? Accent,
        Margin = new Thickness(0, 16, 0, 8),
    };

    /// <summary>The full-width accent rule at the top of each document.</summary>
    private static Container AccentBar(Color? color = null, float height = 4f) => new()
    {
        BackgroundColor = color ?? Accent,
        Height = height,
        Margin = new Thickness(0, 0, 0, 14),
    };

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

    /// <summary>A summary tile.</summary>
    private static Container Tile(string value, string label, Color? accent = null)
    {
        VerticalStack body = new();
        body.Add(Cell(value, size: 14f, bold: true, color: accent ?? Accent, marginBottom: 2f));
        body.Add(Cell(label, size: 8f, color: Muted));
        return new Container
        {
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 6,
            Padding = new Thickness(11, 10),
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
