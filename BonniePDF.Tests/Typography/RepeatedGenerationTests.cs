using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using BonniePDF.Tests.Support;
using Xunit;

namespace BonniePDF.Tests.Typography;

/// <summary>
/// A document font is also a glyph-fallback candidate for the other document fonts, so one parsed
/// font can be registered twice in a document. Which registration a run is tracked against must not
/// depend on the order a hash table happens to enumerate its keys in: the key carries the document's
/// serial number, so that order changed from one generation to the next and so did the embedded
/// font subsets, though the pages were the same.
/// </summary>
[Collection("Serial")]
public sealed partial class RepeatedGenerationTests
{
    // U+2641 (the earth symbol) is in Liberation Sans but not in Liberation Mono.
    private const string Mixed = "Ab ♁ z ♁";

    [GeneratedRegex(@"/ID \[<[0-9A-Fa-f]+> ?<[0-9A-Fa-f]+>\]", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex FileId();

    [GeneratedRegex(@"/(?<key>CreationDate|ModDate) \([^)]*\)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex InfoDates();

    [GeneratedRegex(@"(?<open><xmp:(?:CreateDate|ModifyDate|MetadataDate)[^>]*>)[^<]*", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex XmpDates();

    [GeneratedRegex(@"(?<open><xmpMM:(?:DocumentID|InstanceID)[^>]*>)[^<]*", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex XmpIds();

    [GeneratedRegex(@"\b[A-Z]{6}\+(?=[A-Za-z])", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SubsetPrefix();

    private static string Normalise(byte[] bytes)
    {
        string s = Encoding.Latin1.GetString(bytes);
        s = FileId().Replace(s, "/ID []");
        s = InfoDates().Replace(s, "/${key} ()");
        s = XmpDates().Replace(s, "${open}");
        s = XmpIds().Replace(s, "${open}");
        return SubsetPrefix().Replace(s, "SUBSET+");
    }

    private static byte[] Generate()
    {
        using BonniePDF.Document document = Build();
        return document.GenerateBytes();
    }

    private static BonniePDF.Document Build()
    {
        var document = new BonniePDF.Document();
        string mono = document.Fonts.Add(File.ReadAllBytes(TestFonts.MonoRegularPath)).FontName;
        string sans = document.Fonts.Add(File.ReadAllBytes(TestFonts.SansRegularPath)).FontName;
        document.Add(new TextBlock(Mixed) { FontName = mono, FontSize = 12 });
        document.Add(new TextBlock(Mixed) { FontName = sans, FontSize = 12 });
        document.Add(new TextBlock("♁ ♁ only") { FontName = sans, FontSize = 12 });
        return document;
    }

    [GeneratedRegex(@"/Subtype /CIDFontType2\s+/BaseFont /[A-Z]{6}\+(?<name>[^\s/]+)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CidFont();

    [GeneratedRegex(@"/Length1 (?<size>\d+)\s+/Filter /FlateDecode\s+/Length (?<length>\d+)|<<\s+/Length (?<cmap>\d+)\s*>>\s*stream\s*/CIDInit", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Subset();

    /// <summary>The embedded font programs and ToUnicode maps, whichever object numbers they were given.</summary>
    private static string[] Subsets(byte[] bytes) =>
        Subset().Matches(Encoding.Latin1.GetString(bytes)).Select(m => m.Value).Order(StringComparer.Ordinal).ToArray();

    // Spec 16 section 4, fixtures/fonts/document-font-single-embedding.json: a face is embedded once however many
    // roles it plays. Liberation Mono is "mono"; Liberation Sans is "sans" and holds U+2641, which mono lacks.
    [Theory]
    [InlineData("document-font-is-also-the-fallback")]
    [InlineData("fallback-role-only")]
    [InlineData("requested-role-before-fallback-role")]
    public void AFaceIsEmbeddedOnceHoweverManyRolesItPlays(string fixtureCase)
    {
        using var document = new BonniePDF.Document();
        string mono = document.Fonts.Add(File.ReadAllBytes(TestFonts.MonoRegularPath)).FontName;
        string sans = document.Fonts.Add(File.ReadAllBytes(TestFonts.SansRegularPath)).FontName;
        (string Face, string Text)[] draws = fixtureCase switch
        {
            "document-font-is-also-the-fallback" => [("mono", Mixed), ("sans", Mixed), ("sans", "\u2641 \u2641 only")],
            "fallback-role-only" => [("mono", "Ab \u2641 z")],
            _ => [("sans", "Ab"), ("mono", "Ab \u2641 z")],
        };
        foreach ((string face, string text) in draws)
            document.Add(new TextBlock(text) { FontName = face == "mono" ? mono : sans, FontSize = 12 });

        string pdf = Encoding.Latin1.GetString(document.GenerateBytes());
        string[] embedded = CidFont().Matches(pdf).Select(m => m.Groups["name"].Value).Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(new[] { "LiberationMono", "LiberationSans" }, embedded);
    }

    [Fact]
    public async Task SynchronousAndAsynchronousGenerationEmbedTheSameFonts()
    {
        string[] sync = Subsets(Generate());
        using var document = Build();
        string[] async = Subsets(await document.GenerateBytesAsync());

        Assert.True(sync.Length > 2);
        Assert.Equal(sync, async);
    }

    [Fact]
    public void TheSameDocumentGeneratesTheSameBytesEveryTime()
    {
        string first = Normalise(Generate());
        for (int generation = 2; generation <= 40; generation++)
            Assert.True(first == Normalise(Generate()), $"generation {generation} differs from generation 1");
    }
}
