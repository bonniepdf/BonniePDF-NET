using Xunit;

namespace BonniePDF.Tests.Accessibility;

/// <summary>
/// Round-trip tests for the new accessibility-related properties on
/// <see cref="DocumentSettings"/>. Verifies the public surface only;
/// writer behaviour is covered separately.
/// </summary>
public class DocumentSettingsAccessibilityTests
{
    [Fact]
    public void DefaultAccessibilityIsNone()
    {
        var settings = new DocumentSettings();
        Assert.Equal(AccessibilityMode.None, settings.Accessibility);
    }

    [Fact]
    public void DefaultLanguageIsNull()
    {
        var settings = new DocumentSettings();
        Assert.Null(settings.Language);
    }

    [Fact]
    public void AccessibilityRoundTrips()
    {
        var settings = new DocumentSettings { Accessibility = AccessibilityMode.PdfUa1 };
        Assert.Equal(AccessibilityMode.PdfUa1, settings.Accessibility);
    }

    [Fact]
    public void LanguageRoundTrips()
    {
        var settings = new DocumentSettings { Language = "en-GB" };
        Assert.Equal("en-GB", settings.Language);
    }

    [Fact]
    public void AccessibilityAndPdfAAreOrthogonal()
    {
        // ZUGFeRD-style: PDF/A on, accessibility off.
        var zugferdStyle = new DocumentSettings { Conformance = PdfAConformance.PdfA3b, Accessibility = AccessibilityMode.None };
        Assert.Equal(PdfAConformance.PdfA3b, zugferdStyle.Conformance);
        Assert.Equal(AccessibilityMode.None, zugferdStyle.Accessibility);

        // PDF/UA on, PDF/A off.
        var uaOnly = new DocumentSettings { Conformance = PdfAConformance.None, Accessibility = AccessibilityMode.PdfUa1 };
        Assert.Equal(PdfAConformance.None, uaOnly.Conformance);
        Assert.Equal(AccessibilityMode.PdfUa1, uaOnly.Accessibility);

        // Both on (e.g. PDF/A-3a + PDF/UA-1).
        var both = new DocumentSettings { Conformance = PdfAConformance.PdfA3a, Accessibility = AccessibilityMode.PdfUa1 };
        Assert.Equal(PdfAConformance.PdfA3a, both.Conformance);
        Assert.Equal(AccessibilityMode.PdfUa1, both.Accessibility);
    }

    [Fact]
    public void ConformanceIsTheOnlyPdfASetting()
    {
        // The legacy PdfAEnabled alias was removed before first release.
        Assert.Null(typeof(DocumentSettings).GetProperty("PdfAEnabled"));
    }
}
