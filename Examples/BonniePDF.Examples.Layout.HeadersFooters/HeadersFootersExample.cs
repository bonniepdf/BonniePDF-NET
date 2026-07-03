using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Layout.HeadersFooters;

/// <summary>
/// Page header/footer templates in an accessible, archival document
/// (PDF/A-2a + PDF/UA-1). Header/footer template content is automatically
/// tagged as /Artifact so screen readers skip decorative running heads and
/// page numbers, while the numbered body sections use semantic Heading and
/// TextBlock elements that map to the required structure tree.
///
/// Acceptance: VeraPDF profile ua1 reports zero failures.
/// </summary>
[System.ComponentModel.DisplayName("Headers & Footers")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description("Header/footer templates on every page - template content tagged as Artifact, numbered body sections tagged semantically.")]
public static class HeadersFootersExample
{
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title   = "Headers and Footers Demo",
                Author  = "BonniePDF",
                Subject = "Demonstrates page headers and footers in an accessible PDF/A-2a + PDF/UA-1 document.",
                Keywords = "pdf, accessibility, headers, footers, page numbers, BonniePDF",
            },
            Settings = new DocumentSettings
            {
                Conformance   = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(StandardFonts.Helvetica)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[600]),

                // Header/footer template content is rendered as /Artifact - screen readers skip it.
                HeaderTemplate = pageNum => new TextBlock
                {
                    Text      = $"BonniePDF Documentation - page {pageNum}",
                    FontName  = StandardFonts.Helvetica,
                    FontSize  = 9,
                    TextColor = ColorPalette.Slate[500],
                    Alignment = TextAlignment.Center,
                },
                FooterTemplate = pageNum => new TextBlock
                {
                    Text      = $"Page {pageNum} of {TextElementBase.TotalPagesToken} · Confidential",
                    FontName  = StandardFonts.Helvetica,
                    FontSize  = 9,
                    TextColor = ColorPalette.Slate[500],
                    Alignment = TextAlignment.Center,
                },
            },
        };

        // ── Chapter 1 ─────────────────────────────────────────────────────────

        doc.Add(new Heading("Chapter 1: Headers & Footers")
        {
            TocIdentifier = "ch1",
            Level         = 1,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 20,
            TextColor     = ColorPalette.Slate[800],
            Margin        = new Thickness(0, 0, 0, 12),
        });

        (string subtitle, string body)[] sections =
        {
            ("Header and footer templates",
                "The HeaderTemplate and FooterTemplate delegates are called once per page after all body " +
                "content has been laid out. The element each delegate returns is rendered as a PDF /Artifact " +
                "so that assistive technology ignores decorative running text and page numbers."),

            ("Dynamic content",
                "The page-number integer passed to each delegate lets you build 'Page N of M' footers, " +
                "alternate the running head on even/odd pages, or suppress the template on the title page " +
                "by returning null."),

            ("Any IElement is accepted",
                "Templates are not limited to plain text. You can return a HorizontalStack containing a " +
                "logo image on one side and a page number on the other - all tagged as /Artifact " +
                "automatically."),

            ("Body content is fully tagged",
                "Headings, paragraphs, lists, tables, and images added via doc.Add() receive their normal " +
                "structure-tree entries. The tagging machinery keeps body content and artifact content " +
                "in separate marked-content sequences so their BDC/EMC scopes never intermix."),

            ("Validation",
                "VeraPDF with the PDF/UA-1 profile verifies that (a) every real-content operator is " +
                "wrapped in a tagged marked-content sequence, (b) every artifact is wrapped in an " +
                "untagged BMC sequence, and (c) the structure tree is syntactically valid. This example " +
                "is designed to pass all of those checks."),

            ("Screen-reader experience",
                "When a user navigates the document with a screen reader, the running headers and footers " +
                "are invisible: the AT jumps straight from the end of one page's body text to the beginning " +
                "of the next, with no interruption from repeated page-number announcements."),

            ("Multi-page flow",
                "This document intentionally generates multiple pages so you can confirm that every page " +
                "has both header and footer template elements, each tagged as /Artifact, while the body " +
                "headings and paragraphs carry their own semantic structure-tree entries."),

            ("Summary",
                "PDF/UA-1 requires that all content either carries a structure-tree entry or is explicitly " +
                "marked as an artifact. BonniePDF satisfies both sides of that rule automatically: body " +
                "elements are tagged via the structure tree; template output is wrapped in /Artifact BMC."),
        };

        for (int i = 0; i < sections.Length; i++)
        {
            (string subtitle, string body) = sections[i];
            string number = $"1.{i + 1}";

            doc.Add(new Heading($"{number} {subtitle}")
            {
                TocIdentifier = subtitle.ToLowerInvariant().Replace(' ', '-').Replace(':', '-'),
                Level         = 2,
                FontName      = StandardFonts.HelveticaBold,
                FontSize      = 14,
                TextColor     = ColorPalette.Indigo[600],
                Margin        = new Thickness(0, 14, 0, 6),
            });

            doc.Add(new TextBlock
            {
                Text   = body,
                Margin = new Thickness(0, 0, 0, 6),
            });
        }

        return doc;
    }
}
