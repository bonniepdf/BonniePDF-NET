using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BonniePDF.Core;
using BonniePDF.Drawing;
using Xunit;

namespace BonniePDF.Tests.Drawing;

/// <summary>
/// When a gradient cannot be painted as a native PDF shading, PdfCanvas
/// approximates it: a linear ramp becomes abutting quads perpendicular to its
/// axis, a radial one a stack of overlapping discs.
/// </summary>
/// <remarks>
/// Mirrors core/tests/drawing/gradient-fallback.test.ts. This path is reached in
/// real output - a translucent gradient under PDF/A-1, whose 6.4 forbids the
/// transparency a translucent shading's soft mask needs - so it is normative for
/// bytes like any other emission path. The two implementations had drifted apart
/// on every parameter of it (band count, span, sample point, winding order, and
/// for the radial case the algorithm itself); these tests pin what they now agree
/// on.
/// </remarks>
public partial class GradientFallbackTests
{
    private static readonly GradientStop[] Stops =
    [
        new(0f, Color.Black),
        new(1f, Color.White)
    ];

    /// <summary>Fills through a canvas with no shading resolver, forcing the approximation.</summary>
    private static string FillWith(Paint paint, float width = 100f, float height = 50f)
    {
        var stream = new StringBuilder();
        var canvas = new PdfCanvas(
            stream,
            pageHeight: 1000,
            alphaGraphicsStateResolver: _ => "/GS0");

        canvas.BeginPath();
        canvas.AddRect(0, 0, width, height);
        canvas.FillCurrentPath(paint);
        return stream.ToString();
    }

    private static int Count(string haystack, string needle) =>
        haystack.Split(needle).Length - 1;

    /// <summary>A grey fill (equal channels), which is what a black-to-white ramp emits.</summary>
    [GeneratedRegex(@"^(?<v>[\d.]+) \k<v> \k<v> rg$", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
    private static partial Regex GreyFill();

    /// <summary>A pure-red fill, for the constant-colour fade case.</summary>
    [GeneratedRegex(@"^(?<v>[\d.]+) 0 0 rg$", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
    private static partial Regex RedFill();

    /// <summary>The y operand of a moveTo or lineTo.</summary>
    [GeneratedRegex(@"^-?[\d.]+ (?<y>-?[\d.]+) [lm]$", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
    private static partial Regex SegmentY();

    private static List<double> Values(Regex pattern, string content, string group) =>
        pattern.Matches(content)
            .Select(m => double.Parse(m.Groups[group].Value, CultureInfo.InvariantCulture))
            .ToList();

    private static List<double> GreyLevels(string content) => Values(GreyFill(), content, "v");

    [Fact]
    public void ApproximatesALinearGradientWith100AbuttingQuads()
    {
        // Fixed at 100 rather than derived from the painted area: deriving it would
        // make the bytes depend on layout arithmetic, which this path cannot afford.
        string content = FillWith(new LinearGradientPaint(new Point(0, 0), new Point(100, 0), Stops));
        Assert.Equal(100, Count(content, "f\n"));
        Assert.Equal(100, GreyLevels(content).Count);
        Assert.DoesNotContain("/Sh", content, StringComparison.Ordinal);

        // Note: how the *clip* path spells a rectangle is deliberately not asserted
        // here. The two implementations differ on that (this one emits `re` for an
        // axis-aligned rect, TypeScript always decomposes to m/l/l/l/h), which is a
        // separate parity question with a far wider blast radius than this path.
    }

    [Fact]
    public void ApproximatesARadialGradientWith100OverlappingDiscs()
    {
        string content = FillWith(new RadialGradientPaint(new Point(50, 25), 50f, Stops));
        // Four curveTo per disc, one closed circle each.
        Assert.Equal(400, Count(content, " c\n"));
        Assert.Equal(100, Count(content, "f\n"));
    }

    [Fact]
    public void SamplesEachBandAtItsCentre()
    {
        // Band 0 of a black-to-white ramp is sampled at t=0.005, not at t=0 or
        // t=0.01. Sampling an edge would bias the whole ramp half a band - which is
        // what .NET's rings used to do.
        string content = FillWith(new LinearGradientPaint(new Point(0, 0), new Point(100, 0), Stops));
        List<double> greys = GreyLevels(content);
        Assert.Equal(100, greys.Count);
        Assert.Equal(0.005, greys[0], 4);
        Assert.Equal(0.995, greys[99], 4);
    }

    [Fact]
    public void SpansFarEnoughToCoverAPathThatIsLongAcrossTheAxis()
    {
        // The span is the greatest perpendicular distance to the path's corners. A
        // constant multiple of the axis length is the rule this replaced, and it is
        // too short here: a 10pt axis across a 400pt-tall path would leave most of
        // the path unpainted.
        string content = FillWith(
            new LinearGradientPaint(new Point(0, 0), new Point(10, 0), Stops), width: 10f, height: 400f);
        List<double> ys = Values(SegmentY(), content, "y");
        Assert.True(ys.Max() - ys.Min() >= 400);
    }

    [Fact]
    public void CarriesATranslucentRadialGradientWithoutCompoundingTheOverlap()
    {
        // The discs overlap, so painting each at its raw alpha would composite them
        // source-over and drive the opacity far past the target. Each disc's colour
        // and alpha are pre-corrected for the backdrop beneath it instead - which
        // .NET did not do at all, so a translucent radial gradient came out opaque.
        // A constant-colour fade is the case that shows it: every disc is the same
        // colour, and only the alphas should differ.
        GradientStop[] fade =
        [
            new(0f, new Color(255, 0, 0, 255)),
            new(1f, new Color(255, 0, 0, 0))
        ];
        string content = FillWith(new RadialGradientPaint(new Point(50, 25), 50f, fade));
        Assert.True(Count(content, "/GS0 gs") > 0);

        List<double> reds = Values(RedFill(), content, "v");
        Assert.All(reds, r => Assert.Equal(1d, r));
    }

    [Fact]
    public void PaintsNothingForADegenerateAxis()
    {
        string content = FillWith(new LinearGradientPaint(new Point(10, 10), new Point(10, 10), Stops));
        Assert.Equal(0, Count(content, "f\n"));
    }

    [Fact]
    public void SamplesTheNormalizedStopsSoStopOrderCannotChangeTheResult()
    {
        string ordered = FillWith(new LinearGradientPaint(new Point(0, 0), new Point(100, 0), Stops));
        string reversed = FillWith(
            new LinearGradientPaint(new Point(0, 0), new Point(100, 0), Stops[1], Stops[0]));
        Assert.Equal(ordered, reversed, StringComparer.Ordinal);
    }

    [Fact]
    public void PadsARampThatDoesNotReachTheEndsOfTheDomain()
    {
        // Normalized stops carry a copy of the first and last stop at 0 and 1, so the
        // sampler never has to decide what lies outside the ramp.
        GradientStop[] inset =
        [
            new(0.25f, Color.Black),
            new(0.75f, Color.White)
        ];
        string content = FillWith(new LinearGradientPaint(new Point(0, 0), new Point(100, 0), inset));
        List<double> greys = GreyLevels(content);
        Assert.Equal(0d, greys[0]);   // flat black below 0.25
        Assert.Equal(1d, greys[99]);  // flat white above 0.75
        Assert.Equal(0.49, greys[49], 2);
    }
}
