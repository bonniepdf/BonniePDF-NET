using BonniePDF;

namespace BonniePDF.Examples.LayoutFlow.MultiColumnLayouts;

/// <summary>
/// Multi-column (newspaper-style) layouts - a three-column article with images,
/// two text columns separated by a vertical rule, and balanced versus ragged
/// column heights - in an accessible, archival document (PDF/A-2a + PDF/UA-1).
///
/// There is no dedicated multi-column element: a HorizontalStack divides the
/// page width into columns, each column is a VerticalStack that flows top to
/// bottom, and spacing sets the gutter. Content belongs to the column it is
/// placed in, so what each column holds is explicit in the code - including how
/// material is split when columns of even height are wanted.
///
/// Acceptance: VeraPDF profile ua1 reports zero failures.
/// </summary>
[System.ComponentModel.DisplayName("Multi-Column Layouts")]
[System.ComponentModel.Category("Layout & Flow")]
[System.ComponentModel.Description(
    "Newspaper-style columns built from stacks: a three-story community newsletter with images, divided columns, and balanced versus ragged column heights.")]
public static class MultiColumnLayoutsExample
{
    public static IReadOnlyList<string> RequiredAssets { get; } =
    [
        "images/newsletter/community-garden.jpg",
        "images/newsletter/library.jpg",
        "images/newsletter/high-street.jpg",
    ];

    /// <summary>A community newsletter's three front-page stories: headline, photo asset, alt text, and paragraphs.</summary>
    private static readonly (string Heading, string PhotoPath, string AltText, string[] Paragraphs)[] Article =
    [
        ("Community garden opens",
        "images/newsletter/community-garden.jpg",
        "Raised timber beds and a greenhouse in the new Mill Lane community garden.",
        [
            "The community garden on Mill Lane opened its gates on Saturday after two years of planning. Forty raised beds, a greenhouse and a tool store now stand on what was an empty car park behind the station.",
            "Every bed has been taken for the first season, and a waiting list is open at the library desk. Volunteers meet on Wednesday evenings and Sunday mornings; no experience is needed and tools are provided.",
        ]),
        ("Library open later",
        "images/newsletter/library.jpg",
        "Reading tables with desk lamps in the refurbished central library.",
        [
            "The central library will stay open until 8pm on Tuesdays and Thursdays from next month. The change follows a survey in which most respondents asked for evening access to study space.",
            "The reading room has new tables with power at every seat, and study rooms can now be booked online up to a week ahead. Membership remains free for everyone who lives, works or studies in the town.",
        ]),
        ("High Street works",
        "images/newsletter/high-street.jpg",
        "Overnight resurfacing works along the High Street.",
        [
            "Work to resurface the High Street begins on 12 January and is expected to last six weeks. The road will close between 8pm and 6am, and the number 4 bus will divert along Station Road at night.",
            "Shops stay open throughout, and pavements will remain clear during trading hours. Updates will be posted on the noticeboard outside the town hall every Friday until the work is complete.",
        ]),
    ];

    /// <summary>The editor's note and the listings, set either side of the divider.</summary>
    private static readonly string[] Editor =
    [
        "This is the last issue of the year, and the busiest. Thank you to everyone who sent in news, photographs and corrections; the newsletter is only as good as the town that writes it.",
        "Next year we move to a monthly issue, delivered to every household on the first Saturday of the month. If you would like to help with deliveries in your street, leave your name at the library desk.",
    ];

    private static readonly string[] WhatsOn =
    [
        "The winter market returns to the square on the second weekend of December, with forty stalls, a choir on Saturday evening and the lighting of the tree at five o'clock on Sunday.",
        "The repair café meets in the church hall on the first Sunday of each month. Bring a broken lamp, a torn coat or a bicycle that will not change gear, and leave with it working again.",
    ];

