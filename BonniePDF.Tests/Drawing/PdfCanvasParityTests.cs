using System;
using System.Text;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Rendering;
using Xunit;

namespace BonniePDF.Tests.Drawing;

/// <summary>
/// The bytes a fill or stroke writes, pinned identically in both implementations'
/// suites. They had drifted apart - solid fills without their own q/Q, a second q
/// around gradient shadings, translucency set inside a stroke's q, output for empty
/// paths - and nothing noticed, because nothing pinned them. Mirrors
/// core/tests/drawing/pdf-canvas-parity.test.ts, expected strings included.
/// </summary>
/// <remarks>
/// Shapes are built from segments, not <c>AddRect</c>: how each implementation spells
/// a rectangle (<c>re</c>, or four segments) is a separate open question.
/// </remarks>
public class PdfCanvasParityTests
{
    private const string Rect = "10 980 m\n40 980 l\n40 940 l\n10 940 l\nh\n";

    private static readonly GradientStop[] Stops = [new(0f, Color.Black), new(1f, Color.White)];

    private static string Draw(Action<PdfCanvas> script)
    {
        var stream = new StringBuilder();
        var canvas = new PdfCanvas(stream, pageHeight: 1000, imageResolver: _ => "/Im0", alphaGraphicsStateResolver: _ => "/GS0",
            shadingResolver: _ => new ShadingResources("/Sh0", null));
        script(canvas);
        return stream.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static void RectPath(PdfCanvas canvas)
    {
        canvas.BeginPath();
        canvas.MoveTo(10, 20);
        canvas.LineTo(40, 20);
        canvas.LineTo(40, 60);
        canvas.LineTo(10, 60);
        canvas.ClosePath();
    }

    private static void LinePath(PdfCanvas canvas)
    {
        canvas.BeginPath();
        canvas.MoveTo(0, 0);
        canvas.LineTo(10, 10);
    }

    [Fact]
    public void WrapsASolidFillInItsOwnQ() =>
        Assert.Equal($"q\n1 0 0 rg\n{Rect}f\nQ\n", Draw(c => { RectPath(c); c.FillCurrentPath(new SolidPaint(new Color(255, 0, 0))); }));

    [Fact]
    public void SetsTranslucencyOutsideTheFillQ() =>
        Assert.Equal($"q\n/GS0 gs\nq\n1 0 0 rg\n{Rect}f\nQ\nQ\n", Draw(c => { RectPath(c); c.FillCurrentPath(new Color(255, 0, 0, 128)); }));

    [Fact]
    public void FillsEvenOdd() =>
        Assert.Equal($"q\n0 0 1 rg\n{Rect}f*\nQ\n", Draw(c => { RectPath(c); c.FillCurrentPath(new Color(0, 0, 255), FillRule.EvenOdd); }));

    [Fact]
    public void ClipsAndPaintsAGradientShadingInsideOneQWithNoNegativeZero()
    {
        var paint = new LinearGradientPaint(new Point(10, 0), new Point(40, 0), Stops);
        Assert.Equal($"q\n{Rect}W n\n1 0 0 -1 0 1000 cm\n/Sh0 sh\nQ\n", Draw(c => { RectPath(c); c.FillCurrentPath(paint); }));
    }

    [Fact]
    public void SetsTranslucencyOutsideTheStrokeQ() =>
        Assert.Equal("q\n/GS0 gs\nq\n2 w\n0 J\n0 j\n0 0 1 RG\n0 1000 m\n10 990 l\nS\nQ\nQ\n",
            Draw(c => { LinePath(c); c.StrokeCurrentPath(new StrokeStyle { Paint = new Color(0, 0, 255, 128), Width = 2 }); }));

    [Fact]
    public void WritesAMiterLimitOnlyWhenItIsNotPdfsDefaultOf10()
    {
        string Stroke(float miterLimit) =>
            Draw(c => { LinePath(c); c.StrokeCurrentPath(new StrokeStyle { Paint = Color.Black, Width = 1, MiterLimit = miterLimit }); });
        Assert.Equal("q\n1 w\n0 J\n0 j\n0 0 0 RG\n0 1000 m\n10 990 l\nS\nQ\n", Stroke(10));
        Assert.Equal("q\n1 w\n0 J\n0 j\n4 M\n0 0 0 RG\n0 1000 m\n10 990 l\nS\nQ\n", Stroke(4));
    }

    [Fact]
    public void WritesNothingForAnEmptyPath() =>
        Assert.Equal(string.Empty, Draw(c =>
        {
            c.BeginPath();
            c.FillCurrentPath(new Color(255, 0, 0));
            c.StrokeCurrentPath(new StrokeStyle { Paint = Color.Black, Width = 1 });
        }));

    [Fact]
    public void WritesNothingForAGradientStrokeWhoseTransformCannotBeInverted()
    {
        var paint = new LinearGradientPaint(new Point(0, 0), new Point(10, 0), Stops) { Transform = new PaintTransform(1, 0, 0, 0, 0, 0) };
        Assert.Equal(string.Empty, Draw(c => { LinePath(c); c.StrokeCurrentPath(new StrokeStyle { Paint = paint, Width = 1 }); }));
    }
}
