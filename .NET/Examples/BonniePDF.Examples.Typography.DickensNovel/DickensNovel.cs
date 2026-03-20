using System.Text;
using System.Text.RegularExpressions;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;


namespace BonniePDF.Examples.Typography.DickensNovel;

/// <summary>
/// Reads the full Project Gutenberg plain-text edition of <em>A Tale of Two Cities</em>
/// (pg98.txt) and renders it as a beautifully formatted multi-hundred-page PDF.
/// Demonstrates automatic word-wrapping, justified text, multi-page pagination,
/// PDF/A-compliant Times-family font embedding, running headers, and footer page numbers.
///
/// Source file expected at C:\GITHUB\pg98.txt.
/// Download from https://www.gutenberg.org/cache/epub/98/pg98.txt if not present.
/// </summary>
[System.ComponentModel.DisplayName("Dickens: A Tale of Two Cities")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("Formats the complete Project Gutenberg novel as a book-quality PDF.")]
public static partial class DickensNovelExample
{
    private const string LocalFilePath = @"C:\GITHUB\pg98.txt";

    /// <summary>Matches chapter number lines: "CHAPTER I." … "CHAPTER XXIV."</summary>
    [GeneratedRegex(@"^CHAPTER\s+[IVXLC]+\.$")]
    private static partial Regex ChapterLineRegex();

    /// <inheritdoc/>
    /// <inheritdoc/>
    /// <inheritdoc/>
    /// <inheritdoc/>
    /// <inheritdoc/>
    /// <inheritdoc/>

    /// <summary>
    /// Loads the novel source text, parses it into sections, and renders the full multi-page PDF.
    /// Requires the Project Gutenberg plain-text file to be present at <see cref="LocalFilePath"/>.
    /// </summary>
    /// <param name="outputPath">The full file path where the generated PDF will be saved.</param>
    /// <returns>The path of the generated PDF file.</returns>
    public static Document Generate()
    {
        string raw = LoadText();
        List<NovelSection> sections = ParseNovel(raw);

        // A4 with 72 pt margins (~1 inch) gives 451 pt content width — ideal for body text.
        DocumentSettings settings = new()
        {
            PageSize    = PdfSize.A4,
            Margin      = 72,
            Version     = PdfVersion.Pdf17,
            PdfAEnabled = true,
            DisplayDocTitle = true,

            // Running header: book title left, author right — static across all pages.
            // (The template is called per page after layout, so a closure over fixed strings works.)
            HeaderTemplate = pageNum =>
            {
                if (pageNum <= 1) return null; // no header on the title page
                return new TextBlock
                {
                    Text      = pageNum % 2 == 0
                        ? "A Tale of Two Cities"
                        : "Charles Dickens",
                    FontName  = StandardFonts.TimesItalic,
                    FontSize  = 9,
                    TextColor = Color.Slate500,
                    Alignment = pageNum % 2 == 0 ? TextAlignment.Left : TextAlignment.Right,
                    Margin    = new Thickness(0, 0, 0, 2)
                };
            },

            // Centred page number in footer; blank on the title page.
            FooterTemplate = pageNum =>
            {
                if (pageNum <= 1) return null;
                return new TextBlock
                {
                    Text      = pageNum.ToString(),
                    FontName  = StandardFonts.TimesRoman,
                    FontSize  = 9,
                    TextColor = Color.Slate500,
                    Alignment = TextAlignment.Center,
                    Margin    = new Thickness(0, 2, 0, 0)
                };
            }
        };

        Document doc = new(settings);
        doc.Metadata.Title   = "A Tale of Two Cities";
        doc.Metadata.Author  = "Charles Dickens";
        doc.Metadata.Subject = "Project Gutenberg eBook #98 — A public domain classic.";

        // ── Title Page ───────────────────────────────────────────────────────
        AddTitlePage(doc);
        doc.AddPage();

        // ── Table of Contents ────────────────────────────────────────────────
        doc.Add(new TextBlock
        {
            Text = "Table of Contents",
            FontName = StandardFonts.TimesBold,
            FontSize = 24,
            TextColor = Color.Slate900,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 40, 0, 40)
        });

        List<string> tocIdentifiers = new();
        foreach (var section in sections)
        {
            if (section.Type == SectionType.BookTitle || section.Type == SectionType.Chapter)
            {
                tocIdentifiers.Add(section.Text);
            }
        }

