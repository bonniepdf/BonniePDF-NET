using System.Text;
using System.Text.RegularExpressions;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Typography.DickensNovel;

/// <summary>
/// PDF/UA-1 tagged Dickens novel example.
///
/// Reads the full Project Gutenberg plain-text edition of <em>A Tale of Two Cities</em>
/// (pg98.txt) and renders it as a tagged, multi-hundred-page PDF. Book-level headings are
/// /H1, chapter headings are /H2, and body paragraphs are /P. Running headers and the
/// centred page number in the footer are both tagged as /Artifact.
///
/// The source text (pg98.txt) is bundled next to this example and copied to the
/// build output directory, so it resolves on any platform.
///
/// Acceptance: VeraPDF profile ua1 reports zero failures.
/// </summary>
[System.ComponentModel.DisplayName("Dickens: A Tale of Two Cities (PDF/UA-1)")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("PDF/UA-1 tagged complete Project Gutenberg novel - book and chapter headings tagged semantically.")]
public static partial class DickensNovelTaggedExample
{
    private static readonly string LocalFilePath = Path.Combine(AppContext.BaseDirectory, "pg98.txt");

    [GeneratedRegex(@"^CHAPTER\s+[IVXLC]+\.$")]
    private static partial Regex ChapterLineRegex();

    public static Document Generate()
    {
        string raw = LoadText();
        List<(SectionKind Kind, string Number, string Title, string TocId)> sections = ParseNovel(raw);

        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "A Tale of Two Cities - Tagged (PDF/UA-1)",
                Author   = "Charles Dickens",
                Subject  = "Project Gutenberg eBook #98 - A public domain classic.",
                Keywords = "pdf, pdf/ua, accessibility, dickens, novel, gutenberg",
            },
            Settings = new DocumentSettings
            {
                PageSize      = PdfSize.A4,
                Margin        = 72,
                Version       = PdfVersion.Pdf17,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DisplayDocTitle = true,
                DefaultTextStyle = new TextStyle()
                    .WithFamily(StandardFonts.TimesRoman)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[900]),

                // Running header - tagged as /Artifact automatically.
                HeaderTemplate = pageNum =>
                {
                    if (pageNum <= 1) return null;
                    return new TextBlock
                    {
                        Text      = pageNum % 2 == 0 ? "A Tale of Two Cities" : "Charles Dickens",
                        FontName  = StandardFonts.TimesItalic,
                        FontSize  = 9,
                        TextColor = ColorPalette.Slate[500],
                        Alignment = pageNum % 2 == 0 ? TextAlignment.Left : TextAlignment.Right,
                        Margin    = new Thickness(0, 0, 0, 2),
                    };
                },

