using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace BonniePDF.Tests.Layout;

public class RotationTests
{
    [Fact]
    public void PositionedCanvasRotatesInPdf()
    {
        var ctx = Context();
        var child = new BonniePDF.CanvasElement((canvas, _, _) => canvas.FillRect(0, 0, 80, 20, Color.Black)) { DrawWidth = 80, DrawHeight = 20 };
        new Rotate { Angle = 90, Mode = RotationMode.Constrained, Child = child }.RenderStateful(ctx, 200);
        Assert.Contains("0 -1 1 0 -260 300 cm", ctx.CurrentStream.ToString());
        Assert.Contains("10 270 80 20 re", ctx.CurrentStream.ToString());
    }
    private static RenderContext Context() => new(new PdfSize(300, 300), 10);
    private static IEnumerable<LayoutNode> Nodes(LayoutNode n) => new[] { n }.Concat(n.Children.SelectMany(Nodes));

    [Theory]
    [InlineData(90, 86, 26)]
    [InlineData(-90, 86, 26)]
    [InlineData(180, 26, 86)]
    [InlineData(360, 26, 86)]
    public void ConstrainedMeasurement(float angle, float height, float width)
    {
        var r = new Rotate { Angle = angle, Mode = RotationMode.Constrained, Child = new Container { Width = 80, Height = 20 }, Margin = new Thickness(3) };
        Assert.Equal(height, r.MeasureHeight(200));
        Assert.Equal(width, r.GetPreferredWidth());
    }
    [Fact]
    public void FreeRotationKeepsDimensions()
    {
        var r = new Rotate { Angle = 37, Child = new Container { Width = 80, Height = 20 } };
        Assert.Equal(20, r.MeasureHeight(200)); Assert.Equal(80, r.GetPreferredWidth());
    }
    [Fact]
    public async Task SharedCardinalAndNestedGeometry()
    {
        string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Definition", "Fixtures", "valid", "rotation-geometry.json"));
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
    [Fact]
    public void WrapsBeforePaintingAndRejectsOversize()
    {
        var ctx = Context(); ctx.CurrentY = 260;
        var r = new Rotate { Angle = 90, Mode = RotationMode.Constrained, ContentWidth = 80, Child = new Container { Height = 20 } };
        string before = ctx.CurrentStream.ToString();
        Assert.Equal(RenderStatus.WrapRequired, r.RenderStateful(ctx, 200));
        Assert.Equal(before, ctx.CurrentStream.ToString()); Assert.Equal(260, ctx.CurrentY);
        r.ContentWidth = 400;
        Assert.Throws<InvalidOperationException>(() => r.RenderStateful(ctx, 200));
    }
    [Fact]
    public void ForbiddenPageBreakRestoresContext()
    {
        var ctx = Context();
        var r = new Rotate { Angle = 90, Mode = RotationMode.Constrained, ContentWidth = 80, Child = new PageBreak() };
        Assert.Throws<InvalidOperationException>(() => r.RenderStateful(ctx, 200));
        Assert.Null(ctx.AtomicLayoutBounds); Assert.Null(ctx.ElementTransform);
        Assert.Equal(10, ctx.CurrentX); Assert.Equal(10, ctx.CurrentY);
        Assert.Equal(0, ctx.Canvas.GraphicsStateDepth);
        ctx.NewPage(); Assert.Equal(1, ctx.CurrentPageIndex);
    }
    [Fact]
    public void HyperlinkBoundsRotate()
    {
        var ctx = Context(); var link = new Hyperlink("https://example.com", "Link");
        var r = new Rotate { Angle = 90, Mode = RotationMode.Constrained, ContentWidth = 80, Child = link };
        float height = link.MeasureHeight(80), width = link.GetPreferredWidth();
        r.RenderStateful(ctx, 200);
        Assert.Equal(10, link.RenderX, 3); Assert.Equal(height, link.RenderWidth, 3);
        Assert.Equal(width, link.RenderHeight, 3); Assert.Equal(300 - 10 - width, link.RenderY, 3);
    }
    [Fact]
    public void FullTurnEmitsNoTransform()
    {
        var ctx = Context(); int start = ctx.CurrentStream.Length;
        new Rotate { Angle = 360, Child = new Container { Height = 20 } }.RenderStateful(ctx, 200);
        Assert.DoesNotContain(" cm", ctx.CurrentStream.ToString()[start..]);
        Assert.DoesNotContain("q", ctx.CurrentStream.ToString()[start..]);
    }
}
