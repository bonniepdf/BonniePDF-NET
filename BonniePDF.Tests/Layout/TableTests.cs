using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Elements;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Images;
using Xunit;

namespace BonniePDF.Tests.Layout;

// Table layout: column widths are proportional to weights; row height = tallest cell
// (content + vertical padding), floored by row.MinHeight; cells share a row's Y; cell
// content is inset by padding; styling resolves cell > row > table. Default cell padding 5.
public class TableTests
{
    private const float Pad = 5;

    private static (RenderContext Ctx, RecordingCanvas Canvas) MakeContext(float pageHeight = 5000f)
    {
        RenderContext ctx = new(new PdfSize(5000f, pageHeight), 0f);
        RecordingCanvas canvas = new();
        ctx.SetCanvas(canvas);
        return (ctx, canvas);
    }

    // --- Column widths ---

    [Fact]
    public void ColumnWidths_SplitEvenlyForEqualWeights()
    {
        (RenderContext ctx, _) = MakeContext();
        Probe a = new(), b = new();
        Table t = new(1, 1);
        t.AddRow(a, b);
        t.Render(ctx, 500);

        Assert.Equal(Pad, a.RenderedX, 3);
        Assert.Equal(250 - 2 * Pad, a.RenderedWidth, 3);
        Assert.Equal(250 + Pad, b.RenderedX, 3);
        Assert.Equal(250 - 2 * Pad, b.RenderedWidth, 3);
    }

    [Fact]
    public void ColumnWidths_SplitProportionallyForUnequalWeights()
    {
        (RenderContext ctx, _) = MakeContext();
        Probe a = new(), b = new();
        Table t = new(1, 3); // 400 => 100 / 300
        t.AddRow(a, b);
        t.Render(ctx, 400);

        Assert.Equal(100 - 2 * Pad, a.RenderedWidth, 3);
        Assert.Equal(100 + Pad, b.RenderedX, 3);
        Assert.Equal(300 - 2 * Pad, b.RenderedWidth, 3);
    }

    [Fact]
    public void ColumnWidths_AreInferredEvenlyWhenNoWeightsAreDeclared()
    {
        (RenderContext ctx, _) = MakeContext();
        Probe a = new(), b = new();
        Table t = new(); // no weights: two cells => two even columns
        t.AddRow(a, b);
        t.Render(ctx, 500);

        Assert.Equal(Pad, a.RenderedX, 3);
        Assert.Equal(250 - 2 * Pad, a.RenderedWidth, 3);
        Assert.Equal(250 + Pad, b.RenderedX, 3);
        Assert.Equal(250 - 2 * Pad, b.RenderedWidth, 3);
    }

    [Fact]
    public void ColumnWidths_ExtendDeclaredWeightsWithEvenColumnsForExtraCells()
    {
        (RenderContext ctx, _) = MakeContext();
        Probe a = new(), b = new();
        Table t = new(3); // one weight, two cells => weights 3 and 1 over 400
        t.AddRow(a, b);
        t.Render(ctx, 400);

        Assert.Equal(300 - 2 * Pad, a.RenderedWidth, 3);
        Assert.Equal(300 + Pad, b.RenderedX, 3);
        Assert.Equal(100 - 2 * Pad, b.RenderedWidth, 3);
    }

    [Fact]
    public void ColumnWidths_CountSpansWhenInferringColumns()
    {
        (RenderContext ctx, _) = MakeContext();
        Probe spanned = new();
        Table t = new(); // a span-2 cell plus one more => three even columns of 100
        t.AddRow(TableCell.From(spanned).WithColumnSpan(2), TableCell.From(new Probe()));
        t.Render(ctx, 300);

        Assert.Equal(200 - 2 * Pad, spanned.RenderedWidth, 3); // columns 0+1
    }

