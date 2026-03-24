using System.Text;
using System.Text.RegularExpressions;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Descriptors;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;


namespace BonniePDF.Examples.Typography.DickensNovel;

/// <summary>
/// Fluent API version of the Dickens novel renderer.
/// Reads the full Project Gutenberg plain-text edition of <em>A Tale of Two Cities</em>
/// (pg98.txt) and renders it as a beautifully formatted multi-hundred-page PDF.
///
/// Key Fluent API patterns demonstrated:
/// • <c>doc.WithDefaultTextStyle()</c> — Times Roman 11pt set once; all paragraphs inherit it.
/// • <c>settings.Header() / settings.Footer()</c> — running headers and centred page numbers.
/// • Multiple <c>doc.Page()</c> calls — title, TOC, and body are separate layout sections
///   (equivalent to the traditional API's <c>doc.AddPage()</c> page-break calls).
/// • <c>column.Item().Element(new Heading { ... })</c> — escape hatch for TOC-registered headings.
/// • <c>column.Item().Element(new VisualToc { ... })</c> — escape hatch for the visual TOC.
///
/// Source file expected at C:\GITHUB\pg98.txt.
/// Download from https://www.gutenberg.org/cache/epub/98/pg98.txt if not present.
/// </summary>
[System.ComponentModel.DisplayName("Dickens: A Tale of Two Cities (Fluent API)")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("Formats the complete Project Gutenberg novel as a book-quality PDF using the Fluent API.")]
public static partial class DickensNovelExampleFluent
{
    private const string LocalFilePath = @"C:\GITHUB\pg98.txt";

    /// <summary>Matches chapter number lines: "CHAPTER I." … "CHAPTER XXIV."</summary>
    [GeneratedRegex(@"^CHAPTER\s+[IVXLC]+\.$")]
    private static partial Regex ChapterLineRegex();

