using System.Text;
using BonniePDF.Core;
using BonniePDF.Rendering.Pdf;
using Xunit;

namespace BonniePDF.Tests.Drawing;

// PdfDrawing emits raw PDF content-stream operators. These tests pin the operator
// output: fills are wrapped in q…Q, strokes are inset by half the line width so the
// outer edge aligns with the requested bounds, and the no-op guards short-circuit.
// (.NET formats numbers with "0.###"; the TS port uses 3 decimals — same geometry.)
public class PdfDrawingTests
{
    private static string Emit(System.Action<StringBuilder> fn)
    {
        StringBuilder sb = new();
        fn(sb);
        return sb.ToString();
    }

    [Fact]
    public void FillRect_WrapsFilledRectangleInSaveRestoreWithFillColour()
    {
        string outp = Emit(s => PdfDrawing.FillRect(s, 10, 20, 100, 50, Color.Black));
        Assert.Contains("q", outp);
        Assert.Contains("0 0 0 rg", outp);
        Assert.Contains("10 20 100 50 re f", outp);
        Assert.Contains("Q", outp);
    }

    [Fact]
    public void StrokeRect_InsetsPathByHalfStrokeSoOuterEdgeAlignsWithBounds()
    {
        string outp = Emit(s => PdfDrawing.StrokeRect(s, 10, 10, 100, 50, Color.Black, 2));
        // half-stroke = 1 => path inset to (11, 11, 98, 48); the centred 2pt stroke's
        // outer edge then lands exactly on the 10..110 / 10..60 bounds.
        Assert.Contains("11 11 98 48 re S", outp);
        Assert.Contains("2 w", outp); // line width
    }

    [Fact]
    public void StrokeRect_EmitsNothingForNonPositiveStrokeWidth()
    {
        Assert.Equal("", Emit(s => PdfDrawing.StrokeRect(s, 0, 0, 10, 10, Color.Black, 0)));
        Assert.Equal("", Emit(s => PdfDrawing.StrokeRect(s, 0, 0, 10, 10, Color.Black, -3)));
    }

    [Fact]
    public void DrawBox_EmitsNothingWithoutBackgroundOrVisibleBorder()
    {
        Assert.Equal("", Emit(s => PdfDrawing.DrawBox(s, 0, 0, 10, 10)));
        Assert.Equal("", Emit(s => PdfDrawing.DrawBox(s, 0, 0, 10, 10, null, Color.Black, 0)));
    }

    [Fact]
    public void DrawBox_FillsOnlyWhenGivenJustABackground()
    {
        string outp = Emit(s => PdfDrawing.DrawBox(s, 0, 0, 10, 10, Color.Red));
        Assert.Contains("re f", outp);
        Assert.DoesNotContain("re S", outp);
    }

    [Fact]
    public void DrawBox_FillsAndStrokesWhenGivenBoth()
    {
        string outp = Emit(s => PdfDrawing.DrawBox(s, 0, 0, 10, 10, Color.Red, Color.Black, 2));
        Assert.Contains("re f", outp);
        Assert.Contains("re S", outp);
    }

    [Fact]
    public void FillRoundedRect_WithNonPositiveRadiusIsIdenticalToFillRect()
    {
        string rounded = Emit(s => PdfDrawing.FillRoundedRect(s, 5, 5, 40, 30, 0, Color.Red));
        string plain = Emit(s => PdfDrawing.FillRect(s, 5, 5, 40, 30, Color.Red));
        Assert.Equal(plain, rounded);
    }

    [Fact]
    public void StrokeRoundedRect_EmitsNothingWhenStrokeWidthIsZero()
    {
        Assert.Equal("", Emit(s => PdfDrawing.StrokeRoundedRect(s, 0, 0, 40, 30, 8, Color.Black, 0)));
    }

    [Fact]
    public void FillRoundedRect_WithPositiveRadiusEmitsCurvesNotAPlainRect()
    {
        string outp = Emit(s => PdfDrawing.FillRoundedRect(s, 0, 0, 40, 30, 8, Color.Red));
        Assert.Contains(" c", outp);          // Bézier corner curves
        Assert.DoesNotContain("re", outp);    // not a plain rectangle path
    }

    [Fact]
    public void ClipRoundedRect_WithPositiveRadiusEmitsCurvesThenClipNoPaint()
    {
        string outp = Emit(s => PdfDrawing.ClipRoundedRect(s, 0, 0, 40, 30, 8));
        Assert.Contains(" c", outp);       // Bézier corner curves
        Assert.Contains("W n", outp);      // intersect clip, end path without painting
        Assert.DoesNotContain(" f", outp); // a clip path never fills
        Assert.DoesNotContain(" S", outp); // …or strokes
    }

    [Fact]
    public void ClipRoundedRect_WithNonPositiveRadiusClipsAPlainRectangle()
    {
        string outp = Emit(s => PdfDrawing.ClipRoundedRect(s, 5, 5, 40, 30, 0));
        Assert.Contains("5 5 40 30 re", outp); // plain rectangle path
        Assert.Contains("W n", outp);
        Assert.DoesNotContain(" c", outp);     // no corner curves
    }

    [Fact]
    public void FillPath_EmitsNothingForFewerThanThreePoints()
    {
        Assert.Equal("", Emit(s => PdfDrawing.FillPath(s, new List<Point>(), Color.Red)));
        Assert.Equal("", Emit(s => PdfDrawing.FillPath(s, new List<Point> { new(0, 0), new(10, 0) }, Color.Red)));
    }

    [Fact]
    public void FillPath_MovesToFirstPointLinesToRestThenClosesAndFills()
    {
        string outp = Emit(s => PdfDrawing.FillPath(
            s, new List<Point> { new(0, 0), new(10, 0), new(5, 8) }, Color.Red));
        Assert.Contains("0 0 m", outp);   // move to first
        Assert.Contains("10 0 l", outp);  // line to second
        Assert.Contains("5 8 l", outp);   // line to third
        Assert.Contains("h", outp);       // close path
        Assert.Contains("f", outp);       // fill
    }
}
