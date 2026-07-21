using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Images;
using Xunit;

namespace BonniePDF.Tests.Document;

// Container layout: a fixed Height wins outright; otherwise
// height = childHeight + padding(top+bottom) + border(vertical). The child is measured/
// rendered against effectiveWidth - padding(left+right) - border(horizontal).
public class ContainerTests
{
    private static (RenderContext Ctx, RecordingCanvas Canvas) MakeContext(float pageHeight = 5000f)
    {
        RenderContext ctx = new(new PdfSize(5000f, pageHeight), 0f);
        RecordingCanvas canvas = new();
        ctx.SetCanvas(canvas);
        return (ctx, canvas);
    }

    private static Border AllSidesBorder(float width) => new(width);

    // --- Height measurement ---

    [Fact]
    public void ChildlessContainer_IsJustVerticalPadding()
    {
        Container c = new() { Padding = new Thickness(5, 10, 15, 20) }; // left, top, right, bottom
        Assert.Equal(10 + 20, c.MeasureHeight(500), 3);
    }

    [Fact]
    public void ChildlessContainer_IncludesVerticalBorderWidth()
    {
        Container c = new() { Border = AllSidesBorder(2) }; // verticalWidth = 4
        Assert.Equal(4, c.MeasureHeight(500), 3);
    }

    [Fact]
    public void FixedHeight_OverridesContentMeasurement()
    {
        Container c = new()
        {
            Height = 200,
            Child = new Probe(50),
            Padding = new Thickness(5, 10, 15, 20)
        };
        Assert.Equal(200, c.MeasureHeight(500), 3);
    }

    [Fact]
    public void Height_IsChildPlusVerticalPadding_AndChildMeasuredInsidePadding()
    {
        Probe probe = new(10);
        Container c = new() { Child = probe, Padding = new Thickness(5, 10, 15, 20) };
        Assert.Equal(10 + 10 + 20, c.MeasureHeight(500), 3);
        Assert.Equal(500 - 5 - 15, probe.MeasuredWidth, 3); // inner width
    }

    [Fact]
    public void Border_ShrinksChildMeasureWidthAndAddsToHeight()
    {
        Probe probe = new(10);
        Container c = new() { Child = probe, Border = AllSidesBorder(2) }; // h-width 4, v-width 4
        Assert.Equal(-1, probe.MeasuredWidth); // not yet measured
        Assert.Equal(10 + 4, c.MeasureHeight(500), 3);
        Assert.Equal(500 - 4, probe.MeasuredWidth, 3);
    }

    [Fact]
    public void FixedWidth_IsTheEffectiveWidthChildIsMeasuredAgainst()
    {
        Probe probe = new(10);
        Container c = new() { Child = probe, Width = 200 };
        c.MeasureHeight(500); // even when offered 500, the child is sized to 200
        Assert.Equal(200, probe.MeasuredWidth, 3);
    }

    // --- Preferred width ---

    [Fact]
    public void PreferredWidth_ReturnsFixedWidthWhenSet()
    {
        Container c = new() { Width = 150 };
        Assert.Equal(150, c.GetPreferredWidth(), 3);
    }

    [Fact]
    public void PreferredWidth_IsChildPlusHorizontalPaddingAndBorder()
    {
        Probe probe = new() { PreferredWidth = 40 };
        Container c = new()
        {
            Child = probe,
            Padding = new Thickness(5, 0, 15, 0),
            Border = AllSidesBorder(2) // h-width 4
        };
        Assert.Equal(40 + 5 + 15 + 4, c.GetPreferredWidth(), 3);
    }

    // --- Child positioning on render ---

    [Fact]
    public void Render_OffsetsChildByPaddingAndBorderAndShrinksWidth()
    {
        (RenderContext ctx, _) = MakeContext();
        Probe probe = new(10);
        Container c = new()
        {
            Child = probe,
            Padding = new Thickness(5, 10, 15, 20),
            Border = AllSidesBorder(2)
        };

        float startY = ctx.CurrentY;
        c.Render(ctx, 500);

        Assert.Equal(5, probe.RenderedX, 3);            // currentX + padding.left
        Assert.Equal(startY + 10, probe.RenderedAtY, 3); // + padding.top
        Assert.Equal(500 - 5 - 15 - 4, probe.RenderedWidth, 3); // - padding - border
        Assert.Equal(10 + 10 + 20 + 4, ctx.CurrentY - startY, 3); // full box advance
    }

