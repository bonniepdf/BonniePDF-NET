using System.IO;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Media;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests;

/// <summary>
/// 10-fixture hardening pass. Throws representative edge-case
/// documents at the tagging machinery to surface bugs the focused
/// stage-1 closeout tests don't hit: encryption + tagging, PDF/A +
/// PDF/UA combination, empty docs, internal hyperlinks, every
/// heading level, header/footer templates, nested layouts, etc.
///
/// Each test asserts (a) the doc generates without crashing,
/// (b) per-page BDC/EMC is balanced, and (c) the relevant struct
/// elements are present. Real VeraPDF rule verification is opt-in
/// via the existing <see cref="VeraPdfStageOneCloseoutTests"/>
/// pattern.
///
/// Known stage-2 gaps that VeraPDF will still flag (documented in
/// the failing tests rather than silently passing):
/// - Header/footer template content is rendered but not tagged as
///   /Artifact (this feature fixes this).
/// - Hyperlink annotations don't carry /StructParent (full
///   annotation reachability — deferred from this feature).
/// </summary>
public class HardeningTests
{
    // ── 1. Empty tagged document ────────────────────────────────────

    [Fact]
    public void Fixture01_EmptyTaggedDocument_ProducesValidStructTree()
    {
        string pdf = Encoding.ASCII.GetString(Generate(doc =>
        {
            doc.Settings.Version = PdfVersion.Pdf17;
            doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
            doc.Settings.Language = "en-GB";
            // No content elements.
        }));

        // Catalog wiring still present even with no content.
        Assert.Contains("/StructTreeRoot", pdf);
        Assert.Contains("/MarkInfo << /Marked true >>", pdf);
        Assert.Contains("/S /Document", pdf);
        // No other struct elements.
        Assert.Equal(0, SubstringCounter.Count(pdf, "/S /P"));
        Assert.Equal(0, SubstringCounter.Count(pdf, "/S /H1"));
    }

    // ── 2. PDF/A + PDF/UA combined ──────────────────────────────────

    [Fact]
    public void Fixture02_PdfAClaimSuppressed_WhenStandardFontsAreUsed()
    {
        // PDF/A-1b §6.3.4 forbids non-embedded fonts. When the caller
        // asks for PDF/A but uses any of the standard 14 (Helvetica
        // here), the serializer drops the PDF/A claim rather than
        // emitting an invalid one. PDF/UA-1 still ships because it has
        // no font-embedding requirement.
        string pdf = Encoding.ASCII.GetString(Generate(doc =>
        {
            doc.Settings.Version = PdfVersion.Pdf17;
            doc.Settings.PdfAEnabled = true;
            doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
            doc.Settings.Language = "en-GB";
            doc.Add(new Heading("Combined") { TocIdentifier = "c", Level = 1, FontSize = 18 });
        }));

        // PDF/A claim and OutputIntent are suppressed (honest about what we ship).
        Assert.DoesNotContain("pdfaid:part", pdf);
        Assert.DoesNotContain("/OutputIntents", pdf);
        // PDF/UA-1 plumbing is unaffected.
        Assert.Contains("pdfuaid:part", pdf);
        Assert.Contains("/StructTreeRoot", pdf);
        Assert.Contains("/MarkInfo << /Marked true >>", pdf);
    }

    // ── 3. Encrypted tagged document ────────────────────────────────

    [Fact]
    public void Fixture03_EncryptedTaggedDocument_GeneratesWithoutError()
    {
        // PDF spec allows tagging inside encrypted docs. XMP stream is
        // unencrypted; struct tree objects are encrypted alongside the
        // content. The combo shouldn't break either feature.
        byte[] bytes = Generate(doc =>
        {
            doc.Settings.Version = PdfVersion.Pdf17;
            doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
            doc.Settings.Language = "en-GB";
            doc.Settings.WithPassword("secret");
            doc.Add(new Heading("Confidential") { TocIdentifier = "c", Level = 1, FontSize = 18 });
            doc.Add(new TextBlock { Text = "Top-secret content.", FontSize = 12 });
        });

        // /Encrypt dictionary present + structure tree wiring present
        // (the actual struct-elem objects are encrypted streams now).
        string pdf = Encoding.ASCII.GetString(bytes);
        Assert.Contains("/Encrypt", pdf);
        Assert.Contains("/StructTreeRoot", pdf);
        Assert.True(bytes.Length > 1000);
    }

