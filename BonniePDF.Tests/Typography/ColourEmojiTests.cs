using BonniePDF.Tests.Support;
using System.Security.Cryptography;
using BonniePDF.Drawing;
using System.Text.Json;
using BonniePDF.Typography.TrueType;
using Xunit;

namespace BonniePDF.Tests.Typography;

[Collection("Serial")]
public sealed class ColourEmojiTests
{
    private static string Fixtures => Path.Combine(AppContext.BaseDirectory, "Typography", "Fixtures");
    [Fact]
    public async Task AsyncBitmapGenerationEmbedsAnAlphaMask()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.EmojiRendering = EmojiRendering.Colour;
        var font = doc.Fonts.Add(File.ReadAllBytes(Path.Combine(Fixtures, "NotoColorEmoji-Fixture.ttf")));
        doc.Add(new TextBlock("😀😀") { FontName = font.FontName });
        byte[] pdf = await doc.GenerateBytesAsync();
        Assert.Contains("/SMask", System.Text.Encoding.Latin1.GetString(pdf), StringComparison.Ordinal);
    }
    [Fact]
    public void PaletteTransparencyMatchesIndependentPackedFixtures()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(Fixtures, "emoji.json")));
        foreach (var c in fixture.RootElement.GetProperty("palettePng").EnumerateArray())
        {
            var image = ImageLoader.Load(Convert.FromBase64String(c.GetProperty("base64").GetString()!));
            Assert.Equal(c.GetProperty("rgb").EnumerateArray().Select(v => (byte)v.GetInt32()), image.Data);
            Assert.Equal(c.GetProperty("alpha").EnumerateArray().Select(v => (byte)v.GetInt32()), image.AlphaData);
            Assert.True(image.HasAlpha); Assert.Equal(8, image.BitsPerComponent); Assert.Equal(3, image.Components);
        }
    }
    [Fact]
    public void BitmapArtworkReusesDecodedImageAndResource()
    {
        var font = new TtfFont(Path.Combine(Fixtures, "NotoColorEmoji-Fixture.ttf"));
        var resources = new PdfResourceRegistry();
        var canvas = new PdfCanvas(new System.Text.StringBuilder(), 800, imageResolver: image => "/I" + resources.RegisterImage(image));
        var glyphs = font.Shape("😀");
        Assert.Equal(1, glyphs.Count);
        for (int i = 0; i < 10; i++) ColourGlyphPainter.Draw(canvas, font, glyphs, 0, "😀", i * 24, 40, 24, Color.Black);
        var first = Assert.Single(resources.LoadedImages).Value;
        Assert.True(first.HasAlpha); Assert.Equal(0, first.AlphaData[0]);
        ColourGlyphPainter.Draw(canvas, font, glyphs, 0, "😀", 0, 40, 24, Color.Black);
        Assert.Same(first, Assert.Single(resources.LoadedImages).Value);
    }
    [Fact]
    public void FallbackAndEmergencyWrappingPreserveWholeEmoji()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(Fixtures, "emoji.json")));
        var latin = new TtfFont(Path.Combine(Fixtures, "NotoSans-Regular.ttf"));
        foreach (var f in fixture.RootElement.GetProperty("fonts").EnumerateArray())
        {
            string path = Path.Combine(Fixtures, f.GetProperty("file").GetString()!); var font = new TtfFont(path);
            const string source = "Ready 👩‍💻 👍🏽 🇬🇧 ❤️ 1️⃣";
            var result = TextRunShaper.ShapeTextRuns(source, new[] { new ShaperFont { FontIdentifier = "text", Font = latin }, new ShaperFont { FontIdentifier = "emoji", Font = font } });
            Assert.Empty(result.Unsupported); Assert.Equal(source, string.Concat(result.Runs.Select(r => r.Text)));
            foreach (var c in fixture.RootElement.GetProperty("cases").EnumerateArray())
            {
                if (c.GetProperty("id").GetString() is "mixed" or "repeated" or "text-presentation") continue;
                string cluster = c.GetProperty("text").GetString()!;
                var block = new TextBlock(cluster + cluster) { FontName = path, FontSize = 24, PreserveWhitespace = true };
                var lines = (IList<string>)typeof(TextBlock).GetMethod("GetOrComputeLines", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(block, new object[] { 1f })!;
                Assert.Equal(new[] { cluster, cluster }, lines);
            }
        }
    }
    [Fact]
    public void DefaultsAndValidation()
    {
        Assert.Equal(EmojiRendering.Outline, new DocumentSettings().EmojiRendering);
        Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentSettings { EmojiRendering = (EmojiRendering)99 });
    }
    [Fact]
    public void PreservesSharedEmojiClustersAndProducesConformanceSamples()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(Fixtures, "emoji.json")));
        foreach (var f in fixture.RootElement.GetProperty("fonts").EnumerateArray())
        {
            string name = f.GetProperty("file").GetString()!;
            string path = Path.Combine(Fixtures, name);
            var data = File.ReadAllBytes(path); var font = TtfFont.LoadFromBytes(data);
            Assert.Equal(f.GetProperty("sha256").GetString(), Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant());
            var sources = new List<string>();
            foreach (var c in fixture.RootElement.GetProperty("cases").EnumerateArray())
            {
                string id = c.GetProperty("id").GetString()!, text = c.GetProperty("text").GetString()!;
                if (id == "mixed") continue;
                var result = TextRunShaper.ShapeTextRuns(text, new[] { new ShaperFont { FontIdentifier = "emoji", Font = font } });
                Assert.Empty(result.Unsupported); Assert.Equal(text, string.Concat(result.Runs.Select(r => r.Text)));
                var glyphs = font.Shape(text).Records(); Assert.NotEmpty(glyphs); Assert.All(glyphs, g => Assert.NotEqual(0, g.Id));
                if (id != "repeated") Assert.Single(glyphs);
                if (id != "text-presentation") sources.Add(text);
            }
            using var doc = new BonniePDF.Document();
            doc.Fonts.Add(File.ReadAllBytes(Path.Combine(Fixtures, "NotoSans-Regular.ttf")));
            doc.Settings.EmojiRendering = EmojiRendering.Colour;
            doc.Settings.Language = "en-GB"; doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
            doc.Settings.Conformance = PdfAConformance.PdfA2b; doc.Metadata.Title = "Colour emoji conformance";
            doc.Add(new TextBlock(string.Join(" ", sources)) { FontName = path, FontSize = 24 });
            using var stream = new MemoryStream(); doc.Save(stream, true);
            string svg = doc.GenerateSvgPages()[0];
            Assert.Contains("aria-label=", svg, StringComparison.Ordinal);
            Assert.Contains(name.StartsWith("Twemoji", StringComparison.Ordinal) ? "<path" : "data:image/png;base64,", svg, StringComparison.Ordinal);
            if (Environment.GetEnvironmentVariable("EMOJI_OUTPUT") is string output)
            {
                Directory.CreateDirectory(output); string prefix = "dotnet-" + name.Split('-')[0];
                File.WriteAllBytes(Path.Combine(output, prefix + ".pdf"), stream.ToArray());
                File.WriteAllText(Path.Combine(output, prefix + ".svg"), svg);
            }
            if (!font.HasOutlines)
            {
                using var outline = new BonniePDF.Document(); outline.Add(new TextBlock("😀") { FontName = path });
                using var target = new MemoryStream();
                Assert.Throws<NotSupportedException>(() => outline.Save(target, true));
            }
        }
    }
}