                // Page number footer - tagged as /Artifact automatically.
                FooterTemplate = pageNum =>
                {
                    if (pageNum <= 1) return null;
                    return new TextBlock
                    {
                        Text      = pageNum.ToString(),
                        FontName  = StandardFonts.TimesRoman,
                        FontSize  = 9,
                        TextColor = ColorPalette.Slate[500],
                        Alignment = TextAlignment.Center,
                        Margin    = new Thickness(0, 2, 0, 0),
                    };
                },
            },
        };

        // ── Title page ────────────────────────────────────────────────────────

        doc.Add(new TextBlock
        {
            Text      = "A TALE OF TWO CITIES",
            FontName  = StandardFonts.TimesBold,
            FontSize  = 28,
            TextColor = ColorPalette.Slate[900],
            Alignment = TextAlignment.Center,
            Margin    = new Thickness(0, 100, 0, 12),
        });

        doc.Add(new TextBlock
        {
            Text      = "A Story of the French Revolution",
            FontName  = StandardFonts.TimesItalic,
            FontSize  = 15,
            TextColor = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin    = new Thickness(0, 0, 0, 40),
        });

        doc.Add(new TextBlock
        {
            Text      = "Charles Dickens",
            FontName  = StandardFonts.TimesBold,
            FontSize  = 20,
            TextColor = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin    = new Thickness(0, 0, 0, 60),
        });

        doc.Add(new TextBlock
        {
            Text      = "Project Gutenberg Edition · eBook #98",
            FontSize  = 10,
            TextColor = ColorPalette.Slate[400],
            Alignment = TextAlignment.Center,
        });

        doc.AddPage();

        // ── Table of contents ─────────────────────────────────────────────────

        doc.Add(new Heading("Table of Contents")
        {
            TocIdentifier = "toc",
            Level         = 1,
            FontName      = StandardFonts.TimesBold,
            FontSize      = 24,
            TextColor     = ColorPalette.Slate[900],
            Alignment     = TextAlignment.Center,
            Margin        = new Thickness(0, 40, 0, 30),
        });

        // Collect TOC entries from the parsed sections and emit as paragraphs
        foreach ((SectionKind kind, _, string title, _) in sections)
        {
            if (kind == SectionKind.BookTitle)
            {
                doc.Add(new TextBlock
                {
                    Text      = title,
                    FontName  = StandardFonts.TimesBold,
                    FontSize  = 11,
                    TextColor = ColorPalette.Slate[800],
                    Margin    = new Thickness(0, 8, 0, 2),
                });
            }
            else if (kind == SectionKind.Chapter)
            {
                doc.Add(new TextBlock
                {
                    Text      = "    " + title,
                    FontName  = StandardFonts.TimesRoman,
                    FontSize  = 10,
                    TextColor = ColorPalette.Slate[700],
                    Margin    = new Thickness(0, 2, 0, 2),
                });
            }
        }

        doc.AddPage();

        // ── Body ──────────────────────────────────────────────────────────────

        foreach ((SectionKind kind, string number, string title, string tocId) in sections)
        {
            switch (kind)
            {
                case SectionKind.BookTitle:
                    doc.Add(new Heading(title.ToUpperInvariant())
                    {
                        TocIdentifier = tocId,
                        Level         = 1,
                        FontName      = StandardFonts.TimesBold,
                        FontSize      = 14,
                        TextColor     = ColorPalette.Slate[800],
                        Alignment     = TextAlignment.Center,
                        Margin        = new Thickness(0, 56, 0, 14),
                    });
                    break;

                case SectionKind.Chapter:
                    // Chapter number label (not a heading - it's presentational)
                    doc.Add(new TextBlock
                    {
                        Text      = number,
                        FontName  = StandardFonts.TimesBold,
                        FontSize  = 10,
                        TextColor = ColorPalette.Slate[500],
                        Alignment = TextAlignment.Center,
                        Margin    = new Thickness(0, 32, 0, 6),
                    });
                    doc.Add(new Heading(title)
                    {
                        TocIdentifier = tocId,
                        Level         = 2,
                        FontName      = StandardFonts.TimesBold,
                        FontSize      = 17,
                        TextColor     = ColorPalette.Slate[900],
                        Alignment     = TextAlignment.Center,
                        Margin        = new Thickness(0, 0, 0, 18),
                    });
                    break;

                case SectionKind.Paragraph:
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        doc.Add(new TextBlock
                        {
                            Text      = title,
                            FontSize  = 11,
                            TextColor = ColorPalette.Slate[900],
                            Alignment = TextAlignment.Justify,
                            Margin    = new Thickness(0, 0, 0, 7),
                        });
                    }
                    break;
            }
        }

        return doc;
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

    private enum SectionKind { BookTitle, Chapter, Paragraph }

    private static List<(SectionKind Kind, string Number, string Title, string TocId)> ParseNovel(string raw)
    {
        var result = new List<(SectionKind, string, string, string)>();
        string[] lines = raw.Replace("\r\n", "\n").Split('\n');

        bool pastHeader  = false;
        int  bodyStart   = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            if (!pastHeader && lines[i].Contains("*** START OF THE PROJECT GUTENBERG"))
            {
                pastHeader = true;
                continue;
            }

            if (pastHeader && lines[i].Length > 0 && !char.IsWhiteSpace(lines[i][0])
                && lines[i].StartsWith("Book the ", StringComparison.OrdinalIgnoreCase))
            {
                bodyStart = i;
                break;
            }
        }

        var    para        = new StringBuilder();
        bool   nextIsTitle = false;
        string pendingNum  = "";

        void FlushParagraph()
        {
            string text = para.ToString().Trim();
            if (text.Length > 0)
                result.Add((SectionKind.Paragraph, "", text, ""));
            para.Clear();
        }

        for (int i = bodyStart; i < lines.Length; i++)
        {
            string rawLine = lines[i];
            string line    = rawLine.Trim();

            if (line.StartsWith("*** END OF THE PROJECT GUTENBERG", StringComparison.OrdinalIgnoreCase))
                break;

            if (rawLine.Length > 0 && !char.IsWhiteSpace(rawLine[0])
                && line.StartsWith("Book the ", StringComparison.OrdinalIgnoreCase))
            {
                FlushParagraph();
                nextIsTitle = false;
                result.Add((SectionKind.BookTitle, "", line, line));
                continue;
            }

            if (ChapterLineRegex().IsMatch(line))
            {
                FlushParagraph();
                pendingNum  = line;
                nextIsTitle = true;
                continue;
            }

            if (nextIsTitle)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                result.Add((SectionKind.Chapter, pendingNum, line, line));
                nextIsTitle = false;
                pendingNum  = "";
                continue;
            }

            if (string.IsNullOrWhiteSpace(line)) { FlushParagraph(); continue; }

            if (para.Length > 0) para.Append(' ');
            para.Append(line);
        }

        FlushParagraph();
        return result;
    }
}
