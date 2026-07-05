using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Layout.MultiPageFlow;

/// <summary>
/// A Table with repeating column headers spanning multiple pages, tagged /TH and /TD
/// cells, a "Page N of M" footer Artifact, and prose sections — all PDF/A-2a + PDF/UA-1.
/// </summary>
[System.ComponentModel.DisplayName("Tables")]
[System.ComponentModel.Category("Elements")]
[System.ComponentModel.Description("A Table with repeating headers spanning multiple pages, /TH and /TD tagged cells, a page-number footer, and prose sections. PDF/A-2a + PDF/UA-1.")]
public static class TablesExample
{
    private static readonly (string Name, string Department, string Role)[] Employees =
    [
        ("Alice Chen",      "Engineering",      "Senior Developer"),
        ("Bob Martinez",    "Marketing",        "Brand Manager"),
        ("Carol Johnson",   "Human Resources",  "HR Specialist"),
        ("David Kim",       "Finance",          "Financial Analyst"),
        ("Eva Patel",       "Engineering",      "QA Lead"),
        ("Frank Wilson",    "Sales",            "Account Executive"),
        ("Grace Taylor",    "Operations",       "Project Manager"),
        ("Henry Brown",     "R&D",              "Research Scientist"),
        ("Isabel Garcia",   "Customer Support", "Support Lead"),
        ("James Lee",       "Engineering",      "DevOps Engineer"),
        ("Karen White",     "Legal",            "Compliance Officer"),
        ("Liam Moore",      "IT",               "Systems Administrator"),
        ("Mia Anderson",    "Engineering",      "Frontend Developer"),
        ("Noah Thomas",     "Finance",          "Budget Analyst"),
        ("Olivia Jackson",  "Marketing",        "Content Strategist"),
        ("Peter Harris",    "Sales",            "Sales Manager"),
        ("Quinn Robinson",  "Engineering",      "Backend Developer"),
        ("Rachel Clark",    "Operations",       "Supply Chain Lead"),
        ("Samuel Lewis",    "R&D",              "Data Scientist"),
        ("Tara Walker",     "Customer Support", "CX Analyst"),
        ("Uma Hall",        "Human Resources",  "Recruiter"),
        ("Victor Allen",    "Finance",          "Controller"),
        ("Wendy Young",     "Engineering",      "Platform Architect"),
        ("Xavier King",     "Legal",            "IP Counsel"),
        ("Yara Wright",     "IT",               "Security Engineer"),
        ("Zack Scott",      "Marketing",        "Digital Strategist"),
        ("Amy Green",       "Sales",            "Business Developer"),
        ("Brian Adams",     "Operations",       "Logistics Manager"),
        ("Chloe Baker",     "R&D",              "UX Researcher"),
        ("Dylan Carter",    "Engineering",      "Mobile Developer"),
        ("Eleanor Price",   "Marketing",        "Events Manager"),
        ("Felix Nguyen",    "Engineering",      "Cloud Architect"),
        ("Georgia Moss",    "Finance",          "Treasury Analyst"),
        ("Hugo Reyes",      "R&D",              "AI Researcher"),
        ("Iris Chambers",   "Legal",            "Contract Specialist"),
        ("Jordan Holt",     "Sales",            "Sales Engineer"),
        ("Kira Okafor",     "Operations",       "Process Analyst"),
        ("Leo Novak",       "IT",               "Network Engineer"),
        ("Maya Singh",      "Customer Support", "Escalations Lead"),
        ("Nate Callahan",   "Engineering",      "Site Reliability Engineer"),
        ("Ophelia Dunn",    "Human Resources",  "L&D Manager"),
        ("Patrick Stern",   "Finance",          "Risk Analyst"),
        ("Rosa Tanaka",     "Marketing",        "SEO Specialist"),
        ("Stefan Mehta",    "Engineering",      "Data Engineer"),
        ("Talia Burns",     "Operations",       "Fleet Coordinator"),
    ];

