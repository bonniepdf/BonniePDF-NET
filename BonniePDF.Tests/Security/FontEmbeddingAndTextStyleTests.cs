using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests.Security;

/// <summary>Font embedding, PDF/A claim honesty, and the DefaultTextStyle cascade.</summary>
[Collection("Serial")]
public class FontEmbeddingAndTextStyleTests
{
    // ── FontManager ──────────────────────────────────────────────────

    [Fact]
    public void FontManager_HasBundledFonts_AfterStaticInit()
    {
        // Static ctor auto-registers Liberation fonts from embedded resources
        Assert.True(FontManager.HasBundledFonts());
        Assert.NotNull(FontManager.GetBytesForStandardFont("Helvetica"));
        Assert.NotNull(FontManager.GetBytesForStandardFont("Helvetica-Bold"));
        Assert.NotNull(FontManager.GetBytesForStandardFont("Times-Roman"));
        Assert.NotNull(FontManager.GetBytesForStandardFont("Courier"));
        Assert.Null(FontManager.GetBytesForStandardFont("Symbol")); // not in map
    }

    [Fact]
    public void FontManager_RegisterFontFromStream_CanLookUpByFamily()
    {
        byte[]? bytes = FontManager.GetBytesForStandardFont("Helvetica");
        Assert.NotNull(bytes);
        using var ms = new MemoryStream(bytes);
        FontManager.RegisterFont(ms, "TestCustomG3", "Regular");
        Assert.NotNull(FontManager.GetBytesForFamily("TestCustomG3", "Regular"));
    }

    [Fact]
    public void PdfA_ClaimSuppressed_WhenStandardFontsUsed()
    {
        // Simulate an environment with no embeddable fonts: clear bundled fonts
        // AND disable the OS system-font fallback so there is genuinely no TTF
        // available for Helvetica. The serializer must then drop the PDF/A claim.
        FontManager.Clear();
        InternalConfiguration.AllowSystemFontFallback = false;
        try
        {
            string pdf = Encoding.ASCII.GetString(Generate(doc =>
            {
                doc.Settings.Version = PdfVersion.Pdf17;
                doc.Settings.PdfAEnabled = true;
                doc.Add(new TextBlock { Text = "Test", FontName = StandardFonts.Helvetica });
            }));
            // Without embedded fonts, the PDF/A claim should be suppressed
            Assert.DoesNotContain("pdfaid:part", pdf);
        }
        finally
        {
            InternalConfiguration.AllowSystemFontFallback = true;
            FontManager.RestoreBundledFonts();
        }
    }

    [Fact]
    public void PdfA_ClaimEmitted_WhenFontsRegistered()
    {
        // FontManager.HasBundledFonts() should be true from static ctor (or restored)
        Assert.True(FontManager.HasBundledFonts());
        string pdf = Encoding.ASCII.GetString(Generate(doc =>
        {
            doc.Settings.Version = PdfVersion.Pdf17;
            doc.Settings.PdfAEnabled = true;
            doc.Add(new TextBlock { Text = "Test", FontName = StandardFonts.Helvetica });
        }));
        Assert.Contains("pdfaid:part", pdf);
        Assert.Contains("/OutputIntents", pdf);
    }

    // ── PDF/UA-1 font embedding (veraPDF 7.21.4.1) ───────────────────

    [Fact]
    public void PdfUa1_EmbedsStandardFonts_WithoutPdfAClaim()
    {
        // PDF/UA-1 mandates font embedding independently of PDF/A. A tagged
        // document that uses Helvetica must substitute the embedded Liberation
        // face, not reference an unembedded /Type1 standard font.
        Assert.True(FontManager.HasBundledFonts());
        string pdf = Encoding.ASCII.GetString(Generate(doc =>
        {
            doc.Settings.Version = PdfVersion.Pdf17;
            doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
            doc.Add(new TextBlock { Text = "Accessible text", FontName = StandardFonts.Helvetica });
        }));
        Assert.Contains("/FontFile2", pdf); // embedded TrueType program
        Assert.Contains("LiberationSans", pdf); // metric-compatible substitute
        Assert.DoesNotContain("/BaseFont /Helvetica", pdf); // no unembedded standard-14
    }

    [Fact]
    public void PlainDocument_LeavesStandardFontsUnembedded()
    {
        // Regression guard: the UA-1 embedding path must not leak into ordinary
        // documents, where standard-14 fonts are legitimately referenced without
        // embedding.
        string pdf = Encoding.ASCII.GetString(Generate(doc =>
        {
            doc.Settings.Version = PdfVersion.Pdf17;
            doc.Add(new TextBlock { Text = "Plain text", FontName = StandardFonts.Helvetica });
        }));
        Assert.Contains("/BaseFont /Helvetica", pdf);
        Assert.DoesNotContain("/FontFile2", pdf);
    }

    // ── DefaultTextStyle cascade ─────────────────────────────────────

    [Fact]
    public void DefaultTextStyle_AppliedToUnstyledElements()
    {
        var tb = new TextBlock { Text = "Hello" };
        // Do NOT set FontSize or TextColor - leave them at defaults
        Generate(doc =>
        {
            doc.Settings.DefaultTextStyle = new TextStyle().WithSize(11).WithColor(ColorPalette.Slate[600]);
            doc.Add(tb);
        });
        // After Save (which calls ApplyDefaultStyleToTree), defaults should be applied
        Assert.Equal(11f, tb.FontSize);
        Assert.Equal(ColorPalette.Slate[600], tb.TextColor);
    }

    [Fact]
    public void DefaultTextStyle_DoesNotOverrideExplicitlySetProperties()
    {
        var tb = new TextBlock { Text = "Hello", FontSize = 24 }; // explicit
        Generate(doc =>
        {
            doc.Settings.DefaultTextStyle = new TextStyle().WithSize(11);
            doc.Add(tb);
        });
        Assert.Equal(24f, tb.FontSize); // should NOT be overridden
    }

    [Fact]
    public void DefaultTextStyle_LineHeight_AppliedWhenNotSet()
    {
        var tb = new TextBlock { Text = "Hello" };
        Generate(doc =>
        {
            doc.Settings.DefaultTextStyle = new TextStyle().WithLineHeight(1.5f);
            doc.Add(tb);
        });
        Assert.Equal(1.5f, tb.LineHeight);
    }

    [Fact]
    public void DefaultTextStyle_LineHeight_NotOverriddenWhenExplicit()
    {
        var tb = new TextBlock { Text = "Hello", LineHeight = 2.0f }; // explicit
        Generate(doc =>
        {
            doc.Settings.DefaultTextStyle = new TextStyle().WithLineHeight(1.5f);
            doc.Add(tb);
        });
        Assert.Equal(2.0f, tb.LineHeight); // should NOT be overridden
    }

    [Fact]
    public void DefaultTextStyle_Null_DoesNotThrow()
    {
        // DefaultTextStyle is null by default - Save should not throw
        var tb = new TextBlock { Text = "Hello" };
        var bytes = Generate(doc =>
        {
            doc.Add(tb);
        });
        Assert.NotEmpty(bytes);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static byte[] Generate(Action<BonniePDF.Document> populate)
    {
        using var doc = new BonniePDF.Document();
        populate(doc);
        using var ms = new MemoryStream();
        doc.Save(ms, true);
        return ms.ToArray();
    }
}
