using BonniePDF;

namespace BonniePDF.Examples.TrendCharts.LineAndArea;

/// <summary>
/// Standalone multi-series line chart comparing year-over-year revenue.
/// Two line series on a shared axis with data-point markers and value labels.
/// </summary>
[System.ComponentModel.DisplayName("Multi-Series Line Chart")]
[System.ComponentModel.Category("Trend Charts")]
[System.ComponentModel.Description(
    "Two-series line chart comparing previous and current year revenue on a shared axis with markers and value labels.")]
public static class MultiSeriesLineChartExample
{
    private static readonly string[] MonthLabels =
        ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate() => Generate(now: null);

    public static Document Generate(DateTimeOffset? now, TimeZoneInfo? timeZone = null)
    {
        DateTime day = TimeZoneInfo.ConvertTime(now ?? DateTimeOffset.UtcNow, timeZone ?? TimeZoneInfo.Local).Date;
        int year = day.Year - 1;

        Document document = new(new DocumentSettings
        {
            Orientation = PageOrientation.Landscape,
            FooterTemplate = pageNumber =>
            {
                HorizontalStack stack = new()
                {
                    WidthDistribution = WidthDistribution.Weighted
                };
                stack.Add(new TextBlock
                {
                    Text = "BonniePDF - Multi-Series Line Chart",
                    FontName = StandardFonts.Helvetica,
                    FontSize = 10,
                    Color = ColorPalette.Slate[500]
                }, null);
                stack.Add(PageNumber.Text().Font(StandardFonts.Helvetica, 10).Color(ColorPalette.Slate[500]).AlignRight(), 1f);
                return stack;
            }
        });

        document.Settings.Conformance = PdfAConformance.PdfA2a;
        document.Settings.Accessibility = AccessibilityMode.PdfUa1;
        document.Settings.Language = "en-GB";
        document.Settings.DefaultTextStyle = new TextStyle()
            .WithFamily("Helvetica")
            .WithSize(12)
            .WithColor(ColorPalette.Slate[600]);

        document.Metadata.Title = "Multi-Series Line Chart";
        document.Metadata.Author = "BonniePDF";
        document.Metadata.Subject = $"Year-over-year revenue comparison - {year - 1} vs {year} on a shared axis";

        document.Add(new TextBlock
        {
            Text = "Multi-Series Line Chart: Year-over-Year Comparison",
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        });

        document.Add(new TextBlock
        {
            Text = $"Comparing {year - 1} and {year} revenue with a shared axis",
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        });

        decimal[] previousYearValues =
            [65000, 69000, 80000, 72000, 85000, 88000, 72000, 68000, 75000, 81000, 92000, 105000];
        decimal[] currentYearValues =
            [75000, 78000, 85000, 78000, 92000, 95000, 87000, 78000, 82000, 91000, 97000, 118000];

        ChartSeries previousYear = new()
        {
            Name = $"{year - 1}",
            Color = Color.FromHex("#94a3b8"),
            Style = new ChartSeriesStyle
            {
                DashPattern = [6f, 3f],
                MarkerShape = "diamond",
                MarkerFill = "hollow",
                MarkerRadius = 4f,
            },
        };
        ChartSeries currentYear = new()
        {
            Name = $"{year}",
            Color = Color.FromHex("#4f46e5"),
            Style = new ChartSeriesStyle
            {
                LineWidth = 2.5f,
                MarkerShape = "circle",
                MarkerHaloWidth = 1.5f,
                MarkerRadius = 4.5f,
            },
        };

        for (int i = 0; i < MonthLabels.Length; i++)
        {
            previousYear.DataPoints.Add(new ChartDataPoint { Label = MonthLabels[i], Value = previousYearValues[i], LabelFormat = "N0" });
            currentYear.DataPoints.Add(new ChartDataPoint { Label = MonthLabels[i], Value = currentYearValues[i], LabelFormat = "N0" });
        }

        ChartData chartData = new()
        {
            Title = "Year-over-Year Revenue Comparison",
            Series = [previousYear, currentYear],
            YAxis = new AxisOptions
            {
                Prefix = "£",
                ValueFormat = "K0",
                MaxValue = 150000,
                StepCount = 5,
                Legend = "Revenue"
            },
            XAxis = new AxisOptions
            {
                Legend = "Month"
            }
        };

        LineChart lineChart = new(chartData)
        {
            AltText = $"Multi-series line chart comparing monthly revenue for {year - 1} and {year}.",
            Height = 380,
            StretchToFit = true,
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            // Twelve points per line: value labels would collide, so the axis carries the scale.
            ShowValueLabels = false,
            Theme = "modern",
            AutoLayout = true,
            Curve = "smooth",
            LegendPosition = "top",
            BorderRadius = 8,
            BorderColor = Color.FromHex("#e2e8f0"),
            BorderWidth = 1,
            References =
            [
                new ChartReference
                {
                    Value = 100000,
                    Label = "£100K milestone",
                    LabelPosition = "start",
                    LabelBackgroundColor = Color.FromHex("#eef2ff"),
                    Color = Color.FromHex("#4f46e5"),
                    Thickness = 1.25f,
                    DashPattern = [4f, 3f],
                },
            ],
            BackgroundColor = Color.White
        };

        document.Add(lineChart);

        return document;
    }
}
