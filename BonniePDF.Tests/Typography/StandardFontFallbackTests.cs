using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BonniePDF.Tests.Support;
using BonniePDF.Typography.TrueType;
using Xunit;

namespace BonniePDF.Tests.Typography;

/// <summary>
/// Shared fixture fonts/standard-font-fallback: text in a non-embedded standard text face falls back for
/// characters outside WinAnsiEncoding, first to the face's Liberation substitute. A family that resolves to
/// nothing is reported, and <see cref="DocumentSettings.MissingGlyphs"/> decides whether a missing character is
/// dropped or is an error.
/// </summary>
[Collection("Serial")]
public sealed partial class StandardFontFallbackTests : System.IDisposable
{
    public StandardFontFallbackTests() => InternalConfiguration.EnableCompression = false;

    public void Dispose()
    {
        InternalConfiguration.EnableCompression = true;
        FontManager.Clear();
        FontManager.RestoreBundledFonts();
    }

    private static byte[] NotoSans() =>
        File.ReadAllBytes(Path.Combine(System.AppContext.BaseDirectory, "Typography", "Fixtures", "NotoSans-Regular.ttf"));

    private sealed record Run(string Font, string Text);

    /// <summary>The runs one draw is painted as: a standard face by its name, an embedded face by its PostScript name.</summary>
    private static Run[] RunsOf(string fontName, string text, BonniePDF.Document? document = null)
    {
        var registry = new PdfResourceRegistry();
        using System.IDisposable scope = FontMetrics.BeginDocumentScope(false, document?.Fonts);
        return registry.ResolveTextRuns(fontName, text)
            .Select(run => new Run(run.EmbeddedFont?.PostScriptName ?? run.FontIdentifier, run.Text))
            .ToArray();
    }

