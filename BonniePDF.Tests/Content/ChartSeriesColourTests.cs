using Xunit;

namespace BonniePDF.Tests.Content;

[Collection("Serial")]
public sealed class ChartSeriesColourTests
{
    private const string ChartRed = "#dc2626";
    private const string SeriesGreen = "#16a34a";
    private const string DefaultSeries = "#4f46e5";

    /// <summary>A series with two points. Its colour is set only when <paramref name="colour"/> is given.</summary>
    private static ChartSeries Series(string? colour = null)
    {
        ChartSeries series = new()
        {
            Name = "Sales",
            DataPoints =
            [
                new ChartDataPoint { Label = "A", Value = 4000 },
                new ChartDataPoint { Label = "B", Value = 7000 },
            ],
        };
        if (colour is not null)
        {
            series.Color = Color.FromHex(colour);
        }

        return series;
    }

    private static string SvgOf(IElement chart)
    {
        using BonniePDF.Document doc = new();
        doc.Settings.Language = "en-GB";
        doc.Metadata.Title = "Chart colour";
        doc.Add(chart);
        return doc.GenerateSvgPages()[0];
    }

    [Fact]
    public void ASeriesWithNoColourIsDrawnInTheChartColour()
    {
        BarChart chart = new()
        {
            Data = new ChartData { Series = [Series()] },
            BarColor = Color.FromHex(ChartRed),
            AltText = "Bars",
        };

        string svg = SvgOf(chart);

        Assert.Contains(ChartRed, svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(DefaultSeries, svg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ASeriesWithNoColourStillReadsAsTheDefaultSeriesColour() =>
        Assert.Equal(Color.FromHex(DefaultSeries), Series().Color);

    [Fact]
    public void ASeriesWithNoColourFollowsTheChartColourForLineChartsToo()
    {
        LineChart chart = new()
        {
            Data = new ChartData { Series = [Series()] },
            LineColor = Color.FromHex(ChartRed),
            AltText = "Line",
        };

        Assert.Contains(ChartRed, SvgOf(chart), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheOriginalIndigoIsKeptWhenNeitherColourIsSet()
    {
        BarChart chart = new() { Data = new ChartData { Series = [Series()] }, AltText = "Bars" };

        Assert.Contains(DefaultSeries, SvgOf(chart), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AGroupedChartUsesTheDefaultColourBecauseItHasNoneOfItsOwn()
    {
        GroupedBarChart chart = new()
        {
            Data = new ChartData { Series = [Series(), Series()] },
            AltText = "Groups",
        };

        Assert.Contains(DefaultSeries, SvgOf(chart), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ASeriesColourOfItsOwnWinsOverTheChartColour()
    {
        BarChart chart = new()
        {
            Data = new ChartData { Series = [Series(SeriesGreen)] },
            BarColor = Color.FromHex(ChartRed),
            AltText = "Bars",
        };

        string svg = SvgOf(chart);

        Assert.Contains(SeriesGreen, svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ChartRed, svg, StringComparison.OrdinalIgnoreCase);
    }
}
