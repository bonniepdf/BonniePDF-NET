using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace BonniePDF.Tests.Typography;

/// <summary>
/// Shared fixture fonts/global-family-resolution: a family registered through <see cref="FontManager"/> resolves
/// to that font, after the document's own families and before the standard families and their aliases. A
/// registration's weight and style come from the subfamily it was registered under, and the face is chosen as for
/// a document's fonts.
/// </summary>
[Collection("Serial")]
public sealed partial class GlobalFamilyResolutionTests : System.IDisposable
{
    public void Dispose()
    {
        FontManager.Clear();
        FontManager.RestoreBundledFonts();
    }

    private static byte[] Liberation(string name) => FontManager.GetBytesForFamily(
        name.StartsWith("LiberationSans", System.StringComparison.Ordinal) ? "liberation sans"
        : name.StartsWith("LiberationSerif", System.StringComparison.Ordinal) ? "liberation serif" : "liberation mono",
        name[(name.IndexOf('-', System.StringComparison.Ordinal) + 1)..])!;

    private static byte[] NotoSans() =>
        File.ReadAllBytes(Path.Combine(System.AppContext.BaseDirectory, "Typography", "Fixtures", "NotoSans-Regular.ttf"));

    [GeneratedRegex(@"/Subtype /CIDFontType2\s+/BaseFont /[A-Z]{6}\+(?<name>\S+)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex EmbeddedFont();

    [GeneratedRegex(@"<text x=""(?<x>[\d.]+)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex FirstTextX();

    [GeneratedRegex(@"/Subtype /Type1\s+/BaseFont /(?<name>\S+)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Type1Font();

    private sealed record Generated(string[] Embedded, string[] Type1, Diagnostic[] Warnings);

    private static async Task<Generated> GenerateAsync(System.Action<BonniePDF.Document> build)
    {
        InternalConfiguration.EnableCompression = false;
        try
        {
            using var document = new BonniePDF.Document();
            build(document);
            string pdf = Encoding.Latin1.GetString(await document.GenerateBytesAsync());
            return new Generated(
                EmbeddedFont().Matches(pdf).Select(m => m.Groups["name"].Value).Order(System.StringComparer.Ordinal).ToArray(),
                Type1Font().Matches(pdf).Select(m => m.Groups["name"].Value).Distinct(System.StringComparer.Ordinal).Order(System.StringComparer.Ordinal).ToArray(),
                document.Diagnostics.Where(d => d.Category == DiagnosticCategory.Font).ToArray());
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }

    [Fact]
    public async Task RegisteredFamilyResolves()
    {
        FontManager.RegisterFont(Liberation("LiberationSerif-Regular"), "Brand", "Regular");
        Generated output = await GenerateAsync(d => d.Add(new TextBlock("Hello") { FontFamily = "Brand" }));
        Assert.Equal(new[] { "LiberationSerif" }, output.Embedded);
        Assert.Empty(output.Type1);
        Assert.Empty(output.Warnings);
    }

    [Fact]
    public async Task WeightAndStyleFromTheRegistration()
    {
        FontManager.RegisterFont(Liberation("LiberationSerif-Regular"), "Brand", "Regular");
        FontManager.RegisterFont(Liberation("LiberationSerif-Bold"), "Brand", "Bold");
        FontManager.RegisterFont(Liberation("LiberationSerif-Italic"), "Brand", "Italic");
        Generated output = await GenerateAsync(d =>
        {
            d.Add(new TextBlock("Regular") { FontFamily = "Brand" });
            d.Add(new TextBlock("Semibold") { FontFamily = "Brand", FontWeight = FontWeight.SemiBold });
            d.Add(new TextBlock("Bold italic") { FontFamily = "Brand", FontWeight = FontWeight.Bold, Italic = true });
        });
        // Semibold (600) takes the nearest heavier face, bold; bold italic has no such face, and style comes first.
        Assert.Equal(new[] { "LiberationSerif", "LiberationSerif-Bold", "LiberationSerif-Italic" }, output.Embedded);
    }

    [Fact]
    public async Task FamilyIsMatchedIgnoringCase()
    {
        FontManager.RegisterFont(Liberation("LiberationMono-Regular"), "Brand Mono");
        Generated output = await GenerateAsync(d => d.Add(new TextBlock("code") { FontFamily = "  brand mono " }));
        Assert.Equal(new[] { "LiberationMono" }, output.Embedded);
    }

    [Fact]
    public async Task RegisteredAfterTheFamilyWasSet()
    {
        Generated output = await GenerateAsync(d =>
        {
            d.Add(new TextBlock("Late") { FontFamily = "Brand" });
            FontManager.RegisterFont(Liberation("LiberationSerif-Regular"), "Brand");
        });
        Assert.Equal(new[] { "LiberationSerif" }, output.Embedded);
        Assert.Empty(output.Warnings);
    }

    [Fact]
    public async Task RegistrationComesBeforeStandardAliases()
    {
        FontManager.RegisterFont(Liberation("LiberationSans-Regular"), "Arial", "Regular");
        Generated output = await GenerateAsync(d => d.Add(new TextBlock("Arial") { FontFamily = "Arial" }));
        Assert.Equal(new[] { "LiberationSans" }, output.Embedded);
        Assert.Empty(output.Type1);
    }

    [Fact]
    public async Task DocumentFamilyComesFirst()
    {
        FontManager.RegisterFont(Liberation("LiberationSerif-Regular"), "Brand");
        Generated output = await GenerateAsync(d =>
        {
            d.Fonts.Add(Liberation("LiberationMono-Regular"), family: "Brand");
            d.Add(new TextBlock("Mine") { FontFamily = "Brand" });
        });
        Assert.Equal(new[] { "LiberationMono" }, output.Embedded);
    }

    [Fact]
    public async Task BundledLiberationFamiliesResolve()
    {
        Generated output = await GenerateAsync(d => d.Add(new TextBlock("Serif") { FontFamily = "Liberation Serif" }));
        Assert.Equal(new[] { "LiberationSerif" }, output.Embedded);
        Assert.Empty(output.Warnings);
    }

    [Fact]
    public async Task OneEmbeddingForFamilyAndFallback()
    {
        FontManager.RegisterFont(NotoSans(), "Noto", "Regular");
        Generated output = await GenerateAsync(d =>
        {
            d.Add(new TextBlock("Direct") { FontFamily = "Noto" });
            d.Add(new TextBlock("Price ₹5") { FontName = "Helvetica" });
        });
        Assert.Equal(new[] { "NotoSans-Regular" }, output.Embedded);
    }

    [Fact]
    public async Task UnregisteredFamilyIsStillReported()
    {
        Generated output = await GenerateAsync(d => d.Add(new TextBlock("Papyrus") { FontFamily = "Papyrus" }));
        Assert.Equal(new[] { "Helvetica" }, output.Type1);
        Diagnostic warning = Assert.Single(output.Warnings);
        Assert.Equal("Papyrus", warning.Source);
    }

    [Fact]
    public void LayoutMeasuresTheRegisteredFace()
    {
        // Liberation Mono is 0.6 em per character; Helvetica's "iiii" is far narrower. A right-aligned line ends at
        // the margin only when layout measured it in the face that draws it.
        FontManager.RegisterFont(Liberation("LiberationMono-Regular"), "Brand Mono");
        using var document = new BonniePDF.Document();
        document.Add(new TextBlock("iiii") { FontFamily = "Brand Mono", FontSize = 10, Alignment = TextAlignment.Right });
        string svg = document.GenerateSvgPages()[0];
        double x = double.Parse(FirstTextX().Match(svg).Groups["x"].Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(595.28 - 50, x + 4 * 6, 0);
    }
}
