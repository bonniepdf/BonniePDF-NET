using System.IO;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using Xunit;

namespace BonniePDF.Tests;

/// <summary>
/// this feature / this feature integration smoke tests. Builds a Document, opts into
/// PDF/UA-1, saves it, and inspects the raw bytes for the catalog
/// entries that the writer should now emit.
///
/// VeraPDF integration lives in <c>BonniePDF.Validation</c> — these
/// tests just verify that the byte-level wiring is present so a
/// validator run can pick up from here.
/// </summary>
public class TaggedDocumentTests
{
    [Fact]
    public void UntaggedDocumentEmitsNoStructTreeRoot()
    {
        string pdf = GeneratePdf(accessibility: AccessibilityMode.None, language: null);

        Assert.DoesNotContain("/StructTreeRoot", pdf);
        Assert.DoesNotContain("/Lang (", pdf);
        // /MarkInfo should be absent when neither PDF/A nor tagging is on.
        Assert.DoesNotContain("/MarkInfo", pdf);
    }

    [Fact]
    public void TaggedDocumentEmitsStructTreeRootAndMarkInfo()
    {
        string pdf = GeneratePdf(accessibility: AccessibilityMode.PdfUa1, language: "en-GB");

        Assert.Contains("/StructTreeRoot", pdf);
        Assert.Contains("/MarkInfo << /Marked true >>", pdf);
        Assert.Contains("/Lang (en-GB)", pdf);
        // The Document StructElem must appear as the StructTreeRoot's child.
        Assert.Contains("/Type /StructElem", pdf);
        Assert.Contains("/S /Document", pdf);
        // XMP carries the PDF/UA-1 conformance claim.
        Assert.Contains("pdfuaid:part", pdf);
    }

    [Fact]
    public void PdfAOnlyStillEmitsMarkInfoForBackCompat()
    {
        // Pre-G-1 behaviour: PdfAEnabled adds /MarkInfo. Make sure this
        // path is unchanged when tagging is off.
        string pdf = GeneratePdf(accessibility: AccessibilityMode.None, language: null, pdfA: true);

        Assert.Contains("/MarkInfo << /Marked true >>", pdf);
        Assert.DoesNotContain("/StructTreeRoot", pdf);
    }

    private static string GeneratePdf(AccessibilityMode accessibility, string language, bool pdfA = false)
    {
        using var doc = new Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = accessibility;
        doc.Settings.Language = language;
        doc.Settings.PdfAEnabled = pdfA;

        using var ms = new MemoryStream();
        doc.Save(ms, true);
        // The PDF body is ASCII-compatible up to any binary streams. Catalog
        // dict and StructElem dicts live in the ASCII portion, so reading
        // the whole thing as ASCII is sufficient for the string assertions
        // these tests do.
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}
