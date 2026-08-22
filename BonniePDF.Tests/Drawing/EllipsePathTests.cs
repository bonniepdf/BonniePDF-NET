using System.Linq;
using BonniePDF.Drawing;
using Xunit;

namespace BonniePDF.Tests.Drawing;

/// <summary>
/// AddCircle/AddEllipse are path primitives, not immediate-mode fills: they exist
/// so a circle can be stroked with a full StrokeStyle, filled with a gradient, or
/// pushed as a clip - none of which FillCircle can do. Both canvases build them
/// from EllipseGeometry, so pinning that pins both.
///
/// Mirrors core/tests/drawing/ellipse-path.test.ts.
/// </summary>
public class EllipsePathTests
{
    [Fact]
    public void ToBeziers_StartsAtRightmostPointAndClosesBackOntoIt()
    {
        (float startX, float startY, var segments) = EllipseGeometry.ToBeziers(100f, 50f, 20f, 20f);

        Assert.Equal(120f, startX);
        Assert.Equal(50f, startY);
        Assert.Equal(4, segments.Length);
        Assert.Equal(startX, segments[^1].X);
        Assert.Equal(startY, segments[^1].Y);
    }

    [Fact]
    public void ToBeziers_WalksQuadrantEndPointsInIncreasingYOrder()
    {
        (_, _, var segments) = EllipseGeometry.ToBeziers(0f, 0f, 10f, 10f);

        Assert.Equal(
            new[] { (0f, 10f), (-10f, 0f), (0f, -10f), (10f, 0f) },
            segments.Select(s => (s.X, s.Y)).ToArray());
    }

    [Fact]
    public void ToBeziers_OffsetsControlPointsByKappaTimesRadiusPerAxis()
    {
        (_, _, var segments) = EllipseGeometry.ToBeziers(0f, 0f, 30f, 12f);

        Assert.Equal(30f, segments[0].C1X, 4);
        Assert.Equal(12f * EllipseGeometry.Kappa, segments[0].C1Y, 4);
        Assert.Equal(30f * EllipseGeometry.Kappa, segments[0].C2X, 4);
        Assert.Equal(12f, segments[0].C2Y, 4);
    }

    [Fact]
    public void ToBeziers_ReproducesTheHandRolledCircleTheExamplesUsedToCarry()
    {
        // The six-line helper that was copied into four TypeScript and two C# files.
        const float cx = 297.6f;
        const float cy = 303.1f;
        const float r = 96f;
        float k = r * 0.5522847498f;

        float[][] expected =
        [
            [cx + r, cy + k, cx + k, cy + r, cx, cy + r],
            [cx - k, cy + r, cx - r, cy + k, cx - r, cy],
            [cx - r, cy - k, cx - k, cy - r, cx, cy - r],
            [cx + k, cy - r, cx + r, cy - k, cx + r, cy],
        ];

        (float startX, float startY, var segments) = EllipseGeometry.ToBeziers(cx, cy, r, r);

        Assert.Equal(cx + r, startX);
        Assert.Equal(cy, startY);
        for (int i = 0; i < expected.Length; i++)
        {
            var s = segments[i];
            Assert.Equal(expected[i], new[] { s.C1X, s.C1Y, s.C2X, s.C2Y, s.X, s.Y });
        }
    }

    [Fact]
    public void ToBeziers_ApproximatesTheTrueEllipseToWellUnderAThousandthOfTheRadius()
    {
        // Worst radial error of the cubic approximation sits at the segment midpoint.
        const float r = 100f;
        (_, _, var segments) = EllipseGeometry.ToBeziers(0f, 0f, r, r);
        var s = segments[0];

        const float t = 0.5f;
        const float mt = 1f - t;
        float x = (mt * mt * mt * r) + (3 * mt * mt * t * s.C1X) + (3 * mt * t * t * s.C2X) + (t * t * t * s.X);
        float y = (3 * mt * mt * t * s.C1Y) + (3 * mt * t * t * s.C2Y) + (t * t * t * s.Y);

        Assert.True(Math.Abs(Math.Sqrt((x * x) + (y * y)) - r) < r * 0.0003);
    }
}
