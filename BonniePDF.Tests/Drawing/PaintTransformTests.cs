using System.Collections.Generic;
using System.Text;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Pdf;
using Xunit;

namespace BonniePDF.Tests.Drawing;

/// <summary>
/// A gradient paint may carry an affine transform describing its own coordinate
/// space. It exists because a paint holding only endpoints and a single radius
/// cannot express a sheared linear ramp or an elliptical radial gradient, and
/// real SVG uses both constantly through <c>gradientTransform</c>.
/// </summary>
/// <remarks>
/// Mirrors core/tests/drawing/paint-transform.test.ts. The emission mechanism was
/// already there: BonniePDF paints gradients with the <c>sh</c> operator, which
/// takes its coordinate space from the CTM, so the paint transform is simply a
/// second <c>cm</c>.
/// </remarks>
public class PaintTransformTests
{
    private static readonly GradientStop[] Stops =
    [
        new(0f, Color.Black),
        new(1f, Color.White)
    ];

    private static readonly GradientStop[] TranslucentStops =
    [
        new(0f, new Color(0, 0, 0, 128)),
        new(1f, Color.White)
    ];

    private static (string Content, List<ShadingRequest> Requests) FillWith(
        Paint paint, string? softMaskGs = null)
    {
        var requests = new List<ShadingRequest>();
        var stream = new StringBuilder();
        var canvas = new PdfCanvas(
            stream,
            pageHeight: 1000,
            alphaGraphicsStateResolver: _ => "/GS0",
            shadingResolver: request =>
            {
                requests.Add(request);
                return new ShadingResources("/Sh0", softMaskGs);
            });

        canvas.BeginPath();
        canvas.AddRect(0, 0, 100, 50);
        canvas.FillCurrentPath(paint);
        return (stream.ToString(), requests);
    }

    [Fact]
    public void EmitsNoExtraMatrixWhenThereIsNoTransform()
    {
        // Every existing caller, showcase and golden depends on this: the feature
        // is an addition only if an absent transform changes nothing.
        string without = FillWith(new LinearGradientPaint(new Point(0, 0), new Point(100, 0), Stops)).Content;
        string identity = FillWith(new LinearGradientPaint(new Point(0, 0), new Point(100, 0), Stops)
        {
            Transform = PaintTransform.Identity
        }).Content;

        Assert.Equal(without, identity);
    }

    [Fact]
    public void ConcatenatesTheTransformAsASecondCmBeforeSh()
    {
        (string content, _) = FillWith(new LinearGradientPaint(new Point(0, 0), new Point(100, 0), Stops)
        {
            Transform = new PaintTransform(0, 1, -1, 0, 5, 7)
        });

        Assert.Contains("0 1 -1 0 5 7 cm", content, System.StringComparison.Ordinal);
        Assert.True(content.LastIndexOf("cm", System.StringComparison.Ordinal)
            < content.IndexOf("/Sh0", System.StringComparison.Ordinal));
    }

    [Fact]
    public void LeavesTheShadingCoordinatesInThePaintsOwnSpace()
    {
        // The transform carries the mapping, so /Coords must not be
        // pre-multiplied - doing both would apply it twice.
        (_, List<ShadingRequest> requests) = FillWith(
            new LinearGradientPaint(new Point(0, 0), new Point(100, 0), Stops)
            {
                Transform = new PaintTransform(2, 0, 0, 2, 10, 10)
            });

        Assert.Equal(new[] { 0f, 0f, 100f, 0f }, requests[0].Coords);
    }

    [Fact]
    public void MapsTheSoftMaskBBoxThroughTheInverseTransform()
    {
        // The mask form's /BBox is in paint space once a transform exists. Leaving
        // it in canvas space clips the mask where the transform moves content
        // outside it, which reads as a gradient fading out at one edge.
        (_, List<ShadingRequest> transformed) = FillWith(
            new LinearGradientPaint(new Point(0, 0), new Point(100, 0), TranslucentStops)
            {
                Transform = new PaintTransform(2, 0, 0, 2, 0, 0)
            },
            "/GSm0");
        (_, List<ShadingRequest> plain) = FillWith(
            new LinearGradientPaint(new Point(0, 0), new Point(100, 0), TranslucentStops), "/GSm0");

        // Halved, because paint space is twice the size of canvas space here.
        Assert.Equal(plain[0].Bbox[2] / 2f, transformed[0].Bbox[2], 3);
        Assert.Equal(plain[0].Bbox[3] / 2f, transformed[0].Bbox[3], 3);
    }

    [Fact]
    public void PaintsNothingWhenTheTransformIsNotInvertible()
    {
        // A zero determinant collapses paint space to a line, so there is no
        // shading space left to interpret coordinates in.
        (string content, List<ShadingRequest> requests) = FillWith(
            new LinearGradientPaint(new Point(0, 0), new Point(100, 0), Stops)
            {
                Transform = new PaintTransform(0, 0, 0, 0, 0, 0)
            });

        Assert.Empty(requests);
        Assert.DoesNotContain("/Sh0", content, System.StringComparison.Ordinal);
    }