        doc.Add(new VisualToc
        {
            HeadingIdentifiers = tocIdentifiers,
            FontName = StandardFonts.TimesRoman,
            FontSize = 11,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 40)
        });

        doc.AddPage();

        // ── Body Content ─────────────────────────────────────────────────────
        foreach (NovelSection section in sections)
        {
            switch (section.Type)
            {
                case SectionType.BookTitle:
                    AddBookHeading(doc, section.Text, section.Text);
                    break;

                case SectionType.Chapter:
                    AddChapterHeading(doc, section.Number, section.Title, section.Text);
                    break;

                case SectionType.Paragraph:
                    AddParagraph(doc, section.Text);
                    break;
            }
        }
        return doc;
    }

    // ── Rendering helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Renders the decorative title page with the book title, subtitle, author, and edition note.
    /// Relies on large top margins to visually centre the block without explicit page positioning.
    /// </summary>
    private static void AddTitlePage(Document doc)
    {
        // Large top margin to vertically centre the block on the page.
        doc.Add(new TextBlock
        {
            Text      = "A TALE OF TWO CITIES",
            FontName  = StandardFonts.TimesBold,
            FontSize  = 28,
            TextColor = Color.Slate900,
            Alignment = TextAlignment.Center,
            Margin    = new Thickness(0, 100, 0, 12)
        });

        doc.Add(new TextBlock
        {
            Text      = "A Story of the French Revolution",
            FontName  = StandardFonts.TimesItalic,
            FontSize  = 15,
            TextColor = Color.Slate600,
            Alignment = TextAlignment.Center,
            Margin    = new Thickness(0, 0, 0, 40)
        });

        doc.Add(new TextBlock
        {
            Text      = "———————————————",
            FontName  = StandardFonts.TimesRoman,
            FontSize  = 13,
            TextColor = Color.Slate400,
            Alignment = TextAlignment.Center,
            Margin    = new Thickness(0, 0, 0, 40)
        });

        doc.Add(new TextBlock
        {
            Text      = "Charles Dickens",
            FontName  = StandardFonts.TimesBold,
            FontSize  = 20,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center,
            Margin    = new Thickness(0, 0, 0, 60)
        });

        doc.Add(new TextBlock
        {
            Text      = "Project Gutenberg Edition · eBook #98",
            FontName  = StandardFonts.TimesRoman,
            FontSize  = 10,
            TextColor = Color.Slate400,
            Alignment = TextAlignment.Center,
            Margin    = new Thickness(0, 0, 0, 4)
        });

        doc.Add(new TextBlock
        {
            Text      = "Public domain — free to read, copy, and share.",
            FontName  = StandardFonts.TimesItalic,
            FontSize  = 9,
            TextColor = Color.Slate400,
            Alignment = TextAlignment.Center
        });
    }

    /// <summary>
    /// Renders a "Book the First / Second / Third" divider with ornamental rules.
    /// A generous top margin keeps it visually separated from preceding text so it
    /// sits near the top of whatever page it lands on.
    /// </summary>
    private static void AddBookHeading(Document doc, string title, string tocId)
    {
        doc.Add(new TextBlock
        {
            Text      = "———————————————",
            FontName  = StandardFonts.TimesRoman,
            FontSize  = 11,
            TextColor = Color.Slate300,
            Alignment = TextAlignment.Center,
            Margin    = new Thickness(0, 56, 0, 14)
        });

        doc.Add(new Heading
        {
            TocIdentifier = tocId,
            Text      = title.ToUpperInvariant(),
            FontName  = StandardFonts.TimesBold,
            FontSize  = 14,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center,
            Margin    = new Thickness(0, 0, 0, 14)
        });

        doc.Add(new TextBlock
        {
            Text      = "———————————————",
            FontName  = StandardFonts.TimesRoman,
            FontSize  = 11,
            TextColor = Color.Slate300,
            Alignment = TextAlignment.Center,
            Margin    = new Thickness(0, 0, 0, 32)
        });
    }

    /// <summary>
    /// Renders a chapter number ("CHAPTER I.") followed by its title ("The Period").
    /// The top margin provides enough breathing room to visually open a new chapter
    /// without requiring explicit page breaks.
    /// </summary>
    private static void AddChapterHeading(Document doc, string number, string title, string tocId)
    {
        doc.Add(new TextBlock
        {
            Text      = number,
            FontName  = StandardFonts.TimesBold,
            FontSize  = 10,
            TextColor = Color.Slate500,
            Alignment = TextAlignment.Center,
            Margin    = new Thickness(0, 32, 0, 6)
        });

        doc.Add(new Heading
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
    /// Renders a single body paragraph in 11 pt justified Times Roman.
    /// The small bottom margin produces a traditional single-spaced look
    /// with a thin gap between consecutive paragraphs.
    /// </summary>
    private static void AddParagraph(Document doc, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        doc.Add(new TextBlock
        {
            Text      = text,
            FontName  = StandardFonts.TimesRoman,
            FontSize  = 11,
            TextColor = Color.Slate900,
            Alignment = TextAlignment.Justify,
            Margin    = new Thickness(0, 0, 0, 7)
        });
    }

    // ── Text loading ─────────────────────────────────────────────────────────

    /// <summary>
    /// Reads the novel source text from <see cref="LocalFilePath"/> using UTF-8 encoding.
    /// Throws <see cref="FileNotFoundException"/> if the file is not present, with instructions
    /// on where to download the Project Gutenberg edition.
    /// </summary>
    /// <returns>The raw text content of the novel.</returns>
    private static string LoadText()
    {
        if (File.Exists(LocalFilePath))
            return File.ReadAllText(LocalFilePath, Encoding.UTF8);

        throw new FileNotFoundException(
            $"Novel source file not found at '{LocalFilePath}'. " +
            "Download it from https://www.gutenberg.org/cache/epub/98/pg98.txt " +
            "and save it to that path.");
    }

    // ── Novel parser ─────────────────────────────────────────────────────────

    /// <summary>
    /// Parses the Project Gutenberg plain-text file into a structured list of
    /// sections: book titles, chapter headings, and body paragraphs.
    ///
    /// Key observations about the source file format:
    /// • Lines in the Table of Contents are indented (leading spaces).
    /// • The actual content begins at the first UN-indented "Book the " line.
    /// • Chapter headings occupy their own line: "CHAPTER I." (Roman numerals + period).
    /// • The chapter title is the first non-blank line that follows.
    /// • Paragraphs are separated by blank lines; Gutenberg hard-wraps them at ~72 chars.
    ///   Consecutive non-blank content lines are joined with a space to reconstruct the
    ///   full paragraph before handing it to BonniePDF's word-wrapper.
    /// </summary>
    private static List<NovelSection> ParseNovel(string raw)
    {
        var sections = new List<NovelSection>();
        string[] lines = raw.Replace("\r\n", "\n").Split('\n');

        // ── Step 1: locate the body start ────────────────────────────────────
        // Skip everything up to and including the Gutenberg header, then skip
        // the indented Table of Contents, landing at the first real "Book the" line.
        bool pastHeader = false;
        int  bodyStart  = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            if (!pastHeader && lines[i].Contains("*** START OF THE PROJECT GUTENBERG"))
            {
                pastHeader = true;
                continue;
            }

            // First un-indented "Book the " line after the header = body start
            if (pastHeader
                && lines[i].Length > 0
                && !char.IsWhiteSpace(lines[i][0])
                && lines[i].StartsWith("Book the ", StringComparison.OrdinalIgnoreCase))
            {
                bodyStart = i;
                break;
            }
        }

        // ── Step 2: parse body into sections ─────────────────────────────────
        var    paraBuilder    = new StringBuilder();
        bool   nextIsChTitle  = false;
        string pendingChNum   = "";

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
            string line     = rawLine.Trim();

            // ── end of book content ───────────────────────────────────────────
            if (line.StartsWith("*** END OF THE PROJECT GUTENBERG", StringComparison.OrdinalIgnoreCase))
                break;

            // ── book title: un-indented, starts with "Book the" ───────────────
            if (rawLine.Length > 0
                && !char.IsWhiteSpace(rawLine[0])
                && line.StartsWith("Book the ", StringComparison.OrdinalIgnoreCase))
            {
                FlushParagraph();
                nextIsChTitle = false;
                sections.Add(new NovelSection(SectionType.BookTitle, line));
                continue;
            }

            // ── chapter number: "CHAPTER I." … "CHAPTER XXIV." ───────────────
            if (ChapterLineRegex().IsMatch(line))
            {
                FlushParagraph();
                pendingChNum  = line;
                nextIsChTitle = true;
                continue;
            }

            // ── chapter title: first non-blank line after the chapter number ──
            if (nextIsChTitle)
            {
                if (string.IsNullOrWhiteSpace(line)) continue; // skip inter-line blanks
                sections.Add(new NovelSection(SectionType.Chapter, pendingChNum, line));
                nextIsChTitle = false;
                pendingChNum  = "";
                continue;
            }

            // ── blank line: flush current paragraph ──────────────────────────
            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph();
                continue;
            }

            // ── regular text: join Gutenberg-wrapped lines into one paragraph ─
            if (paraBuilder.Length > 0) paraBuilder.Append(' ');
            paraBuilder.Append(line);
        }

        FlushParagraph(); // tail paragraph
        return sections;
    }
}