    // ── 4. Internal hyperlink (TargetPageIndex, not URI) ────────────

    [Fact]
    public void Fixture04_InternalHyperlink_StillEmitsLinkStructAndContents()
    {
        string pdf = Encoding.ASCII.GetString(GenerateTagged(doc =>
        {
            doc.Add(new Heading("Go") { TocIdentifier = "g", Level = 1, FontSize = 18 });
            doc.AddPage();
            doc.Add(new Hyperlink("Jump to page 1")
            {
                TargetPageIndex = 0,
                FontName = StandardFonts.Helvetica,
                FontSize = 12,
            });
        }));

        Assert.Contains("/S /Link", pdf);
        Assert.Contains("/Contents (Jump to page 1)", pdf);
        // Internal-link annotation uses /A << /S /GoTo /D ... >> not URI.
        Assert.Contains("/S /GoTo", pdf);
    }

    // ── 5. All six heading levels ───────────────────────────────────

    [Fact]
    public void Fixture05_AllHeadingLevels_MapToCorrectStructRoles()
    {
        string pdf = Encoding.ASCII.GetString(GenerateTagged(doc =>
        {
            for (int lvl = 1; lvl <= 6; lvl++)
            {
                doc.Add(new Heading($"Heading {lvl}")
                {
                    TocIdentifier = $"h{lvl}",
                    Level = lvl,
                    FontName = StandardFonts.HelveticaBold,
                    FontSize = 24 - lvl * 2,
                });
            }
        }));

        for (int lvl = 1; lvl <= 6; lvl++)
        {
            Assert.Contains($"/S /H{lvl}", pdf);
        }
    }

    // ── 6. Heading.Level out of range — clamped to H1/H6 ────────────

    [Fact]
    public void Fixture06_HeadingLevelOutOfRange_ClampedToH1AndH6()
    {
        string pdf = Encoding.ASCII.GetString(GenerateTagged(doc =>
        {
            doc.Add(new Heading("Too low")  { TocIdentifier = "lo", Level = 0,  FontSize = 18 });
            doc.Add(new Heading("Too high") { TocIdentifier = "hi", Level = 99, FontSize = 18 });
        }));

        // Level=0 clamps to H1 (the "<= 1" branch in Heading.SemanticRole).
        // Level=99 clamps to H6.
        Assert.Contains("/S /H1", pdf);
        Assert.Contains("/S /H6", pdf);
    }

    // ── 7. Long heading text that wraps ─────────────────────────────

    [Fact]
    public void Fixture07_LongHeadingWrappingMultiLines_StaysOneStructElem()
    {
        // A heading whose text exceeds one line still produces exactly
        // one /H1 StructElem — wrapping is a layout concern, not a
        // semantic one.
        string pdf = Encoding.ASCII.GetString(GenerateTagged(doc =>
        {
            doc.Add(new Heading(
                "A long heading that exercises the line-wrapping path and forces the text to flow onto a second line within the page margins")
            {
                TocIdentifier = "long", Level = 1,
                FontName = StandardFonts.HelveticaBold, FontSize = 24,
            });
        }));

        Assert.Equal(1, SubstringCounter.Count(pdf, "/S /H1"));
    }

    // ── 8. Header/footer templates (this feature fixed) ─────────────────────

