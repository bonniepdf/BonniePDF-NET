using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Rendering;
using Xunit;

namespace BonniePDF.Tests.Layout;

// VerticalStack spacing: MeasureHeight reserves (n-1) * Spacing, and BOTH render
// paths must consume it - RenderStateful (top-level, paginating) and RenderContent
// (non-stateful, used whenever the stack is nested inside an element that calls
// child.Render() directly). Skipping it on one path bunches the children together
// and dumps the reserved space as a gap below the stack.
public class VerticalStackTests
{
    private static RenderContext MakeContext() => new(new PdfSize(5000f, 5000f), 0f);

    [Fact]
    public void MeasureHeight_ReservesSpacingBetweenChildren()
    {
        VerticalStack stack = new() { Spacing = 6 };
        stack.Add(new Probe(10));
        stack.Add(new Probe(10));
        stack.Add(new Probe(10));

        Assert.Equal((10 * 3) + (6 * 2), stack.MeasureHeight(500), 3);
    }

    [Fact]
    public void RenderStateful_AppliesSpacingBetweenChildren()
    {
        Probe a = new(10);
        Probe b = new(10);
        VerticalStack stack = new() { Spacing = 6 };
        stack.Add(a);
        stack.Add(b);

        RenderContext ctx = MakeContext();
        float startY = ctx.CurrentY;
        stack.RenderStateful(ctx, 500);

        Assert.Equal(startY, a.RenderedAtY, 3);
        Assert.Equal(startY + 10 + 6, b.RenderedAtY, 3);
    }

    [Fact]
    public void RenderContent_AppliesSpacingWhenNested()
    {
        Probe a = new(10);
        Probe b = new(10);
        Probe c = new(10);
        VerticalStack stack = new() { Spacing = 6 };
        stack.Add(a);
        stack.Add(b);
        stack.Add(c);
        Container outer = new() { Child = stack };

        RenderContext ctx = MakeContext();
        float startY = ctx.CurrentY;
        outer.Render(ctx, 500);

        Assert.Equal(startY, a.RenderedAtY, 3);
        Assert.Equal(startY + 10 + 6, b.RenderedAtY, 3);
        Assert.Equal(startY + ((10 + 6) * 2), c.RenderedAtY, 3);
    }

    [Fact]
    public void RenderContent_AdvancesYByMeasuredHeightIncludingSpacing()
    {
        VerticalStack stack = new() { Spacing = 6 };
        stack.Add(new Probe(10));
        stack.Add(new Probe(10));
        Container outer = new() { Child = stack };

        RenderContext ctx = MakeContext();
        float startY = ctx.CurrentY;
        outer.Render(ctx, 500);

        Assert.Equal(10 + 6 + 10, ctx.CurrentY - startY, 3);
    }

    /// <summary>Records the Y it was rendered at; never advances Y itself.</summary>
    private sealed class Probe : ElementBase
    {
        private readonly float _contentHeight;

        public Probe(float contentHeight = 10) => _contentHeight = contentHeight;

        public float RenderedAtY { get; private set; } = -1;

        protected override float GetPreferredContentWidth() => 0;

        protected override float MeasureContentHeight(float contentWidth) => _contentHeight;

        protected override void RenderContent(RenderContext ctx, float contentWidth) => RenderedAtY = ctx.CurrentY;
    }
}
