using BonniePDF.Drawing;
using Xunit;

namespace BonniePDF.Tests.Document;

public class RichTextTests
{
    // The default style is Helvetica 12pt; line height is font size * multiplier (default 1.2).
    private const string DefaultFont = "Helvetica";
    private const float DefaultSize = 12f;
    private const float LineHeight = DefaultSize * 1.2f; // 14.4

    private static float W(string text, float size = DefaultSize, string font = DefaultFont) =>
        FontMetrics.GetStringWidth(text, font, size);

    private static void Close(float expected, float actual, float tol = 0.01f) =>
        Assert.True(Math.Abs(expected - actual) <= tol, $"expected {expected} ± {tol}, got {actual}");

    private static TextStyle Sized(float size) => new() { FontSize = size };

    private static (RenderContext Ctx, RecordingCanvas Canvas) MakeContext(float pageHeight = 5000f)
    {
        RenderContext ctx = new(new PdfSize(5000f, pageHeight), 0f);
        RecordingCanvas canvas = new();
        ctx.SetCanvas(canvas);
        return (ctx, canvas);
    }

    // --- Line height and measurement ---

    [Fact]
    public void EmptyParagraph_OccupiesOneDefaultLine()
    {
        Close(LineHeight, new RichText().MeasureHeight(500));
    }

    [Fact]
    public void SingleLine_HasDefaultLineHeight()
    {
        Close(LineHeight, new RichText().Span("Hello world").MeasureHeight(500));
    }

    [Fact]
    public void LineHeight_IsDrivenByTallestFontOnTheLine()
    {
        RichText rt = new RichText().Span("small ").Span("big", Sized(24));
        Close(24f * 1.2f, rt.MeasureHeight(500));
    }

    [Fact]
    public void LineHeight_HonoursDefaultStyleMultiplier()
    {
        TextStyle defaultStyle = new() { FontSize = 12f, LineHeight = 1.5f };
        Close(12f * 1.5f, new RichText { DefaultStyle = defaultStyle }.MeasureHeight(500));

        RichText rt = new()
        {
            DefaultStyle = defaultStyle
        };
        rt.Line("one").Line("two");

        Close(2f * 12f * 1.5f, rt.MeasureHeight(500));
    }

    [Fact]
    public void LineHeight_UsesGreatestFontSizeAndMultiplierProductAcrossMixedRuns()
    {
        TextStyle loose = new() { FontSize = 12f, LineHeight = 3f };
        TextStyle large = new() { FontSize = 24f };
        RichText rt = new RichText().Span("loose ", loose).Span("large", large);

        Close(12f * 3f, rt.MeasureHeight(500));
    }

    [Fact]
    public void TotalHeight_IsSumOfPerLineHeights()
    {
        RichText rt = new RichText().Line("one").Line("two");
        Close(2 * LineHeight, rt.MeasureHeight(500));
    }

    // --- Word wrapping ---

    [Fact]
    public void Wrapping_KeepsWordsOnOneLineWhenTheyFit()
    {
        float full = W("Hello world");
        Close(LineHeight, new RichText().Span("Hello world").MeasureHeight(full + 50));
    }

    [Fact]
    public void Wrapping_BreaksWhenNextWordExceedsWidth()
    {
        float full = W("Hello world");
        float firstWord = W("Hello");
        float width = full - 1;
        Assert.True(width > firstWord);
        Close(2 * LineHeight, new RichText().Span("Hello world").MeasureHeight(width));
    }

    [Fact]
    public void Wrapping_ReflowsWhenWidthChanges()
    {
        RichText rt = new RichText().Span("Hello world");
        float full = W("Hello world");
        Close(LineHeight, rt.MeasureHeight(full + 50));   // 1 line
        Close(2 * LineHeight, rt.MeasureHeight(full - 1)); // re-flows to 2
        Close(LineHeight, rt.MeasureHeight(full + 50));    // back to 1
    }

    // --- Explicit breaks ---

    [Fact]
    public void ExplicitLine_EmitsOneLinePerCall()
    {
        Close(LineHeight, new RichText().Line("a").MeasureHeight(500));
        Close(3 * LineHeight, new RichText().Line("a").Line("b").Line("c").MeasureHeight(500));
    }

    [Fact]
    public void SpanThenLineBreakThenSpan_MakesTwoLines()
    {
        RichText rt = new RichText().Span("a").LineBreak().Span("b");
        Close(2 * LineHeight, rt.MeasureHeight(500));
    }

    [Fact]
    public void LeadingLineBreak_ProducesBlankLineAboveText()
    {
        RichText rt = new RichText().LineBreak().Span("b");
        Close(2 * LineHeight, rt.MeasureHeight(500));
    }

    // --- Horizontal alignment ---

