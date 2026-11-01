using System;
using Xunit;

namespace BonniePDF.Tests.Content;

/// <summary>
/// Cartesian value scale - shared conformance fixture charts/value-scale.
/// Values sit linearly between MinValue and MaxValue; bars, areas and stems start at zero limited to
/// the range; a value below that origin is labelled below (or left of) its point; a range that cannot
/// be drawn is rejected. The fixture's non-finite rows have no decimal
/// equivalent and are asserted by the TypeScript suite only.
///
/// Mirrors core/tests/content/chart-value-scale.test.ts.
/// </summary>
public class ChartValueScaleTests
{
    [Theory]
    [InlineData("0", "50", "12.5", "0.25")]
    [InlineData("0", "50", "0", "0")]
    [InlineData("0", "50", "75", "1.5")]
    [InlineData("0", "50", "-25", "-0.5")]
    [InlineData("20", "50", "35", "0.5")]
    [InlineData("20", "50", "20", "0")]
    [InlineData("20", "50", "50", "1")]
    [InlineData("20", "50", "65", "1.5")]
    [InlineData("20", "50", "5", "-0.5")]
    [InlineData("-20", "60", "0", "0.25")]
    [InlineData("-20", "60", "-20", "0")]
    [InlineData("-20", "60", "40", "0.75")]
    [InlineData("-20", "60", "-40", "-0.25")]
    [InlineData("-50", "-10", "-30", "0.5")]
    [InlineData("-50", "-10", "-10", "1")]
    [InlineData("0.5", "1.5", "0.75", "0.25")]
    public void PlacesValuesLinearlyBetweenTheBounds(string min, string max, string value, string fraction) =>
        Assert.Equal(D(fraction), ChartValueScale.Fraction(D(value), D(min), D(max)));

    [Theory]
    [InlineData("0", "50", "0")]
    [InlineData("20", "50", "0")]
    [InlineData("-20", "60", "0.25")]
    [InlineData("-10", "30", "0.25")]
    [InlineData("-50", "-10", "1")]
    [InlineData("-10", "0", "1")]
    [InlineData("0", "10", "0")]
    public void StartsMarksAtZeroLimitedToTheRange(string min, string max, string fraction) =>
        Assert.Equal(D(fraction), ChartValueScale.OriginFraction(D(min), D(max)));

    [Theory]
    [InlineData("0", "50", "12.5", false)]
    [InlineData("0", "50", "0", false)]
    [InlineData("0", "50", "-5", true)]
    [InlineData("-20", "60", "0", false)]
    [InlineData("-20", "60", "-0.5", true)]
    [InlineData("-50", "-10", "-10", false)]
    [InlineData("-50", "-10", "-30", true)]
    [InlineData("-50", "-10", "-5", false)]
    [InlineData("20", "50", "20", false)]
    [InlineData("20", "50", "10", true)]
    public void LabelsAValueBelowTheOriginBelowItsPoint(string min, string max, string value, bool belowOrigin) =>
        Assert.Equal(belowOrigin, ChartValueScale.IsBelowOrigin(D(value), D(min), D(max)));

    [Theory]
    [InlineData("10", "10")]
    [InlineData("50", "10")]
    [InlineData("0", "0")]
    [InlineData("-5", "-10")]
    public void RejectsARangeThatCannotBeDrawn(string min, string max) =>
        Assert.Throws<InvalidOperationException>(() => ChartValueScale.Check(D(min), D(max)));

    [Fact]
    public void RejectsTheRangeWhenAChartWithSeriesIsDrawnAndNotBefore()
    {
        BarChart chart = new(new ChartData { YAxis = new AxisOptions { MinValue = 10, MaxValue = 10 } });
        chart.ToSvg();
        chart.Data.Series.Add(new ChartSeries { DataPoints = [new() { Label = "A", Value = 5 }] });
        Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
    }

    private static decimal D(string text) => decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
}
