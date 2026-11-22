using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BonniePDF.Rendering;
using Xunit;

namespace BonniePDF.Tests.Content;

public class ChartEnhancementsTests
{
    private static IEnumerable<LayoutNode> Walk(LayoutNode node)
    {
        yield return node;
        if (node.Children is null) yield break;
        foreach (LayoutNode child in node.Children)
            foreach (LayoutNode desc in Walk(child))
                yield return desc;
    }

    private static ChartBase[] BuildPartOneCharts() =>
    [
        new StackedBarChart
        {
            Theme = "modern",
            Title = "Revenue Mix",
            Subtitle = "Quarterly breakdown by segment",
            Caption = "Source: Finance",
            Mode = "absolute",
            ShowStackTotals = true,
            YAxis = new AxisOptions { Prefix = "$", ValueFormat = "K1" },
            Data = new ChartData
            {
                Series =
                [
                    new ChartSeries { Name = "Direct", DataPoints = [new() { Label = "Q1", Value = 42000 }, new() { Label = "Q2", Value = 51000 }] },
                    new ChartSeries { Name = "Partner", DataPoints = [new() { Label = "Q1", Value = 18000 }, new() { Label = "Q2", Value = 24000 }] },
                ],
            },
            AltText = "Stacked bar chart of quarterly revenue mix",
        },
        new StackedAreaChart
        {
            Theme = "modern",
            Title = "Traffic Share",
            Mode = "percent",
            Curve = "smooth",
            LegendPosition = "inline",
            Data = new ChartData
            {
                Series =
                [
                    new ChartSeries { Name = "Web", DataPoints = [new() { Label = "Jan", Value = 60 }, new() { Label = "Feb", Value = 65 }, new() { Label = "Mar", Value = 70 }] },
                    new ChartSeries { Name = "Mobile", DataPoints = [new() { Label = "Jan", Value = 40 }, new() { Label = "Feb", Value = 35 }, new() { Label = "Mar", Value = 30 }] },
                ],
            },
            AltText = "100% stacked area chart of traffic share",
        },
        new ComboChart
        {
            Theme = "modern",
            Title = "Revenue vs Margin",
            YAxis = new AxisOptions { Prefix = "$", ValueFormat = "K0" },
            SecondaryYAxis = new AxisOptions { Suffix = "%", ValueFormat = "F0" },
            Data = new ChartData
            {
                Series =
                [
                    new ChartSeries { Name = "Revenue", SeriesType = "bar", ValueAxis = "primary", DataPoints = [new() { Label = "Q1", Value = 120000 }, new() { Label = "Q2", Value = 145000 }] },
                    new ChartSeries { Name = "Margin", SeriesType = "line", ValueAxis = "secondary", DataPoints = [new() { Label = "Q1", Value = 22 }, new() { Label = "Q2", Value = 27 }] },
                ],
            },
            AltText = "Combo chart comparing revenue bars and margin line",
        },
    ];

    private static ChartBase[] BuildPartTwoCharts() =>
    [
        new WaterfallChart
        {
            Theme = "modern",
            Title = "ARR Bridge",
            YAxis = new AxisOptions { Prefix = "$", ValueFormat = "K0" },
            Data = new ChartData
            {
                Series =
                [
                    new ChartSeries
                    {
                        Name = "ARR",
                        DataPoints =
                        [
                            new() { Label = "Start", Value = 500000, Kind = "total" },
                            new() { Label = "New", Value = 140000, Kind = "delta" },
                            new() { Label = "Churn", Value = -45000, Kind = "delta" },
                            new() { Label = "End", Kind = "total" },
                        ],
                    },
                ],
            },
            AltText = "Waterfall chart showing ARR bridge",
        },
        new ScatterChart
        {
            Theme = "modern",
            Title = "Latency vs Throughput",
            ShowTrendline = "linear",
            XAxis = new AxisOptions { Legend = "Req/s", AutoScale = true },
            YAxis = new AxisOptions { Legend = "ms", AutoScale = true },
            Data = new ChartData
            {
                Series =
                [
                    new ChartSeries
                    {
                        Name = "Cluster A",
                        DataPoints =
                        [
                            new() { Label = "p1", X = 100, Value = 12, Size = 10 },
                            new() { Label = "p2", X = 250, Value = 18, Size = 25 },
                            new() { Label = "p3", X = 400, Value = 29, Size = 40 },
                        ],
                    },
                ],
            },
            AltText = "Scatter plot with bubble sizing and linear trendline",
        },
    ];

    private static ChartBase[] BuildPartThreeCharts() =>
    [
        new BulletChart
        {
            Theme = "modern",
            Label = "NPS",
            Value = 64,
            Target = 70,
            Ranges = [new() { Max = 40 }, new() { Max = 70 }, new() { Max = 100 }],
            ShowScale = true,
            ShowValueLabel = true,
            AltText = "Bullet chart showing NPS against target",
        },
        new HeatmapChart
        {
            Theme = "modern",
            Title = "Cohort Retention",
            XCategories = ["M0", "M1", "M2"],
            YCategories = ["Jan", "Feb"],
            Values =
            [
                [100m, 82m, 71m],
                [100m, 79m, null],
            ],
            ValueSuffix = "%",
            AltText = "Cohort retention heatmap",
        },
    ];

