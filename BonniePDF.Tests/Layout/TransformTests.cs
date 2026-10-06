using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace BonniePDF.Tests.Layout;

public class TransformTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreservesFlowAndWrapsBeforePainting(bool shift)
    {
        var child = new Container { Width = 80, Height = 20 };
        ElementBase element = shift ? new Translate { X = 5, Y = -7, Child = child } : new Flip { Horizontal = true, Child = child };
        element.Margin = new Thickness(3);
        Assert.Equal(26, element.MeasureHeight(200)); Assert.Equal(86, element.GetPreferredWidth());
        var ctx = Context(); ctx.CurrentY = 280; string before = ctx.CurrentStream.ToString();
        Assert.Equal(RenderStatus.WrapRequired, ((IStatefulElement)element).RenderStateful(ctx,200));
        Assert.Equal(before, ctx.CurrentStream.ToString());
        child.Height = 400;
        Assert.Throws<InvalidOperationException>(() => ((IStatefulElement)element).RenderStateful(ctx,200));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ForbiddenPageBreakRestoresState(bool shift)
    {
        IStatefulElement element = shift ? new Translate { X = 5, Child = new PageBreak() } : new Flip { Horizontal = true, Child = new PageBreak() };
        var ctx = Context(); Assert.Throws<InvalidOperationException>(() => element.RenderStateful(ctx,200));
        Assert.Null(ctx.AtomicLayoutBounds); Assert.Null(ctx.ElementTransform);
        Assert.Equal(0, ctx.Canvas.GraphicsStateDepth); Assert.Equal(10, ctx.CurrentY); ctx.NewPage();
    }
    [Fact]
    public void MirroredLinkMovesWithoutChangingFlow()
    {
        var ctx = Context(); var link = new Hyperlink("https://example.com", "Link");
        float h = link.MeasureHeight(80), w = link.GetPreferredWidth();
        new Translate { X = 5, Y = -7, Child = new Flip { Horizontal = true, ContentWidth = 80, Child = link } }.RenderStateful(ctx,200);
        Assert.Equal(95-w, link.RenderX,3); Assert.Equal(300-3-h, link.RenderY,3); Assert.Equal(10+h, ctx.CurrentY,3);
    }
    private static RenderContext Context() => new(new PdfSize(300, 300), 10);
    private static IEnumerable<LayoutNode> Nodes(LayoutNode n) => new[] { n }.Concat(n.Children.SelectMany(Nodes));

    [Fact]
    public async Task SharedFlipTranslateAndNestedGeometry()
    {
        string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Definition", "Fixtures", "valid", "transforms.json"));
        using var fixture = JsonDocument.Parse(json);
        using var doc = await BonniePDF.Document.FromDefinitionAsync(json);
        var output = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true });
        var all = Nodes(output.Snapshot.Root).ToArray();
        byte[] bytes = doc.GenerateBytes();
        string raw = Encoding.Latin1.GetString(bytes);
        int start = raw.IndexOf("stream\n", StringComparison.Ordinal) + 7;
        int end = raw.IndexOf("endstream", start, StringComparison.Ordinal);
        using var compressed = new MemoryStream(bytes, start, end - start);
        using var inflated = new ZLibStream(compressed, CompressionMode.Decompress);
        using var content = new MemoryStream();
        inflated.CopyTo(content);
        string hash = Convert.ToHexString(SHA256.HashData(content.ToArray())).ToLowerInvariant();
        Assert.Equal(fixture.RootElement.GetProperty("_expect").GetProperty("contentStreamSha256").GetString(), hash);

        foreach (var expected in fixture.RootElement.GetProperty("_expect").GetProperty("rectangles").EnumerateArray())
        {
            var rect = all.Single(n => n.SourceId == expected.GetProperty("id").GetString()).Rects[0];
            Assert.Equal(expected.GetProperty("x").GetSingle(), rect.X, 3);
            Assert.Equal(expected.GetProperty("y").GetSingle(), rect.Y, 3);
            Assert.Equal(expected.GetProperty("width").GetSingle(), rect.Width, 3);
            Assert.Equal(expected.GetProperty("height").GetSingle(), rect.Height, 3);
        }
    }
}
