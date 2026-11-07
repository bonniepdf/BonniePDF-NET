using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.FieldServiceProperty.TenancyAgreement;

/// <summary>
/// The longest ordinary contract most people ever sign, and the one they are
/// least likely to have read. The job of the layout is to make it readable: the
/// terms a tenant actually asks about (who, where, how long, how much, what
/// deposit) come first in a grid; the clauses are numbered so "clause 5.2" means
/// one thing in an argument; and the schedule at the end says when every payment
/// falls due, so nobody has to count months.
///
/// The numbers are derived rather than typed. The deposit is five weeks' rent,
/// worked out from the annual rent in whole pence and rounded down, which is the
/// cap for a tenancy of this size; the schedule is twelve payments generated
/// from the start date.
///
/// This is a sample for layout purposes, not legal advice, and every name and
/// address in it is fictional.
/// </summary>
[System.ComponentModel.DisplayName("Tenancy agreement")]
[System.ComponentModel.Category("Field Service & Property")]
[System.ComponentModel.Description(
    "An assured shorthold tenancy agreement: key terms up front, numbered clauses, a deposit calculated from the rent, a twelve-month rent schedule, and signature blocks for landlord, tenants, and witnesses. PDF/A-2a + PDF/UA-1.")]
public static class TenancyAgreementExample
{
    private const string Reference = "AST-2026-0731";
    private const string Landlord = "Sample Property Holdings Ltd";
    private const int Months = 12;

    /// <summary>Pence per calendar month.</summary>
    private const int Rent = 125_000;

    private const int DueDay = 1;

    private static readonly string[] LandlordAddress = ["1 Exemplar Street", "Sample City", "SC1 4AB"];

    private static readonly string[] Tenants = ["Alex Sample", "Jordan Example"];

    private static readonly string[] Property = ["Flat 3, 22 Example Road", "Sample Town", "ST2 6PQ"];

    private static readonly DateTime Start = new(2026, 11, 1);
    private static readonly DateTime Signed = new(2026, 10, 20);

    private sealed record ClauseBlock(int Number, string Title, string[] Items);

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        int annual = Rent * 12;

