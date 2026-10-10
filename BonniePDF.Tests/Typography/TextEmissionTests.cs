using System.Text;
using System.Text.Json;
using BonniePDF.Drawing;
using BonniePDF.Typography.TrueType;
using Xunit;

namespace BonniePDF.Tests.Typography;

public sealed class TextEmissionTests
{
    [Fact]
    public void PreservesPreOptimisationContentAndCharacterMaps()
    {
        string root = Path.Combine(AppContext.BaseDirectory, "Typography", "Fixtures");
        var font = new TtfFont(Path.Combine(root, "NotoSans-Regular.ttf"));
        // Captured from the preceding committed renderer. Includes empty strings,
        // kerning, ligatures, marks, escaping, spacing, rotation and feature overrides.
        using var fixtures = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "text-emission.json")));
        foreach (var fixture in fixtures.RootElement.EnumerateArray())
        {
            string text = DecodeUnits(fixture.GetProperty("textUnits"));
            string mode = fixture.GetProperty("mode").GetString()!;
            var tracker = new GlyphUsageTracker(font);
            var content = new StringBuilder();
            var canvas = new PdfCanvas(content, 800, embeddedFontResolver: _ => font,
                glyphTracker: (_, source) => tracker.MarkUsed(source),
                textRunResolver: (_, source) => new List<ResolvedTextRun>
                {
                    new() { ResourceName = "/F1", FontIdentifier = "font", EmbeddedFont = font,
                        Tracker = tracker, CanTrackPreparedGlyphs = true, Text = source }
                });
            if (mode is "spaced" or "justify") canvas.SetTextSpacing(0.2f, 0.5f);
            if (mode == "features") canvas.SetTextFeatures(new Dictionary<string, int>(StringComparer.Ordinal) { ["liga"] = 0, ["kern"] = 0 });
            if (mode == "justify") canvas.DrawTextJustified(10, 10, text, Color.Black, 12, "font", 500);
            else if (mode == "rotated") canvas.DrawTextRotated(10, 10, text, Color.Black, 12, "font", 30);
            else canvas.DrawText(10, 10, text, Color.Black, 12, "font", TextAlignment.Left);
            Assert.Equal(fixture.GetProperty("content").GetString(), content.ToString());
            Assert.Equal(fixture.GetProperty("sources").EnumerateObject().Select(p => (ushort.Parse(p.Name), DecodeUnits(p.Value))).OrderBy(p => p.Item1),
                tracker.GlyphSources.Select(p => (p.Key, p.Value)).OrderBy(p => p.Key));
            Assert.Equal(fixture.GetProperty("cids").EnumerateObject().Select(p => (ushort.Parse(p.Name), p.Value.GetUInt16())).OrderBy(p => p.Item1),
                tracker.CidToGlyphMap.Select(p => (p.Key, p.Value)).OrderBy(p => p.Key));
        }
    }

    private static string DecodeUnits(JsonElement units) => new(units.EnumerateArray().Select(v => (char)v.GetInt32()).ToArray());

    [Fact]
    public void RepeatedSingleCharacterKeepsItsCidAfterAnEquivalentMultiCharacterSource()
    {
        var font = new TtfFont(Path.Combine(AppContext.BaseDirectory, "Typography", "Fixtures", "NotoSans-Regular.ttf"));
        var tracker = new GlyphUsageTracker(font);
        ushort decomposed = Assert.Single(tracker.MarkShaped("e\u0301", font.Shape("e\u0301")));
        ushort composed = Assert.Single(tracker.MarkShaped("\u00e9", font.Shape("\u00e9")));
        Assert.NotEqual(decomposed, composed);
        Assert.Equal(tracker.CidToGlyphMap[decomposed], tracker.CidToGlyphMap[composed]);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(composed, Assert.Single(tracker.MarkShaped("\u00e9", font.Shape("\u00e9"))));
            Assert.Equal(decomposed, Assert.Single(tracker.MarkShaped("e\u0301", font.Shape("e\u0301"))));
        }
        Assert.Equal("e\u0301", tracker.GlyphSources[decomposed]);
        Assert.Equal("\u00e9", tracker.GlyphSources[composed]);
        Assert.Equal(2, tracker.GlyphSources.Count);
    }
}
