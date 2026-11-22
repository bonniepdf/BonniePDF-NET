namespace BonniePDF.Examples.TrendCharts.LineAndArea;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Area Chart...");
        using (var doc = AreaChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "area_chart_example.pdf"));
        }

        Console.WriteLine("Generating Line Chart...");
        using (var doc = LineChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "line_chart_example.pdf"));
        }

        Console.WriteLine("Generating Stacked Area Chart...");
        using (var doc = StackedAreaChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "stacked_area_chart_example.pdf"));
        }

        Console.WriteLine("Generating Scatter Chart...");
        using (var doc = ScatterChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "scatter_chart_example.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}