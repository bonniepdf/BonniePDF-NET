using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;

namespace BonniePDF.Tests.Content;

public class ChartExtractionTests
{
    [Fact]
    public void ExtractedChartsPreserveSvgOutput()
    {
        string fixturePath = Path.Combine(AppContext.BaseDirectory, "Content", "Fixtures", "chart-extraction.json");
        JsonArray cases = JsonNode.Parse(File.ReadAllText(fixturePath))!.AsArray();
        foreach (JsonNode? item in cases)
        {
            JsonObject c = item!.AsObject();
            string type = c["type"]!.GetValue<string>();
            string variant = c["variant"]!.GetValue<string>();
            ChartBase chart = type switch
            {
                "BarChart" => new BarChart(), "GroupedBarChart" => new GroupedBarChart(),
                "LineChart" => new LineChart(), "AreaChart" => new AreaChart(),
                "DotPlotChart" => new DotPlotChart(), "LollipopChart" => new LollipopChart(),
                "PieChart" => new PieChart(), _ => throw new InvalidOperationException(type),
            };
            chart.Width = 640;
            chart.Height = 360;
            chart.BackgroundColor = Color.FromHex("#ffffff80");
            chart.Title = string.Equals(variant, "minimal", StringComparison.Ordinal) ? "" : "Sales & <margin>";
            if (!string.Equals(variant, "empty", StringComparison.Ordinal))
            {
                for (int i = 0; i < 2; i++)
                {
                    ChartSeries series = new() { Name = $"Series {i + 1}" };
                    if (i == 1) series.Color = Color.FromHex("#0e749080");
                    string[] labels = ["A & B", "<C>", "D"];
                    decimal[] values = [12.5m, 32.25m, 21.75m];
                    for (int n = 0; n < values.Length; n++)
                        series.DataPoints.Add(new ChartDataPoint { Label = labels[n], Value = values[n] + i * 4, LabelFormat = "F2" });
                    chart.Data.Series.Add(series);
                }
            }
            chart.Data.YAxis = new AxisOptions { MaxValue = 50, StepCount = 5, Legend = "Revenue", VerticalLegend = true };
            chart.Data.XAxis.Legend = "Quarter";
            if (chart is CartesianChartBase cartesian)
            {
                cartesian.Orientation = string.Equals(variant, "horizontal", StringComparison.Ordinal) ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
                cartesian.ShowLegend = !string.Equals(variant, "minimal", StringComparison.Ordinal);
            }
            if (chart is PieChart pie)
            {
                pie.IsDonut = !string.Equals(variant, "horizontal", StringComparison.Ordinal);
                pie.ShowLegend = !string.Equals(variant, "minimal", StringComparison.Ordinal);
            }
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chart.ToSvg()))).ToLowerInvariant();
            Assert.Equal(c["netSvg"]!.GetValue<string>(), hash);
        }
    }
}