    [Fact]
    public void CarriesTheTransformOnRadialGradientsToo()
    {
        (string content, List<ShadingRequest> requests) = FillWith(
            new RadialGradientPaint(new Point(50, 50), 40f, Stops)
            {
                Transform = new PaintTransform(1, 0, 0, 0.5f, 0, 0)
            });

        Assert.Equal(new[] { 50f, 50f, 0f, 50f, 50f, 40f }, requests[0].Coords);
        // The non-uniform scale is what makes the gradient elliptical; the paint
        // itself still carries one radius.
        Assert.Contains("1 0 0 0.5 0 0 cm", content, System.StringComparison.Ordinal);
    }

    [Fact]
    public void TreatsAbsentAndIdentityAlike()
    {
        Assert.True(PaintTransform.Identity.IsIdentity);
        Assert.True(((PaintTransform?)null).IsIdentityOrAbsent());
        Assert.False(new PaintTransform(1, 0, 0, 1, 1, 0).IsIdentity);
    }

    [Fact]
    public void InvertsAndRefusesToInvertACollapsedTransform()
    {
        PaintTransform inverse = new PaintTransform(2, 0, 0, 4, 6, 8).Invert()!.Value;
        Assert.Equal(0.5f, inverse.A, 5);
        Assert.Equal(0.25f, inverse.D, 5);
        Assert.Equal(-3f, inverse.E, 5);
        Assert.Equal(-2f, inverse.F, 5);

        Assert.Null(new PaintTransform(1, 2, 2, 4, 0, 0).Invert());
    }

    [Fact]
    public void TakesTheContainingBoxOfARotatedBox()
    {
        // A rotation turns a rectangle into a parallelogram, and a /BBox has to
        // contain it.
        (float x0, float y0, float x1, float y1) =
            new PaintTransform(0, 1, -1, 0, 0, 0).MapBox(0, 0, 10, 20);

        Assert.Equal(-20f, x0, 5);
        Assert.Equal(0f, y0, 5);
        Assert.Equal(0f, x1, 5);
        Assert.Equal(10f, y1, 5);
    }

    [Fact]
    public void SvgExportWritesGradientTransform()
    {
        var canvas = new SvgCanvas(200, 100);
        canvas.BeginPath();
        canvas.AddRect(0, 0, 100, 50);
        canvas.FillCurrentPath(new LinearGradientPaint(new Point(0, 0), new Point(100, 0), Stops)
        {
            Transform = new PaintTransform(1, 2, 3, 4, 5, 6)
        });

        Assert.Contains("gradientTransform=\"matrix(1 2 3 4 5 6)\"", canvas.GetSvg(), System.StringComparison.Ordinal);
    }

    [Fact]
    public void SvgExportOmitsTheAttributeWhenThereIsNoTransform()
    {
        var canvas = new SvgCanvas(200, 100);
        canvas.BeginPath();
        canvas.AddRect(0, 0, 100, 50);
        canvas.FillCurrentPath(new LinearGradientPaint(new Point(0, 0), new Point(100, 0), Stops));

        Assert.DoesNotContain("gradientTransform", canvas.GetSvg(), System.StringComparison.Ordinal);
    }

    [Fact]
    public void Svg_CarriesTheCanvasOffsetInTheMatrixNotInThePaintGeometry()
    {
        // The canvas offset is a canvas-space translation. Added to the gradient's
        // own coordinates it would pass through gradientTransform and be scaled
        // with them: here x1 would become 2 * (0 + 10) = 20 on the page, not 10.
        var canvas = new SvgCanvas(200, 100, xOffset: 10, yOffset: 20);
        canvas.BeginPath();
        canvas.AddRect(0, 0, 100, 50);
        canvas.FillCurrentPath(new LinearGradientPaint(new Point(0, 0), new Point(100, 0), Stops)
        {
            Transform = new PaintTransform(2, 0, 0, 1, 5, 6)
        });

        string svg = canvas.GetSvg();
        Assert.Contains("x1=\"0\" y1=\"0\" x2=\"100\" y2=\"0\"", svg, System.StringComparison.Ordinal);
        Assert.Contains("gradientTransform=\"matrix(2 0 0 1 15 26)\"", svg, System.StringComparison.Ordinal);
    }

    [Fact]
    public void Svg_StillOffsetsAGradientWithoutATransform()
    {
        var canvas = new SvgCanvas(200, 100, xOffset: 10, yOffset: 20);
        canvas.BeginPath();
        canvas.AddRect(0, 0, 100, 50);
        canvas.FillCurrentPath(new RadialGradientPaint(new Point(50, 25), 25f, Stops));

        Assert.Contains("cx=\"60\" cy=\"45\"", canvas.GetSvg(), System.StringComparison.Ordinal);
    }
}
