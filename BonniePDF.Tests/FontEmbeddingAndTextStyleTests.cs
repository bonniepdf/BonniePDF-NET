using System.IO;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests;

/// <summary>this feature Stage 2 and this feature tests.</summary>
public class FontEmbeddingAndTextStyleTests
{
    // ── this feature Stage 2: FontManager ─────────────────────────────────────

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
        // Clear fonts so auto-registration didn't happen in this registry instance
        FontManager.Clear();
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
            // Restore auto-registered fonts for subsequent tests
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

    // ── DefaultTextStyle cascade ────────────────────────────────

    [Fact]
    public void DefaultTextStyle_AppliedToUnstyledElements()
    {
        var tb = new TextBlock { Text = "Hello" };
        // Do NOT set FontSize or TextColor — leave them at defaults
        Generate(doc =>
        {
            doc.Settings.DefaultTextStyle = new TextStyle().WithSize(11).WithColor(Color.Slate600);
            doc.Add(tb);
        });
        // After Save (which calls ApplyDefaultStyleToTree), defaults should be applied
        Assert.Equal(11f, tb.FontSize);
        Assert.Equal(Color.Slate600, tb.TextColor);
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
    public void DefaultTextStyle_LineHeightMultiplier_AppliedWhenNotSet()
    {
        var tb = new TextBlock { Text = "Hello" };
        Generate(doc =>
        {
            doc.Settings.DefaultTextStyle = new TextStyle().WithLineHeight(1.5f);
            doc.Add(tb);
        });
        Assert.Equal(1.5f, tb.LineHeightMultiplier);
    }

    [Fact]
    public void DefaultTextStyle_LineHeightMultiplier_NotOverriddenWhenExplicit()
    {
        var tb = new TextBlock { Text = "Hello", LineHeightMultiplier = 2.0f }; // explicit
        Generate(doc =>
        {
            doc.Settings.DefaultTextStyle = new TextStyle().WithLineHeight(1.5f);
            doc.Add(tb);
        });
        Assert.Equal(2.0f, tb.LineHeightMultiplier); // should NOT be overridden
    }

    [Fact]
    public void DefaultTextStyle_Null_DoesNotThrow()
    {
        // DefaultTextStyle is null by default — Save should not throw
        var tb = new TextBlock { Text = "Hello" };
        var bytes = Generate(doc =>
        {
            doc.Add(tb);
        });
        Assert.NotEmpty(bytes);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static byte[] Generate(Action<Document> populate)
    {
        using var doc = new Document();
        populate(doc);
        using var ms = new MemoryStream();
        doc.Save(ms, true);
        return ms.ToArray();
    }
}
