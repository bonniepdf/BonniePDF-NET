using System.Text;
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
        // Do NOT set FontSize or Color - leave them at defaults
        Generate(doc =>
        {
            doc.Settings.DefaultTextStyle = new TextStyle().WithSize(11).WithColor(ColorPalette.Slate[600]);
            doc.Add(tb);
        });
        // After Save (which calls ApplyDefaultStyleToTree), defaults should be applied
        Assert.Equal(11f, tb.FontSize);
        Assert.Equal(ColorPalette.Slate[600], tb.Color);
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
    public void DefaultTextStyle_Family_KeptWhenOnlyWeightIsExplicit()
    {
        var tb = new TextBlock { Text = "Hello", FontWeight = FontWeight.Bold }; // weight only
        Generate(doc =>
        {
            doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Times");
            doc.Add(tb);
        });
        Assert.Equal("Times-Bold", tb.FontName);
    }

    [Fact]
    public void DefaultTextStyle_Family_KeptWhenOnlyItalicIsExplicit()
    {
        var tb = new TextBlock { Text = "Hello", Italic = true };
        Generate(doc =>
        {
            doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Times");
            doc.Add(tb);
        });
        Assert.Equal("Times-Italic", tb.FontName);
    }

    [Fact]
    public void DefaultTextStyle_DoesNotOverrideExplicitFamily()
    {
        var tb = new TextBlock { Text = "Hello", FontFamily = "Courier", FontWeight = FontWeight.Bold };
        Generate(doc =>
        {
            doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Times");
            doc.Add(tb);
        });
        Assert.Equal("Courier-Bold", tb.FontName);
    }

    [Fact]
    public void DefaultTextStyle_DoesNotOverrideExplicitNormalWeight()
    {
        var tb = new TextBlock { Text = "Hello", FontWeight = FontWeight.Normal };
        Generate(doc =>
        {
            doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Times").WithWeight(FontWeight.Bold);
            doc.Add(tb);
        });
        Assert.Equal("Times-Roman", tb.FontName);
    }

    [Fact]
    public void DefaultTextStyle_Weight_AppliedWithoutFamily()
    {
        var tb = new TextBlock { Text = "Hello" };
        Generate(doc =>
        {
            doc.Settings.DefaultTextStyle = new TextStyle().WithWeight(FontWeight.Bold);
            doc.Add(tb);
        });
        Assert.Equal("Helvetica-Bold", tb.FontName);
    }

    [Fact]
    public void DefaultTextStyle_WeightAndItalic_NotAppliedToDirectlyNamedFont()
    {
        var tb = new TextBlock { Text = "Hello", FontName = "Courier" };
        Generate(doc =>
        {
            doc.Settings.DefaultTextStyle = new TextStyle().WithWeight(FontWeight.Bold).WithItalic();
            doc.Add(tb);
        });
        Assert.Equal("Courier", tb.FontName);
    }

    [Fact]
    public void DefaultTextStyle_Decoration_AppliedUnlessExplicit()
    {
        var plain = new TextBlock { Text = "Plain" };
        var explicitNone = new TextBlock { Text = "Explicit", Decoration = TextDecoration.None };
        Generate(doc =>
        {
            doc.Settings.DefaultTextStyle = new TextStyle { Decoration = TextDecoration.Underline };
            doc.Add(plain);
            doc.Add(explicitNone);
        });
        Assert.Equal(TextDecoration.Underline, plain.Decoration);
        Assert.Equal(TextDecoration.None, explicitNone.Decoration);
    }

    [Fact]
    public void DefaultTextStyle_ChangedDefault_AppliedOnLaterGeneration_ButNeverOverExplicitValues()
    {
        var cascaded = new TextBlock { Text = "Cascaded" };
        var explicitly = new TextBlock { Text = "Explicit", FontSize = 24, Color = Color.FromHex("#FF0000"), LineHeight = 2f };
        using var doc = new BonniePDF.Document();
        doc.Settings.DefaultTextStyle = new TextStyle().WithSize(11).WithColor(ColorPalette.Slate[600]).WithLineHeight(1.5f);
        doc.Add(cascaded);
        doc.Add(explicitly);
        doc.GenerateBytes();
        Assert.Equal(11f, cascaded.FontSize);

        doc.Settings.DefaultTextStyle = new TextStyle().WithSize(14).WithColor(ColorPalette.Slate[900]).WithLineHeight(1.3f);
        doc.GenerateBytes();
        Assert.Equal(14f, cascaded.FontSize);
        Assert.Equal(ColorPalette.Slate[900], cascaded.Color);
        Assert.Equal(1.3f, cascaded.LineHeight);
        Assert.Equal(24f, explicitly.FontSize);
        Assert.Equal(Color.FromHex("#FF0000"), explicitly.Color);
        Assert.Equal(2f, explicitly.LineHeight);
    }

    [Fact]
    public void DefaultTextStyle_Removed_ReturnsCascadedValuesToLibraryDefaults()
    {
        var tb = new TextBlock { Text = "Hello" };
        using var doc = new BonniePDF.Document();
        doc.Settings.DefaultTextStyle = new TextStyle { Decoration = TextDecoration.Underline }
            .WithFamily("Times").WithWeight(FontWeight.Bold).WithSize(20).WithColor(ColorPalette.Slate[600]).WithLineHeight(1.5f);
        doc.Add(tb);
        doc.GenerateBytes();
        Assert.Equal("Times-Bold", tb.FontName);
        Assert.Equal(20f, tb.FontSize);

        doc.Settings.DefaultTextStyle = null;
        doc.GenerateBytes();
        Assert.Equal("Helvetica", tb.FontName);
        Assert.Equal(12f, tb.FontSize);
        Assert.Equal(Color.Black, tb.Color);
        Assert.Equal(1.2f, tb.LineHeight);
        Assert.Equal(TextDecoration.None, tb.Decoration);
    }

    [Fact]
    public void DefaultTextStyle_DroppedProperty_ClearsOldValue_ExplicitValuesKept()
    {
        var cascaded = new TextBlock { Text = "Cascaded" };
        var explicitly = new TextBlock { Text = "Explicit", FontSize = 30, FontWeight = FontWeight.Bold };
        using var doc = new BonniePDF.Document();
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Times").WithSize(20);
        doc.Add(cascaded);
        doc.Add(explicitly);
        doc.GenerateBytes();
        Assert.Equal("Times-Bold", explicitly.FontName);

        doc.Settings.DefaultTextStyle = new TextStyle().WithSize(14); // no family any more
        doc.GenerateBytes();
        Assert.Equal("Helvetica", cascaded.FontName);
        Assert.Equal(14f, cascaded.FontSize);
        Assert.Equal("Helvetica-Bold", explicitly.FontName);

        doc.Settings.DefaultTextStyle = null;
        doc.GenerateBytes();
        Assert.Equal(30f, explicitly.FontSize);
        Assert.Equal("Helvetica-Bold", explicitly.FontName);
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
