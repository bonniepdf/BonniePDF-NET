using BonniePDF.Fluent;
using BonniePDF.Drawing;
using Xunit;

namespace BonniePDF.Tests.Layout;

public class LayoutMarginPaddingTests
{
    [Fact]
    public void ElementBase_OffsetsContentByLeftMarginWhileReservingRightMargin()
    {
        RenderContext ctx = new(PdfSize.A4, 50);
        ProbeElement element = new() { Margin = new Thickness(20) };

        element.Render(ctx, 500);

        Assert.Equal(70, element.RenderedX, 0.1);
        Assert.Equal(460, element.RenderedWidth, 0.1);
        Assert.Equal(50, ctx.CurrentX, 0.1);
    }

    [Fact]
    public void Table_OffsetsStatefulRowsByTableLeftMargin()
    {
        RenderContext ctx = new(PdfSize.A4, 50);
        ProbeElement probe = new();
        Table table = new(1)
        {
            Margin = new Thickness(10, 0, 10, 0),
            CellPadding = 5
        };
        table.AddRow(probe);

        RenderStatus status = table.RenderStateful(ctx, 500);

        Assert.Equal(RenderStatus.Complete, status);
        Assert.Equal(65, probe.RenderedX, 0.1);
        Assert.Equal(470, probe.RenderedWidth, 0.1);
        Assert.Equal(50, ctx.CurrentX, 0.1);
    }

    [Fact]
    public void List_DoesNotDoubleApplyLeftMargin()
    {
        (RenderContext ctx, MockCanvas canvas) = CreateMockedContext();
        OrderedList list = new()
        {
            Margin = new Thickness(10, 0, 10, 0)
        };
        list.AddItem("Item");

        list.Render(ctx, 500);

        Assert.Contains("DrawText(60,50,'1.',12,Helvetica,Left)", canvas.Operations);
    }

    [Fact]
    public void VisualToc_OffsetsStatefulEntriesByLeftMargin()
    {
        (RenderContext ctx, MockCanvas canvas) = CreateMockedContext();
        VisualToc toc = new()
        {
            Margin = new Thickness(10, 0, 10, 0),
            HeadingIdentifiers = ["Chapter"]
        };
        ctx.TocEntries["Chapter"] = 1;

        RenderStatus status = toc.RenderStateful(ctx, 500);

        Assert.Equal(RenderStatus.Complete, status);
        Assert.Contains("DrawText(60,50,'Chapter',12,Helvetica,Left)", canvas.Operations);
        Assert.Equal(50, ctx.CurrentX, 0.1);
    }

    [Fact]
    public void StandaloneTableCell_AppliesPaddingAndAdvancesByPaddedHeight()
    {
        RenderContext ctx = new(PdfSize.A4, 50);
        ProbeElement probe = new();
        TableCell cell = new()
        {
            Content = probe,
            Padding = new Thickness(10, 20, 30, 40)
        };

        cell.Render(ctx, 500);

        Assert.Equal(60, probe.RenderedX, 0.1);
        Assert.Equal(460, probe.RenderedWidth, 0.1);
        Assert.Equal(120, ctx.CurrentY, 0.1);
    }

    [Fact]
    public void ContentSlot_AppliesOwnMarginsBeforeRenderingChildren()
    {
        RenderContext ctx = new(PdfSize.A4, 50);
        ProbeElement probe = new();
        ContentSlot slot = new()
        {
            Margin = new Thickness(10, 20, 30, 40),
            Child = probe
        };

        RenderStatus status = slot.RenderStateful(ctx, 500);

        Assert.Equal(RenderStatus.Complete, status);
        Assert.Equal(60, probe.RenderedX, 0.1);
        Assert.Equal(460, probe.RenderedWidth, 0.1);
        Assert.Equal(50, ctx.CurrentX, 0.1);
        Assert.Equal(120, ctx.CurrentY, 0.1);
    }

    private static (RenderContext Ctx, MockCanvas Canvas) CreateMockedContext()
    {
        RenderContext ctx = new(PdfSize.A4, 50);
        MockCanvas canvas = new();
        ctx.SetCanvas(canvas);
        return (ctx, canvas);
    }

    private sealed class ProbeElement : ElementBase
    {
        public float RenderedX { get; private set; }
        public float RenderedWidth { get; private set; }

        protected override float MeasureContentHeight(float contentWidth) => 10;

        protected override void RenderContent(RenderContext ctx, float contentWidth)
        {
            RenderedX = ctx.CurrentX;
            RenderedWidth = contentWidth;
            ctx.CurrentY += 10;
        }
    }

    private sealed class MockCanvas : ICanvas
    {
        public List<string> Operations { get; } = [];
        public float Width => 800;
        public float Height => 450;