    [Fact]
    public void Render_AdvancesYByFullFixedHeightEvenWhenChildShorter()
    {
        (RenderContext ctx, _) = MakeContext();
        Container c = new() { Child = new Probe(10), Height = 200 };
        float startY = ctx.CurrentY;
        c.Render(ctx, 500);
        Assert.Equal(200, ctx.CurrentY - startY, 3);
    }

    // --- Horizontal alignment of a narrower child ---

    private static Probe RenderAligned(HorizontalAlignment? alignment, float preferredWidth = 100f)
    {
        (RenderContext ctx, _) = MakeContext();
        Probe probe = new(10) { PreferredWidth = preferredWidth };
        Container c = new() { Child = probe };
        if (alignment.HasValue) c.HorizontalAlignment = alignment.Value;
        c.Render(ctx, 500);
        return probe;
    }

    [Fact]
    public void Render_LeftAlignsByDefault()
    {
        Probe probe = RenderAligned(null);
        Assert.Equal(0, probe.RenderedX, 3);
        Assert.Equal(500, probe.RenderedWidth, 3); // fills, no centering
    }

    [Fact]
    public void Render_StartKeepsChildPinnedLeft()
    {
        Probe probe = RenderAligned(HorizontalAlignment.Start);
        Assert.Equal(0, probe.RenderedX, 3);
        Assert.Equal(500, probe.RenderedWidth, 3);
    }

    [Fact]
    public void Render_CenterOffsetsByHalfSlackAndUsesPreferredWidth()
    {
        Probe probe = RenderAligned(HorizontalAlignment.Center);
        Assert.Equal((500 - 100) / 2f, probe.RenderedX, 3); // 200
        Assert.Equal(100, probe.RenderedWidth, 3);
    }

    [Fact]
    public void Render_EndRightAlignsChild()
    {
        Probe probe = RenderAligned(HorizontalAlignment.End);
        Assert.Equal(500 - 100, probe.RenderedX, 3); // 400
        Assert.Equal(100, probe.RenderedWidth, 3);
    }

    [Fact]
    public void Render_DoesNotOffsetWhenChildFillsWidth()
    {
        Probe probe = RenderAligned(HorizontalAlignment.Center, preferredWidth: 0f);
        Assert.Equal(0, probe.RenderedX, 3);
        Assert.Equal(500, probe.RenderedWidth, 3);
    }

    [Fact]
    public void Render_HonoursAlignmentWithinFixedWidthAndPadding()
    {
        (RenderContext ctx, _) = MakeContext();
        Probe probe = new(10) { PreferredWidth = 40 };
        Container c = new()
        {
            Child = probe,
            Width = 200,
            Padding = new Thickness(10, 0, 10, 0), // inner width 180
            HorizontalAlignment = HorizontalAlignment.Center
        };
        c.Render(ctx, 500);
        // inner width 180, slack 140, half = 70, plus padding.left 10
        Assert.Equal(10 + 70, probe.RenderedX, 3);
        Assert.Equal(40, probe.RenderedWidth, 3);
    }

    // --- Background and border drawing ---

    [Fact]
    public void Render_FillsBackgroundAcrossFullBox()
    {
        (RenderContext ctx, RecordingCanvas canvas) = MakeContext();
        Container c = new() { Child = new Probe(10), BackgroundColor = ColorPalette.Amber[400] };
        c.Render(ctx, 500);

        Assert.Single(canvas.Rects);
        Assert.Equal((0f, 0f, 500f, 10f), canvas.Rects[0]);
    }

    [Fact]
    public void Render_UsesRoundedFillWhenCornerRadiusSet()
    {
        (RenderContext ctx, RecordingCanvas canvas) = MakeContext();
        Container c = new() { Child = new Probe(10), BackgroundColor = ColorPalette.Amber[400], CornerRadius = 6 };
        c.Render(ctx, 500);

        Assert.Empty(canvas.Rects); // not a sharp fill
        Assert.Single(canvas.RoundedRects);
        var r = canvas.RoundedRects[0];
        Assert.Equal((0f, 0f, 500f, 6f), (r.X, r.Y, r.Width, r.Radius));
    }

