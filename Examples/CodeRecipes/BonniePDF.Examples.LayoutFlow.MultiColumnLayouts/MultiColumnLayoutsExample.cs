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
    "Newspaper-style columns built from stacks: a three-column article with images, divided columns, and balanced versus ragged column heights.")]
public static class MultiColumnLayoutsExample
{
    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        // Deterministic filler from a seeded source; the TypeScript twin of this
        // example reads the same sequence, so both documents carry the same text.
        PlaceholderSource source = Placeholders.WithSeed(42);

        (string Heading, string[] Paragraphs, byte[] ImageData)[] articleColumns =
        {
            ("Front Page", [source.Paragraph(), source.Paragraph()], source.Image(153, 64)),
            ("The Brief", [source.Paragraph(), source.Paragraph()], source.Image(153, 64)),
            ("Dispatches", [source.Paragraph(), source.Paragraph()], source.Image(153, 64)),
        };

        string[][] dividerParagraphs =
        {
            [source.Paragraph(), source.Paragraph()],
            [source.Paragraph(), source.Paragraph()],
        };

        string[] balanceParagraphs =
        [
            source.Paragraph(),
            source.Paragraph(),
            source.Paragraph(),
            source.Paragraph(),
            source.Paragraph(),
            source.Paragraph(),
        ];

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
                "Three equal columns, each a VerticalStack holding its own heading, image and justified paragraphs. " +
                "The number of stack children is the number of columns; the stack\u2019s spacing (18pt here) is the gutter.",
            Margin = new Thickness(0, 0, 0, 10),
        });

        var article = new HorizontalStack
        {
            Spacing = 18,
            WidthDistribution = WidthDistribution.Equal,
        };
        foreach ((string heading, string[] paragraphs, byte[] imageData) in articleColumns)
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
                AltText = "Abstract placeholder illustration.",
                Width = 153,
                Height = 64,
                FitMode = ImageFitMode.Cover,
            });

            foreach (string text in paragraphs)
            {
                column.Add(new TextBlock
                {
                    Text = text,
                    Alignment = TextAlignment.Justify,
                });
            }

            article.Add(column);
        }

        doc.Add(article);

        // ── Columns with a divider ──────────────────────────────────────────────

        doc.Add(new PageBreak());

        doc.Add(SectionHeading("Columns with a Divider", "s-divider", 0));
        doc.Add(new TextBlock
        {
            Text = "A vertical rule between two weighted text columns. The rule is a fixed-width Container added " +
                   "without a weight, so it keeps its own 2pt while the remaining width is split evenly between the " +
                   "text columns. The row carries an explicit height and the rule matches it, so the rule spans the " +
                   "whole block regardless of how the paragraphs inside fall.",
            Margin = new Thickness(0, 0, 0, 10),
        });

        // The two text columns carry weight 1 each; the rule is added without a
        // weight, which under weighted distribution means "keep your own width".
        // 223pt is the measured height of the taller column: the row and the rule
        // share it, so the rule ends where the columns do.
        var divided = new HorizontalStack
        {
            Spacing = 20,
            WidthDistribution = WidthDistribution.Weighted,
            Height = 223,
        };
        divided.Add(ColumnOf(dividerParagraphs[0]), 1f);
        divided.Add(DividerRule(223));
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

    /// <summary>A column of justified paragraphs with a uniform gap between them.</summary>
    private static VerticalStack ColumnOf(string[] paragraphs)
    {
        var column = new VerticalStack { Spacing = 12 };
        foreach (string text in paragraphs)
        {
            column.Add(new TextBlock
            {
                Text = text,
                Alignment = TextAlignment.Justify,
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
}
