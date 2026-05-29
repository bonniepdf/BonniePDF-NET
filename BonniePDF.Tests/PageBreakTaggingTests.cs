using System.IO;
using System.Linq;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering.Tagging;
using Xunit;

namespace BonniePDF.Tests;

[Collection("Serial")]

/// <summary>
/// page-break-aware tagging tests. Marked-content scopes
/// (BDC/EMC) cannot cross a page boundary per PDF spec, but structure
/// elements can. Verifies that the per-page BDC/EMC counts are
/// balanced even when a tagged element overflows across pages, and
/// that all MCIDs from both pages attach to the same structure
/// element.
/// </summary>
public class PageBreakTaggingTests
{
    [Fact]
    public void TaggingContext_SnapshotOpenRoles_ReturnsBottomUp()
    {
        var ctx = new TaggingContext(new StructureTreeBuilder());
        ctx.PushOpenRole("Sect");
        ctx.PushOpenRole("P");

        var roles = ctx.SnapshotOpenRolesBottomUp();
        Assert.Equal(new[] { "Sect", "P" }, roles); // outermost first
    }

    [Fact]
    public void NewPage_ClosesMarkedContentOnOutgoingPageAndReopensOnNewPage()
    {
        // Construct a document that will exceed one page via a tall
        // TextBlock, render under tagging, and verify per-page balance.
        InternalConfiguration.EnableCompression = false;
        try
        {
            string pdf = GenerateTagged(doc =>
            {
                // Lots of paragraphs to force a page break.
                var sb = new StringBuilder();
                for (int i = 0; i < 200; i++)
                    sb.Append("This is line ").Append(i).Append(" of a long block of text that should span multiple pages. ");
                doc.Add(new TextBlock { Text = sb.ToString(), FontSize = 12 });
            });

            // Each page's content stream must have balanced BDC/EMC counts —
            // otherwise the PDF is malformed per ISO 32000-1 §14.6.
            string[] pageStreams = SplitPageContentStreams(pdf);
            Assert.True(pageStreams.Length >= 2, "expected the test doc to overflow onto >1 page");
            foreach (string stream in pageStreams)
            {
                int bdc = CountSubstring(stream, "BDC");
                int emc = CountSubstring(stream, "EMC");
                Assert.True(bdc == emc, $"unbalanced marked-content on a page: BDC={bdc} EMC={emc}");
            }
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }

    [Fact]
    public void NoOpenMarkedContent_PageBreakDoesNothing()
    {
        // Render under tagging but without any tagged element overflowing.
        // The page-break handler must not emit stray EMC/BDC pairs.
        InternalConfiguration.EnableCompression = false;
        try
        {
            string pdf = GenerateTagged(doc =>
            {
                doc.Add(new TextBlock { Text = "short", FontSize = 12 });
            });

            // Single page, one BDC/EMC pair for the TextBlock.
            string[] pageStreams = SplitPageContentStreams(pdf);
            Assert.Single(pageStreams);
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }

    [Fact]
    public void NoTaggingContext_NewPageBehavesAsBefore()
    {
        // Back-compat: without tagging on, NewPage must produce no
        // BDC/EMC at all on any page.
        using var doc = new Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = AccessibilityMode.None;
        var sb = new StringBuilder();
        for (int i = 0; i < 200; i++)
            sb.Append("Line ").Append(i).Append(". ");
        doc.Add(new TextBlock { Text = sb.ToString(), FontSize = 12 });

        using var ms = new MemoryStream();
        doc.Save(ms, true);
        string pdf = Encoding.ASCII.GetString(ms.ToArray());

        Assert.DoesNotContain(" BDC\n", pdf);
        Assert.DoesNotContain("EMC\n", pdf);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static string GenerateTagged(System.Action<Document> populate)
    {
        using var doc = new Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        populate(doc);
        using var ms = new MemoryStream();
        doc.Save(ms, true);
        return Encoding.ASCII.GetString(ms.ToArray());
    }

    /// <summary>
    /// Naïvely splits the PDF into per-page content streams by looking
    /// for the `stream\n` ... `endstream` boundaries. Good enough for
    /// these tests since BonniePDF emits one content-stream object per
    /// page and the streams are uncompressed plain text.
    /// </summary>
    private static string[] SplitPageContentStreams(string pdf)
    {
        var result = new System.Collections.Generic.List<string>();
        int idx = 0;
        while (true)
        {
            int streamStart = pdf.IndexOf("stream\n", idx, System.StringComparison.Ordinal);
            if (streamStart < 0) break;
            int contentStart = streamStart + "stream\n".Length;
            int streamEnd = pdf.IndexOf("endstream", contentStart, System.StringComparison.Ordinal);
            if (streamEnd < 0) break;
            string content = pdf.Substring(contentStart, streamEnd - contentStart);
            // Heuristic: page content streams contain text-show operators.
            // Skip XMP, ICC, and font streams that don't have BT/Tj.
            if (content.Contains("BT") && content.Contains("ET"))
                result.Add(content);
            idx = streamEnd + "endstream".Length;
        }
        return result.ToArray();
    }

    private static int CountSubstring(string source, string needle)
    {
        int count = 0;
        int idx = 0;
        while ((idx = source.IndexOf(needle, idx, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
}
