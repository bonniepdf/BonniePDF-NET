using BonniePDF;

namespace BonniePDF.Examples.LinksReferences.CrossReferences;

/// <summary>
/// Cross-references in a short staff handbook: <see cref="Section"/> (named anchors),
/// <see cref="SectionLink"/> (clickable internal jumps) and <see cref="PageNumberOf"/>
/// ("see page 4"). The contents page sits ahead of the chapters it points at, and the
/// chapters flow freely, so every page number is worked out by the two-pass layout
/// rather than typed in.
/// </summary>
[System.ComponentModel.DisplayName("Cross-References")]
[System.ComponentModel.Category("Links & References")]
[System.ComponentModel.Description("A short staff handbook with a linked contents page, live page numbers, \"see page N\" references and back-links.")]
public static class CrossReferencesExample
{
    public static IReadOnlyList<string> RequiredAssets { get; } =
    [
        "images/handbook/office.jpg",
    ];

    /// <summary>One colour for every link, so readers learn what is clickable.</summary>
    private static readonly Color Link = ColorPalette.Blue[700];

    private static readonly (string Id, string Title, string[] Paragraphs, (string Id, string Title) SeeAlso)[] Chapters =
    [
        ("ch-welcome", "Welcome to Harbour & Finch",
            [
                "This handbook explains how we work: the hours we keep, how to book time off, how to claim expenses and how we look after each other. It applies to everyone on a permanent or fixed-term contract, from your first day.",
                "Nothing here replaces your contract. Where the two differ, your contract wins, and your manager can tell you which parts apply to your role. We review the handbook every spring and send a summary of anything that changed.",
                "If something is unclear, ask. Most questions are answered faster by a message to the people team than by reading a policy twice, and every question we are asked helps us write the next edition more clearly."
            ],
            ("ch-hours", "Working hours")),
        ("ch-hours", "Working hours",
            [
                "Our core hours are 10:00 to 16:00, Monday to Friday. Outside those hours you can start and finish when it suits you and your team, as long as your contracted weekly hours are met and your calendar shows when you are available.",
                "Most roles can be done from home up to three days a week. Agree a regular pattern with your manager and keep it in the team calendar, so colleagues know when to expect you in the studio and when to book a video call instead.",
                "If you work late to meet a deadline, take the time back within the next two weeks. We do not pay overtime for salaried roles, but we do expect managers to notice when someone is regularly working beyond their hours, and to fix the workload rather than the person.",
                "Part-time and compressed-hours patterns are open to everyone after their probation period. Requests are answered in writing within four weeks, and a refusal always gives the business reason behind it."
            ],
            ("ch-leave", "Leave and holidays")),
        ("ch-leave", "Leave and holidays",
            [
                "Full-time staff have 27 days of annual leave plus the eight bank holidays in England. The leave year runs from January to December, and up to five unused days carry over to the end of March.",
                "Book leave in the HR system at least twice as many days ahead as the length of the break: a week off needs two weeks of notice. Your manager approves requests within three working days, and only refuses one when cover genuinely cannot be arranged.",
                "Between Christmas and New Year the studio closes, and those three days come from your allowance. New starters who join in the autumn can borrow up to three days against the following year to cover them.",
                "Sick leave, parental leave and compassionate leave are separate from your holiday allowance. Tell your manager by 10:00 on the first day you are unwell; you do not need a fit note for the first seven days."
            ],
            ("ch-expenses", "Expenses")),
        ("ch-expenses", "Expenses",
            [
                "We reimburse reasonable costs you incur for work: travel to clients, accommodation when a trip needs an overnight stay, and meals while you are away. Equipment for home working is covered once, up to £400, in your first three months.",
                "Claim within 30 days through the expenses app, with a photo of each receipt. Claims approved by the 20th are paid with that month’s salary; later claims are paid the following month.",
                "Travel by standard-class rail where the journey is under four hours, and book through the travel portal so the booking appears in our carbon report. If driving is cheaper or the only practical option, claim 45p per mile for the first 10,000 miles in a tax year."
            ],
            ("ch-safety", "Health and safety")),
        ("ch-safety", "Health and safety",
            [
                "Everyone is responsible for keeping the studio safe. Report hazards, near misses and accidents in the safety log the same day, even when nobody was hurt; the log is how we find problems before they cause harm.",
                "Each floor has two trained first aiders and a fire marshal, named on the noticeboard by the kitchen. The fire alarm is tested every Wednesday at 11:00; at any other time, leave by the nearest exit and meet at the assembly point on Quay Street.",
                "If you work from home, complete the short workstation assessment in your first month. We will send a chair, a monitor or a footrest if it shows you need one, and arrange an eye test if you use a screen for most of the day."
            ],
            ("ch-welcome", "Welcome to Harbour & Finch")),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate() => Generate(LoadDefaultAssets());

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        Document doc = new(new DocumentSettings
        {
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            Margin = 56,
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[700]),
            DisplayDocTitle = true,
            FooterTemplate = PageTemplate.FromElements(@default: new TextBlock("Harbour & Finch staff handbook  ·  page {{CurrentPage}} of {{TotalPages}}")
            {
                FontSize = 8,
                Color = ColorPalette.Slate[500],
                Alignment = TextAlignment.Center
            })
        })
        {
            Metadata = new DocumentMetadata
            {
                Title = "Staff Handbook",
                Author = "Harbour & Finch",
                Subject = "Section anchors, internal links, and page references"
            }
        };

