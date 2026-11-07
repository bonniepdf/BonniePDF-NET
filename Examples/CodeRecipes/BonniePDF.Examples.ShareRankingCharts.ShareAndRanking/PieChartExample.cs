using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.ShareRankingCharts.ShareAndRanking;

/// <summary>
/// A donut chart of revenue share by channel - slice colours, legend and a formatted total -
/// with a summary table alongside. A pie suits a handful of parts of one whole; for values
/// over time, use a bar or line chart. A pie has no axes, so <see cref="ChartData"/> carries
/// a single series.
/// </summary>
[System.ComponentModel.DisplayName("Pie Chart")]
[System.ComponentModel.Category("Share & Ranking Charts")]
[System.ComponentModel.Description("A donut chart of revenue by sales channel, with each channel’s share in the legend, the total in the centre and a summary table alongside.")]
public static class PieChartExample
{
    /// <summary>Channel, revenue in pounds, and change on last year in percent - five shares, not twelve months.</summary>
    private static readonly (string Channel, decimal Revenue, int Change)[] Channels =
    [
        ("Online shop", 412_600m, 18),
        ("Retail stores", 298_400m, -4),
        ("Wholesale", 176_900m, 6),
        ("Marketplaces", 94_300m, 31),
        ("Events", 38_700m, -12),
    ];

    /// <summary>One colour per channel, in slice order.</summary>
    private static readonly string[] Colors = ["#4f46e5", "#0d9488", "#f59e0b", "#e11d48", "#94a3b8"];

    private static readonly CultureInfo EnGb = CultureInfo.GetCultureInfo("en-GB");

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        decimal total = Channels.Sum(c => c.Revenue);
        string Share(decimal revenue) => $"{Math.Round(revenue / total * 100, MidpointRounding.AwayFromZero)}%";
        string Money(decimal value) => "£" + value.ToString("N0", EnGb);

        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Orientation = PageOrientation.Landscape,
            Margin = 44,
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[600]),
            FooterTemplate = PageTemplate.FromElements(@default: new TextBlock("Northwind Homewares  ·  Revenue review, year to March 2026")
            {
                FontSize = 9,
                Color = ColorPalette.Slate[500]
            })
        })
        {
            Metadata = new DocumentMetadata
            {
                Title = "Revenue by Channel",
                Author = "BonniePDF",
                Subject = "Pie / donut chart example - revenue share by sales channel with legend and total"
            }
        };

        doc.Add(new Heading("Revenue by channel")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = ColorPalette.Slate[900],
            Margin = new Thickness(0, 0, 0, 4)
        });
        doc.Add(new TextBlock("Year to March 2026. The online shop now brings in over two fifths of revenue.")
        {
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 14)
        });

        ChartData data = new()
        {
            Series =
            [
                new ChartSeries
                {
                    Name = "Revenue",
                    // The share goes in the label, so the legend reads "Online shop (40%) - 412,600".
                    DataPoints = Channels
                        .Select(c => new ChartDataPoint { Label = $"{c.Channel} ({Share(c.Revenue)})", Value = c.Revenue, LabelFormat = "N0" })
                        .ToList()
                }
            ]
        };

        PieChart chart = new(data)
        {
            AltText = "Donut chart of revenue by channel, year to March 2026: "
                + string.Join(", ", Channels.Select(c => $"{c.Channel} {Share(c.Revenue)}"))
                + $". Total {Money(total)}.",
            Height = 360,
            StretchToFit = true,
            IsDonut = true,
            TotalLabel = "Total revenue (£)",
            // The total goes in the donut hole, which widens to fit it, instead of under the legend.
            TotalPosition = "center",
            TotalValueFormat = "N0",
            ShowLegend = true,
            Colors = Colors,
            BackgroundColor = Color.White
        };

        // The same figures as a table, with the year-on-year change the chart cannot show.
        Table table = new(3f, 2f, 1.4f, 1.4f)
        {
            HeaderBackgroundColor = ColorPalette.Slate[800],
            HeaderTextColor = Color.White,
            CellPadding = 6
        };
        table.AddHeader(Cell("Channel", bold: true), Cell("Revenue", bold: true, right: true), Cell("Share", bold: true, right: true), Cell("vs LY", bold: true, right: true));
        foreach (var (channel, revenue, change) in Channels)
        {
            table.AddRow(
                Cell(channel),
                Cell(Money(revenue), right: true),
                Cell(Share(revenue), right: true),
                Cell($"{(change > 0 ? "+" : "")}{change}%", right: true, color: change < 0 ? ColorPalette.Red[700] : ColorPalette.Emerald[700]));
        }
        table.AddRow(Cell("Total", bold: true), Cell(Money(total), bold: true, right: true), Cell("100%", bold: true, right: true), Cell(""));

        VerticalStack summary = new() { Spacing = 10 };
        summary.Add(new Heading("Summary") { Level = 2, FontName = StandardFonts.HelveticaBold, FontSize = 14, Color = ColorPalette.Slate[900] });
        summary.Add(table);
        summary.Add(new TextBlock("Marketplaces grew fastest from a small base. Events fell as two trade fairs moved to the following year.") { FontSize = 10 });

        HorizontalStack row = new() { Spacing = 28, WidthDistribution = WidthDistribution.Weighted };
        row.Add(chart, 3);
        row.Add(summary, 2);
        doc.Add(row);

        return doc;
    }

    /// <summary>A 10pt table cell, optionally bold, right-aligned or coloured.</summary>
    private static TextBlock Cell(string text, bool bold = false, bool right = false, Color? color = null) => new(text)
    {
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = 10,
        Alignment = right ? TextAlignment.Right : TextAlignment.Left,
        Color = color ?? ColorPalette.Slate[700]
    };
}
