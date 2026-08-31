using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Lists;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.EmploymentContract;

/// <summary>
/// A multi-page written statement of employment particulars. Where the payslip
/// is a dense one-page grid, this is the opposite problem: long-form text that
/// has to stay navigable and citable across pages. The techniques are the ones
/// legal drafting actually relies on — hierarchical clause numbering, a
/// document outline so a reader can jump to a clause rather than scroll, a
/// running header carrying the agreement reference on every page, a
/// defined-terms section, a schedule forced onto its own page, and an execution
/// block with signature and witness fields. Output is PDF/A-2a + PDF/UA-1.
/// </summary>
[System.ComponentModel.DisplayName("Employment Contract")]
[System.ComponentModel.Category("HR")]
[System.ComponentModel.Description("A multi-page written statement of employment particulars: numbered clauses with indented sub-clauses, a defined-terms section, a running header, a schedule on its own page, and an execution block with signature and witness fields. Carries a document outline so a reader can jump to a clause. PDF/A-2a + PDF/UA-1.")]
public static class EmploymentContractExample
{
    // ── Design tokens ─────────────────────────────────────────────────────────
    private static readonly Color Ink = ColorPalette.Slate[800];
    private static readonly Color Muted = ColorPalette.Slate[500];
    private static readonly Color Faint = ColorPalette.Slate[400];
    private static readonly Color Line = ColorPalette.Slate[200];
    private static readonly Color Panel = ColorPalette.Slate[50];
    private static readonly Color Accent = ColorPalette.Teal[700];

    // ── Deterministic sample fixture ──────────────────────────────────────────
    private static readonly ContractData Sample = new(
        new Employer("Sample Company Ltd", "12 Harbour Square, Bristol BS1 4QA, United Kingdom", "Company No. 09876543"),
        new Employee("Sample Employee", "48 Sample Road, Bristol BS6 6DR, United Kingdom"),
        new Role("Senior Platform Engineer", "Engineering", "Head of Platform", "1 October 2026"),
        new Terms(
            "£72,000 per annum, paid monthly in arrears",
            "37.5 hours per week, Monday to Friday",
            "28 days per year, inclusive of public holidays",
            "Six months from the start date",
            "Three months by either party after probation",
            "Bristol office, with hybrid working by agreement"),
        Reference: "CON-2026-0188",
        Definitions:
        [
            new Definition("Commencement Date", "The date shown in the particulars above, on which employment begins."),
            new Definition("Confidential Information", "Information not in the public domain relating to the business, clients, or systems of the Employer."),
            new Definition("Garden Leave", "A period during notice in which the Employee remains employed but is not required to attend work."),
            new Definition("Intellectual Property", "Inventions, designs, code, and works of authorship created in the course of employment."),
        ],
        Clauses:
        [
            new Clause("Appointment and duties",
            [
                "The Employee shall perform the duties reasonably associated with the role, and any other duties reasonably assigned from time to time.",
                "The Employee shall devote their full working time and attention to the business of the Employer during working hours.",
                "The Employee shall not, without prior written consent, engage in any other paid occupation that conflicts with the role.",
            ], Intro: "The Employer appoints the Employee to the role set out in the particulars above."),
            new Clause("Probationary period",
            [
                "The first six months of employment are probationary, during which either party may terminate on two weeks written notice.",
                "The Employer may extend the probationary period once, by up to three months, on written notice before it expires.",
                "Confirmation of appointment shall be given in writing on satisfactory completion.",
            ]),
            new Clause("Remuneration and benefits",
            [
                "Salary is paid monthly in arrears by credit transfer, subject to deductions required by law.",
                "Salary is reviewed annually. A review does not imply an increase.",
                "The Employee is eligible to join the Employer pension scheme, subject to its rules from time to time.",
            ]),
            new Clause("Confidentiality and intellectual property",
            [
                "The Employee shall not disclose Confidential Information to any third party except as required to perform the role or by law.",
                "All Intellectual Property created in the course of employment vests in the Employer on creation.",
                "On termination the Employee shall return all property and materials belonging to the Employer.",
            ], Intro: "These obligations continue after employment ends."),
            new Clause("Termination",
            [
                "After probation, either party may terminate on three months written notice.",
                "The Employer may place the Employee on Garden Leave for all or part of any notice period.",
                "The Employer may terminate without notice in cases of gross misconduct.",
            ]),
        ],
        // Verified against the rendered sample: clauses 1-2 sit on page 1,
        // clauses 3-5 on page 2, and the page break puts the schedule and
        // execution on page 3.
        Outline: new OutlinePages(Particulars: 0, Definitions: 0, Clauses: [0, 0, 1, 1, 1], Schedule: 2, Execution: 2),
        Locale: "en-GB");