        // ── Contents page ──
        // Anchor for the "back to contents" links at the end of each chapter.
        doc.Add(new Section("contents"));
        doc.Add(new TextBlock("HARBOUR & FINCH")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 9,
            LetterSpacing = 1.5f,
            Color = ColorPalette.Teal[700],
            Margin = new Thickness(0, 0, 0, 6)
        });
        doc.Add(new Heading("Staff Handbook")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 30,
            Color = ColorPalette.Slate[900],
            Margin = new Thickness(0, 0, 0, 8)
        });
        doc.Add(new TextBlock("Edition 4, spring 2026. Select a chapter to jump to it; every page number below is filled in when the handbook is laid out.")
        {
            Margin = new Thickness(0, 0, 0, 16)
        });
        doc.Add(new Container
        {
            Height = 150,
            CornerRadius = 6,
            ClipContent = true,
            Margin = new Thickness(0, 0, 0, 20),
            Child = new Image
            {
                ImageData = Asset(assets, "images/handbook/office.jpg"),
                Height = 150,
                FitMode = ImageFitMode.Cover,
                AltText = "Open-plan studio office with exposed brick walls and oak desks.",
            },
        });
        doc.Add(new Heading("Contents")
        {
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 15,
            Color = ColorPalette.Slate[900],
            Margin = new Thickness(0, 0, 0, 6)
        });

        for (int i = 0; i < Chapters.Length; i++)
        {
            var (id, title, _, _) = Chapters[i];
            // A contents line: a numbered, clickable title and its live page number.
            HorizontalStack line = new() { Spacing = 12, WidthDistribution = WidthDistribution.Weighted };
            line.Add(new SectionLink($"{i + 1}.  {title}", id) { FontSize = 12, Color = Link }, 8);
            line.Add(new PageNumberOf(id) { FontName = StandardFonts.HelveticaBold, FontSize = 12, Alignment = TextAlignment.Right }, 1);
            doc.Add(new Container
            {
                Border = new Border(0, 0, 0, 1),
                BorderColors = new BorderColors(ColorPalette.Slate[200]),
                Padding = new Thickness(0, 9, 0, 9),
                Child = line
            });
        }

        // ── Chapters: they flow, so page numbers come from layout, not from page breaks ──
        doc.Add(new PageBreak());
        for (int i = 0; i < Chapters.Length; i++)
        {
            var (id, title, paragraphs, (seeId, seeTitle)) = Chapters[i];

            // The anchor sits exactly where the chapter starts.
            doc.Add(new Section(id));
            doc.Add(new Heading($"{i + 1}. {title}")
            {
                Level = 2,
                FontName = StandardFonts.HelveticaBold,
                FontSize = 17,
                Color = ColorPalette.Slate[900],
                Margin = new Thickness(0, i == 0 ? 0 : 22, 0, 8)
            });
            foreach (string paragraph in paragraphs)
                doc.Add(new TextBlock(paragraph) { LineHeight = 1.45f, Margin = new Thickness(0, 0, 0, 8) });

            // "See also … on page N": a link to the chapter and its resolved page number.
            HorizontalStack seeAlso = new() { Spacing = 4, Margin = new Thickness(0, 2, 0, 4) };
            TextBlock lead = Muted("See also:");
            seeAlso.Add(lead, lead.GetPreferredWidth());
            SectionLink link = new(seeTitle, seeId) { FontSize = 10, Color = Link };
            seeAlso.Add(link, link.GetPreferredWidth());
            TextBlock onPage = Muted("on page");
            seeAlso.Add(onPage, onPage.GetPreferredWidth());
            seeAlso.Add(new PageNumberOf(seeId) { FontSize = 10, Color = ColorPalette.Slate[500] });
            doc.Add(seeAlso);

            // Back-link to the contents page, resolved the same way.
            doc.Add(new SectionLink("Back to contents", "contents") { FontSize = 10, Color = Link });
        }

        return doc;
    }

    /// <summary>Small grey text for the "see also" line.</summary>
    private static TextBlock Muted(string text) => new(text) { FontSize = 10, Color = ColorPalette.Slate[500] };

    private static byte[] Asset(IReadOnlyDictionary<string, byte[]> assets, string path) =>
        assets.TryGetValue(path, out byte[]? bytes)
            ? bytes
            : throw new InvalidOperationException($"Missing required asset: {path}");

    private static Dictionary<string, byte[]> LoadDefaultAssets()
    {
        string assetsDir = FindAssetsDirectory();
        Dictionary<string, byte[]> result = new(StringComparer.Ordinal);
        foreach (string key in RequiredAssets)
        {
            string fullPath = Path.Combine(assetsDir, key.Replace('/', Path.DirectorySeparatorChar));
            result[key] = File.ReadAllBytes(fullPath);
        }

        return result;
    }

    private static string FindAssetsDirectory()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, "Examples", "assets");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            string localCandidate = Path.Combine(dir, "assets");
            if (Directory.Exists(localCandidate))
            {
                return localCandidate;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
    }
}