        public void SaveState() => Operations.Add("SaveState()");
        public void RestoreState() => Operations.Add("RestoreState()");
        public void FillRect(float x, float y, float width, float height, Color color) =>
            Operations.Add($"FillRect({x},{y},{width},{height})");
        public void StrokeRect(float x, float y, float width, float height, Color color, float strokeWidth) =>
            Operations.Add($"StrokeRect({x},{y},{width},{height},{strokeWidth})");
        public void DrawLine(float x1, float y1, float x2, float y2, Color color, float lineWidth) =>
            Operations.Add($"DrawLine({x1},{y1},{x2},{y2},{lineWidth})");
        public void FillCircle(float cx, float cy, float radius, Color color) =>
            Operations.Add($"FillCircle({cx},{cy},{radius})");
        public void FillEllipse(float cx, float cy, float rx, float ry, Color color) =>
            Operations.Add($"FillEllipse({cx},{cy},{rx},{ry})");
        public void FillPath(IList<Point> points, Color color) =>
            Operations.Add($"FillPath({points.Count} points)");
        public void FillRoundedRect(float x, float y, float width, float height, float radius, Color color) =>
            Operations.Add($"FillRoundedRect({x},{y},{width},{height},{radius})");
        public void StrokeRoundedRect(float x, float y, float width, float height, float radius, Color color, float strokeWidth) =>
            Operations.Add($"StrokeRoundedRect({x},{y},{width},{height},{radius},{strokeWidth})");
        public void DrawText(float x, float y, string text, Color color, float fontSize, string fontFamily, TextAlignment align) =>
            Operations.Add($"DrawText({x},{y},'{text}',{fontSize},{fontFamily},{align})");
        public void DrawTextRotated(float x, float y, string text, Color color, float fontSize, string fontFamily, float angleDegrees) =>
            Operations.Add($"DrawTextRotated({x},{y},'{text}',{fontSize},{fontFamily},{angleDegrees})");
        public void DrawTextJustified(float x, float y, string text, Color color, float fontSize, string fontFamily, float availableWidth) =>
            Operations.Add($"DrawTextJustified({x},{y},'{text}',{fontSize},{fontFamily},{availableWidth})");
        public Size MeasureText(string text, float fontSize, string fontFamily) => new(text.Length * fontSize * 0.6f, fontSize);
        public void SetTextSpacing(float letterSpacing, float wordSpacing) { }
        public void DrawImage(ImageInfo image, float x, float y, float width, float height) =>
            Operations.Add($"DrawImage({x},{y},{width},{height})");
        public void DrawFormXObject(string resourceName, float x, float y) =>
            Operations.Add($"DrawFormXObject({resourceName},{x},{y})");
        public void BeginMarkedContent(string role) => Operations.Add($"BeginMarkedContent({role})");
        public void BeginContainerContent(string role) => Operations.Add($"BeginContainerContent({role})");
        public void EndMarkedContent() => Operations.Add("EndMarkedContent()");
        public void BeginArtifact() => Operations.Add("BeginArtifact()");
        public void EndArtifact() => Operations.Add("EndArtifact()");
        public void BeginPath() => Operations.Add("BeginPath()");
        public void MoveTo(float x, float y) => Operations.Add($"MoveTo({x},{y})");
        public void LineTo(float x, float y) => Operations.Add($"LineTo({x},{y})");
        public void CubicBezierTo(float cp1x, float cp1y, float cp2x, float cp2y, float x, float y) =>
            Operations.Add($"CubicBezierTo({cp1x},{cp1y},{cp2x},{cp2y},{x},{y})");
        public void QuadraticBezierTo(float cpx, float cpy, float x, float y) =>
            Operations.Add($"QuadraticBezierTo({cpx},{cpy},{x},{y})");
        public void AddRect(float x, float y, float width, float height) =>
            Operations.Add($"AddRect({x},{y},{width},{height})");
        public void AddEllipse(float cx, float cy, float rx, float ry) =>
            Operations.Add($"AddEllipse({cx},{cy},{rx},{ry})");
        public void AddCircle(float cx, float cy, float radius) =>
            Operations.Add($"AddCircle({cx},{cy},{radius})");
        public void ClosePath() => Operations.Add("ClosePath()");
        public void FillCurrentPath(Paint paint, FillRule fillRule) =>
            Operations.Add($"FillCurrentPath({fillRule})");
        public void StrokeCurrentPath(StrokeStyle style) =>
            Operations.Add("StrokeCurrentPath()");
        public void Translate(float dx, float dy) => Operations.Add($"Translate({dx},{dy})");
        public void Scale(float sx, float sy) => Operations.Add($"Scale({sx},{sy})");
        public void Rotate(float angleDegrees) => Operations.Add($"Rotate({angleDegrees})");
        public void Transform(float a, float b, float c, float d, float tx, float ty) =>
            Operations.Add($"Transform({a},{b},{c},{d},{tx},{ty})");
        public void ClipToCurrentPath(FillRule fillRule) =>
            Operations.Add($"ClipToCurrentPath({fillRule})");
        public void SetGlobalAlpha(float alpha) => Operations.Add($"SetGlobalAlpha({alpha})");
    }
}