    public static Document Generate() => BuildDocument(Sample, CreateSettings(Sample));

    /// <summary>Base page and document settings, including the outline.</summary>
    public static DocumentSettings CreateSettings(ContractData data)
    {
        DocumentSettings settings = new(PdfSize.A4, PageOrientation.Portrait, 46)
        {
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            // A running header: on a document that will be printed, filed, and
            // referred back to, every page needs to say which agreement it is.
            HeaderTemplate = _ => new TextBlock
            {
                Text = $"{data.Reference} · {data.Employer.Name} and {data.Employee.Name}",
                FontName = StandardFonts.Helvetica,
                FontSize = 7f,
                TextColor = Faint,
            },
            FooterTemplate = pageNumber => new TextBlock
            {
                Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken}",
                FontName = StandardFonts.Helvetica,
                FontSize = 7f,
                TextColor = Faint,
                Alignment = TextAlignment.Center,
            },
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);

        // The outline. A reader opening a five-clause agreement should be able
        // to jump to "Termination" rather than scroll for it. Pagination is
        // resolved at render time, not while the element tree is built, so a
        // bookmark cannot discover its own page — the outline has to be told.
        OutlinePages pages = data.Outline;
        BookmarkItem clauses = new() { Title = "Clauses", PageIndex = pages.Clauses.Length > 0 ? pages.Clauses[0] : 0 };
        for (int i = 0; i < data.Clauses.Length; i++)
        {
            clauses.Children.Add(new BookmarkItem { Title = $"{i + 1}. {data.Clauses[i].Heading}", PageIndex = pages.Clauses[i] });
        }

        settings.Bookmarks.Add(new BookmarkItem { Title = "Particulars", PageIndex = pages.Particulars });
        settings.Bookmarks.Add(new BookmarkItem { Title = "Definitions", PageIndex = pages.Definitions });
        settings.Bookmarks.Add(clauses);
        settings.Bookmarks.Add(new BookmarkItem { Title = "Schedule 1 — Benefits and allowances", PageIndex = pages.Schedule });
        settings.Bookmarks.Add(new BookmarkItem { Title = "Execution", PageIndex = pages.Execution });