// ── Supporting types ──────────────────────────────────────────────────────────

/// <summary>Discriminates between the three kinds of content parsed from the Project Gutenberg source.</summary>
internal enum SectionType { BookTitle, Chapter, Paragraph }

/// <summary>
/// Represents a single parsed section of the novel — a book division, chapter heading, or body paragraph.
/// </summary>
internal sealed class NovelSection
{
    /// <summary>Creates a BookTitle or Paragraph section.</summary>
    public NovelSection(SectionType type, string text)
    {
        Type = type;
        Text = text;
    }

    /// <summary>Creates a Chapter section with a separate number and title.</summary>
    public NovelSection(SectionType type, string number, string title)
    {
        Type   = type;
        Number = number;
        Title  = title;
        Text   = $"{number} — {title}";
    }

    /// <summary>Gets the type of this section (BookTitle, Chapter, or Paragraph).</summary>
    public SectionType Type   { get; }

    /// <summary>Full text (BookTitle / Paragraph) or "CHAPTER X. — Title" (Chapter).</summary>
    public string Text   { get; }

    /// <summary>e.g. "CHAPTER I."  (Chapter sections only)</summary>
    public string Number { get; }

    /// <summary>e.g. "The Period"  (Chapter sections only)</summary>
    public string Title  { get; }
}

