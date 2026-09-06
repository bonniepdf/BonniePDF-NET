using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// The two deprecated chart wrappers report preferred width differently, and the
/// difference is load-bearing: auto-sizing containers use it to size their content.
/// Only ChartElement contributes the chart's width; GroupedBarChartElement never has.
/// Mirrors the TypeScript chart-element preferred-width tests.
/// </summary>
#pragma warning disable CS0618 // the wrappers under test are themselves obsolete
public class ChartElementPreferredWidthTests
{
    [Fact]
    public void ChartElement_ReportsChartWidth()
    {
        BarChart chart = new() { Width = 420, Height = 200 };

        Assert.Equal(420f, new ChartElement(chart, new ChartData()).GetPreferredWidth());
    }

    [Fact]
    public void GroupedBarChartElement_ReportsNoPreferredWidth()
    {
        GroupedBarChart chart = new() { Width = 420, Height = 200 };

        Assert.Equal(0f, new GroupedBarChartElement(chart, new ChartData()).GetPreferredWidth());
    }
}
#pragma warning restore CS0618
