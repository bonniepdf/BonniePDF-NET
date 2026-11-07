using System.Text;
using Xunit;

namespace BonniePDF.Tests.Accessibility;

[Collection("Serial")]

/// <summary>
/// Hyperlink tagging. A standalone <see cref="Hyperlink"/>
/// element in a tagged document produces a <c>/Link</c> structure
/// element wrapping the link text, and the link annotation carries
/// <c>/Contents</c> mirroring the visible text (VeraPDF profile
/// <c>ua1</c> rule 7-18).
///
/// Annotation reachability from the structure tree (<c>/OBJR</c>
/// + <c>/StructParent</c> + the corresponding <c>/ParentTree</c>
/// keys) is intentionally deferred to a follow-up within stage 1 -
/// see the design note in the matching commit message.
/// </summary>
public class HyperlinkTaggingTests
{
    private static byte[] GenerateBytes(System.Action<BonniePDF.Document> populate)
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        populate(doc);
        using var ms = new MemoryStream();
        doc.Save(ms, true);
        return ms.ToArray();
    }

    [Fact]
    public void Hyperlink_SemanticRoleIsLink()
    {
        var link = new Hyperlink("https://example.com", "Example");
        Assert.Equal(SemanticRole.Link, link.SemanticRole);
    }

    [Fact]
    public void TaggedDocument_HyperlinkEmitsLinkStructElem()
    {
        string pdf = Encoding.ASCII.GetString(GenerateBytes(doc =>
        {
            doc.Add(new Hyperlink("https://example.com", "Visit Example")
            {
                FontName = StandardFonts.Helvetica,
                FontSize = 12,
            });
        }));

        Assert.Contains("/S /Link", pdf);
    }

    [Fact]
    public void LinkAnnotation_CarriesContentsMatchingLinkText()
    {
        // Rule 7-18 - VeraPDF requires /Contents on every /Link
        // annotation in a tagged document. Emitted unconditionally so
        // untagged docs benefit too.
        string pdf = Encoding.ASCII.GetString(GenerateBytes(doc =>
        {
            doc.Add(new Hyperlink("https://example.com", "Visit Example")
            {
                FontName = StandardFonts.Helvetica,
                FontSize = 12,
            });
        }));

        // The annotation dict on a tagged page has both /Subtype /Link
        // and /Contents (Visit Example).
        Assert.Contains("/Subtype /Link", pdf);
        Assert.Contains("/Contents (Visit Example)", pdf);
    }

    [Fact]
    public void LinkAnnotation_DrawsNoBorder()
    {
        // Spec 11 §2 - the /Border default is [0 0 1], which many viewers
        // draw as a box around the link, so every link writes [0 0 0].
        string pdf = Encoding.ASCII.GetString(GenerateBytes(doc =>
        {
            doc.Add(new Hyperlink("https://example.com", "Visit Example")
            {
                FontName = StandardFonts.Helvetica,
                FontSize = 12,
            });
        }));

        Assert.Contains("/Border [0 0 0]", pdf);
    }

    [Fact]
    public void LinkContents_EscapesParenthesesInLinkText()
    {
        // Defensive - link text that contains literal parens needs PDF
        // escaping in the /Contents string literal.
        string pdf = Encoding.ASCII.GetString(GenerateBytes(doc =>
        {
            doc.Add(new Hyperlink("https://example.com", "Click here (now)")
            {
                FontName = StandardFonts.Helvetica,
                FontSize = 12,
            });
        }));

        // PdfSyntax.Escape wraps parens with backslashes.
        Assert.Contains("/Contents (Click here \\(now\\))", pdf);
    }

    [Fact]
    public void UntaggedDocument_HyperlinkStillGetsContents_NoStructElem()
    {
        // /Contents is emitted regardless of tagging. /Link struct elem
        // is gated on tagging.
        using var doc = new BonniePDF.Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = AccessibilityMode.None;
        doc.Add(new Hyperlink("https://example.com", "x")
        {
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
        });

        using var ms = new MemoryStream();
        doc.Save(ms, true);
        string pdf = Encoding.ASCII.GetString(ms.ToArray());

        Assert.Contains("/Contents (x)", pdf);
        Assert.DoesNotContain("/S /Link", pdf);
    }

    [Fact]
    public void TaggedDocument_LinkPagesStayBdcEmcBalanced()
    {
        InternalConfiguration.EnableCompression = false;
        try
        {
            string pdf = Encoding.ASCII.GetString(GenerateBytes(doc =>
            {
                doc.Add(new Hyperlink("https://a.example.com", "A") { FontSize = 12 });
                doc.Add(new Hyperlink("https://b.example.com", "B") { FontSize = 12 });
            }));

            string[] streams = PdfPageStreamSplitter.Split(pdf);
            Assert.NotEmpty(streams);
            foreach (string stream in streams)
            {
                int bdc = SubstringCounter.Count(stream, "BDC");
                int emc = SubstringCounter.Count(stream, "EMC");
                Assert.True(bdc == emc, $"unbalanced marked-content: BDC={bdc} EMC={emc}");
            }
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }
}