    private static float FirstDrawX(TextAlignment align)
    {
        (RenderContext ctx, RecordingCanvas canvas) = MakeContext();
        RichText rt = new RichText().Span("Hello");
        rt.Alignment = align;
        rt.Render(ctx, 500);
        return canvas.Texts[0].X;
    }

    [Fact]
    public void Alignment_LeftPlacesLineAtStart()
    {
        Close(0, FirstDrawX(TextAlignment.Left), 0.05f);
    }

    [Fact]
    public void Alignment_CenterOffsetsByHalfTheSlack()
    {
        Close((500 - W("Hello")) / 2f, FirstDrawX(TextAlignment.Center), 0.05f);
    }

    [Fact]
    public void Alignment_RightOffsetsByFullSlack()
    {
        Close(500 - W("Hello"), FirstDrawX(TextAlignment.Right), 0.05f);
    }

    // --- Justify ---

    [Fact]
    public void Justify_StretchesNonLastLineButLeavesLastLineRagged()
    {
        const float width = 80f;
        // "Hello world" (~62) fits on line 1; "Bonnie" is forced to line 2.
        RichText rt = new RichText().Span("Hello world Bonnie");
        rt.Alignment = TextAlignment.Justify;
        (RenderContext ctx, RecordingCanvas canvas) = MakeContext();
        rt.Render(ctx, width);

        var line1 = canvas.Texts.Where(t => Math.Abs(t.Y - 0) < 0.001f).ToList();
        var line2 = canvas.Texts.Where(t => Math.Abs(t.Y - LineHeight) < 0.001f).ToList();

        Assert.Equal(new[] { "Hello", "world" }, line1.Select(t => t.Text));
        float rightEdge = line1.Max(t => t.X + W(t.Text));
        Close(width, rightEdge, 0.05f); // flush to the right margin

        Assert.Equal(new[] { "Bonnie" }, line2.Select(t => t.Text));
        Close(0, line2[0].X, 0.05f);
    }

    // --- Run merging ---

    [Fact]
    public void RunMerging_MergesAdjacentSameStyleSpansIntoOneDrawCall()
    {
        (RenderContext ctx, RecordingCanvas canvas) = MakeContext();
        new RichText().Span("Hello").Span("World").Render(ctx, 500);
        Assert.Single(canvas.Texts);
        Assert.Equal("HelloWorld", canvas.Texts[0].Text);
    }

    [Fact]
    public void RunMerging_KeepsDifferentStyledSpansSeparate()
    {
        TextStyle bold = new() { FontWeight = FontWeight.Bold };
        (RenderContext ctx, RecordingCanvas canvas) = MakeContext();
        new RichText().Span("Hello", bold).Span("World").Render(ctx, 500);
        Assert.Equal(new[] { "Hello", "World" }, canvas.Texts.Select(t => t.Text));
        Assert.NotEqual(canvas.Texts[0].FontFamily, canvas.Texts[1].FontFamily);
    }

    // --- Decorations ---

    [Fact]
    public void Decoration_UnderlineIsDrawnBelowBaselineAtSpecOffset()
    {
        TextStyle style = new() { Decoration = TextDecoration.Underline };
        (RenderContext ctx, RecordingCanvas canvas) = MakeContext();
        new RichText().Span("X", style).Render(ctx, 500);

        Assert.Single(canvas.Lines);
        var line = canvas.Lines[0];
        float expectedY = 0 + DefaultSize + DefaultSize / 7f; // y + fontSize + fontSize/7
        Close(expectedY, line.Y1, 0.05f);
        Close(expectedY, line.Y2, 0.05f);
        Close(0, line.X1, 0.05f);
        Close(W("X"), line.X2, 0.05f);
        Close(Math.Max(0.5f, DefaultSize / 18f), line.Thickness, 0.01f);
    }

    [Fact]
    public void Decoration_StrikethroughIsDrawnThroughTextAtSpecOffset()
    {
        TextStyle style = new() { Decoration = TextDecoration.Strikethrough };
        (RenderContext ctx, RecordingCanvas canvas) = MakeContext();
        new RichText().Span("X", style).Render(ctx, 500);

        Assert.Single(canvas.Lines);
        Close(DefaultSize * 0.55f, canvas.Lines[0].Y1, 0.05f);
    }

    // --- Stateful pagination (widow/orphan control) ---

    [Fact]
    public void Pagination_BalancesSplitToNeverLeaveASingleWidowLine()
    {
        // Five 14.4pt lines. A 60pt page fits four geometrically, which would orphan the
        // 5th line alone; widow control (MinLinesAfterBreak = 2) must break 3 / 2 instead.
        RichText rt = new RichText().Line("L1").Line("L2").Line("L3").Line("L4").Line("L5");

        (RenderContext ctx1, RecordingCanvas canvas1) = MakeContext(60f);
        RenderStatus status1 = rt.RenderStateful(ctx1, 500);
        Assert.Equal(RenderStatus.Overflow, status1);
        Assert.Equal(new[] { "L1", "L2", "L3" }, canvas1.Texts.Select(t => t.Text)); // 3, not 4

        (RenderContext ctx2, RecordingCanvas canvas2) = MakeContext(5000f);
        RenderStatus status2 = rt.RenderStateful(ctx2, 500);
        Assert.Equal(RenderStatus.Complete, status2);
        Assert.Equal(new[] { "L4", "L5" }, canvas2.Texts.Select(t => t.Text));
    }

