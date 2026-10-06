using System.Collections.Generic;
using System.Linq;
using System.Text;
using BonniePDF.Tests.Accessibility;
using Xunit;

namespace BonniePDF.Tests.Compliance;

// Mirrors the shared fixture honest-claim/unembedded-standard-fonts and the
// TypeScript suite tests/compliance/honest-claim.test.ts: the honest-claim guard
// withholds the PDF/A and PDF/UA-1 claims when the standard-14 fonts cannot be
// embedded, keeps the tagging, reports one compliance warning per withheld claim,
// and never changes the document.
[Collection("Serial")]
public sealed class HonestClaimTests : System.IDisposable
{
    public HonestClaimTests()
    {
        // No fonts available: no bundled Liberation faces and no system fallback.
        FontManager.Clear();
        InternalConfiguration.AllowSystemFontFallback = false;
    }

    public void Dispose()
    {
        InternalConfiguration.AllowSystemFontFallback = true;
        FontManager.RestoreBundledFonts();
    }

    private static BonniePDF.Document FixtureDocument(PdfAConformance conformance, AccessibilityMode accessibility)
    {
        var doc = new BonniePDF.Document();
        doc.Metadata.Title = "Honest claims";
        doc.Settings.Language = "en-GB";
        doc.Settings.Conformance = conformance;
        doc.Settings.Accessibility = accessibility;
        doc.Add(new TextBlock { Text = "Honest claims", FontSize = 12 });
        return doc;
    }

    private sealed record Inspection(
        bool PdfaidClaim, bool PdfuaidClaim, bool OutputIntent, bool StructTreeRoot, bool MarkInfo, int EmbeddedFonts);

    private static Inspection Inspect(byte[] bytes)
    {
        // The XMP packet, the catalog and the font descriptors are uncompressed.
        string pdf = Encoding.Latin1.GetString(bytes);
        return new Inspection(
            pdf.Contains("pdfaid:part", System.StringComparison.Ordinal),
            pdf.Contains("pdfuaid:part", System.StringComparison.Ordinal),
            pdf.Contains("/OutputIntents", System.StringComparison.Ordinal),
            pdf.Contains("/StructTreeRoot", System.StringComparison.Ordinal),
            pdf.Contains("/MarkInfo", System.StringComparison.Ordinal),
            SubstringCounter.Count(pdf, "/FontFile2"));
    }

    private static List<(DiagnosticCategory Category, DiagnosticSeverity Severity, string? Source)> ComplianceDiagnostics(BonniePDF.Document doc) =>
        doc.Diagnostics.Where(d => d.Category == DiagnosticCategory.Compliance)
            .Select(d => (d.Category, d.Severity, d.Source))
            .ToList();

    [Fact]
    public void PdfAAndPdfUa_WithoutFonts_WithholdsBothClaims_KeepsTagging_ReportsEachOnce()
    {
        using var doc = FixtureDocument(PdfAConformance.PdfA2a, AccessibilityMode.PdfUa1);
        Inspection result = Inspect(doc.GenerateBytes());

        Assert.Equal(new Inspection(false, false, false, true, true, 0), result);
        Assert.Equal(
            new[]
            {
                (DiagnosticCategory.Compliance, DiagnosticSeverity.Warning, (string?)"pdf/a-2a"),
                (DiagnosticCategory.Compliance, DiagnosticSeverity.Warning, (string?)"pdf/ua-1"),
            },
            ComplianceDiagnostics(doc));
    }

    [Fact]
    public void PdfUaOnly_WithoutFonts_KeepsTheOutputIntent_WhichIsNotAClaim()
    {
        using var doc = FixtureDocument(PdfAConformance.None, AccessibilityMode.PdfUa1);
        Inspection result = Inspect(doc.GenerateBytes());

        Assert.Equal(new Inspection(false, false, true, true, true, 0), result);
        Assert.Equal(
            new[] { (DiagnosticCategory.Compliance, DiagnosticSeverity.Warning, (string?)"pdf/ua-1") },
            ComplianceDiagnostics(doc));
    }

    [Fact]
    public void LeavesTheDocumentUnchanged_SoALaterGenerationWithFontsCarriesTheFullClaims()
    {
        using var doc = FixtureDocument(PdfAConformance.PdfA2a, AccessibilityMode.PdfUa1);
        doc.GenerateBytes();
        Assert.Equal(PdfAConformance.PdfA2a, doc.Settings.Conformance);
        Assert.Equal(AccessibilityMode.PdfUa1, doc.Settings.Accessibility);

        FontManager.RestoreBundledFonts();
        Inspection result = Inspect(doc.GenerateBytes());

        Assert.True(result.PdfaidClaim);
        Assert.True(result.PdfuaidClaim);
        Assert.True(result.OutputIntent);
        Assert.True(result.StructTreeRoot);
        Assert.True(result.EmbeddedFonts >= 1);
        Assert.Empty(ComplianceDiagnostics(doc));
    }

    [Fact]
    public void ReportsNothing_WhenNoClaimWasRequested()
    {
        using var doc = FixtureDocument(PdfAConformance.None, AccessibilityMode.None);
        doc.GenerateBytes();
        Assert.Empty(ComplianceDiagnostics(doc));
    }
}
