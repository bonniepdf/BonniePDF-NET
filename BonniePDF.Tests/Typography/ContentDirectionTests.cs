using BonniePDF.Tests.Support;
using System.Security.Cryptography;
using System.Text.Json;
using BonniePDF.Typography.TrueType;
using Xunit;

namespace BonniePDF.Tests.Typography;

[Collection("Serial")]
public sealed partial class ContentDirectionTests
{
    [System.Text.RegularExpressions.GeneratedRegex(@"1 0 0 1 ([\d.]+) [\d.]+ Tm")]
    private static partial System.Text.RegularExpressions.Regex TextPosition();

    private static string FixturePath(string name) => Path.Combine(AppContext.BaseDirectory, "Typography", "Fixtures", name);

    [Fact]
    public void ArabicAndHebrewMatchIndependentReferenceGlyphs()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(FixturePath("rtl.json")));
        foreach (var sample in fixture.RootElement.GetProperty("shaping").EnumerateArray())
        {
            byte[] bytes = File.ReadAllBytes(FixturePath(sample.GetProperty("font").GetString()!));
            Assert.Equal(sample.GetProperty("sha256").GetString(), Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
            var font = TtfFont.LoadFromBytes(bytes);
            using var scope = TextFlow.Enter(ContentDirection.RightToLeft);
            var actual = font.Shape(sample.GetProperty("text").GetString()!);
            var expected = sample.GetProperty("glyphs").EnumerateArray().Select(g => g.EnumerateArray().Select(v => v.GetInt32()).ToArray()).ToArray();
            Assert.Equal(expected.Length, actual.Count);
            for (int i = 0; i < actual.Count; i++)
            {
                var g = actual.Info(i);
                Assert.Equal(expected[i], new[] { g.Id, g.Cluster, g.AdvanceX, g.AdvanceY, g.OffsetX, g.OffsetY, g.SourceLength });
            }
        }
    }

    [Fact]
    public void DirectionScopesRestoreAndDoNotShareCachedGlyphOrder()
    {
        var font = new TtfFont(FixturePath("NotoSansHebrew-Regular.ttf"));
        const string text = "שלום ";
        var ltr = font.Shape(text).Records().Select(g => g.Cluster).ToArray();
        using (TextFlow.Enter(ContentDirection.RightToLeft))
            Assert.Equal(new[] { 4, 3, 2, 1, 0 }, font.Shape(text).Records().Select(g => g.Cluster));
        Assert.Equal(new[] { 3, 2, 1, 0, 4 }, ltr);
        Assert.Equal(ltr, font.Shape(text).Records().Select(g => g.Cluster));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RowsAndTablesMirrorPositionsButKeepLogicalChildOrder(bool rtl)
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(FixturePath("rtl.json")));
        var direction = rtl ? ContentDirection.RightToLeft : ContentDirection.LeftToRight;
        var row = new HorizontalStack { ContentDirection = direction, Spacing = 10, WidthDistribution = WidthDistribution.Auto };
        var first = new Probe(80); var second = new Probe(210);
        row.Add(first); row.Add(second);
        row.Render(new RenderContext(new PdfSize(500, 500), 0), 300);
        var expected = fixture.RootElement.GetProperty("row").GetProperty(rtl ? "rtlX" : "ltrX");
        Assert.Equal(expected[0].GetSingle(), first.X);
        Assert.Equal(expected[1].GetSingle(), second.X);
        Assert.Same(first, row.Children[0]);

        var cells = new[] { new Probe(0), new Probe(0), new Probe(0) };
        var table = new Table(3, 1, 1) { ContentDirection = direction, CellPadding = 0 };
        table.AddRow(cells);
        table.Render(new RenderContext(new PdfSize(500, 500), 0), 300);
        expected = fixture.RootElement.GetProperty("table").GetProperty(rtl ? "rtlX" : "ltrX");
        for (int i = 0; i < cells.Length; i++) Assert.Equal(expected[i].GetSingle(), cells[i].X);
    }

    [Fact]
    public void NestedDirectionOverridesAndWrappedParagraphRangesArePreserved()
    {
        var text = new TextBlock("שלום");
        var island = new Container { ContentDirection = ContentDirection.LeftToRight, Child = text };
        var outer = new VerticalStack { ContentDirection = ContentDirection.RightToLeft };
        outer.Add(island);
        ContentDirectionResolver.Apply(outer, ContentDirection.LeftToRight);
        Assert.Equal(ContentDirection.LeftToRight, text.EffectiveContentDirection);
        Assert.Equal(ContentDirection.RightToLeft, outer.EffectiveContentDirection);

        var lines = TextFlow.MapLines("שלום 123\nمرحبا ERP", new[] { "שלום", "123", "مرحبا ERP" });
        Assert.Equal("שלום 123", lines[1]!.Paragraph);
        Assert.Equal(5, lines[1]!.Start);
        Assert.Equal("مرحبا ERP", lines[2]!.Paragraph);
    }

    [Fact]
    public void RtlFirstLineIndentAndJustifiedLastLineUseInlineStart()
    {
        var block = new TextBlock("abc")
        {
            ContentDirection = ContentDirection.RightToLeft,
            FirstLineIndent = 20,
            Alignment = TextAlignment.Justify,
        };
        var ctx = new RenderContext(new PdfSize(500, 500), 0);
        block.Render(ctx, 300);
        var match = TextPosition().Match(ctx.CurrentStream.ToString());
        Assert.True(match.Success);
        float actual = float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(280 - FontMetrics.GetStringWidth("abc", block.FontName, block.FontSize), actual, 2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ColouredTableTextPreservesImplicitAndPhysicalAlignment(bool explicitLeft)
    {
        var block = new TextBlock("abc");
        if (explicitLeft) block.Alignment = TextAlignment.Left;
        var table = new Table(1) { ContentDirection = ContentDirection.RightToLeft, CellPadding = 0, RowTextColor = Color.Red };
        table.AddRow(block);
        ContentDirectionResolver.Apply(table, ContentDirection.LeftToRight);
        var ctx = new RenderContext(new PdfSize(500, 500), 0);
        table.Render(ctx, 100);
        var match = TextPosition().Match(ctx.CurrentStream.ToString());
        Assert.True(match.Success);
        float actual = float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(explicitLeft ? 0 : 100 - FontMetrics.GetStringWidth("abc", block.FontName, block.FontSize), actual, 2);
    }

    private sealed class Probe(float preferredWidth) : ElementBase
    {
        internal float X;
        protected override float GetPreferredContentWidth() => preferredWidth;
        protected override float MeasureContentHeight(float width) => 10;
        protected override void RenderContent(RenderContext ctx, float width) { X = ctx.CurrentX; ctx.CurrentY += 10; }
    }
}
