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

    /// <summary>
    /// Fills through a canvas that falls back, in one of the two ways that happens.
    /// </summary>
    /// <remarks>
    /// <paramref name="declines"/> models PDF/A-1: a resolver exists and refuses
    /// this gradient because it is translucent, which is also the case where the
    /// alpha ExtGState is forced to 1. With no resolver at all the alpha is
    /// honoured. The two differ in whether the bands are corrected for the
    /// backdrop beneath them.
    /// </remarks>
    private static string FillWith(Paint paint, float width = 100f, float height = 50f, bool declines = false)
    {
        var stream = new StringBuilder();
        var canvas = new PdfCanvas(
            stream,
            pageHeight: 1000,
            alphaGraphicsStateResolver: _ => "/GS0",
            shadingResolver: declines ? _ => null : null);

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

    /// <summary>Any RGB fill colour.</summary>
    [GeneratedRegex(@"^(?<r>[\d.]+) (?<g>[\d.]+) (?<b>[\d.]+) rg$", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
    private static partial Regex AnyFill();

    /// <summary>The x operand of a moveTo.</summary>
    [GeneratedRegex(@"^(?<x>-?[\d.]+) -?[\d.]+ m$", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
    private static partial Regex SegmentStartX();

    /// <summary>The x operand of a moveTo or lineTo.</summary>
    [GeneratedRegex(@"^(?<x>-?[\d.]+) -?[\d.]+ [lm]$", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
    private static partial Regex SegmentX();

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
        // 100 discs plus the /Extend flood behind them, which is a quad, not a disc.
        Assert.Equal(101, Count(content, "f\n"));
    }

    [Fact]
    public void FloodsPastTheRimAsTheShadingItStandsInForDoes()
    {
        // A native radial shading carries /Extend [true true] and fills the whole
        // clipped region; the stack of discs stops at the rim. Without the flood the
        // fallback drew a bare circle where the shading fills the path.
        string content = FillWith(new RadialGradientPaint(new Point(50, 25), 20f, Stops));
        // The flood is painted first, in the last stop's colour, and is the only
        // quad in an otherwise all-Bezier fill.
        string firstFill = content[..content.IndexOf("f\n", StringComparison.Ordinal)];
        Assert.Contains("1 1 1 rg", firstFill, StringComparison.Ordinal);
        Assert.DoesNotContain(" c\n", firstFill, StringComparison.Ordinal);
    }

    [Fact]
    public void DoesNotFloodWhenTheRampFadesToNothing()
    {
        GradientStop[] fade =
        [
            new(0f, new Color(255, 0, 0, 255)),
            new(1f, new Color(255, 0, 0, 0))
        ];
        string content = FillWith(new RadialGradientPaint(new Point(50, 25), 20f, fade));
        // Discs only: a transparent flood would paint nothing, so it is not emitted.
        Assert.Equal(Count(content, "f\n"), Count(content, " c\n") / 4);
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
        // Painted from the far end back, so the first fill is the last band.
        Assert.Equal(0.995, greys[0], 4);
        Assert.Equal(0.005, greys[99], 4);
    }

    [Fact]
    public void StacksTheBandsSoEachIsFullyCoveredByASinglePath()
    {
        // A hairline overlap is not enough, and measuring is what showed it: a
        // renderer antialiases each quad's edges and composites, so two paths each
        // covering a boundary pixel ~60% leave it ~16% uncovered. Coverage
        // multiplies, it does not add, and the gaps read as a moire lattice on any
        // ramp that is not axis-aligned. Every band therefore reaches the near end.
        string content = FillWith(new LinearGradientPaint(new Point(0, 0), new Point(100, 0), Stops));
        // One moveTo per band, all at the same near end. (The clip path contributes
        // none here: this implementation spells an axis-aligned rectangle `re`, per
        // the note in ApproximatesALinearGradientWith100AbuttingQuads.)
        List<double> starts = Values(SegmentStartX(), content, "x");
        Assert.Equal(100, starts.Count);
        Assert.Single(starts.Distinct());
    }

    [Fact]
    public void SkipsTheBackdropCorrectionWhenTheAlphaWillNotSurvive()
    {
        // The correction solves for a (colour, alpha) pair, so it is right only where
        // the alpha is honoured. A resolver that declined did so because this is
        // PDF/A-1, where the alpha ExtGState is forced to 1 - and a corrected colour
        // with its alpha discarded is simply the wrong colour, which shows up as a
        // visibly distorted ramp. There the bands carry the sample itself.
        GradientStop[] translucent =
        [
            new(0f, new Color(255, 0, 0, 255)),
            new(1f, new Color(0, 0, 255, 60))
        ];
        var paint = new LinearGradientPaint(new Point(0, 0), new Point(100, 0), translucent);

        static (double R, double B) FirstOf(string content)
        {
            Match m = AnyFill().Match(content);
            return (double.Parse(m.Groups["r"].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups["b"].Value, CultureInfo.InvariantCulture));
        }

        // Band 99 is painted first and sampled at t=0.995: essentially the last stop.
        (double R, double B) declined = FirstOf(FillWith(paint, declines: true));
        Assert.Equal(0.005, declined.R, 2);
        Assert.Equal(0.995, declined.B, 2);
        // With the alpha honoured the same band is corrected for what lies beneath.
        Assert.NotEqual(declined, FirstOf(FillWith(paint)));
    }

    [Fact]
    public void CoversThePathWhereItRunsPastTheEndOfTheAxis()
    {
        // /Extend [true true] on the shading floods beyond both ends. Without it the
        // bands cover only t=0..1, and everything past the axis is left unpainted -
        // which is what turned a rotated swatch into an octagon.
        string content = FillWith(
            new LinearGradientPaint(new Point(0, 0), new Point(10, 0), Stops), width: 100f, height: 50f);
        List<double> xs = Values(SegmentX(), content, "x");
        Assert.True(xs.Max() >= 100);
        // The flood is painted first and carries the last stop's colour.
        Assert.Contains("1 1 1 rg", content[..content.IndexOf("f\n", StringComparison.Ordinal)],
            StringComparison.Ordinal);
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
        Assert.Equal(1d, greys[0]);   // flat white above 0.75 (far band, painted first)
        Assert.Equal(0d, greys[99]);  // flat black below 0.25
        Assert.Equal(0.49, greys[50], 2);
    }
}