    /// <summary>
    /// Loads and parses the novel, then renders it as a multi-page PDF using Pdf.Create().
    /// </summary>
    public static Document Generate()
    {
        string raw = LoadText();
        List<NovelSection> sections = ParseNovel(raw);

        // Collect TOC identifiers before entering the builder
        List<string> tocIdentifiers = new();
        foreach (NovelSection section in sections)
        {
            if (section.Type == SectionType.BookTitle || section.Type == SectionType.Chapter)
                tocIdentifiers.Add(section.Text);
        }

        return Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title   = "A Tale of Two Cities";
                metadata.Author  = "Charles Dickens";
                metadata.Subject = "Project Gutenberg eBook #98 — A public domain classic.";
            });

            doc.WithSettings(settings =>
            {
                settings.PageSize       = PdfSize.A4;
                settings.Margin         = 72;
                settings.Version        = PdfVersion.Pdf17;
                settings.PdfAEnabled    = true;
                settings.DisplayDocTitle = true;

                // Running header: alternates book title (even pages) / author (odd pages).
                // Skipped on page 1 (the title page) by returning early from the callback.
                settings.Header((pageNumber, header) =>
                {
                    if (pageNumber <= 1) return;

                    string headerText = pageNumber % 2 == 0
                        ? "A Tale of Two Cities"
                        : "Charles Dickens";

                    var textBlock = header
                        .Text(headerText)
                        .Font(StandardFonts.TimesItalic)
                        .FontSize(9)
                        .TextColor(Color.Slate500)
                        .Margin(0, 0, 0, 2);

                    if (pageNumber % 2 == 0)
                        textBlock.AlignLeft();
                    else
                        textBlock.AlignRight();
                });

                // Centred page number in footer; blank on the title page.
                settings.Footer((pageNumber, footer) =>
                {
                    if (pageNumber <= 1) return;

                    footer.Text(pageNumber.ToString())
                        .Font(StandardFonts.TimesRoman)
                        .FontSize(9)
                        .TextColor(Color.Slate500)
                        .AlignCenter()
                        .Margin(0, 2, 0, 0);
                });
            });

            // Document default: Times Roman 11pt — body paragraphs inherit this
            doc.WithDefaultTextStyle(style => style
                .WithFamily(StandardFonts.TimesRoman)
                .WithSize(11));

            // ── Page 1: Title Page ────────────────────────────────────────────
            doc.Page(page =>
            {
                page.Content().Column(column =>
                {
                    // Large top margin visually centres the block on the page
                    column.Item()
                        .Text("A TALE OF TWO CITIES")
                        .Font(StandardFonts.TimesBold)
                        .FontSize(28)
                        .TextColor(Color.Slate900)
                        .AlignCenter()
                        .Margin(0, 100, 0, 12);

                    column.Item()
                        .Text("A Story of the French Revolution")
                        .Font(StandardFonts.TimesItalic)
                        .FontSize(15)
                        .TextColor(Color.Slate600)
                        .AlignCenter()
                        .Margin(0, 0, 0, 40);

                    column.Item()
                        .Text("———————————————")
                        .FontSize(13)
                        .TextColor(Color.Slate400)
                        .AlignCenter()
                        .Margin(0, 0, 0, 40);

                    column.Item()
                        .Text("Charles Dickens")
                        .Font(StandardFonts.TimesBold)
                        .FontSize(20)
                        .TextColor(Color.Slate800)
                        .AlignCenter()
                        .Margin(0, 0, 0, 60);

                    column.Item()
                        .Text("Project Gutenberg Edition · eBook #98")
                        .FontSize(10)
                        .TextColor(Color.Slate400)
                        .AlignCenter()
                        .Margin(0, 0, 0, 4);

                    column.Item()
                        .Text("Public domain — free to read, copy, and share.")
                        .Font(StandardFonts.TimesItalic)
                        .FontSize(9)
                        .TextColor(Color.Slate400)
                        .AlignCenter();
                });
            });

            // ── Page 2: Table of Contents ─────────────────────────────────────
            doc.Page(page =>
            {
                page.Content().Column(column =>
                {
                    column.Item()
                        .Text("Table of Contents")
                        .Font(StandardFonts.TimesBold)
                        .FontSize(24)
                        .TextColor(Color.Slate900)
                        .AlignCenter()
                        .Margin(0, 40, 0, 40);

                    // VisualToc has no fluent equivalent — use Element() escape hatch
                    column.Item().Element(new VisualToc
                    {
                        HeadingIdentifiers = tocIdentifiers,
                        FontName  = StandardFonts.TimesRoman,
                        FontSize  = 11,
                        TextColor = Color.Slate800,
                        Margin    = new Thickness(0, 0, 0, 40)
                    });
                });
            });

            // ── Pages 3+: Body Content ────────────────────────────────────────
            doc.Page(page =>
            {
                page.Content().Column(column =>
                {
                    foreach (NovelSection section in sections)
                    {
                        switch (section.Type)
                        {
                            case SectionType.BookTitle:
                                AddBookHeading(column, section.Text);
                                break;
                            case SectionType.Chapter:
                                AddChapterHeading(column, section.Number, section.Title, section.Text);
                                break;
                            case SectionType.Paragraph:
                                AddParagraph(column, section.Text);
                                break;
                        }
                    }
                });
            });

        }).Build();

    }

    // ── Rendering helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Renders a "Book the First / Second / Third" divider with ornamental rules.
    /// Uses the Element() escape hatch for the Heading so the TOC entry is registered.
    /// </summary>
    private static void AddBookHeading(ColumnDescriptor column, string title)
    {
        column.Item()
            .Text("———————————————")
            .FontSize(11)
            .TextColor(Color.Slate300)
            .AlignCenter()
            .Margin(0, 56, 0, 14);

        // Heading registers TocIdentifier for VisualToc — requires Element() escape hatch
        column.Item().Element(new Heading
        {
            TocIdentifier = title,
            Text      = title.ToUpperInvariant(),
            FontName  = StandardFonts.TimesBold,
            FontSize  = 14,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center,
            Margin    = new Thickness(0, 0, 0, 14)
        });

        column.Item()
            .Text("———————————————")
            .FontSize(11)
            .TextColor(Color.Slate300)
            .AlignCenter()
            .Margin(0, 0, 0, 32);
    }

    /// <summary>
    /// Renders a chapter number ("CHAPTER I.") followed by its title ("The Period").
    /// </summary>
    private static void AddChapterHeading(ColumnDescriptor column, string number, string title, string tocId)
    {
        column.Item()
            .Text(number)
            .Font(StandardFonts.TimesBold)
            .FontSize(10)
            .TextColor(Color.Slate500)
            .AlignCenter()
            .Margin(0, 32, 0, 6);

        column.Item().Element(new Heading
        {
            TocIdentifier = tocId,
            Text      = title,
            FontName  = StandardFonts.TimesBold,
            FontSize  = 17,
            TextColor = Color.Slate900,
            Alignment = TextAlignment.Center,
            Margin    = new Thickness(0, 0, 0, 18)
        });
    }

    /// <summary>
    /// Renders a single body paragraph in 11pt justified Times Roman.
    /// Font and size are inherited from the document default — only alignment is set.
    /// </summary>
    private static void AddParagraph(ColumnDescriptor column, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        column.Item()
            .Text(text)
            .TextColor(Color.Slate900)
            .AlignJustify()
            .Margin(0, 0, 0, 7);
    }

    // ── Text loading ──────────────────────────────────────────────────────────

    private static string LoadText()
    {
        if (File.Exists(LocalFilePath))
            return File.ReadAllText(LocalFilePath, Encoding.UTF8);

        throw new FileNotFoundException(
            $"Novel source file not found at '{LocalFilePath}'. " +
            "Download it from https://www.gutenberg.org/cache/epub/98/pg98.txt " +
            "and save it to that path.");
    }

    // ── Novel parser ──────────────────────────────────────────────────────────

    private static List<NovelSection> ParseNovel(string raw)
    {
        var sections = new List<NovelSection>();
        string[] lines = raw.Replace("\r\n", "\n").Split('\n');

        bool pastHeader = false;
        int  bodyStart  = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            if (!pastHeader && lines[i].Contains("*** START OF THE PROJECT GUTENBERG"))
            {
                pastHeader = true;
                continue;
            }

            if (pastHeader
                && lines[i].Length > 0
                && !char.IsWhiteSpace(lines[i][0])
                && lines[i].StartsWith("Book the ", System.StringComparison.OrdinalIgnoreCase))
            {
                bodyStart = i;
                break;
            }
        }

        var    paraBuilder   = new StringBuilder();
        bool   nextIsChTitle = false;
        string pendingChNum  = "";

        void FlushParagraph()
        {
            string para = paraBuilder.ToString().Trim();
            if (para.Length > 0)
                sections.Add(new NovelSection(SectionType.Paragraph, para));
            paraBuilder.Clear();
        }

        for (int i = bodyStart; i < lines.Length; i++)
        {
            string rawLine = lines[i];
            string line    = rawLine.Trim();

            if (line.StartsWith("*** END OF THE PROJECT GUTENBERG", System.StringComparison.OrdinalIgnoreCase))
                break;

            if (rawLine.Length > 0
                && !char.IsWhiteSpace(rawLine[0])
                && line.StartsWith("Book the ", System.StringComparison.OrdinalIgnoreCase))
            {
                FlushParagraph();
                nextIsChTitle = false;
                sections.Add(new NovelSection(SectionType.BookTitle, line));
                continue;
            }

            if (ChapterLineRegex().IsMatch(line))
            {
                FlushParagraph();
                pendingChNum  = line;
                nextIsChTitle = true;
                continue;
            }

            if (nextIsChTitle)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                sections.Add(new NovelSection(SectionType.Chapter, pendingChNum, line));
                nextIsChTitle = false;
                pendingChNum  = "";
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph();
                continue;
            }

            if (paraBuilder.Length > 0) paraBuilder.Append(' ');
            paraBuilder.Append(line);
        }

        FlushParagraph();
        return sections;
    }
}

