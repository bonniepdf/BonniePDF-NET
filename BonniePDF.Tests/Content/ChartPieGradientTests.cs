using System;
using Xunit;

namespace BonniePDF.Tests.Content;

/// <summary>
/// Pie slice gradients - shared conformance fixture charts/pie-gradient.
/// Each slice fades from its tint at the inner edge to its own colour at the rim; the tint moves each
/// channel 45% of the way to white in integers, and translucent colours are rejected.
///
/// Mirrors core/tests/content/chart-pie-gradient.test.ts.
/// </summary>
public class ChartPieGradientTests
{
    [Theory]
    [InlineData("#2563eb", "#87a9f4")]
    [InlineData("#0d9488", "#7ac4be")]
    [InlineData("#7c3aed", "#b793f5")]
    [InlineData("#d97706", "#eab476")]
    [InlineData("#4f46e5", "#9e99f1")]
    [InlineData("#000000", "#737373")]
    [InlineData("#808080", "#b9b9b9")]
    [InlineData("#ffffff", "#ffffff")]
    public void TintsEachChannelTowardsWhite(string color, string tint) =>
        Assert.Equal(tint, ChartTint.Tint(Color.FromHex(color)).ToHexString());

    [Theory]
    [InlineData("#2563eb80")]
    [InlineData("#00000000")]
    public void RejectsTranslucentColours(string color) =>
        Assert.Throws<ArgumentException>(() => ChartTint.Tint(Color.FromHex(color)));

    [Fact]
    public void DrawsNativeGradientSlicesOnlyWhenAsked()
    {
        ChartData Data() => new() { Series = [new ChartSeries { DataPoints = [new() { Label = "A", Value = 3 }, new() { Label = "B", Value = 1 }] }] };
        Assert.DoesNotContain("<linearGradient", new PieChart(Data()).ToSvg());
        string svg = new PieChart(Data()) { SliceGradient = true, Colors = ["#2563eb", "#d97706"] }.ToSvg();
        Assert.Contains("#87a9f4", svg);
        Assert.Contains("#eab476", svg);
    }
}
