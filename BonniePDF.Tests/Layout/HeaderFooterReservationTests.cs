using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// Per-page chrome reservation - shared conformance fixture
/// layout/header-footer-reservation. Header and footer consume body content
/// height: the content band is inset by max(margin, chromeHeight) at each edge,
/// on every page including overflow pages, and layout throws when the two bands
/// leave no usable body height.
///
/// Chrome heights come from a Container with an explicit height so the numbers
/// are font-independent and the TypeScript suite can assert exactly the same
/// ones.
///
/// Mirrors core/tests/layout/header-footer-reservation.test.ts.
/// </summary>
public class HeaderFooterReservationTests
{
    private static readonly PdfSize Page = PdfSize.A4;
    private const float Margin = 50f;

    /// <summary>A page-chrome element of an exact height, independent of any font metric.</summary>
    private static IElement Chrome(float height) => new Container { Height = height };

    private static IEnumerable<LayoutNode> Walk(LayoutNode node)
    {
        yield return node;
        foreach (LayoutNode child in node.Children)
            foreach (LayoutNode d in Walk(child))
                yield return d;
    }

    /// <summary>Lays out a document through the real pipeline and reports page 1's content band.</summary>
    private static (float Top, float Bottom) BandOf(float headerHeight, float footerHeight)
    {
        var doc = new BonniePDF.Document();
        doc.Settings.PageSize = Page;
        doc.Settings.Margin = Margin;
        if (headerHeight > 0f) doc.Settings.HeaderTemplate = _ => Chrome(headerHeight);
        if (footerHeight > 0f) doc.Settings.FooterTemplate = _ => Chrome(footerHeight);
        doc.Add(new Container { Height = 1f, Child = new TextBlock { Text = "probe", FontSize = 10 } });

        RenderContext ctx = new(Page, Margin);
        float top = float.NaN;
        float bottom = float.NaN;
        foreach (PageRenderOutput _ in PdfRenderPipeline.RenderPagesStreaming(doc.Pages, doc.Settings, ctx))
        {
            if (!float.IsNaN(top)) continue;
            top = ctx.ContentTop;
            bottom = ctx.ContentBottom;
        }

        return (top, bottom);
    }

    [Fact]
    public void ReservesNothingBeyondMargin_WhenChromeFitsInsideIt()
    {
        (float top, float bottom) = BandOf(20f, 20f);
        Assert.Equal(Margin, top);
        Assert.Equal(Page.Height - Margin, bottom, 3);
    }

    [Fact]
    public void InsetsContentBand_ByChromeTallerThanMargin()
    {
        (float top, float bottom) = BandOf(120f, 90f);
        Assert.Equal(120f, top);
        Assert.Equal(Page.Height - 90f, bottom, 3);
    }

    [Fact]
    public void ResetsCursorToContentTop_NotMargin_OnPageBreak()
    {
        RenderContext ctx = new(Page, Margin);
        ctx.SetChromeReservation(_ => new ChromeReservation(120f, 90f));
        ctx.SwitchToPage(0);

        Assert.Equal(Margin, ctx.CurrentY); // constructor ran before the reservation

        ctx.NewPage();
        Assert.Equal(120f, ctx.CurrentY);
        Assert.Equal(Page.Height - 90f, ctx.MaxY, 3);

        ctx.NewPage();
        Assert.Equal(120f, ctx.CurrentY);
    }

    [Fact]
    public void IgnoresReservation_WhileChromeItselfIsPainted()
    {
        RenderContext ctx = new(Page, Margin);
        ctx.SetChromeReservation(_ => new ChromeReservation(120f, 90f));
        ctx.SwitchToPage(0);

        ctx.ChromeReservationSuspended = true;
        Assert.Equal(Margin, ctx.ContentTop);
        Assert.Equal(Page.Height - Margin, ctx.ContentBottom, 3);
    }

    [Fact]
    public void Throws_WhenHeaderAndFooterLeaveNoUsableBodyHeight()
    {
        RenderContext ctx = new(Page, Margin);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
        {
            ctx.SetChromeReservation(_ => new ChromeReservation(500f, 400f));
            ctx.SwitchToPage(0);
        });

        Assert.Contains("Page 1", error.Message, StringComparison.Ordinal);
        Assert.Contains("500pt", error.Message, StringComparison.Ordinal);
        Assert.Contains("400pt", error.Message, StringComparison.Ordinal);
        Assert.Contains("841.89pt", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void KeepsEveryOverflowPage_InsideTheReservedBand()
    {
        var doc = new BonniePDF.Document();
        doc.Settings.PageSize = Page;
        doc.Settings.Margin = Margin;
        doc.Settings.HeaderTemplate = _ => Chrome(120f);
        doc.Settings.FooterTemplate = _ => Chrome(90f);
        doc.Add(new TextBlock
        {
            Text = string.Join(' ', Enumerable.Range(0, 2000).Select(i => $"line {i}")),
            FontSize = 11,
        });

        LayoutSnapshot? snapshot = doc
            .GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true })
            .Snapshot;
        Assert.NotNull(snapshot);

        IList<LayoutRect> rects = Walk(snapshot.Root)
            .First(n => string.Equals(n.Type, "TextBlock", StringComparison.Ordinal))
            .Rects;

        Assert.True(rects.Count > 1, "the text did not overflow onto a second page");
        foreach (LayoutRect rect in rects)
        {
            Assert.True(rect.Y >= 120f - 0.001f, $"rect starts at {rect.Y}, above the reserved header band");
            Assert.True(
                rect.Y + rect.Height <= Page.Height - 90f + 0.001f,
                $"rect ends at {rect.Y + rect.Height}, below the reserved footer band");
        }
    }

    [Fact]
    public void MeasuresChromePerPage_SoPage2CanReserveADifferentBand()
    {
        RenderContext ctx = new(Page, Margin);
        ctx.SetChromeReservation(pageNumber => new ChromeReservation(pageNumber == 1 ? 0f : 200f, 0f));
        ctx.SwitchToPage(0);
        Assert.Equal(Margin, ctx.ContentTop);

        ctx.NewPage();
        Assert.Equal(200f, ctx.ContentTop);
    }

    [Fact]
    public void StartsBodyContent_BelowAHeaderTallerThanTheMargin()
    {
        var doc = new BonniePDF.Document();
        doc.Settings.PageSize = Page;
        doc.Settings.Margin = Margin;
        doc.Settings.HeaderTemplate = _ => Chrome(120f);
        doc.Add(new TextBlock { Text = "body", FontSize = 10 });

        LayoutSnapshot? snapshot = doc
            .GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true })
            .Snapshot;
        Assert.NotNull(snapshot);

        LayoutNode text = Walk(snapshot.Root).First(n => string.Equals(n.Type, "TextBlock", StringComparison.Ordinal));

        // Without the reservation this lands at the margin (50) and collides
        // with the 120pt header painted above it.
        Assert.Equal(120f, text.Rects[0].Y, 3);
    }
}