    [Fact]
    public void Render_ClipsContentToRoundedRectWhenClipContentSet()
    {
        (RenderContext ctx, RecordingCanvas canvas) = MakeContext();
        Container c = new()
        {
            Child = new Probe(10),
            BackgroundColor = ColorPalette.Amber[400],
            CornerRadius = 6,
            ClipContent = true
        };
        c.Render(ctx, 500);

        // The whole box (full width, full height incl. child) is clipped to the rounded outline,
        // and the clip is balanced (opened and closed exactly once).
        Assert.Single(canvas.Clips);
        Assert.Equal((0f, 0f, 500f, 10f, 6f), canvas.Clips[0]);
        Assert.Equal(0, canvas.OpenClips);
    }

    [Fact]
    public void Render_DoesNotClipWhenCornerRadiusIsZeroEvenIfClipContentSet()
    {
        (RenderContext ctx, RecordingCanvas canvas) = MakeContext();
        Container c = new() { Child = new Probe(10), ClipContent = true }; // CornerRadius defaults to 0
        c.Render(ctx, 500);

        Assert.Empty(canvas.Clips);
    }

    [Fact]
    public void Render_CornerRadiusAloneDoesNotClip()
    {
        (RenderContext ctx, RecordingCanvas canvas) = MakeContext();
        Container c = new() { Child = new Probe(10), BackgroundColor = ColorPalette.Amber[400], CornerRadius = 6 };
        c.Render(ctx, 500);

        // A rounded fill is emitted, but content is not masked unless ClipContent is set.
        Assert.Single(canvas.RoundedRects);
        Assert.Empty(canvas.Clips);
    }

    [Fact]
    public void Render_WrapsContentInCrispEdgeRegionWhenCrispEdgesSet()
    {
        (RenderContext ctx, RecordingCanvas canvas) = MakeContext();
        Container c = new() { Child = new Probe(10), BackgroundColor = ColorPalette.Amber[400], CrispEdges = true };
        c.Render(ctx, 500);

        // Exactly one balanced Begin/End crisp-edge pair around the content.
        Assert.Equal(1, canvas.CrispBegins);
        Assert.Equal(1, canvas.CrispEnds);
    }

    [Fact]
    public void Render_DoesNotOpenCrispEdgeRegionByDefault()
    {
        (RenderContext ctx, RecordingCanvas canvas) = MakeContext();
        Container c = new() { Child = new Probe(10) };
        c.Render(ctx, 500);

        Assert.Equal(0, canvas.CrispBegins);
    }

    [Fact]
    public void Render_DrawsEachBorderSideAsAStroke()
    {
        (RenderContext ctx, RecordingCanvas canvas) = MakeContext();
        Container c = new()
        {
            Child = new Probe(10),
            Border = AllSidesBorder(2),
            BorderColors = new BorderColors(Color.Red)
        };
        c.Render(ctx, 500);

        const float h = 10 + 4; // child + vertical border
        // All four sides are stroked with the correct geometry. Stroke order is an
        // implementation detail (it differs from the TS port) and is not asserted.
        Assert.Equal(4, canvas.Lines.Count);
        Assert.Contains((0f, 0f, 500f, 0f, 2f), canvas.Lines);   // top
        Assert.Contains((0f, h, 500f, h, 2f), canvas.Lines);     // bottom
        Assert.Contains((0f, 0f, 0f, h, 2f), canvas.Lines);      // left
        Assert.Contains((500f, 0f, 500f, h, 2f), canvas.Lines);  // right
    }

    // --- Pagination decision (uses a real context so NewPage's machinery runs) ---

    [Fact]
    public void Pagination_BreaksWhenContainerWontFitAndNotAtTop()
    {
        RenderContext ctx = new(new PdfSize(500f, 100f), 0f); // MaxY = 100, real canvas
        ctx.CurrentY = 90f;
        Container c = new() { Child = new Probe(50) }; // 90 + 50 = 140 > 100
        c.Render(ctx, 500);
        Assert.Equal(1, ctx.CurrentPageIndex); // broke onto a second page
    }

    [Fact]
    public void Pagination_DoesNotBreakWhenAlreadyAtTop()
    {
        RenderContext ctx = new(new PdfSize(500f, 100f), 0f);
        ctx.CurrentY = 0f; // at the top margin
        Container c = new() { Child = new Probe(200) }; // taller than page, but at top
        c.Render(ctx, 500);
        Assert.Equal(0, ctx.CurrentPageIndex); // no blank-page loop
    }