    [Fact]
    public void ColumnWidths_WidenToTheWidestRowAcrossHeadersRowsAndFooters()
    {
        (RenderContext ctx, _) = MakeContext();
        Probe only = new();
        Table t = new();
        t.AddRow(only);                                  // 1 cell
        t.AddFooter(new Probe(), new Probe(), new Probe()); // 3 cells wins
        t.Render(ctx, 300);

        // The single-cell row still occupies just its own first column.
        Assert.Equal(100 - 2 * Pad, only.RenderedWidth, 3);
    }

    // --- Height measurement ---

    [Fact]
    public void RowHeight_IsTallestCellContentPlusVerticalPadding()
    {
        Table t = new(1, 1);
        t.AddRow(new Probe(20), new Probe(40)); // tallest = 40
        Assert.Equal(40 + 2 * Pad, t.MeasureHeight(500), 3);
    }

    [Fact]
    public void TotalHeight_SumsEveryRow()
    {
        Table t = new(1);
        t.AddHeader(new Probe(10)); // 10 + 10
        t.AddRow(new Probe(20));    // 20 + 10
        t.AddRow(new Probe(20));    // 20 + 10
        Assert.Equal(20 + 30 + 30, t.MeasureHeight(500), 3);
    }

    [Fact]
    public void RowMinHeight_IsAFloorNotAnOverride()
    {
        Table t1 = new(1);
        t1.AddRow(TableRow.From(TableCell.From(new Probe(10))).WithMinHeight(100));
        Assert.Equal(100, t1.MeasureHeight(500), 3); // floored up

        Table t2 = new(1);
        t2.AddRow(TableRow.From(TableCell.From(new Probe(200))).WithMinHeight(100));
        Assert.Equal(200 + 2 * Pad, t2.MeasureHeight(500), 3); // content wins
    }

    // --- Column span ---

    [Fact]
    public void ColumnSpan_GivesCellTheSummedWidthOfCoveredColumns()
    {
        (RenderContext ctx, _) = MakeContext();
        Probe spanned = new();
        Table t = new(1, 1, 1); // 300 => 100 / 100 / 100
        t.AddRow(TableCell.From(spanned).WithColumnSpan(2), TableCell.From(new Probe()));
        t.Render(ctx, 300);

        Assert.Equal(200 - 2 * Pad, spanned.RenderedWidth, 3); // columns 0+1
    }

    [Fact]
    public void ColumnSpan_PlacesTheNextCellAfterTheCoveredColumns()
    {
        (RenderContext ctx, _) = MakeContext();
        Probe spanned = new(), next = new();
        Table t = new(1, 1, 1); // 300 => 100 / 100 / 100
        t.AddRow(TableCell.From(spanned).WithColumnSpan(2), TableCell.From(next));
        t.Render(ctx, 300);

        Assert.Equal(Pad, spanned.RenderedX, 3);
        Assert.Equal(200 - 2 * Pad, spanned.RenderedWidth, 3); // columns 0+1
        Assert.Equal(200 + Pad, next.RenderedX, 3);            // starts after column 1
        Assert.Equal(100 - 2 * Pad, next.RenderedWidth, 3);    // column 2
    }

    [Fact]
    public void ColumnSpan_TakesTheNextCellsWidthFromItsOwnColumnNotItsCellIndex()
    {
        (RenderContext ctx, _) = MakeContext();
        Probe spanned = new(), next = new();
        Table t = new(1, 2, 3); // 600 => 100 / 200 / 300
        t.AddRow(TableCell.From(spanned).WithColumnSpan(2), TableCell.From(next));
        t.Render(ctx, 600);

        Assert.Equal(300 - 2 * Pad, spanned.RenderedWidth, 3); // columns 0+1 = 100+200
        Assert.Equal(300 + Pad, next.RenderedX, 3);
        Assert.Equal(300 - 2 * Pad, next.RenderedWidth, 3);    // column 2, not column 1
    }

    // --- Cells share a row ---