    [Fact]
    public void Pagination_CompletesAndAdvancesYWhenEverythingFits()
    {
        RichText rt = new RichText().Line("one").Line("two");
        (RenderContext ctx, _) = MakeContext(5000f);
        float startY = ctx.CurrentY;
        RenderStatus status = rt.RenderStateful(ctx, 500);
        Assert.Equal(RenderStatus.Complete, status);
        Close(2 * LineHeight, ctx.CurrentY - startY);
    }

    [Fact]
    public void Pagination_ResetStateRendersAgainFromTop()
    {
        RichText rt = new RichText().Line("one").Line("two");
        (RenderContext ctx1, _) = MakeContext(5000f);
        rt.RenderStateful(ctx1, 500);
        rt.ResetState();
        (RenderContext ctx2, RecordingCanvas canvas2) = MakeContext(5000f);
        RenderStatus status = rt.RenderStateful(ctx2, 500);
        Assert.Equal(RenderStatus.Complete, status);
        Assert.Equal(new[] { "one", "two" }, canvas2.Texts.Select(t => t.Text));
    }

    // Style snapshotting: the fluent builders mutate and return `this`, so a
    // style shared across spans and mutated between them must not leak its final
    // state back onto earlier spans. Shared conformance behaviour — the
    // TypeScript sibling asserts the same in richtext.test.ts.
    [Fact]
    public void Span_SnapshotsStyleAtCallTime_ImmuneToLaterMutation()
    {
        TextStyle shared = new TextStyle().WithSize(9);
        (RenderContext ctx, RecordingCanvas canvas) = MakeContext();

        RichText rt = new RichText().Span("first", shared);
        shared.WithSize(30); // mutate the shared style after the first span
        rt.Span("second", shared);
        rt.Render(ctx, 5000);

        var sizes = canvas.Texts.Select(t => t.FontSize).ToList();
        // The first span kept size 9; the second is 30. The aliasing bug
        // produced [30, 30] (no 9).
        Assert.Contains(9f, sizes);
        Assert.Contains(30f, sizes);
    }

    [Fact]
    public void Clone_ReturnsIndependentCopy()
    {
        TextStyle a = new TextStyle().WithSize(10).WithColor(Color.Red).Bold();
        a.Italic = true;

        TextStyle b = a.Clone();
        Assert.Equal(10, b.FontSize);
        Assert.Equal(Color.Red, b.Color);
        Assert.Equal(FontWeight.Bold, b.FontWeight);
        Assert.True(b.Italic);

        // Mutating the original must not touch the clone.
        a.WithSize(20).WithColor(Color.Black);
        a.Italic = false;
        Assert.Equal(10, b.FontSize);
        Assert.Equal(Color.Red, b.Color);
        Assert.True(b.Italic);
    }

    /// <summary>Canvas that captures structured draw calls so geometry can be asserted exactly.</summary>
    private sealed class RecordingCanvas : ICanvas
    {
        public List<(float X, float Y, string Text, float FontSize, string FontFamily)> Texts { get; } = new();
        public List<(float X1, float Y1, float X2, float Y2, float Thickness)> Lines { get; } = new();
        public List<(float X, float Y, float Width, float Height)> Rects { get; } = new();

        public float Width => 5000;
        public float Height => 5000;

        public void DrawText(float x, float y, string text, Color color, float fontSize, string fontFamily, TextAlignment align) =>
            Texts.Add((x, y, text, fontSize, fontFamily));
        public void DrawLine(float x1, float y1, float x2, float y2, Color color, float lineWidth) =>
            Lines.Add((x1, y1, x2, y2, lineWidth));
        public void FillRect(float x, float y, float width, float height, Color color) =>
            Rects.Add((x, y, width, height));

        public void SaveState() { }
        public void RestoreState() { }
        public void StrokeRect(float x, float y, float width, float height, Color color, float strokeWidth) { }
        public void FillCircle(float cx, float cy, float radius, Color color) { }
        public void FillEllipse(float cx, float cy, float rx, float ry, Color color) { }
        public void FillPath(IList<Point> points, Color color) { }
        public void FillRoundedRect(float x, float y, float width, float height, float radius, Color color) { }
        public void StrokeRoundedRect(float x, float y, float width, float height, float radius, Color color, float strokeWidth) { }
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
        public void AddEllipse(float cx, float cy, float rx, float ry) { }
        public void AddCircle(float cx, float cy, float radius) { }
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
