using BonniePDF.Configuration;
using Xunit;

namespace BonniePDF.Tests.Accessibility;

/// <summary>
/// Round-trip tests for the new accessibility-related properties on
/// <see cref="DocumentSettings"/>. this feature of the this feature (PDF/UA) plan -
/// only verifies the public surface; no writer behaviour yet.
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
        var zugferdStyle = new DocumentSettings { PdfAEnabled = true, Accessibility = AccessibilityMode.None };
        Assert.True(zugferdStyle.PdfAEnabled);
        Assert.Equal(AccessibilityMode.None, zugferdStyle.Accessibility);

        // PDF/UA on, PDF/A off.
        var uaOnly = new DocumentSettings { PdfAEnabled = false, Accessibility = AccessibilityMode.PdfUa1 };
        Assert.False(uaOnly.PdfAEnabled);
        Assert.Equal(AccessibilityMode.PdfUa1, uaOnly.Accessibility);

        // Both on (e.g. PDF/A-3a + PDF/UA-1).
        var both = new DocumentSettings { PdfAEnabled = true, Accessibility = AccessibilityMode.PdfUa1 };
        Assert.True(both.PdfAEnabled);
        Assert.Equal(AccessibilityMode.PdfUa1, both.Accessibility);
    }
}
