using System.Collections.Generic;
using BonniePDF;

namespace BonniePDF.Examples.TablesLists.MultiPageFlow;

/// <summary>
/// A Table with repeating column headers spanning multiple pages, tagged accessible table cells
/// cells, a decorative "Page N of M" footer, and prose sections — all PDF/A-2a + PDF/UA-1.
/// </summary>
[System.ComponentModel.DisplayName("Tables")]
[System.ComponentModel.Category("Tables & Lists")]
[System.ComponentModel.Description(
    "A Table with repeating headers spanning multiple pages, accessible cells, a page-number footer, and prose sections. PDF/A-2a + PDF/UA-1.")]
public static class TablesExample
{
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

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]>? assets = null)
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "Employee Directory",
                Author = "BonniePDF",
                Subject = "Table with repeating headers spanning multiple pages. PDF/A-2a + PDF/UA-1.",
                Keywords = "pdf, table, accessibility, pdf/ua, pdf/a, multi-page, BonniePDF",
            },
            Settings = new DocumentSettings
            {
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(StandardFonts.Helvetica)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[700]),
                FooterTemplate = pageNum => new TextBlock
                {
                    Text = $"Page {pageNum} of {TextElementBase.TotalPagesToken}",
                    FontSize = 9,
                    Color = ColorPalette.Slate[400],
                    Alignment = TextAlignment.Right,
                },
            },
        };

        // ── Part 1: Employee table ─────────────────────────────────────────────

        doc.Add(new Heading("Part 1: Employee Directory")
        {
            TocIdentifier = "directory",
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 20,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 10),
        });

        doc.Add(new TextBlock
        {
            Text =
                "The table below lists team members by name, role, and department. Column headers repeat on every page for easy reference. Each cell is automatically tagged for screen readers.",
            Margin = new Thickness(0, 0, 0, 10),
        });

        var table = new Table(46f, 130f, 130f, 120f)
        {
            RepeatHeadersOnNewPage = true,
            HeaderBackgroundColor = ColorPalette.Slate[100],
            HeaderTextColor = ColorPalette.Slate[800],
            Border = new Border(0.5f),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
        };

        table.AddHeader(
            new TableCell
            {
                Content = new TextBlock { Text = "Photo", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
                IsHeader = true
            },
            new TableCell
            {
                Content = new TextBlock { Text = "Name", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
                IsHeader = true
            },
            new TableCell
            {
                Content = new TextBlock { Text = "Role", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
                IsHeader = true
            },
            new TableCell
            {
                Content = new TextBlock { Text = "Department", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
                IsHeader = true
            });

        var employees = Placeholders.WithSeed(42).Employees(45);

        int rowIndex = 0;
        foreach (var emp in employees)
        {
            byte[] photoData;
            if (assets != null && assets.TryGetValue($"images/employees/employee-{emp.Id}.jpg", out byte[]? assetBytes))
            {
                photoData = assetBytes;
            }
            else
            {
                photoData = emp.Avatar;
            }

            // The photo is cropped round by a clipping container the size of the photo.
            var photo = new Container
            {
                Width = 34,
                Height = 34,
                CornerRadius = 17,
                ClipContent = true,
                Child = new Image
                {
                    ImageData = photoData,
                    Width = 34,
                    Height = 34,
                    FitMode = ImageFitMode.Cover,
                    AltText = $"Photo of {emp.Name}"
                },
            };

            Color bg = rowIndex++ % 2 == 0 ? Color.White : ColorPalette.Slate[50];
            table.AddRow(
                new TableCell
                {
                    Content = photo,
                    BackgroundColor = bg
                },
                new TableCell
                {
                    Content = new TextBlock { Text = emp.Name, FontSize = 10, Color = ColorPalette.Slate[800], Margin = new Thickness(0, 10, 0, 0) },
                    BackgroundColor = bg
                },
                new TableCell
                {
                    Content = new TextBlock { Text = emp.Role, FontSize = 10, Color = ColorPalette.Slate[600], Margin = new Thickness(0, 10, 0, 0) },
                    BackgroundColor = bg
                },
                new TableCell
                {
                    Content = DepartmentPill(emp.Department),
                    BackgroundColor = bg
                });
        }

        doc.Add(table);

        // ── Part 2: Prose sections ─────────────────────────────────────────────

        doc.Add(new Heading("Part 2: Company Overview")
        {
            TocIdentifier = "overview",
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 20,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 20, 0, 10),
        });

        foreach ((string title, string body) in OverviewSections)
        {
            // Each heading is kept with its paragraph, so a heading never ends a page on its own.
            VerticalStack section = new() { BreakAllowed = false };
            section.Add(new Heading(title)
            {
                TocIdentifier = title.ToLowerInvariant().Replace(' ', '-'),
                Level = 2,
                FontName = StandardFonts.HelveticaBold,
                FontSize = 14,
                Color = ColorPalette.Slate[700],
                Margin = new Thickness(0, 14, 0, 6),
            });

            section.Add(new TextBlock { Text = body, Margin = new Thickness(0, 0, 0, 6) });
            doc.Add(section);
        }

        return doc;
    }

    /// <summary>Each department's hue, so a department reads at a glance down the column.</summary>
    private static ColorPalette HueOf(string department) => department switch
    {
        "Engineering" => ColorPalette.Indigo,
        "Marketing" => ColorPalette.Rose,
        "Sales" => ColorPalette.Amber,
        "HR" => ColorPalette.Emerald,
        "Finance" => ColorPalette.Sky,
        "Design" => ColorPalette.Violet,
        "Operations" => ColorPalette.Teal,
        _ => ColorPalette.Slate,
    };

    /// <summary>
    /// A department as a small tinted pill: a light fill with dark text of the same hue. The auto-width
    /// stack sizes the pill to its text instead of letting it fill the cell.
    /// </summary>
    private static HorizontalStack DepartmentPill(string department)
    {
        var hue = HueOf(department);
        var wrap = new HorizontalStack { WidthDistribution = WidthDistribution.Auto, Margin = new Thickness(0, 9, 0, 0) };
        wrap.Add(new Container
        {
            BackgroundColor = hue[100],
            CornerRadius = 8,
            Padding = new Thickness(8, 3, 8, 3),
            Child = new TextBlock { Text = department, FontSize = 9, Color = hue[800] },
        });
        return wrap;
    }
}
