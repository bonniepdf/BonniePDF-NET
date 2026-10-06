using BonniePDF.Fluent;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// Page slots (Background / Foreground) and the Layers primitive.
/// Mirrors the TypeScript page-slots-layers.test.ts.
/// </summary>
public class PageSlotsLayersTests
{
    private static TextBlock Text(string content) => new() { Text = content, FontSize = 12 };

    /// <summary>Renders one page's stream through the streaming pipeline.</summary>
    private static string RenderPageStream(BonniePDF.Document doc, int index = 0)
    {
        RenderContext ctx = new(doc.Settings.GetOrientedPageSize(), doc.Settings.Margin);
        List<PageRenderOutput> outputs =
            PdfRenderPipeline.RenderPagesStreaming(doc.Pages, doc.Settings, ctx).ToList();
        return outputs[index].ContentStream;
    }

    #region Page slots (DocumentSettings)

    [Fact]
    public void PageBackground_RendersBehindContent()
    {
        using BonniePDF.Document doc = new();
        doc.Add(Text("BODYMARKER"));
        doc.Settings.PageBackground = _ => Text("BACKGROUNDMARKER");

        string stream = RenderPageStream(doc);

        Assert.Contains("BACKGROUNDMARKER", stream);
        Assert.Contains("BODYMARKER", stream);
        // Painter's model: whatever is emitted first is drawn first (behind).
        Assert.True(stream.IndexOf("BACKGROUNDMARKER") < stream.IndexOf("BODYMARKER"));
    }

    [Fact]
    public void PageForeground_RendersOnTopOfContent()
    {
        using BonniePDF.Document doc = new();
        doc.Add(Text("BODYMARKER"));
        doc.Settings.PageForeground = _ => Text("FOREGROUNDMARKER");

        string stream = RenderPageStream(doc);

        Assert.Contains("FOREGROUNDMARKER", stream);
        Assert.True(stream.IndexOf("BODYMARKER") < stream.IndexOf("FOREGROUNDMARKER"));
    }

    [Fact]
    public void PageSlots_OrderBackgroundContentForeground()
    {
        using BonniePDF.Document doc = new();
        doc.Add(Text("BODYMARKER"));
        doc.Settings.PageBackground = _ => Text("BACKGROUNDMARKER");
        doc.Settings.PageForeground = _ => Text("FOREGROUNDMARKER");

        string stream = RenderPageStream(doc);

        int bg = stream.IndexOf("BACKGROUNDMARKER");
        int body = stream.IndexOf("BODYMARKER");
        int fg = stream.IndexOf("FOREGROUNDMARKER");
        Assert.True(bg < body);
        Assert.True(body < fg);
    }

    [Fact]
    public void PageBackground_RendersBehindWatermark()
    {
        using BonniePDF.Document doc = new();
        doc.Add(Text("BODYMARKER"));
        doc.Settings.PageBackground = _ => Text("BACKGROUNDMARKER");
        doc.Settings.Watermark = new Watermark { Text = "WMARK" };

        string stream = RenderPageStream(doc);
        Assert.True(stream.IndexOf("BACKGROUNDMARKER") < stream.IndexOf("WMARK"));
    }

    [Fact]
    public void Watermark_RendersOverBodyButUnderForeground()
    {
        using BonniePDF.Document doc = new();
        doc.Add(Text("BODYMARKER"));
        doc.Settings.Watermark = new Watermark { Text = "WMARK" };
        doc.Settings.PageForeground = _ => Text("FOREGROUNDMARKER");

        string stream = RenderPageStream(doc);
        // Painter's model: body first, then the watermark on top, then the
        // explicit foreground stamp above everything.
        Assert.True(stream.IndexOf("BODYMARKER") < stream.IndexOf("WMARK"));
        Assert.True(stream.IndexOf("WMARK") < stream.IndexOf("FOREGROUNDMARKER"));
    }

    [Fact]
    public void PageSlots_NoOpWhenUnset()
    {
        using BonniePDF.Document doc = new();
        doc.Add(Text("BODYMARKER"));
        string stream = RenderPageStream(doc);
        Assert.Contains("BODYMARKER", stream);
    }

    [Fact]
    public void PageSlot_TemplateReturningNull_RendersNothingExtra()
    {
        using BonniePDF.Document doc = new();
        doc.Add(Text("BODYMARKER"));
        doc.Settings.PageBackground = _ => null;
        string stream = RenderPageStream(doc);
        Assert.Contains("BODYMARKER", stream);
    }