    /// <summary>A leaf element that records the width it was measured/rendered at and advances Y by a fixed height.</summary>
    private sealed class Probe : ElementBase
    {
        private readonly float _contentHeight;
        public float MeasuredWidth { get; private set; } = -1;
        public float RenderedX { get; private set; } = -1;
        public float RenderedWidth { get; private set; } = -1;
        public float RenderedAtY { get; private set; } = -1;
        public float PreferredWidth { get; set; }

        public Probe(float contentHeight = 10) => _contentHeight = contentHeight;

        protected override float GetPreferredContentWidth() => PreferredWidth;

        protected override float MeasureContentHeight(float contentWidth)
        {
            MeasuredWidth = contentWidth;
            return _contentHeight;
        }

        protected override void RenderContent(RenderContext ctx, float contentWidth)
        {
            RenderedX = ctx.CurrentX;
            RenderedWidth = contentWidth;
            RenderedAtY = ctx.CurrentY;
            ctx.CurrentY += _contentHeight;
        }
    }

    private sealed class RecordingCanvas : ICanvas
    {
        public List<(float X, float Y, float Width, float Height)> Rects { get; } = new();
        public List<(float X, float Y, float Width, float Height, float Radius)> RoundedRects { get; } = new();
        public List<(float X1, float Y1, float X2, float Y2, float Thickness)> Lines { get; } = new();
        public List<(float X, float Y, float Width, float Height, float Radius)> Clips { get; } = new();
        public int OpenClips { get; private set; }
        public int CrispBegins { get; private set; }
        public int CrispEnds { get; private set; }

        public float Width => 5000;
        public float Height => 5000;

        public void BeginRoundedClip(float x, float y, float width, float height, float radius)
        {
            Clips.Add((x, y, width, height, radius));
            OpenClips++;
        }
        public void EndClip() => OpenClips--;

        public void BeginCrispEdges() => CrispBegins++;
        public void EndCrispEdges() => CrispEnds++;

        public void FillRect(float x, float y, float width, float height, Color color) =>
            Rects.Add((x, y, width, height));
        public void FillRoundedRect(float x, float y, float width, float height, float radius, Color color) =>
            RoundedRects.Add((x, y, width, height, radius));
        public void DrawLine(float x1, float y1, float x2, float y2, Color color, float lineWidth) =>
            Lines.Add((x1, y1, x2, y2, lineWidth));

        public void SaveState() { }
        public void RestoreState() { }
        public void StrokeRect(float x, float y, float width, float height, Color color, float strokeWidth) { }
        public void FillCircle(float cx, float cy, float radius, Color color) { }
        public void FillEllipse(float cx, float cy, float rx, float ry, Color color) { }
        public void FillPath(IList<Point> points, Color color) { }
        public void StrokeRoundedRect(float x, float y, float width, float height, float radius, Color color, float strokeWidth) { }
        public void DrawText(float x, float y, string text, Color color, float fontSize, string fontFamily, TextAlignment align) { }
        public void DrawTextRotated(float x, float y, string text, Color color, float fontSize, string fontFamily, float angleDegrees) { }
        public void DrawTextJustified(float x, float y, string text, Color color, float fontSize, string fontFamily, float availableWidth) { }
        public Size MeasureText(string text, float fontSize, string fontFamily) => new(text.Length * fontSize * 0.6f, fontSize);
        public void SetTextSpacing(float letterSpacing, float wordSpacing) { }
        public void DrawImage(ImageInfo image, float x, float y, float width, float height) { }
        public void DrawFormXObject(string resourceName, float x, float y) { }
        public void BeginMarkedContent(string role) { }
        public void BeginContainerContent(string role) { }
        public void EndMarkedContent() { }
        public void BeginArtifact() { }
        public void EndArtifact() { }
        public void BeginPath() { }
        public void MoveTo(float x, float y) { }
        public void LineTo(float x, float y) { }
        public void CubicBezierTo(float cp1x, float cp1y, float cp2x, float cp2y, float x, float y) { }
        public void QuadraticBezierTo(float cpx, float cpy, float x, float y) { }
        public void AddRect(float x, float y, float width, float height) { }
        public void ClosePath() { }
        public void FillCurrentPath(Paint paint, FillRule fillRule) { }
        public void StrokeCurrentPath(StrokeStyle style) { }
        public void Translate(float dx, float dy) { }
        public void Scale(float sx, float sy) { }
        public void Rotate(float angleDegrees) { }
        public void Transform(float a, float b, float c, float d, float tx, float ty) { }
        public void ClipToCurrentPath(FillRule fillRule) { }
        public void SetGlobalAlpha(float alpha) { }
    }
}
