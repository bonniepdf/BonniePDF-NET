using System.Globalization;
using BonniePDF.Charts.Models;

namespace BonniePDF.Examples.Charting.ChartShowcase.Data;

public static class ExampleChartData
{
    public static IList<string> MonthLabels => Enumerable.Range(1, 12)
        .Select(m => DateTimeFormatInfo.CurrentInfo.GetAbbreviatedMonthName(m))
        .ToList();

    public static IList<ChartDataPoint> MonthlySales => new List<ChartDataPoint>
    {
        new ChartDataPoint { Label = MonthLabels[0], Value = 4500, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[1], Value = 6200, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[2], Value = 5800, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[3], Value = 7200, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[4], Value = 8500, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[5], Value = 9100, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[6], Value = 8200, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[7], Value = 7800, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[8], Value = 9500, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[9], Value = 10200, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[10], Value = 11500, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[11], Value = 12800, LabelFormat = "N0" }
    };


    public static IList<ChartDataPoint> QuarterlyRevenue => new List<ChartDataPoint>()
    {
        new ChartDataPoint { Label = "Q1", Value = 4500, LabelFormat = "N0" },
        new ChartDataPoint { Label = "Q2", Value = 6200, LabelFormat = "N0" },
        new ChartDataPoint { Label = "Q3", Value = 8500, LabelFormat = "N0" },
        new ChartDataPoint { Label = "Q4", Value = 11500, LabelFormat = "N0" }
    };

    public static IList<ChartDataPoint> ProductSales => new List<ChartDataPoint>()
    {
        new ChartDataPoint { Label = "Prod A", Value = 4500, LabelFormat = "N0" },
        new ChartDataPoint { Label = "Prod B", Value = 6200, LabelFormat = "N0" },
        new ChartDataPoint { Label = "Prod C", Value = 5800, LabelFormat = "N0" },
        new ChartDataPoint { Label = "Prod D", Value = 7200, LabelFormat = "N0" },
        new ChartDataPoint { Label = "Prod E", Value = 8500, LabelFormat = "N0" },
        new ChartDataPoint { Label = "Prod F", Value = 9100, LabelFormat = "N0" },
        new ChartDataPoint { Label = "Prod G", Value = 8200, LabelFormat = "N0" },
        new ChartDataPoint { Label = "Prod H", Value = 9500, LabelFormat = "N0" }
    };

    public static IList<ChartDataPoint> PieChartSales => new List<ChartDataPoint>()
    {
        new ChartDataPoint { Label = MonthLabels[0], Value = 7234, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[1], Value = 4891, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[2], Value = 8456, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[3], Value = 3127, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[4], Value = 9542, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[5], Value = 6789, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[6], Value = 5234, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[7], Value = 8901, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[8], Value = 2456, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[9], Value = 7890, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[10], Value = 6123, LabelFormat = "N0" },
        new ChartDataPoint { Label = MonthLabels[11], Value = 9876, LabelFormat = "N0" }
    };
}

