using System;
using System.Globalization;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// VerticalAlignment is the counterpart to HorizontalAlignment: it consumes the
/// slack in a fixed-height container so content can be anchored to the bottom
/// without hand-computing a top padding against the font's metrics. Cover type on
/// a full-bleed page is the motivating case.
///
/// Mirrors core/tests/layout/container-vertical-alignment.test.ts.
/// </summary>
public class ContainerVerticalAlignmentTests
{
    /// <summary>
    /// First text baseline in the rendered page. Parsed by hand rather than with a
    /// Regex, which the analyzers would require to be source-generated.
    /// </summary>
    private static float FirstTextBaseline(string svg)
    {
        int text = svg.IndexOf("<text", StringComparison.Ordinal);
        Assert.True(text >= 0, "no text found in rendered page");
        int y = svg.IndexOf(" y=", text, StringComparison.Ordinal) + 4;
        int end = svg.IndexOf('"', y);
        return float.Parse(svg[y..end], CultureInfo.InvariantCulture);
    }

    private static float BaselineOf(VerticalAlignment? align, float height = 400f)
    {
        DocumentSettings settings = new() { Margin = 0 };
        using BonniePDF.Document doc = new(settings);
        doc.Add(new Container
        {
            Height = height,
            VerticalAlignment = align,
            Child = new TextBlock("Anchor") { FontSize = 20 },
        });

        return FirstTextBaseline(doc.GenerateSvgDocument(new BonniePDF.SvgPageExportOptions()).Pages[0]);
    }

    [Fact]
    public void LeavesTheChildAtTheTopByDefault()
    {
        Assert.Equal(BaselineOf(VerticalAlignment.Start), BaselineOf(null), 3);
    }

    [Fact]
    public void CentresTheChildInTheContainerHeight()
    {
        float top = BaselineOf(VerticalAlignment.Start);
        float middle = BaselineOf(VerticalAlignment.Center);
        float bottom = BaselineOf(VerticalAlignment.End);

        Assert.Equal((bottom - top) / 2f, middle - top, 2);
    }

    [Fact]
    public void AnchorsTheChildToTheBottomEdge()
    {
        float top = BaselineOf(VerticalAlignment.Start);
        float bottom = BaselineOf(VerticalAlignment.End);

        // Slack is the container height less the single line's height.
        Assert.Equal(400f - (20f * 1.2f), bottom - top, 1);
    }

    [Fact]
    public void IsANoOpWhenTheChildFillsTheContainer()
    {
        Assert.Equal(
            BaselineOf(VerticalAlignment.Start, height: 24f),
            BaselineOf(VerticalAlignment.End, height: 24f),
            3);
    }
}