    [Fact]
    public void Cells_RenderAtSameYAndTableAdvancesByOneRowHeight()
    {
        (RenderContext ctx, _) = MakeContext();
        Probe a = new(20), b = new(40);
        Table t = new(1, 1);
        t.AddRow(a, b);
        float startY = ctx.CurrentY;
        t.Render(ctx, 500);

        float rowHeight = 40 + 2 * Pad;
        Assert.Equal(startY + Pad, a.RenderedAtY, 3);
        Assert.Equal(startY + Pad, b.RenderedAtY, 3);
        Assert.Equal(rowHeight, ctx.CurrentY - startY, 3);
    }

    // --- Background styling precedence ---

    [Fact]
    public void CellBackground_ResolvesCellThenRowThenTableDefault()
    {
        (RenderContext ctx, RecordingCanvas canvas) = MakeContext();
        Table t = new(1, 1) { RowBackgroundColor = Color.Blue }; // table default
        TableRow row = TableRow.From(
            TableCell.From(new Probe(20)).WithBackground(Color.Red), // cell override
            TableCell.From(new Probe(20))                            // inherits row
        ).WithBackground(ColorPalette.Amber[400]);                            // row beats table default
        t.AddRow(row);
        t.Render(ctx, 500);

        const float rowHeight = 20 + 2 * Pad;
        Assert.Equal(new List<(float, float, float, float, Color)>
        {
            (0, 0, 250, rowHeight, Color.Red),
            (250, 0, 250, rowHeight, ColorPalette.Amber[400]),
        }, canvas.Rects);
    }

    // --- Stateful pagination ---

    [Fact]
    public void Pagination_OverflowsRowsThatDoNotFitAndCompletesNextPage()
    {
        Probe r0 = new(40), r1 = new(40); // row height 50 with padding
        Table t = new(1);
        t.AddRow(r0);
        t.AddRow(r1);

        (RenderContext ctx1, _) = MakeContext(60f); // maxY 60 => one 50pt row fits
        RenderStatus status1 = t.RenderStateful(ctx1, 500);
        Assert.Equal(RenderStatus.Overflow, status1);
        Assert.NotEqual(-1, r0.RenderedAtY); // row 0 drawn
        Assert.Equal(-1, r1.RenderedAtY);    // row 1 deferred

        (RenderContext ctx2, _) = MakeContext(5000f);
        RenderStatus status2 = t.RenderStateful(ctx2, 500);
        Assert.Equal(RenderStatus.Complete, status2);
        Assert.NotEqual(-1, r1.RenderedAtY); // row 1 now drawn
    }

    /// <summary>Leaf cell content: records where it was measured/rendered and advances Y by a fixed height.</summary>
    private sealed class Probe : ElementBase
    {
        private readonly float _contentHeight;
        public float MeasuredWidth { get; private set; } = -1;
        public float RenderedX { get; private set; } = -1;
        public float RenderedWidth { get; private set; } = -1;
        public float RenderedAtY { get; private set; } = -1;

        public Probe(float contentHeight = 20) => _contentHeight = contentHeight;

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
        public List<(float X, float Y, float Width, float Height, Color Color)> Rects { get; } = new();

        public float Width => 5000;
        public float Height => 5000;

        public void FillRect(float x, float y, float width, float height, Color color) =>
            Rects.Add((x, y, width, height, color));

        public void SaveState() { }
        public void RestoreState() { }
        public void StrokeRect(float x, float y, float width, float height, Color color, float strokeWidth) { }
        public void DrawLine(float x1, float y1, float x2, float y2, Color color, float lineWidth) { }
        public void FillCircle(float cx, float cy, float radius, Color color) { }
        public void FillEllipse(float cx, float cy, float rx, float ry, Color color) { }
        public void FillPath(IList<Point> points, Color color) { }
        public void FillRoundedRect(float x, float y, float width, float height, float radius, Color color) { }
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