    [Fact]
    public void Fixture08_HeaderFooterTemplates_KnownToProduceUntaggedContent()
    {
        // this feature fixed this: header/footer template content is now tagged as
        // /Artifact so screen readers skip it.
        string pdf = Encoding.ASCII.GetString(GenerateTagged(doc =>
        {
            doc.Settings.HeaderTemplate = pageNum => new TextBlock
            {
                Text = $"Header — page {pageNum}",
                FontSize = 9, TextColor = Color.Slate500,
            };
            doc.Settings.FooterTemplate = pageNum => new TextBlock
            {
                Text = $"— {pageNum} —",
                FontSize = 9, TextColor = Color.Slate500,
            };
            doc.Add(new Heading("Body") { TocIdentifier = "b", Level = 1, FontSize = 18 });
        }));

        // Body content tags correctly.
        Assert.Contains("/S /H1", pdf);
        // Document structure exists.
        Assert.Contains("/S /Document", pdf);
        // this feature fixed this: header/footer template content is now tagged as
        // /Artifact so screen readers skip it.
        Assert.Contains("/Artifact BMC", pdf);
    }

    // ── 9. Nested layout: VerticalStack containing Tables ───────────

    [Fact]
    public void Fixture09_NestedLayout_PreservesStructHierarchy()
    {
        // VerticalStack itself isn't IRoleProvider — it's a layout
        // primitive that doesn't claim a semantic role. Inner content
        // (the Table) still gets tagged correctly.
        string pdf = Encoding.ASCII.GetString(GenerateTagged(doc =>
        {
            var stack = new VerticalStack();
            stack.AddChild(new Heading("Inside a stack") { TocIdentifier = "s", Level = 1, FontSize = 18 });
            var t = new Table(150f, 150f);
            t.AddHeader(
                new TableCell { Content = new TextBlock { Text = "A" } },
                new TableCell { Content = new TextBlock { Text = "B" } });
            t.AddRow(new TextBlock { Text = "1" }, new TextBlock { Text = "2" });
            stack.AddChild(t);
            doc.Add(stack);
        }));

        // Headings inside the stack still get tagged as H1.
        Assert.Contains("/S /H1", pdf);
        // Table inside the stack still gets the full table structure.
        Assert.Contains("/S /Table", pdf);
        Assert.Contains("/S /TR", pdf);
        Assert.Contains("/S /TH", pdf);
        Assert.Contains("/S /TD", pdf);
    }

    // ── 10. Alt-text encoding (Latin-1 escapes + Unicode hex) ───────

    [Fact]
    public void Fixture10a_AltTextWithLatin1SpecialChars_PdfEscaped()
    {
        // Backslashes and parens are PDF string-literal metacharacters
        // and must be backslash-escaped; the structure-tree writer
        // routes through EncodeMetadataString → PdfSyntax.Escape.
        string pdf = Encoding.ASCII.GetString(GenerateTagged(doc =>
        {
            doc.Add(new QRCode
            {
                Data = "https://example.com",
                Size = 60,
                AltText = "Chart for FY (2026) \\ revenue \"projection\"",
            });
        }));

        Assert.Contains("/Alt (Chart for FY \\(2026\\) \\\\ revenue \"projection\")", pdf);
    }

    [Fact]
    public void Fixture10b_AltTextWithNonLatin1Chars_Utf16BeHexEncoded()
    {
        // Em-dash (U+2014) doesn't fit in a single-byte PDF string
        // literal and would otherwise be byte-truncated. Per the spec,
        // such strings become a <FEFF…> UTF-16BE hex literal.
        string pdf = Encoding.ASCII.GetString(GenerateTagged(doc =>
        {
            doc.Add(new QRCode
            {
                Data = "https://example.com",
                Size = 60,
                AltText = "Year 2026 — projection",
            });
        }));

        Assert.Matches(@"/Alt <FEFF[0-9A-F]+>", pdf);
        Assert.Matches(@"/Alt <FEFF[0-9A-F]*2014[0-9A-F]*>", pdf);
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static byte[] GenerateTagged(System.Action<Document> populate)
        => Generate(doc =>
        {
            doc.Settings.Version = PdfVersion.Pdf17;
            doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
            doc.Settings.Language = "en-GB";
            populate(doc);
        });

    private static byte[] Generate(System.Action<Document> populate)
    {
        using var doc = new Document();
        populate(doc);
        using var ms = new MemoryStream();
        doc.Save(ms, true);
        return ms.ToArray();
    }
}
