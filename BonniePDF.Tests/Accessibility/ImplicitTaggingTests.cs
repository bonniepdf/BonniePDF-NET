using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Elements.Text;
using Xunit;

namespace BonniePDF.Tests.Accessibility;

[Collection("Serial")]

/// <summary>
/// Implicit tagging tests. Asserts that existing elements
/// (Heading, TextBlock) automatically emit structure elements and
/// marked-content sequences when the document opts into
/// <see cref="AccessibilityMode.PdfUa1"/>. No source-level semantic
/// verbs needed - authors just flip the flag.
/// </summary>
public class ImplicitTaggingTests
{
    [Fact]
    public void HeadingAndTextBlocks_GenerateHAndPStructElems()
    {
        InternalConfiguration.EnableCompression = false;
        try
        {
            string pdf = GenerateTagged(doc =>
            {
                doc.Add(new Heading("Title") { TocIdentifier = "h1", Level = 1, FontSize = 24 });
                doc.Add(new TextBlock { Text = "First paragraph.", FontSize = 12 });
                doc.Add(new TextBlock { Text = "Second paragraph.", FontSize = 12 });
            });

            // Each Heading + TextBlock contributes a StructElem in the tree.
            Assert.Contains("/S /Document", pdf);
            Assert.Contains("/S /H1", pdf);
            Assert.Contains("/S /P", pdf);

            // The canvas wraps each tagged element in BDC/EMC. With three
            // tagged elements we expect at least three BDC + three EMC pairs.
            Assert.True(CountSubstring(pdf, "BDC") >= 3,
                $"expected >=3 BDC operators, got {CountSubstring(pdf, "BDC")}");
            Assert.True(CountSubstring(pdf, "EMC") >= 3,
                $"expected >=3 EMC operators, got {CountSubstring(pdf, "EMC")}");
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }

    [Fact]
    public void HeadingLevel_MapsToCorrectStructRole()
    {
        string pdf = GenerateTagged(doc =>
        {
            doc.Add(new Heading("Top") { TocIdentifier = "a", Level = 1, FontSize = 24 });
            doc.Add(new Heading("Sub") { TocIdentifier = "b", Level = 2, FontSize = 18 });
            doc.Add(new Heading("SubSub") { TocIdentifier = "c", Level = 3, FontSize = 14 });
        });

        Assert.Contains("/S /H1", pdf);
        Assert.Contains("/S /H2", pdf);
        Assert.Contains("/S /H3", pdf);
    }

    [Fact]
    public void UntaggedDocument_EmitsNoStructElemAndNoMarkedContent()
    {
        // The back-compat invariant from the plan: when Accessibility =
        // None, existing elements produce the same content streams they
        // always have - no /StructElem, no BDC, no EMC.
        string pdf = GeneratePdf(doc =>
        {
            doc.Settings.Accessibility = AccessibilityMode.None;
            doc.Add(new Heading("Title") { TocIdentifier = "h1", Level = 1, FontSize = 24 });
            doc.Add(new TextBlock { Text = "Paragraph.", FontSize = 12 });
        });

        Assert.DoesNotContain("/Type /StructElem", pdf);
        Assert.DoesNotContain(" BDC", pdf);
        Assert.DoesNotContain("EMC\n", pdf);
    }

    [Fact]
    public void TaggedDocument_HeadingLevelDefaultsToOne()
    {
        // Heading's Level property defaults to 1 - existing call sites
        // that don't set Level get H1 implicitly.
        string pdf = GenerateTagged(doc =>
        {
            doc.Add(new Heading("No level set") { TocIdentifier = "x", FontSize = 24 });
        });

        Assert.Contains("/S /H1", pdf);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static string GenerateTagged(System.Action<BonniePDF.Document> populate)
    {
        return GeneratePdf(doc =>
        {
            doc.Settings.Version = PdfVersion.Pdf17;
            doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
            doc.Settings.Language = "en-GB";
            populate(doc);
        });
    }

    private static string GeneratePdf(System.Action<BonniePDF.Document> populate)
    {
        using var doc = new BonniePDF.Document();
        populate(doc);
        using var ms = new MemoryStream();
        doc.Save(ms, true);
        return Encoding.ASCII.GetString(ms.ToArray());
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
