using BonniePDF.Tests.Support;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BonniePDF.Typography.TrueType;
using Xunit;
namespace BonniePDF.Tests.Typography;
[Collection("Serial")]
public sealed class EmbeddedShapingTests
{
    private static string FontPath => Path.Combine(AppContext.BaseDirectory,"Typography","Fixtures","NotoSans-Regular.ttf");
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Typography","Fixtures","harfbuzz.json")));
    [Fact]
    public void MatchesIndependentShapingFixturesAndMetrics()
    {
        using var fixture=Fixture(); var root=fixture.RootElement; var bytes=File.ReadAllBytes(FontPath);var font=TtfFont.LoadFromBytes(bytes);
        Assert.Equal(root.GetProperty("font").GetProperty("sha256").GetString(),Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
        foreach(var c in root.GetProperty("cases").EnumerateArray()) {
            string text=c.GetProperty("text").GetString()!;
            var features=c.GetProperty("features").EnumerateObject().ToDictionary(p=>p.Name,p=>p.Value.GetInt32(),StringComparer.Ordinal);
            var expected=c.GetProperty("glyphs").EnumerateArray().Select(g=>g.EnumerateArray().Select(n=>n.GetInt32()).ToArray()).ToArray();
            var actual=font.Shape(text,features).Records().Select(g=>new[]{g.Id,g.Cluster,g.AdvanceX,g.AdvanceY,g.OffsetX,g.OffsetY,g.UnsafeToBreak?1:0,g.SourceLength}).ToArray();
            Assert.True(expected.Length==actual.Length,c.GetProperty("id").GetString());
            for(int i=0;i<expected.Length;i++) Assert.Equal(expected[i],actual[i]);
            Assert.Equal(expected.Sum(g=>g[2])*12d/1000,FontMetrics.GetStringWidth(text,FontPath,12,features),3);
        }
    }
    [Fact]
    public void KeepsSourceCoordinatesAcrossControlsAndOtherScripts()
    {
        var font=new TtfFont(FontPath);const string source="fi\t\u03b1fi";
        Assert.Equal(new[]{"fi","\u03b1","fi"},font.Shape(source).Records().Select(g=>source.Substring(g.Cluster,g.SourceLength)));
    }
    [Fact]
    public void FallbackKeepsTheCombiningGraphemeTogether()
    {
        var primary=new TtfFont(BonniePDF.Tests.Support.TestFonts.SansRegularPath);var fallback=new TtfFont(FontPath);
        Assert.Equal(0,primary.GetGlyphId(0x1dcb));Assert.NotEqual(0,fallback.GetGlyphId(0x1dcb));
        var result=TextRunShaper.ShapeTextRuns("a\u1dcb",new[]{new ShaperFont{FontIdentifier="primary",Font=primary},new ShaperFont{FontIdentifier="fallback",Font=fallback}});
        var run=Assert.Single(result.Runs);Assert.Equal(1,run.FontIndex);Assert.Equal("a\u1dcb",run.Text);Assert.Empty(result.Unsupported);
    }
    [Fact]
    public void EmergencyWrappingPreservesGraphemesAndReshapesLigatures()
    {
        static IList<string> Lines(string text,float width)
        {
            var block=new TextBlock(text){FontName=FontPath,FontSize=12,PreserveWhitespace=true};
            return block.LinesAt(width);
        }
        Assert.Equal(new[]{"a\u0308\u0301","a\u0308\u0301"},Lines("a\u0308\u0301a\u0308\u0301",1));
        Assert.Equal(new[]{"ff","i"},Lines("ffi",8.4f));Assert.Single(new TtfFont(FontPath).Shape("ff").Records());
    }
    [Fact]
    public void PdfCodesDistinguishDifferentSourcesOfTheSameGlyph()
    {
        var font=new TtfFont(FontPath);var tracker=new GlyphUsageTracker(font);
        var first=tracker.MarkShaped("é",font.Shape("é"))[0];var second=tracker.MarkShaped("e\u0301",font.Shape("e\u0301"))[0];
        Assert.NotEqual(first,second);Assert.Equal(tracker.CidToGlyphMap[first],tracker.CidToGlyphMap[second]);
        Assert.Equal("é",tracker.GlyphSources[first]);Assert.Equal("e\u0301",tracker.GlyphSources[second]);
    }
    [Fact]
    public void RichTextWrappingMeasuresKerningAcrossWords()
    {
        string serif=BonniePDF.Tests.Support.TestFonts.SerifRegularPath;
        var rich=new RichText();rich.DefaultStyle.FontFamily=serif;rich.DefaultStyle.FontSize=12;rich.Span("A A");
        float width=(1479+512+1479-113-113)*12f/2048;
        Assert.Equal(width,FontMetrics.GetStringWidth("A A",serif,12),3);
        Assert.Equal(14.4f,rich.MeasureHeight(width+0.001f),3);
    }
    [Fact]
    public void PathAliasesUseShapedGlyphCountsForSpacing()
    {
        FontMetrics.RegisterFontAlias("Stage6NotoPath",FontPath);
        Assert.Equal(1,FontMetrics.GetGlyphCount("ffi","Stage6NotoPath"));
        Assert.Equal(3,FontMetrics.GetGlyphCount("ffi","Stage6NotoPath",new Dictionary<string,int>(StringComparer.Ordinal){{"liga",0}}));
        Assert.True(FontMetrics.UsesEmbeddedMetrics("Stage6NotoPath"));
    }
    [Fact]
    public void FeatureSettingsAreOwnedAndValidated()
    {
        var input=new Dictionary<string,int>(StringComparer.Ordinal){{"liga",0}};var style=new TextStyle{FontFeatures=input};input["liga"]=1;
        Assert.Equal(0,style.FontFeatures!["liga"]);
        Assert.Throws<ArgumentException>(()=>style.FontFeatures=new Dictionary<string,int>(StringComparer.Ordinal){{"bad",1}});
        Assert.Throws<ArgumentOutOfRangeException>(()=>style.FontFeatures=new Dictionary<string,int>(StringComparer.Ordinal){{"liga",-1}});
    }
    [Fact]
    public void LigaturesAreTrackedWithTheirSourceAndKerningCanBeDisabled()
    {
        var font=new TtfFont(FontPath);var ligature=font.Shape("ffi");var glyph=Assert.Single(ligature.Records());var tracker=new GlyphUsageTracker(font);tracker.MarkShaped("ffi",ligature);
        Assert.Contains((ushort)glyph.Id,tracker.UsedGlyphIds);Assert.Equal("ffi",tracker.GlyphSources[(ushort)glyph.Id]);
        var cmap=new ToUnicodeCMapBuilder();cmap.AddSourceMapping((ushort)glyph.Id,"ffi");Assert.Contains("006600660069",cmap.Build(),StringComparison.Ordinal);
        Assert.Equal(3,font.Shape("ffi",new Dictionary<string,int>(StringComparer.Ordinal){{"liga",0}}).Count);
        Assert.True(FontMetrics.GetStringWidth("AV",FontPath,12)<FontMetrics.GetStringWidth("AV",FontPath,12,new Dictionary<string,int>(StringComparer.Ordinal){{"kern",0}}));
        Assert.False(FontMetrics.IsSafeTextBreak("ffi",FontPath,1));Assert.True(FontMetrics.IsSafeTextBreak("ffi",FontPath,3));
    }
    [Fact]
    public void PdfIncludesClusterUnicodeAndActualText()
    {
        InternalConfiguration.EnableCompression=false;
        try { using var doc=Create("ffi é e\u0301");using var stream=new MemoryStream();doc.Save(stream,true);string pdf=Encoding.ASCII.GetString(stream.ToArray());
            Assert.Contains("006600660069",pdf,StringComparison.Ordinal);Assert.Contains("/ActualText",pdf,StringComparison.Ordinal);Assert.Contains("/CIDToGIDMap",pdf,StringComparison.Ordinal);
        } finally {InternalConfiguration.EnableCompression=true;}
    }
    [Fact]
    public void SvgOutlinesSubstitutionsAtAccurateScale()
    {
        using var doc=Create("ffi a\u0308\u0301");string svg=doc.GenerateSvgPages()[0];
        Assert.Contains("role=\"img\" aria-label=\"ffi a",svg,StringComparison.Ordinal);Assert.Contains("<path d=\"M",svg,StringComparison.Ordinal);Assert.Contains("scale(0.012 -0.012)",svg,StringComparison.Ordinal);
    }
    [Fact]
    public void SvgPositionsUnsubstitutedTextWithoutViewerShaping()
    {
        using var doc=Create("ffi",new Dictionary<string,int>(StringComparer.Ordinal){{"liga",0}});string svg=doc.GenerateSvgPages()[0];
        Assert.Contains("<text x=\"",svg,StringComparison.Ordinal);Assert.Contains("font-variant-ligatures:none",svg,StringComparison.Ordinal);
    }
    [Fact]
    public void ConformanceSample()
    {
        using var doc=Create("office affinity ffi fi fl é e\u0301 a\u0308\u0301");
        doc.Metadata.Title="Latin shaping conformance";doc.Settings.Language="en-GB";
        doc.Settings.Conformance=PdfAConformance.PdfA2b;doc.Settings.Accessibility=AccessibilityMode.PdfUa1;
        doc.Add(new TextBlock("AV ffi"){FontName=FontPath,FontSize=12,FontFeatures=new Dictionary<string,int>(StringComparer.Ordinal){{"liga",0},{"kern",0}}});
        doc.Add(new TextBlock("AV ffi words"){FontName=FontPath,FontSize=12,LetterSpacing=0.5f,WordSpacing=2});
        var rich=new RichText();rich.DefaultStyle.FontFamily=FontPath;rich.DefaultStyle.FontSize=12;
        rich.Span("f",new TextStyle{FontFeatures=new Dictionary<string,int>(StringComparer.Ordinal){{"liga",0}}})
            .Span("fi",new TextStyle{FontFeatures=new Dictionary<string,int>(StringComparer.Ordinal){{"liga",1}}});doc.Add(rich);
        using var stream=new MemoryStream();doc.Save(stream,true);Assert.True(stream.Length>0);
        string? output=Environment.GetEnvironmentVariable("SHAPING_OUTPUT");
        if(output is not null) {Directory.CreateDirectory(output);File.WriteAllBytes(Path.Combine(output,"dotnet.pdf"),stream.ToArray());File.WriteAllText(Path.Combine(output,"dotnet.svg"),doc.GenerateSvgPages()[0]);}
    }
    [Fact]
    public void CachedShapingUsesFeatureValuesAndOriginalSources()
    {
        var font = new TtfFont(FontPath);
        var features = new Dictionary<string, int>(StringComparer.Ordinal) { ["liga"] = 0, ["kern"] = 1 };
        var separate = font.Shape("ffi", features);
        Assert.Equal(3, separate.Count);
        Assert.Same(separate, font.Shape("ffi", new Dictionary<string, int>(StringComparer.Ordinal) { ["kern"] = 1, ["liga"] = 0 }));
        features["liga"] = 1;
        Assert.Single(font.Shape("ffi", features).Records());
        Assert.Equal(3, separate.Count);
        Assert.Same(font.Shape("ffi"), font.Shape("ffi", new Dictionary<string, int>(StringComparer.Ordinal)));
        Assert.NotSame(font.Shape("é"), font.Shape("e\u0301"));
        // A packed run has no way to change its glyphs; later shaping leaves this one as it was.
        Assert.Equal(new[] { separate.Id(0), separate.Id(1), separate.Id(2) }, font.Shape("ffi", new Dictionary<string, int>(StringComparer.Ordinal) { ["kern"] = 1, ["liga"] = 0 }).Records().Select(g => g.Id));
    }
    [Fact]
    public void RepeatedDefaultShapingDoesNotReallocateTheRun()
    {
        var font = new TtfFont(FontPath);
        font.Shape("office AV ffi");
        font.Shape("office AV ffi");
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) font.Shape("office AV ffi");
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 1024);
    }
    [Fact]
    public void ShapingCacheEvictsUniqueTextAndBypassesLongRuns()
    {
        var font = new TtfFont(FontPath);
        var first = font.Shape("first");
        for (int i = 0; i < 300; i++) font.Shape("unique " + i.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.NotSame(first, font.Shape("first"));
        string budgetText = new('a', 500);
        var budgetFirst = font.Shape(budgetText + "0");
        for (int i = 1; i < 40; i++) font.Shape(budgetText + i.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.NotSame(budgetFirst, font.Shape(budgetText + "0"));
        string longText = new('a', 513);
        Assert.NotSame(font.Shape(longText), font.Shape(longText));
        var parallel = new BonniePDF.Typography.Engine.GlyphRun[32];
        System.Threading.Tasks.Parallel.For(0, parallel.Length, i => parallel[i] = font.Shape("shared ffi"));
        Assert.All(parallel, run => Assert.Same(parallel[0], run));
    }
    [Fact]
    public void PrimaryCoverageFastPathStillFiltersControlsAndReportsMissingGlyphs()
    {
        var font = new TtfFont(FontPath);
        var candidates = new[] { new ShaperFont { FontIdentifier = "primary", Font = font } };
        Assert.Equal("office", Assert.Single(TextRunShaper.ShapeTextRuns("office", candidates).Runs).Text);
        var filtered = TextRunShaper.ShapeTextRuns("a\tb\u200bc", candidates);
        Assert.Equal("abc", Assert.Single(filtered.Runs).Text);
        var missing = TextRunShaper.ShapeTextRuns("a\U0010ffffb", candidates);
        Assert.Equal("ab", Assert.Single(missing.Runs).Text);
        Assert.Equal(new[] { 0x10ffff }, missing.Unsupported);
    }
    private static BonniePDF.Document Create(string text,IReadOnlyDictionary<string,int>? features=null)
    {
        var doc=new BonniePDF.Document();doc.Add(new TextBlock(text){FontName=FontPath,FontSize=12,FontFeatures=features});return doc;
    }
}