    private static ChartBase[] BuildAllNewCharts() => [.. BuildPartOneCharts(), .. BuildPartTwoCharts(), .. BuildPartThreeCharts()];

    [Fact]
    public void RendersAllSevenNewChartFamiliesToPdfAndSvg()
    {
        using var doc = new BonniePDF.Document();
        foreach (ChartBase chart in BuildAllNewCharts())
            doc.AddPage().Add(chart);

        byte[] pdf = doc.GenerateBytes();
        Assert.True(pdf.Length > 2000);

        var svgs = doc.GenerateSvgPages();
        Assert.True(svgs.Count >= 7);
        foreach (string svg in svgs)
            Assert.Contains("<svg", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void TagsAllSevenNewChartFamiliesAndMarksImageLikeInLayoutSnapshot()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Metadata.Title = "Charts Accessibility Test";

        foreach (ChartBase chart in BuildAllNewCharts())
        {
            chart.Height = 180;
            doc.Pages[0].Add(chart);
        }

        byte[] pdf = doc.GenerateBytes();
        string text = Encoding.Latin1.GetString(pdf);
        Assert.Contains("/Figure", text, StringComparison.Ordinal);

        LayoutSnapshot? snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot;
        Assert.NotNull(snapshot);

        string[] expectedTypes =
        [
            "StackedBarChart", "StackedAreaChart", "ComboChart",
            "WaterfallChart", "ScatterChart", "BulletChart", "HeatmapChart",
        ];
        var chartNodes = Walk(snapshot!.Root)
            .Where(e => expectedTypes.Contains(e.Type, StringComparer.Ordinal))
            .ToList();

        Assert.Equal(7, chartNodes.Count);
        Assert.All(chartNodes, n =>
        {
            Assert.NotNull(n.A11y);
            Assert.True(n.A11y!.ImageLike);
            Assert.Equal("Figure", n.A11y.Role);
        });
    }

    [Fact]
    public void CascadesDocumentDefaultFontWhenInheritDocumentFontEnabled()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.DefaultTextStyle = new TextStyle { FontFamily = "Courier" };
        doc.Pages[0].Add(new BarChart
        {
            Theme = "modern",
            InheritDocumentFont = true,
            Title = "Inherited Font",
            Data = new ChartData
            {
                Series = [new ChartSeries { Name = "A", DataPoints = [new() { Label = "X", Value = 10 }] }],
            },
        });

        var svgs = doc.GenerateSvgPages();
        Assert.Contains("Courier", svgs[0], StringComparison.Ordinal);
    }

    [Fact]
    public void RendersAreaGradientUnderPdfA1bAndDarkPrintThemes()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.Conformance = PdfAConformance.PdfA1b;
        doc.Metadata.Title = "PDF/A-1b Charts";

        doc.Pages[0].Add(new AreaChart
        {
            Theme = "print",
            Title = "Print Theme Area",
            Curve = "smooth",
            AreaGradient = true,
            Data = new ChartData
            {
                Series = [new ChartSeries { Name = "Load", DataPoints = [new() { Label = "A", Value = 10 }, new() { Label = "B", Value = 25 }, new() { Label = "C", Value = 18 }] }],
            },
        });
        doc.Pages[0].Add(new LineChart
        {
            Theme = "dark",
            Title = "Dark Theme Line",
            Curve = "smooth",
            YAxis = new AxisOptions { AutoScale = true, ValueFormat = "K1" },
            Data = new ChartData
            {
                Series = [new ChartSeries { Name = "Metric", DataPoints = [new() { Label = "A", Value = 12500 }, new() { Label = "B", Value = 28400 }] }],
            },
        });
        doc.Pages[0].Add(new PieChart
        {
            Theme = "modern",
            Title = "Donut Callouts",
            IsDonut = true,
            DonutHoleRatio = 0.55f,
            SliceBorderWidth = 1.5f,
            SliceLabelMode = "callout",
            LegendFormat = "labelPercent",
            Data = new ChartData
            {
                Series = [new ChartSeries { Name = "Share", DataPoints = [new() { Label = "Alpha", Value = 60 }, new() { Label = "Beta", Value = 40 }] }],
            },
        });

        byte[] bytes = doc.GenerateBytes();
        Assert.True(bytes.Length > 1000);
    }

    [Fact]
    public async Task RoundTripsAllSevenNewChartsViaDocumentDefinition()
    {
        using var doc = new BonniePDF.Document();
        foreach (ChartBase chart in BuildAllNewCharts())
            doc.Pages[0].Add(chart);

        DefinitionWriteResult written = doc.ToDefinition();
        string json = written.ToJson();
        DefinitionValidationResult validation = DocumentDefinition.Validate(json);
        Assert.True(validation.IsValid);

        using BonniePDF.Document reloaded = await BonniePDF.Document.FromDefinitionAsync(json);
        byte[] pdf = reloaded.GenerateBytes();
        Assert.True(pdf.Length > 2000);
    }
}
