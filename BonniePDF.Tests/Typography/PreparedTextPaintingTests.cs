using System.Text;
using BonniePDF.Drawing;
using BonniePDF.Typography.TrueType;
using Xunit;

namespace BonniePDF.Tests.Typography;

public sealed class PreparedTextPaintingTests
{
    private static string FontPath => Path.Combine(AppContext.BaseDirectory, "Typography", "Fixtures", "NotoSans-Regular.ttf");

    [Theory]
    [InlineData("center", false)]
    [InlineData("center", true)]
    [InlineData("right", false)]
    [InlineData("justify", true)]
    [InlineData("rotate", false)]
    public void PreparedTrackingPreservesContentCidsAndSubsetMembership(string mode, bool features)
    {
        var text = string.Concat(Enumerable.Repeat("ffi AV é e\u0301 a\u0308\u0301 ", 35));
        var font = new TtfFont(FontPath);
        var legacy = Draw(font, text, mode, features, false);
        var prepared = Draw(font, text, mode, features, true);
        Assert.Equal(legacy.Content, prepared.Content);
        Assert.Equal(legacy.Tracker.GlyphSources.OrderBy(x => x.Key), prepared.Tracker.GlyphSources.OrderBy(x => x.Key));
        Assert.Equal(legacy.Tracker.CidToGlyphMap.OrderBy(x => x.Key), prepared.Tracker.CidToGlyphMap.OrderBy(x => x.Key));
        Assert.Equal(legacy.Tracker.UsedCodepoints.Order(), prepared.Tracker.UsedCodepoints.Order());
        Assert.Equal(legacy.Tracker.UsedGlyphIds.Order(), prepared.Tracker.UsedGlyphIds.Order());
    }

    [Fact]
    public void PreparedTrackingReducesAllocationsForRunsBeyondTheFontCache()
    {
        var font = new TtfFont(FontPath);
        string text = string.Concat(Enumerable.Repeat("office AV cafe ", 80));
        Draw(font, text, "center", false, false);
        Draw(font, text, "center", false, true);
        long Allocated(bool prepared)
        {
            long start = GC.GetAllocatedBytesForCurrentThread();
            Draw(font, text, "center", false, prepared);
            return GC.GetAllocatedBytesForCurrentThread() - start;
        }
        long legacy = Allocated(false), prepared = Allocated(true);
        Assert.True(prepared < legacy * 0.75, $"Prepared {prepared}, legacy {legacy}");
    }

    [Fact]
    public void BreakPredicateRetainsOriginalShapeWhenFeatureValuesChange()
    {
        var features = new Dictionary<string, int>(StringComparer.Ordinal) { ["liga"] = 1 };
        var safe = FontMetrics.GetTextBreakPredicate("ffi" + new string('a', 600), FontPath, features);
        features["liga"] = 0;
        Assert.False(safe(1));
        Assert.False(safe(2));
        Assert.True(safe(3));
        Assert.True(FontMetrics.GetTextBreakPredicate("ffi", FontPath, features)(1));
        Assert.True(FontMetrics.GetTextBreakPredicate("ffi", StandardFonts.Helvetica)(1));
    }

    [Fact]
    public void SharedFacesWithSeparatePdfResourcesEachTrackTheirOwnGlyphs()
    {
        var font = new TtfFont(FontPath);
        var resources = new PdfResourceRegistry();
        foreach (var (name, resourceName) in new[] { ("virtual:first", "/F1"), ("virtual:second", "/F2") })
        {
            resources.EmbeddedFonts[name] = new EmbeddedFontInfo { Font = font, Tracker = new GlyphUsageTracker(font), ResourceName = resourceName, SubsetPrefix = "ABCDEF" };
            resources.FontNameMap[name] = resourceName;
        }

        // A run is resolved to the registration its own identifier names, whichever order the
        // registrations are enumerated in, so each one tracks prepared glyphs against its own tracker.
        foreach (string name in new[] { "virtual:second", "virtual:first" })
        {
            var run = resources.ResolveTextRuns(name, "ffi")[0];
            Assert.True(run.CanTrackPreparedGlyphs);
            Assert.Same(resources.EmbeddedFonts[name].Tracker, run.Tracker);
            Assert.Equal(resources.EmbeddedFonts[name].ResourceName, run.ResourceName);
        }

        var whole = resources.WholeTextFont("virtual:second", "ffi");
        Assert.NotNull(whole);
        Assert.True(whole.CanTrackPreparedGlyphs);
        Assert.Same(resources.EmbeddedFonts["virtual:second"].Tracker, whole.Tracker);
    }

    [Fact]
    public void ARunThatCannotTrackPreparedGlyphsKeepsItsTrackingCallback()
    {
        var font = new TtfFont(FontPath);
        var resources = new PdfResourceRegistry();
        resources.EmbeddedFonts["virtual:only"] = new EmbeddedFontInfo { Font = font, Tracker = new GlyphUsageTracker(font), ResourceName = "/F1", SubsetPrefix = "ABCDEF" };
        resources.FontNameMap["virtual:only"] = "/F1";
        var run = resources.ResolveTextRuns("virtual:only", "ffi")[0];
        var separate = new ResolvedTextRun
        {
            ResourceName = run.ResourceName, EmbeddedFont = run.EmbeddedFont, Tracker = run.Tracker,
            CanTrackPreparedGlyphs = false, FontIdentifier = run.FontIdentifier, Text = run.Text
        };
        int calls = 0;
        var canvas = new PdfCanvas(new StringBuilder(), 800,
            glyphTracker: (name, text) => { calls++; resources.TrackGlyphUsage(name, text); },
            textRunResolver: (_, _) => new[] { separate });
        canvas.DrawText(0, 0, "ffi", Color.Black, 12, separate.FontIdentifier, TextAlignment.Left);
        Assert.Equal(1, calls);
        Assert.True(resources.EmbeddedFonts[separate.FontIdentifier].Tracker.UsedGlyphIds.Count > 1);
    }

    private static (string Content, GlyphUsageTracker Tracker) Draw(TtfFont font, string text, string mode, bool features, bool prepared)
    {
        var tracker = new GlyphUsageTracker(font);
        var content = new StringBuilder();
        var canvas = new PdfCanvas(content, 800, embeddedFontResolver: _ => font,
            glyphTracker: (_, source) => tracker.MarkUsed(source),
            textRunResolver: (_, source) => new List<ResolvedTextRun>
            {
                new() { ResourceName = "/F1", FontIdentifier = "font", EmbeddedFont = font, Tracker = tracker, CanTrackPreparedGlyphs = prepared, Text = source }
            });
        canvas.SetTextSpacing(0.2f, 0.5f);
        if (features) canvas.SetTextFeatures(new Dictionary<string, int>(StringComparer.Ordinal) { ["liga"] = 0 });
        switch (mode)
        {
            case "justify": canvas.DrawTextJustified(10, 10, text, Color.Black, 12, "font", 500); break;
            case "rotate": canvas.DrawTextRotated(10, 10, text, Color.Black, 12, "font", 30); break;
            default: canvas.DrawText(300, 10, text, Color.Black, 12, "font", mode == "right" ? TextAlignment.Right : TextAlignment.Center); break;
        }
        return (content.ToString(), tracker);
    }
}