        // Five weeks of the annual rent, whole pence, rounded down.
        int deposit = annual * 5 / 52;
        DateTime end = Start.AddMonths(Months).AddDays(-1);

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 46 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.FooterTemplate = pageNumber => Band(
            $"Tenancy agreement {Reference} · sample document, not legal advice",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Tenancy agreement {Reference} — {Property[0]}";
        doc.Metadata.Author = Landlord;
        doc.Metadata.Subject =
            "Assured shorthold tenancy agreement: key terms, numbered clauses, deposit, rent schedule, and signatures (sample document, not legal advice).";
        doc.Metadata.Keywords = "tenancy agreement, assured shorthold tenancy, lease, rent schedule, deposit, property, pdf/ua, pdf/a";

        doc.Add(AccentBar());
        doc.Add(Cell("ASSURED SHORTHOLD TENANCY", size: 8.5f, bold: true, color: Accent, marginBottom: 4f));
        doc.Add(H1("Tenancy agreement"));
        doc.Add(Cell($"{Reference} · {string.Join(", ", Property)}", color: Muted, marginBottom: 14f));

        doc.Add(WeightedRow(
            [
                Tile(Money(Rent), "Rent per month"),
                Tile(Money(deposit), "Deposit"),
                Tile($"{Months} months", "Fixed term"),
                Tile(OrdinalDay(DueDay), "Rent due each month"),
            ],
            spacing: 10f,
            marginBottom: 8f));

        doc.Add(WeightedRow(
            [
                PartyBlock("Landlord", [Landlord, .. LandlordAddress]),
                PartyBlock("Tenants", Tenants),
                PartyBlock("Property", Property),
            ],
            spacing: 20f,
            marginBottom: 4f));
        doc.Add(WeightedRow(
            [
                Field("Start date", GbDate(Start)),
                Field("End date", GbDate(end)),
                Field("Signed on", GbDate(Signed)),
                Field("Annual rent", Money(annual)),
            ],
            marginBottom: 4f));

        doc.Add(H2("Agreed terms"));
        foreach (ClauseBlock c in Clauses(deposit, end))
        {
            doc.Add(ClauseHeading(c.Number, c.Title));
            for (int i = 0; i < c.Items.Length; i++)
            {
                doc.Add(SubClause($"{c.Number}.{i + 1}", c.Items[i]));
            }
        }

        doc.Add(H2("Schedule: rent payments"));
        doc.Add(Cell(
            $"Twelve payments of {Money(Rent)}, each due on the {OrdinalDay(DueDay)} of the month in advance. "
            + $"The total over the fixed term is {Money(annual)}.",
            size: 9f,
            color: Muted,
            marginBottom: 8f));
        Table schedule = new(0.7f, 2.2f, 2.2f, 1.6f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        schedule.AddHeader(Th("No."), Th("Due date"), Th("Rent period"), Th("Amount", TextAlignment.Right));
        for (int i = 0; i < Months; i++)
        {
            DateTime due = Start.AddMonths(i);
            DateTime periodEnd = due.AddMonths(1).AddDays(-1);
            schedule.AddRow(
                Td((i + 1).ToString(CultureInfo.InvariantCulture), color: Muted),
                Td(GbDate(due)),
                Td($"{GbDate(due)} – {GbDate(periodEnd)}", color: Muted),
                Td(Money(Rent), align: TextAlignment.Right));
        }

        schedule.AddRow(Td(string.Empty), Td("Total", bold: true), Td(string.Empty), Td(Money(annual), bold: true, align: TextAlignment.Right));
        doc.Add(schedule);

        doc.Add(H2("Signed by the parties"));
        doc.Add(Cell(
            "By signing, each party confirms they have read and agree to the terms above. Tenants sign in the presence of a witness.",
            size: 9f,
            color: Muted,
            marginBottom: 8f));
        doc.Add(WeightedRow(
            [
                Slot("Landlord", Landlord),
                Slot("Tenant", Tenants[0]),
                Slot("Tenant", Tenants[1]),
            ],
            spacing: 22f,
            marginBottom: 10f));
        doc.Add(WeightedRow(
            [Slot("Witness", "Name and address"), Slot("Witness", "Name and address"), Slot("Date", "Signed on")],
            spacing: 22f,
            marginBottom: 6f));

        return doc;
    }

    /// <summary>The clauses of the agreement. Every figure in them comes from the tenancy data.</summary>
    private static ClauseBlock[] Clauses(int deposit, DateTime end)
    {
        string tenants = string.Join(" and ", Tenants);
        string propertyLine = string.Join(", ", Property);
        return
        [
            new(1, "Parties and property",
            [
                $"This agreement is made between {Landlord} (the Landlord) and {tenants} (together the Tenant).",
                $"The Landlord lets and the Tenant takes the property at {propertyLine} (the Property), with the fixtures and fittings listed in the signed inventory.",
            ]),
            new(2, "Term",
            [
                $"The tenancy starts on {GbDate(Start)} and continues for a fixed term of {Months} months, ending on {GbDate(end)}.",
                "If both parties stay on after the fixed term without signing a new agreement, the tenancy continues month to month on the same terms.",
            ]),
            new(3, "Rent",
            [
                $"The Tenant pays {Money(Rent)} a month, in advance, on the {OrdinalDay(DueDay)} day of each month, as set out in the schedule.",
                "Rent is paid by bank transfer to the account the Landlord names in writing. The Landlord will not charge any fee for late payment other than interest on rent that is more than 14 days overdue.",
                "The rent stays the same for the fixed term. The Landlord may propose a new rent for any period after it, in writing.",
            ]),
            new(4, "Deposit",
            [
                $"The Tenant has paid a deposit of {Money(deposit)}, which is five weeks' rent. The Landlord will protect it in a government-approved scheme within 30 days of receiving it and give the Tenant the scheme's details.",
                "The deposit is returned within 10 days of the end of the tenancy, less any amount agreed or decided to be owed for damage beyond fair wear and tear, unpaid rent, or unpaid bills.",
            ]),
            new(5, "The Tenant's responsibilities",
            [
                "Keep the Property clean and in good condition, and tell the Landlord promptly about any repair that is needed.",
                "Not sublet the Property or let anyone else live there without the Landlord’s written consent, and not use it for any business.",
                "Allow the Landlord or their agent to enter to inspect or repair the Property, on at least 24 hours’ written notice except in an emergency.",
            ]),
            new(6, "The Landlord's responsibilities",
            [
                "Keep the structure, exterior, and the installations for water, gas, electricity, and heating in good repair and working order.",
                "Make sure the Property meets the safety requirements that apply to it, including gas, electrical, and fire safety, and give the Tenant copies of the certificates.",
                "Let the Tenant live in the Property undisturbed for the whole term, as long as the Tenant keeps to this agreement.",
            ]),
            new(7, "Ending the tenancy",
            [
                "The Tenant may end the tenancy at the end of the fixed term by giving the Landlord at least one month’s written notice before it ends.",
                "The Landlord can only bring the tenancy to an end by following the legal process, which includes giving the notice the law requires.",
            ]),
        ];
    }

    /// <summary>Pence as pounds: £1,234.56.</summary>
    private static string Money(int pence) => NumberFormatter.Format(pence / 100m, "N2", "£");

    /// <summary>1 → 1st, 2 → 2nd, 3 → 3rd, 4 → 4th … for the days of a month.</summary>
    private static string OrdinalDay(int day)
    {
        string suffix = day % 10 == 1 && day != 11 ? "st"
            : day % 10 == 2 && day != 12 ? "nd"
            : day % 10 == 3 && day != 13 ? "rd"
            : "th";
        return $"{day}{suffix}";
    }

    private static Heading ClauseHeading(int number, string title) => new($"{number}. {title}")
    {
        Level = 3,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 10.5f,
        Color = Heading,
        Margin = new Thickness(0, 8, 0, 4),
    };

    /// <summary>A numbered sub-clause: the number hangs in its own column so wrapped lines align with the text.</summary>
    private static HorizontalStack SubClause(string number, string text)
    {
        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 6,
            Margin = new Thickness(0, 0, 0, 4),
        };
        row.Add(Cell(number, size: 9f, color: Muted), 0.5f);
        row.Add(Cell(text, size: 9f), 9f);
        return row;
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

    private static readonly Color Accent = ColorPalette.Cyan[700];

    private static readonly Color Rule = ColorPalette.Slate[200];

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

    /// <summary>A labelled address block.</summary>
    private static VerticalStack PartyBlock(string label, IEnumerable<string> address)
    {
        VerticalStack stack = new();
        stack.Add(Cell(label.ToUpperInvariant(), size: 7f, bold: true, color: Muted, marginBottom: 3f));
        stack.Add(AddressLines(address, size: 9f));
        return stack;
    }

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

    private static TextBlock AddressLines(IEnumerable<string> items, float size = 9f) => Cell(string.Join('\n', items), size: size);
}