    [Fact]
    public void PageSlot_ReceivesOneBasedPageNumber()
    {
        using BonniePDF.Document doc = new();
        doc.Add(Text("BODYMARKER"));
        List<int> seen = new();
        doc.Settings.PageForeground = n =>
        {
            seen.Add(n);
            return Text($"PAGE{n}");
        };
        string stream = RenderPageStream(doc);
        Assert.Contains(1, seen);
        Assert.Contains("PAGE1", stream);
    }

    #endregion

    #region Page slots (Fluent API)

    [Fact]
    public void Fluent_WiresBackgroundAndForeground()
    {
        BonniePDF.Document doc = Pdf.Create(b => b
            .Page(p =>
            {
                p.Background().Child = Text("BACKGROUNDMARKER");
                p.Content().Child = Text("BODYMARKER");
                p.Foreground().Child = Text("FOREGROUNDMARKER");
            })).Build();

        Assert.NotNull(doc.Settings.PageBackground);
        Assert.NotNull(doc.Settings.PageForeground);

        string stream = RenderPageStream(doc);
        int bg = stream.IndexOf("BACKGROUNDMARKER");
        int body = stream.IndexOf("BODYMARKER");
        int fg = stream.IndexOf("FOREGROUNDMARKER");
        Assert.True(bg >= 0);
        Assert.True(bg < body);
        Assert.True(body < fg);
    }

    #endregion

    #region Layers primitive

    private static string RenderLayers(IElement element)
    {
        RenderContext ctx = new(PdfSize.A4, 50);
        element.Render(ctx, 400);
        return ctx.CurrentStream.ToString();
    }

    [Fact]
    public void Layers_PaintsBackgroundPrimaryForegroundInOrder()
    {
        Layers layers = new();
        layers.AddBackground(Text("LAYERBG"));
        layers.PrimaryLayer = Text("LAYERPRIMARY");
        layers.AddForeground(Text("LAYERFG"));

        string stream = RenderLayers(layers);
        Assert.True(stream.IndexOf("LAYERBG") < stream.IndexOf("LAYERPRIMARY"));
        Assert.True(stream.IndexOf("LAYERPRIMARY") < stream.IndexOf("LAYERFG"));
    }

    [Fact]
    public void Layers_HeightFollowsPrimaryLayer()
    {
        TextBlock primary = Text("LAYERPRIMARY");
        Layers layers = new();
        layers.AddBackground(Text("TALL\nTALL\nTALL\nTALL"));
        layers.PrimaryLayer = primary;

        Assert.Equal(primary.MeasureHeight(400), layers.MeasureHeight(400), 3);
    }

    [Fact]
    public void Layers_AdvancesCursorByPrimaryHeightOnly()
    {
        TextBlock primary = Text("LAYERPRIMARY");
        Layers layers = new();
        layers.AddBackground(Text("IGNOREDBG"));
        layers.PrimaryLayer = primary;
        layers.AddForeground(Text("IGNOREDFG"));

        RenderContext ctx = new(PdfSize.A4, 50);
        float startY = ctx.CurrentY;
        layers.Render(ctx, 400);
        Assert.Equal(primary.MeasureHeight(400), ctx.CurrentY - startY, 3);
    }

    [Fact]
    public void Layers_FallsBackToTallestLayerWithoutPrimary()
    {
        TextBlock tall = Text("A\nB\nC\nD\nE");
        TextBlock @short = Text("X");
        Layers layers = new();
        layers.AddBackground(@short);
        layers.AddForeground(tall);

        Assert.Equal(tall.MeasureHeight(400), layers.MeasureHeight(400), 3);
    }

    [Fact]
    public void Container_Layers_FillsContainerWithZStack()
    {
        Container container = new Container().Layers(l => l
            .Background(Text("CLAYERBG"))
            .PrimaryLayer(Text("CLAYERPRIMARY"))
            .Foreground(Text("CLAYERFG")));

        Assert.IsType<Layers>(container.Child);

        string stream = RenderLayers(container);
        Assert.True(stream.IndexOf("CLAYERBG") < stream.IndexOf("CLAYERPRIMARY"));
        Assert.True(stream.IndexOf("CLAYERPRIMARY") < stream.IndexOf("CLAYERFG"));
    }

    #endregion
}
