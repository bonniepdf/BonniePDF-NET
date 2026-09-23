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
/// A gradient stroke has no native PDF form, so PdfCanvas strokes the path one
/// straight segment at a time, each in the colour sampled at its midpoint. These
/// tests pin how the path is cut up and how the colour and alpha are applied.
/// </summary>
/// <remarks>Mirrors core/tests/drawing/gradient-stroke.test.ts.</remarks>
public partial class GradientStrokeTests
{
    private static readonly GradientStop[] Stops =
    [
        new(0f, Color.Black),
        new(1f, Color.White)
    ];

    private static string Stroke(Paint paint, Action<PdfCanvas> draw)
    {
        var stream = new StringBuilder();
        var canvas = new PdfCanvas(stream, pageHeight: 1000, alphaGraphicsStateResolver: _ => "/GS0");
        canvas.BeginPath();
        draw(canvas);
        canvas.StrokeCurrentPath(new StrokeStyle { Paint = paint, Width = 1 });
        return stream.ToString();
    }

    private static int Segments(string content) => content.Split(" l S\n").Length - 1;

    [GeneratedRegex(@"^(?<v>[\d.]+) \k<v> \k<v> RG$", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
    private static partial Regex GreyStroke();

    private static List<double> Greys(string content) =>
        GreyStroke().Matches(content)
            .Select(m => double.Parse(m.Groups["v"].Value, CultureInfo.InvariantCulture))
            .ToList();

    [Fact]
    public void DoesNotJoinSeparateSubpaths()
    {
        string content = Stroke(new LinearGradientPaint(new Point(0, 0), new Point(10, 0), Stops), c =>
        {
            c.MoveTo(0, 0);
            c.LineTo(10, 0);
            c.MoveTo(0, 20);
            c.LineTo(10, 20);
        });
        // Two lines, and no segment from the end of the first to the start of the second.
        Assert.Equal(2, Segments(content));
    }

    [Fact]
    public void StrokesTheClosingSegmentOfAClosedSubpath()
    {
        string content = Stroke(new LinearGradientPaint(new Point(0, 0), new Point(10, 0), Stops),
            c => c.AddRect(0, 0, 10, 10));
        Assert.Equal(4, Segments(content));
    }

    [Fact]
    public void SamplesInTheSpaceTheGradientIsDefinedInNotOnThePage()
    {
        // Under a 2x horizontal scale the segment runs to x=100 on the page, but in
        // the gradient's own space it ends at 50: its midpoint is t=0.25, not 0.5.
        string content = Stroke(new LinearGradientPaint(new Point(0, 0), new Point(100, 0), Stops), c =>
        {
            c.Scale(2, 1);
            c.MoveTo(0, 0);
            c.LineTo(50, 0);
        });
        Assert.Equal([0.25], Greys(content));
    }

    [Fact]
    public void MeasuresARadialGradientByItsOwnRadiusUnderARotation()
    {
        // A 90 degree rotation leaves the radius unchanged; a radius read from the
        // matrix's first component alone would be zero.
        string content = Stroke(new RadialGradientPaint(new Point(0, 0), 100f, Stops), c =>
        {
            c.Rotate(90);
            c.MoveTo(0, 0);
            c.LineTo(100, 0);
        });
        Assert.Equal([0.5], Greys(content));
    }

    [Fact]
    public void KeepsEachTranslucentSegmentInItsOwnGraphicsState()
    {
        // Setting the alpha is a no-op for an opaque colour, so without its own q/Q
        // a translucent segment's alpha would carry on into the opaque ones after it.
        GradientStop[] fade =
        [
            new(0f, Color.FromScRgb(0, 0, 0, 0.5f)),
            new(0.5f, Color.FromScRgb(0, 0, 0, 1)),
            new(1f, Color.FromScRgb(0, 0, 0, 1))
        ];
        string content = Stroke(new LinearGradientPaint(new Point(0, 0), new Point(100, 0), fade), c =>
        {
            c.MoveTo(0, 0);
            for (int x = 10; x <= 100; x += 10) c.LineTo(x, 0);
        });
        string[] lines = content.Split('\n');
        List<int> alphaLines = Enumerable.Range(0, lines.Length)
            .Where(i => lines[i].EndsWith(" gs", StringComparison.Ordinal))
            .ToList();
        Assert.Equal(5, alphaLines.Count);
        foreach (int i in alphaLines)
        {
            Assert.Equal("q", lines[i - 1]);
            Assert.Equal("Q", lines[i + 3]);
        }
        Assert.Equal(10, Segments(content));
    }
}