    /// <summary>Six paragraphs of one feature, used to compare column splits.</summary>
    private static readonly string[] MarketHall =
    [
        "The market hall on Church Street was built in 1868 to give the weekly cattle market a roof. Its iron columns were cast in the town's own foundry, and the clock above the entrance was paid for by public subscription.",
        "For a century the hall was the busiest building in the town. Farmers sold livestock on Tuesdays, and on Saturdays the floor filled with stalls selling cheese, cloth, ironmongery and seasonal produce.",
        "The cattle market moved to the edge of town in 1972, and the hall stood half used for many years. A leaking roof and a failed heating system closed it to the public in 2015.",
        "Restoration began in 2023 with a grant from the heritage fund and the work of a local building trust. The roof was relaid with the original slate, and the columns were stripped and repainted in their first colour.",
        "The hall reopened in the spring with twenty permanent stalls, a café and a space for exhibitions. The clock, silent for nine years, strikes the hour again.",
        "Guided tours run on the first Saturday of each month and take about an hour. They are free, but places are limited, so book at the library desk or at the café in the hall.",
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate() => Generate(LoadDefaultAssets());

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        (string Heading, string[] Paragraphs, string AltText, byte[] ImageData)[] articleColumns = Article
            .Select(story => (story.Heading, story.Paragraphs, story.AltText, Asset(assets, story.PhotoPath)))
            .ToArray();

        string[][] dividerParagraphs = [Editor, WhatsOn];

        string[] balanceParagraphs = MarketHall;

        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "Multi-Column Layouts Demo",
                Author = "BonniePDF",
                Subject =
                    "Demonstrates newspaper-style multi-column layouts in an accessible PDF/A-2a + PDF/UA-1 document.",
                Keywords = "pdf, accessibility, layout, columns, multi-column, newspaper, BonniePDF",
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
            },
        };

        doc.Add(new Heading("Multi-Column Layouts")
        {
            TocIdentifier = "intro",
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 10),
        });

        doc.Add(new TextBlock
        {
            Text = "A multi-column layout sets text in vertical columns read left to right, the way newspapers and " +
                   "magazines have been printed for centuries. On a wide page it keeps line lengths comfortable and " +
                   "lets related material sit side by side. In BonniePDF the pattern is direct composition: a " +
                   "HorizontalStack divides the page width into columns, each column is a VerticalStack that flows " +
                   "from top to bottom, and the stack\u2019s spacing sets the gutter between columns. Content belongs to " +
                   "the column you place it in, so what each column contains is always explicit in the code.",
            Margin = new Thickness(0, 0, 0, 10),
        });

        // A masthead-style rule under the introduction. A horizontal Line fills the
        // width it is given; thickness is its height.
        doc.Add(new Line
        {
            Thickness = 1,
            Color = ColorPalette.Slate[300],
            Margin = new Thickness(0, 0, 0, 16),
        });

        // ── A three-column article ──────────────────────────────────────────────

        doc.Add(SectionHeading("A Three-Column Article", "s-article", 0));
        doc.Add(new TextBlock
        {
            Text =
                "Three equal columns, each a VerticalStack holding its own heading, image and paragraphs. " +
                "The number of stack children is the number of columns; the stack\u2019s spacing (18pt here) is the gutter.",
            Margin = new Thickness(0, 0, 0, 10),
        });

        var article = new HorizontalStack
        {
            Spacing = 18,
            WidthDistribution = WidthDistribution.Equal,
        };
        foreach ((string heading, string[] paragraphs, string altText, byte[] imageData) in articleColumns)
        {
            var column = new VerticalStack { Spacing = 12 };

            column.Add(new TextBlock
            {
                Text = heading,
                FontName = StandardFonts.HelveticaBold,
                FontSize = 12,
                Color = ColorPalette.Slate[800],
            });

            column.Add(new Image
            {
                ImageData = imageData,
                AltText = altText,
                Width = 153,
                Height = 64,
                FitMode = ImageFitMode.Cover,
            });

            foreach (string text in paragraphs)
            {
                column.Add(new TextBlock
                {
                    Text = text,
                    Alignment = TextAlignment.Left,
                });
            }

            article.Add(column);
        }

        doc.Add(article);

        // ── Columns with a divider ──────────────────────────────────────────────


        doc.Add(SectionHeading("Columns with a Divider", "s-divider", 0));
        doc.Add(new TextBlock
        {
            Text = "A vertical rule between two weighted text columns: an unweighted 2pt Container keeps its width while the columns share the rest, and the row and the rule share one explicit height.",
            Margin = new Thickness(0, 0, 0, 10),
        });

        // The two text columns carry weight 1 each; the rule is added without a
        // weight, which under weighted distribution means "keep your own width".
        // 144pt is the measured height of the taller column: the row and the rule
        // share it, so the rule ends where the columns do.
        var divided = new HorizontalStack
        {
            Spacing = 20,
            WidthDistribution = WidthDistribution.Weighted,
            Height = 144,
        };
        divided.Add(ColumnOf(dividerParagraphs[0]), 1f);
        divided.Add(DividerRule(144));
        divided.Add(ColumnOf(dividerParagraphs[1]), 1f);
        doc.Add(divided);

        // ── Balancing column heights ────────────────────────────────────────────

        doc.Add(new PageBreak());

        doc.Add(SectionHeading("Balancing Column Heights", "s-balance", 0));
        doc.Add(new TextBlock
        {
            Text = "Each column holds exactly what you place in it, so the split you choose decides the heights. " +
                   "Below, the same six paragraphs are arranged two ways: pushed into the first column, which leaves " +
                   "the second standing empty, and divided evenly, which levels the two.",
            Margin = new Thickness(0, 0, 0, 10),
        });

        doc.Add(SubCaption("Same material, all in the first column", 12));
        var lumped = new HorizontalStack
        {
            Spacing = 20,
            WidthDistribution = WidthDistribution.Equal,
        };
        lumped.Add(ColumnOf(balanceParagraphs[..4]));
        lumped.Add(ColumnOf(balanceParagraphs[4..]));
        doc.Add(lumped);

        doc.Add(SubCaption("Same material, split evenly", 20));
        var balanced = new HorizontalStack
        {
            Spacing = 20,
            WidthDistribution = WidthDistribution.Equal,
        };
        balanced.Add(ColumnOf(balanceParagraphs[..3]));
        balanced.Add(ColumnOf(balanceParagraphs[3..]));
        doc.Add(balanced);

        return doc;
    }

    /// <summary>The section heading used throughout - a level-2 Heading in the house style.</summary>
    private static Heading SectionHeading(string text, string tocIdentifier, float topMargin) => new(text)
    {
        TocIdentifier = tocIdentifier,
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 14,
        Color = ColorPalette.Indigo[600],
        Margin = new Thickness(0, topMargin, 0, 8),
    };

    /// <summary>The small bold label used above the balancing demonstrations.</summary>
    private static TextBlock SubCaption(string text, float topMargin) => new()
    {
        Text = text,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 11,
        Color = ColorPalette.Slate[600],
        Margin = new Thickness(0, topMargin, 0, 6),
    };

    /// <summary>A column of paragraphs with a uniform gap between them.</summary>
    private static VerticalStack ColumnOf(string[] paragraphs)
    {
        var column = new VerticalStack { Spacing = 12 };
        foreach (string text in paragraphs)
        {
            column.Add(new TextBlock
            {
                Text = text,
                Alignment = TextAlignment.Left,
            });
        }

        return column;
    }

    /// <summary>
    /// The vertical rule between the divided columns: a 2pt-wide filled Container
    /// sized to the row height. Under weighted distribution an unweighted child
    /// keeps its preferred width, which is exactly what a fixed-width rule needs.
    /// </summary>
    private static Container DividerRule(float height) => new()
    {
        Width = 2,
        Height = height,
        BackgroundColor = ColorPalette.Slate[400],
    };

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