        return settings;
    }

    /// <summary>Pure builder: the same data always produces the same document.</summary>
    public static Document BuildDocument(ContractData data, DocumentSettings settings)
    {
        Document doc = new(settings);
        doc.Metadata.Title = $"Statement of Employment Particulars — {data.Employee.Name}";
        doc.Metadata.Author = data.Employer.Name;
        doc.Metadata.Subject = "Written statement of employment particulars (fictional sample data).";

        doc.Add(TitleBlock(data));
        doc.Add(Parties(data));
        doc.Add(Particulars(data));
        doc.Add(Definitions(data));
        for (int i = 0; i < data.Clauses.Length; i++)
        {
            doc.Add(ClauseSection(data.Clauses[i], i));
        }
        doc.Add(new PageBreak());
        doc.Add(Schedule(data));
        doc.Add(Execution(data));
        return doc;
    }

    // ── Section builders (top to bottom) ──────────────────────────────────────

    private static VerticalStack TitleBlock(ContractData data)
    {
        // A real level-1 heading, not styled body text: the clause headings
        // below are level 2, and PDF/UA-1 requires headings to nest without
        // skipping a level, so the title has to be the H1 they sit under.
        Heading title = new("Statement of Employment Particulars")
        {
            Level = 1,
            TocIdentifier = "title",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 17f,
            TextColor = Accent,
        };

        TextBlock reference = Text($"Agreement reference {data.Reference}", size: 8.5f, color: Muted);
        reference.Margin = new Thickness(0, 3, 0, 0);

        VerticalStack stack = new();
        stack.Add(title);
        stack.Add(reference);
        stack.Add(new Container { Height = 2, BackgroundColor = Accent, Margin = new Thickness(0, 10, 0, 14) });
        return stack;
    }

    private static Container Parties(ContractData data)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 20 };
        row.Add(Party("The Employer", data.Employer.Name, data.Employer.Address, data.Employer.Registration), 1);
        row.Add(Party("The Employee", data.Employee.Name, data.Employee.Address, null), 1);

        return new Container
        {
            BackgroundColor = Panel,
            Padding = new Thickness(12, 12, 12, 12),
            Margin = new Thickness(0, 0, 0, 14),
            Child = row,
        };
    }

    private static VerticalStack Party(string role, string name, string address, string? extra)
    {
        VerticalStack stack = new() { Spacing = 2 };
        stack.Add(Text(role.ToUpperInvariant(), bold: true, size: 6.5f, color: Faint));
        stack.Add(Text(name, bold: true, size: 10));
        stack.Add(Text(address, size: 8.5f, color: Muted));
        if (extra is not null)
        {
            stack.Add(Text(extra, size: 8.5f, color: Faint));
        }
        return stack;
    }

    private static VerticalStack Particulars(ContractData data)
    {
        (string, string)[] rows =
        [
            ("Job title", data.Role.Title),
            ("Department", data.Role.Department),
            ("Reports to", data.Role.ReportsTo),
            ("Commencement Date", data.Role.StartDate),
            ("Place of work", data.Terms.Location),
            ("Salary", data.Terms.Salary),
            ("Hours of work", data.Terms.Hours),
            ("Holiday entitlement", data.Terms.Holiday),
            ("Probationary period", data.Terms.Probation),
            ("Notice period", data.Terms.Notice),
        ];

        VerticalStack stack = new() { Margin = new Thickness(0, 0, 0, 16) };
        stack.Add(LabelledTable(rows, "Particular", "Detail"));
        return stack;
    }

    private static VerticalStack Definitions(ContractData data)
    {
        VerticalStack stack = new() { Margin = new Thickness(0, 0, 0, 14) };
        stack.Add(SectionHeading("Definitions", "definitions"));
        foreach (Definition definition in data.Definitions)
        {
            VerticalStack entry = new() { Spacing = 1, Margin = new Thickness(0, 0, 0, 6) };
            entry.Add(Text(definition.Term, bold: true, size: 9));
            entry.Add(Text(definition.Meaning, size: 8.5f, color: Muted));
            stack.Add(entry);
        }
        return stack;
    }

    /// <summary>
    /// One numbered clause: a bookmarked heading, optional lead-in prose, then
    /// the sub-clauses as an indented numbered list. Rendering sub-clauses as a
    /// real list rather than hand-numbered paragraphs is what keeps "4.2" stable
    /// when a clause is inserted above it.
    /// </summary>
    private static VerticalStack ClauseSection(Clause item, int index)
    {
        VerticalStack stack = new() { Margin = new Thickness(0, 0, 0, 14) };
        stack.Add(SectionHeading($"{index + 1}.  {item.Heading}", $"clause-{index + 1}"));

        if (item.Intro is not null)
        {
            TextBlock intro = Text(item.Intro);
            intro.Margin = new Thickness(0, 0, 0, 6);
            stack.Add(intro);
        }

        OrderedList list = new()
        {
            NumberFormat = NumberFormat.Decimal,
            FontName = StandardFonts.Helvetica,
            FontSize = 9.5f,
            TextColor = Ink,
            ItemSpacing = 5,
            Indent = 16,
        };
        foreach (string paragraph in item.Paragraphs)
        {
            list.AddItem(paragraph);
        }
        stack.Add(list);
        return stack;
    }

    /// <summary>The schedule lives on its own page, as schedules conventionally do.</summary>
    private static VerticalStack Schedule(ContractData data)
    {
        (string, string)[] rows =
        [
            ("Annual leave year", "1 January to 31 December"),
            ("Pension contribution", "Employer 5%, Employee 5% of qualifying earnings"),
            ("Expenses", "Reimbursed monthly against receipts, per the expenses policy"),
            ("Equipment", "Laptop and peripherals provided; returned on termination"),
            ("Training budget", "£1,500 per year, subject to prior approval"),
        ];

        TextBlock intro = Text($"This schedule forms part of the agreement between {data.Employer.Name} and {data.Employee.Name}.", size: 8.5f, color: Muted);
        intro.Margin = new Thickness(0, 0, 0, 8);

        VerticalStack stack = new();
        stack.Add(SectionHeading("Schedule 1 — Benefits and allowances", "schedule-1"));
        stack.Add(intro);
        stack.Add(LabelledTable(rows, "Item", "Provision"));
        return stack;
    }

    private static VerticalStack Execution(ContractData data)
    {
        TextBlock intro = Text("By signing below the parties agree to the terms set out in this statement and its schedules.");
        intro.Margin = new Thickness(0, 0, 0, 6);

        VerticalStack stack = new() { Margin = new Thickness(0, 8, 0, 0) };
        stack.Add(SectionHeading("Execution", "execution"));
        stack.Add(intro);
        stack.Add(SignaturePair($"Signed for and on behalf of {data.Employer.Name}", "Date"));
        stack.Add(new Spacer(6));
        stack.Add(SignaturePair($"Signed by {data.Employee.Name}", "Date"));
        stack.Add(new Spacer(6));
        stack.Add(SignaturePair("Witness signature", "Witness name"));
        return stack;
    }

    // ── Small helpers ─────────────────────────────────────────────────────────

    private static HorizontalStack SignaturePair(string left, string right)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 20 };
        row.Add(SignatureField(left), 1.6f);
        row.Add(SignatureField(right), 1f);
        return row;
    }

    /// <summary>A ruled line with its caption underneath — one execution field.</summary>
    private static VerticalStack SignatureField(string caption)
    {
        VerticalStack stack = new() { Spacing = 4 };
        stack.Add(new Spacer(24));
        stack.Add(new Line { Thickness = 0.75f, Color = Faint });
        stack.Add(Text(caption, size: 7.5f, color: Muted));
        return stack;
    }

    private static Table LabelledTable((string Label, string Value)[] rows, string leftHead, string rightHead)
    {
        Table table = new(1f, 2.2f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Line),
            HeaderBackgroundColor = Panel,
        };
        table.AddHeader(ColumnHead(leftHead), ColumnHead(rightHead));
        foreach ((string label, string value) in rows)
        {
            table.AddRow(Text(label, bold: true, size: 9), Text(value));
        }
        return table;
    }

    /// <summary>A bookmarked clause heading. TocIdentifier is what the outline targets.</summary>
    private static Heading SectionHeading(string text, string id) => new(text)
    {
        Level = 2,
        TocIdentifier = id,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 11.5f,
        TextColor = Accent,
        Margin = new Thickness(0, 4, 0, 6),
    };

    private static TextBlock ColumnHead(string text) => Text(text, bold: true, size: 7.5f, color: Muted);

    private static TextBlock Text(string text, bool bold = false, float size = 9.5f, Color? color = null, TextAlignment align = TextAlignment.Left) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = size,
        TextColor = color ?? Ink,
        Alignment = align,
    };

    // ── Domain model (the data-to-document boundary) ──────────────────────────

    public sealed record Employer(string Name, string Address, string Registration);

    public sealed record Employee(string Name, string Address);

    public sealed record Role(string Title, string Department, string ReportsTo, string StartDate);

    public sealed record Terms(string Salary, string Hours, string Holiday, string Probation, string Notice, string Location);

    public sealed record Definition(string Term, string Meaning);

    /// <summary>One numbered clause. <paramref name="Paragraphs"/> are the indented sub-clauses.</summary>
    public sealed record Clause(string Heading, string[] Paragraphs, string? Intro = null);

    /// <summary>0-based page each outline entry targets; Clauses[i] is clause i + 1.</summary>
    public sealed record OutlinePages(int Particulars, int Definitions, int[] Clauses, int Schedule, int Execution);

    public sealed record ContractData(Employer Employer, Employee Employee, Role Role, Terms Terms, string Reference, Definition[] Definitions, Clause[] Clauses, OutlinePages Outline, string Locale);
}
