using BonniePDF;

namespace BonniePDF.Examples.Layout.HeadersFooters;

/// <summary>
/// Page header/footer templates in an accessible, archival document
/// (PDF/A-2a + PDF/UA-1). Header/footer template content is automatically
/// marked decorative, so screen readers skip running heads and page numbers,
/// while the numbered body sections use semantic Heading and TextBlock
/// elements.
///
/// Acceptance: VeraPDF profile ua1 reports zero failures.
/// </summary>
[System.ComponentModel.DisplayName("Headers &amp; Footers")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description("Header/footer templates on every page - template content marked decorative, numbered body sections tagged semantically.")]
public static class HeadersFootersExample
{
    private static readonly (string Title, string Body)[] Sections =
    {
        ("Introduction",
            "This document demonstrates the header and footer template system in BonniePDF. Headers and footers are automatically rendered on each page after all content has been laid out. They are marked as decorative elements and are skipped by screen readers."),

        ("Header Templates",
            "The header template function receives the page number and returns an IElement that is rendered at the top of each page. This allows for dynamic content such as section titles, document names, or logos. The element is positioned in the header region defined by the document margin."),

        ("Footer Templates",
            "Similarly, the footer template function creates elements for the bottom of each page. Common uses include page numbers, copyright notices, and document revision dates. You may return null to suppress the footer on a specific page."),

        ("Dynamic Content",
            "Templates can contain any valid IElement, including HorizontalStack for multi-column layouts, TextBlocks for simple text, or even Images for logos and branding. The total page count is available through the TotalPagesToken sentinel."),

        ("Page Numbering",
            "The page number passed to templates is 1-based, making it easy to display \"Page 1 of N\" style pagination. The total page count is resolved after layout completes, ensuring accuracy even when content reflows during finalisation."),

        ("Rendering Order",
            "Templates are rendered after all page content is finalised, ensuring accurate page counts and proper positioning regardless of how content flows across pages. This means you can safely reference the total page count in both header and footer templates."),

        ("Conditional Templates",
            "You can conditionally render headers or footers by returning null from the template function. For example, you might omit the header on the first page to give more room for a title section, or hide the footer on a table of contents page."),

        ("Accessibility",
            "Header and footer templates are automatically marked as decorative elements. This means assistive technologies skip over them when reading the document, preventing repeated announcement of decorative running headers on every page turn."),
    };

    public static Document Generate()
    {
        Document doc = new(new DocumentSettings
        {
            Conformance   = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language      = "en-GB",
            DefaultTextStyle = new TextStyle()
                .WithFamily(StandardFonts.Helvetica)
                .WithSize(11)
                .WithColor(ColorPalette.Slate[700]),

            // Header/footer template content is marked decorative - screen readers skip it.
            HeaderTemplate = pageNum => new TextBlock
            {
                Text      = $"BonniePDF - Headers & Footers - Page {pageNum}",
                FontName  = StandardFonts.Helvetica,
                FontSize  = 9,
                Color = ColorPalette.Slate[500],
                Alignment = TextAlignment.Center,
            },
            FooterTemplate = pageNum => new TextBlock
            {
                Text      = $"Page {pageNum} of {TextElementBase.TotalPagesToken}",
                FontName  = StandardFonts.Helvetica,
                FontSize  = 9,
                Color = ColorPalette.Slate[400],
                Alignment = TextAlignment.Center,
            },
        })
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Headers and Footers Demo",
                Author   = "BonniePDF",
                Subject  = "Demonstrates page headers and footers in an accessible PDF/A-2a + PDF/UA-1 document.",
                Keywords = "pdf, accessibility, headers, footers, page numbers, BonniePDF",
            },
        };

        doc.Add(new Heading("Chapter 1: Headers & Footers")
        {
            TocIdentifier = "intro",
            Level         = 1,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 22,
            Color     = ColorPalette.Slate[800],
            Margin        = new Thickness(0, 0, 0, 14),
        });

        doc.Add(new TextBlock
        {
            Text   = "Headers and footers are rendered as decorative elements - they are visible on every page but are skipped by screen readers. The numbered sections below describe how the template system works.",
            Margin = new Thickness(0, 0, 0, 12),
        });

        for (int i = 0; i < Sections.Length; i++)
        {
            (string title, string body) = Sections[i];

            doc.Add(new Heading($"1.{i + 1} {title}")
            {
                TocIdentifier = Slug(title),
                Level         = 2,
                FontName      = StandardFonts.HelveticaBold,
                FontSize      = 14,
                Color     = ColorPalette.Indigo[600],
                Margin        = new Thickness(0, 14, 0, 6),
            });

            doc.Add(new TextBlock { Text = body, Margin = new Thickness(0, 0, 0, 8) });

            // Second paragraph to push content onto additional pages
            doc.Add(new TextBlock
            {
                Text = "This section is part of the Headers & Footers demonstration. Each page carries the same " +
                       "running header and footer so you can observe them across multiple pages. The content here " +
                       "intentionally repeats to ensure the document spans several pages.",
                Margin = new Thickness(0, 0, 0, 8),
            });
        }

        return doc;
    }

    /// <summary>Lowercases and hyphenates a section title for its TOC anchor.</summary>
    private static string Slug(string title)
    {
        var sb = new System.Text.StringBuilder(title.Length);
        foreach (char c in title.ToLowerInvariant())
        {
            if (char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c)) sb.Append(c);
            else if (sb.Length > 0 && sb[^1] != '-') sb.Append('-');
        }
        return sb.ToString().Trim('-');
    }
}
