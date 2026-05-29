using System.IO;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering.Tagging;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests;

[Collection("Serial")]

/// <summary>
/// VisualToc tagging. A tagged <see cref="VisualToc"/> emits a
/// <c>/TOC</c> structure element (via IRoleProvider auto-wrap on the
/// element) containing one <c>/TOCI</c> per entry (pushed directly by
/// <c>DrawTaggedTocEntry</c>).
///
/// The plan's TOCI > Reference + Lbl nested shape is deferred — the
/// current TOC renders heading text + page number as marked-content
/// inside each TOCI directly, which is a valid PDF/UA TOC
/// representation. Splitting the inner content into separate
/// Reference / Lbl structure elements is a stage-1 follow-up.
/// </summary>
public class VisualTocTaggingTests
{
    [Fact]
    public void VisualToc_SemanticRoleIsTOC()
    {
        var toc = new VisualToc();
        Assert.Equal(SemanticRole.TOC, toc.SemanticRole);
    }

    [Fact]
    public void TaggedDocument_VisualTocEmitsTocAndTociStructElems()
    {
        string pdf = GenerateTaggedTocPdf();

        Assert.Contains("/S /TOC", pdf);
        Assert.Contains("/S /TOCI", pdf);
    }

    [Fact]
    public void TociCountMatchesHeadingCount()
    {
        // Three headings registered, three matching TOC entries — should
        // produce exactly three TOCI struct elements.
        string pdf = GenerateTaggedTocPdf();
        Assert.Equal(3, SubstringCounter.Count(pdf, "/S /TOCI"));
    }

    [Fact]
    public void UntaggedVisualToc_EmitsNoTocStructElems()
    {
        // Back-compat: accessibility off => no /S /TOC, no /S /TOCI.
        string pdf = GenerateTocPdf(accessibility: AccessibilityMode.None);
        Assert.DoesNotContain("/S /TOC", pdf);
        Assert.DoesNotContain("/S /TOCI", pdf);
    }

    [Fact]
    public void TaggedVisualToc_PagesStayBdcEmcBalanced()
    {
        // TOC is a container role that emits BMC; TOCI entries emit BDC.
        // Count opens = BDC + BMC to get the correct balance.
        InternalConfiguration.EnableCompression = false;
        try
        {
            string pdf = GenerateTaggedTocPdf();
            string[] streams = PdfPageStreamSplitter.Split(pdf);
            Assert.NotEmpty(streams);
            foreach (string stream in streams)
            {
                int opens = SubstringCounter.Count(stream, "BDC") + SubstringCounter.Count(stream, "BMC");
                int emc = SubstringCounter.Count(stream, "EMC");
                Assert.True(opens == emc, $"unbalanced marked-content: opens={opens} EMC={emc}");
            }
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static string GenerateTaggedTocPdf() => GenerateTocPdf(accessibility: AccessibilityMode.PdfUa1);

    private static string GenerateTocPdf(AccessibilityMode accessibility)
    {
        using var doc = new Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = accessibility;
        doc.Settings.Language = "en-GB";

        // Three headings then a VisualToc that references them.
        doc.Add(new Heading("Chapter 1") { TocIdentifier = "ch1", Level = 1, FontSize = 24 });
        doc.Add(new TextBlock { Text = "Content of chapter 1.", FontSize = 12 });
        doc.Add(new Heading("Chapter 2") { TocIdentifier = "ch2", Level = 1, FontSize = 24 });
        doc.Add(new TextBlock { Text = "Content of chapter 2.", FontSize = 12 });
        doc.Add(new Heading("Chapter 3") { TocIdentifier = "ch3", Level = 1, FontSize = 24 });
        doc.Add(new TextBlock { Text = "Content of chapter 3.", FontSize = 12 });

        var toc = new VisualToc
        {
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
        };
        toc.HeadingIdentifiers.Add("ch1");
        toc.HeadingIdentifiers.Add("ch2");
        toc.HeadingIdentifiers.Add("ch3");
        doc.Add(toc);

        using var ms = new MemoryStream();
        doc.Save(ms, true);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}