    private static readonly (string Title, string Body)[] OverviewSections =
    [
        ("Our Mission",
            "We are dedicated to creating sustainable value for our stakeholders through innovation, " +
            "operational excellence, and responsible business practices. Our mission guides every " +
            "decision we make, from product development to customer service."),
        ("Corporate Values",
            "Integrity, innovation, and inclusivity form the foundation of our corporate culture. " +
            "We believe in doing the right thing, even when no one is watching, and we encourage " +
            "creative thinking across every level of the organisation."),
        ("Strategic Vision",
            "Looking ahead, we envision a future where technology seamlessly integrates with everyday " +
            "life to enhance human potential. Our strategic initiatives focus on emerging markets, " +
            "sustainable technologies, and digital transformation."),
        ("Sustainability",
            "Environmental responsibility is a core value that influences everything we do. We have " +
            "committed to achieving carbon neutrality and have implemented comprehensive sustainability " +
            "programmes across all our facilities."),
        ("Conclusion",
            "This document demonstrates the pagination capabilities of BonniePDF. The table " +
            "automatically spans multiple pages with column headers repeating on each page, while " +
            "text sections flow naturally across page boundaries."),
    ];

    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Employee Directory",
                Author   = "BonniePDF",
                Subject  = "Table with repeating headers spanning multiple pages. PDF/A-2a + PDF/UA-1.",
                Keywords = "pdf, table, accessibility, pdf/ua, pdf/a, multi-page, BonniePDF",
            },
            Settings = new DocumentSettings
            {
                Conformance   = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(StandardFonts.Helvetica)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[700]),
                FooterTemplate = pageNum => new TextBlock
                {
                    Text      = $"Page {pageNum} of {TextElementBase.TotalPagesToken}",
                    FontSize  = 9,
                    TextColor = ColorPalette.Slate[400],
                    Alignment = TextAlignment.Right,
                },
            },
        };

        // ── Part 1: Employee table ─────────────────────────────────────────────

        doc.Add(new Heading("Part 1: Employee Directory")
        {
            TocIdentifier = "directory",
            Level         = 1,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 20,
            TextColor     = ColorPalette.Slate[800],
            Margin        = new Thickness(0, 0, 0, 10),
        });

        doc.Add(new TextBlock
        {
            Text   = "The table below lists team members by department. Column headers repeat on every page for easy reference. Each cell is tagged as /TH (header) or /TD (data) within the /Table structure tree.",
            Margin = new Thickness(0, 0, 0, 10),
        });

        var table = new Table(100f, 160f, 140f)
        {
            RepeatHeadersOnNewPage = true,
            HeaderBackgroundColor  = ColorPalette.Blue[600],
            HeaderTextColor        = Color.White,
        };

        table.AddHeader(
            new TableCell { Content = new TextBlock { Text = "Department", FontName = StandardFonts.HelveticaBold, FontSize = 10 }, IsHeader = true },
            new TableCell { Content = new TextBlock { Text = "Name",       FontName = StandardFonts.HelveticaBold, FontSize = 10 }, IsHeader = true },
            new TableCell { Content = new TextBlock { Text = "Role",       FontName = StandardFonts.HelveticaBold, FontSize = 10 }, IsHeader = true });

        int rowIndex = 0;
        foreach ((string name, string dept, string role) in Employees)
        {
            Color bg = rowIndex++ % 2 == 0 ? Color.White : ColorPalette.Slate[50];
            table.AddRow(
                new TableCell { Content = new TextBlock { Text = dept, FontSize = 10, TextColor = ColorPalette.Slate[600] }, BackgroundColor = bg },
                new TableCell { Content = new TextBlock { Text = name, FontSize = 10, TextColor = ColorPalette.Slate[800] }, BackgroundColor = bg },
                new TableCell { Content = new TextBlock { Text = role, FontSize = 10, TextColor = ColorPalette.Slate[600] }, BackgroundColor = bg });
        }

        doc.Add(table);

        // ── Part 2: Prose sections ─────────────────────────────────────────────

        doc.Add(new Heading("Part 2: Company Overview")
        {
            TocIdentifier = "overview",
            Level         = 1,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 20,
            TextColor     = ColorPalette.Slate[800],
            Margin        = new Thickness(0, 20, 0, 10),
        });

        foreach ((string title, string body) in OverviewSections)
        {
            doc.Add(new Heading(title)
            {
                TocIdentifier = title.ToLowerInvariant().Replace(' ', '-'),
                Level         = 2,
                FontName      = StandardFonts.HelveticaBold,
                FontSize      = 14,
                TextColor     = ColorPalette.Slate[700],
                Margin        = new Thickness(0, 14, 0, 6),
            });

            doc.Add(new TextBlock { Text = body, Margin = new Thickness(0, 0, 0, 6) });
        }

        return doc;
    }
}
