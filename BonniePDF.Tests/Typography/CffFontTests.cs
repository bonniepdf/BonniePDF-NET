using System.Text;
using System.Text.Json;
using BonniePDF.Typography.TrueType;
using BonniePDF.Typography.Engine;
using Xunit;
namespace BonniePDF.Tests.Typography;
[Collection("Serial")]
public sealed class CffFontTests
{
    [Fact]
    public async Task MatchesIndependentShapingAndEmbedsCompleteCubicPrograms()
    {
        using var fixture=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Typography","Fixtures","cff.json")));
        foreach(var f in fixture.RootElement.GetProperty("fonts").EnumerateArray())
        {
            string name=f.GetProperty("id").GetString()!;var bytes=Convert.FromBase64String(f.GetProperty("fontBase64").GetString()!);var font=TtfFont.LoadFromBytes(bytes);
            Assert.True(font.HasCff);Assert.True(font.HasOutlines);
            foreach(var c in f.GetProperty("cases").EnumerateArray())
            {
                var features=c.GetProperty("features").EnumerateObject().ToDictionary(p=>p.Name,p=>p.Value.GetInt32(),StringComparer.Ordinal);
                var expected=c.GetProperty("glyphs").EnumerateArray().Select(g=>g.EnumerateArray().Select(n=>n.GetInt32()).ToArray()).ToArray();
                var actual=font.Shape(c.GetProperty("text").GetString()!,features).Select(g=>new[]{g.Id,g.Cluster,g.AdvanceX,g.AdvanceY,g.OffsetX,g.OffsetY}).ToArray();
                Assert.Equal(expected.Length,actual.Length);for(int i=0;i<expected.Length;i++)Assert.Equal(expected[i],actual[i]);
            }
            var program=CffProgramWriter.Build(font,font.PostScriptName);var embedded=new CffFont(program,font.EngineFace.GlyphCount,font.UnitsPerEm,new OutlineReadLimits());
            var before=font.EngineFace.Font.Cff!.GetEmbeddingData();var after=embedded.GetEmbeddingData();
            Assert.Equal(before.Chars.Length,after.Chars.Length);for(int i=0;i<before.Chars.Length;i++)Assert.Equal(before.Chars[i],after.Chars[i]);
            for(int i=0;i<before.Globals.Length;i++)Assert.Equal(before.Globals[i],after.Globals[i]);
            for(int i=0;i<before.Privates.Length;i++)Assert.Equal(before.Privates[i],after.Privates[i]);
            for(int i=0;i<font.EngineFace.GlyphCount;i++){var a=font.EngineFace.ReadGlyph(i).Commands;var b=embedded.ReadGlyph(i).Commands;Assert.Equal(a.Count,b.Count);for(int j=0;j<a.Count;j++)Assert.Equal(a[j],b[j]);}
            foreach(bool asynchronous in new[]{false,true})
            {
                using var doc=new BonniePDF.Document();doc.Settings.Conformance=PdfAConformance.PdfA2b;doc.Settings.Accessibility=AccessibilityMode.PdfUa1;doc.Settings.Language="en-GB";doc.Metadata.Title="Static OpenType reference";
                var family=doc.Fonts.Add(bytes);doc.Add(new TextBlock(f.GetProperty("cases")[0].GetProperty("text").GetString()!){FontName=family.FontName,FontSize=18});
                using var stream=new MemoryStream();if(asynchronous)await doc.SaveAsync(stream,true);else doc.Save(stream,true);
                string raw=Encoding.Latin1.GetString(stream.ToArray());Assert.Contains("/CIDFontType0C",raw,StringComparison.Ordinal);Assert.Contains("/FontFile3",raw,StringComparison.Ordinal);Assert.DoesNotContain("/CIDToGIDMap",raw,StringComparison.Ordinal);Assert.DoesNotContain("/FontFile2",raw,StringComparison.Ordinal);
                string svg=doc.GenerateSvgPages()[0];Assert.Contains("<path",svg,StringComparison.Ordinal);Assert.DoesNotContain("<text",svg,StringComparison.Ordinal);
                if(Environment.GetEnvironmentVariable("CFF_OUTPUT") is string output){Directory.CreateDirectory(output);string prefix=Path.Combine(output,"dotnet-"+name+"-"+asynchronous.ToString().ToLowerInvariant());File.WriteAllBytes(prefix+".pdf",stream.ToArray());File.WriteAllText(prefix+".svg",svg);File.WriteAllBytes(prefix+".cff",program);}
            }
        }
    }
}