    [GeneratedRegex(@"/Subtype /CIDFontType2\s+/BaseFont /[A-Z]{6}\+(?<name>\S+)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex EmbeddedFont();

    private sealed record Generated(string[] Embedded, string Pdf, Diagnostic[] Warnings);

    private static async Task<Generated> GenerateAsync((string Font, string Text)[] draws, System.Action<BonniePDF.Document>? configure = null)
    {
        using var document = new BonniePDF.Document();
        configure?.Invoke(document);
        foreach ((string font, string text) in draws) document.Add(new TextBlock(text) { FontName = font, FontSize = 12 });
        string pdf = Encoding.Latin1.GetString(await document.GenerateBytesAsync());
        string[] embedded = EmbeddedFont().Matches(pdf).Select(m => m.Groups["name"].Value).Order(System.StringComparer.Ordinal).ToArray();
        Diagnostic[] warnings = document.Diagnostics.Where(d => d.Category == DiagnosticCategory.Font).ToArray();
        return new Generated(embedded, pdf, warnings);
    }

    [Fact]
    public async Task CoveredTextIsUnchanged()
    {
        const string text = "Café €5 – naïve ‘quoted’";
        Assert.Equal(new[] {new Run("Helvetica", text)}, RunsOf("Helvetica", text));
        Generated output = await GenerateAsync([("Helvetica", text)]);
        Assert.Empty(output.Embedded);
        Assert.Contains("(Café \u00805 \u0096 naïve \u0091quoted\u0092)", output.Pdf, System.StringComparison.Ordinal);
        Assert.Empty(output.Warnings);
    }

    [Fact]
    public async Task LatinExtendedUsesTheSubstitute()
    {
        Assert.Equal(
            new[] {new Run("LiberationSans", "Ł"), new Run("Helvetica", "ód"), new Run("LiberationSans", "ź")},
            RunsOf("Helvetica", "Łódź"));
        Generated output = await GenerateAsync([("Helvetica", "Łódź")]);
        Assert.Equal(new[] {"LiberationSans"}, output.Embedded);
        Assert.DoesNotContain("(?", output.Pdf, System.StringComparison.Ordinal);
        Assert.Empty(output.Warnings);
    }

    [Fact]
    public async Task SubstituteMatchesWeightAndStyle()
    {
        Assert.Equal(new[] {new Run("LiberationSans-Bold", "Ż")}, RunsOf("Helvetica-Bold", "Ż"));
        Assert.Equal(new[] {new Run("LiberationSerif-Italic", "Ω")}, RunsOf("Times-Italic", "Ω"));
        Assert.Equal(new[] {new Run("LiberationMono", "Ж")}, RunsOf("Courier", "Ж"));
        Generated output = await GenerateAsync([("Helvetica-Bold", "Ż"), ("Times-Italic", "Ω"), ("Courier", "Ж")]);
        Assert.Equal(new[] {"LiberationMono", "LiberationSans-Bold", "LiberationSerif-Italic"}, output.Embedded);
    }

    [Fact]
    public void AClusterFallsBackWhole() =>
        Assert.Equal(
            new[] {new Run("Helvetica", "caf"), new Run("LiberationSans", "é")},
            RunsOf("Helvetica", "café"));

    [Fact]
    public async Task ListBullet()
    {
        Assert.Equal(new[] {new Run("LiberationSans", "◦"), new Run("Helvetica", " item")}, RunsOf("Helvetica", "◦ item"));
        Generated output = await GenerateAsync([("Helvetica", "◦ item")]);
        Assert.DoesNotContain("(?", output.Pdf, System.StringComparison.Ordinal);
        Assert.Contains("( item)", output.Pdf, System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubstituteThenDocumentFaces()
    {
        using var document = new BonniePDF.Document();
        document.Fonts.Add(NotoSans());
        Assert.Equal(
            new[] {new Run("LiberationSans", "Ł"), new Run("Helvetica", " "), new Run("NotoSans-Regular", "₹")},
            RunsOf("Helvetica", "Ł ₹", document));
        Generated output = await GenerateAsync([("Helvetica", "Ł ₹")], d => d.Fonts.Add(NotoSans()));
        Assert.Equal(new[] {"LiberationSans", "NotoSans-Regular"}, output.Embedded);
        Assert.Empty(output.Warnings);
    }

    [Fact]
    public async Task MissingCharacterIsDropped()
    {
        Assert.Equal(new[] {new Run("Helvetica", " done")}, RunsOf("Helvetica", "✓ done"));
        Generated output = await GenerateAsync([("Helvetica", "✓ done")]);
        Assert.Empty(output.Embedded);
        Assert.Contains("( done)", output.Pdf, System.StringComparison.Ordinal);
        Diagnostic warning = Assert.Single(output.Warnings);
        Assert.Equal("Helvetica", warning.Source);
        Assert.Contains("U+2713", warning.Message, System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingCharacterThrows()
    {
        using var document = new BonniePDF.Document();
        document.Settings.MissingGlyphs = MissingGlyphs.Throw;
        document.Add(new TextBlock("✓ done") { FontName = "Helvetica" });
        MissingGlyphException error = await Assert.ThrowsAsync<MissingGlyphException>(() => document.GenerateBytesAsync());
        Assert.Equal(new[] {0x2713}, error.CodePoints);
        Assert.Equal("Helvetica", error.FontName);
        Assert.Contains("U+2713", error.Message, System.StringComparison.Ordinal);
        Assert.Throws<MissingGlyphException>(() => document.GenerateBytes());
    }

    [Fact]
    public async Task ThrowDoesNotAffectCoveredText()
    {
        Generated output = await GenerateAsync([("Helvetica", "Łódź")], d => d.Settings.MissingGlyphs = MissingGlyphs.Throw);
        Assert.Equal(new[] {"LiberationSans"}, output.Embedded);
        Assert.Empty(output.Warnings);
    }

    [Fact]
    public async Task NoSubstituteRegistered()
    {
        FontManager.Clear();
        using var document = new BonniePDF.Document();
        document.Fonts.Add(NotoSans());
        Assert.Equal(new[] {new Run("NotoSans-Regular", "Ł"), new Run("Helvetica", "x")}, RunsOf("Helvetica", "Łx", document));
        Generated output = await GenerateAsync([("Helvetica", "Łx")], d => d.Fonts.Add(NotoSans()));
        Assert.Equal(new[] {"NotoSans-Regular"}, output.Embedded);
        Assert.Empty(output.Warnings);
    }

    [Fact]
    public async Task NoSubstituteAndNoFaces()
    {
        FontManager.Clear();
        Assert.Equal(new[] {new Run("Helvetica", "x")}, RunsOf("Helvetica", "Łx"));
        Generated output = await GenerateAsync([("Helvetica", "Łx")]);
        Assert.Empty(output.Embedded);
        Diagnostic warning = Assert.Single(output.Warnings);
        Assert.Contains("U+0141", warning.Message, System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmbeddingForcedIsUnchanged()
    {
        Generated output = await GenerateAsync([("Helvetica", "Łódź")], d =>
        {
            d.Settings.Accessibility = AccessibilityMode.PdfUa1;
            d.Settings.Language = "pl";
        });
        Assert.Equal(new[] {"LiberationSans"}, output.Embedded);
        Assert.Empty(output.Warnings);
    }

    [Fact]
    public void MeasurementMatchesTheRuns()
    {
        TtfFont sans = TtfFont.LoadFromBytes(File.ReadAllBytes(TestFonts.SansRegularPath));
        double expected = sans.Shape("Ł").TotalAdvanceX() * 10.0 / sans.UnitsPerEm + FontMetrics.GetStringWidth("x", "Helvetica", 10);
        Assert.Equal(expected, FontMetrics.GetStringWidth("Łx", "Helvetica", 10), 3);
    }

    [Fact]
    public async Task UnknownFamilyIsReportedOnce()
    {
        using var document = new BonniePDF.Document();
        document.Add(new TextBlock("One") { FontFamily = "Papyrus" });
        document.Add(new TextBlock("Two") { FontFamily = "Papyrus" });
        document.Add(new TextBlock("Three") { FontFamily = "Arial" });
        string pdf = Encoding.Latin1.GetString(await document.GenerateBytesAsync());
        Assert.Contains("/BaseFont /Helvetica", pdf, System.StringComparison.Ordinal);
        Diagnostic warning = Assert.Single(document.Diagnostics, d => d.Category == DiagnosticCategory.Font);
        Assert.Equal("Papyrus", warning.Source);
        Assert.Contains("Papyrus", warning.Message, System.StringComparison.Ordinal);
        Assert.Contains("Helvetica", warning.Message, System.StringComparison.Ordinal);
    }

    [Fact]
    public void SvgExportPaintsTheRuns()
    {
        using var document = new BonniePDF.Document();
        document.Add(new TextBlock("Łódź") { FontName = "Helvetica", FontSize = 12 });
        string svg = document.GenerateSvgPages()[0];
        Assert.DoesNotMatch(@"<text[^>]*font-family=""Helvetica[^""]*""[^>]*>[^<]*ód", svg);
        Assert.Contains("liberation-sans", svg, System.StringComparison.Ordinal);
    }

    [Fact]
    public void SvgExportDoesNotLetTheViewerDrawADroppedCharacter()
    {
        using var document = new BonniePDF.Document();
        document.Add(new TextBlock("✓ done") { FontName = "Helvetica", FontSize = 12 });
        string svg = document.GenerateSvgPages()[0];
        Assert.DoesNotContain("✓", svg, System.StringComparison.Ordinal);
        Assert.DoesNotContain("&#x2713;", svg, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("&#10003;", svg, System.StringComparison.Ordinal);
    }
}
