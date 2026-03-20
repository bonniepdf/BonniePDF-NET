using BonniePDF.Examples.Charting.ChartShowcase.Components;

namespace BonniePDF.Examples.Charting.ChartShowcase;
internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Charting Showcase...");
        using (var doc = ChartShowcaseExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "charting_example.pdf"));
        }
        Console.WriteLine("Generating Area Chart...");
        using (var doc = AreaChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "area_chart_example.pdf"));
        }
        Console.WriteLine("Generating Bar Chart...");
        using (var doc = BarChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "bar_chart_example.pdf"));
        }
        Console.WriteLine("Generating Dot Plot Chart...");
        using (var doc = DotPlotChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "dot_plot_chart_example.pdf"));
        }
        Console.WriteLine("Generating Grouped Bar Chart...");
        using (var doc = GroupedBarChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "grouped_bar_chart_example.pdf"));
        }
        Console.WriteLine("Generating Line Chart...");
        using (var doc = LineChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "line_chart_example.pdf"));
        }
        Console.WriteLine("Generating Lollipop Chart...");
        using (var doc = LollipopChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "lollipop_chart_example.pdf"));
        }
        Console.WriteLine("Generating Pie Chart...");
        using (var doc = PieChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "pie_chart_example.pdf"));
        }
        Console.WriteLine("Generating Horizontal Bar Chart...");
        using (var doc = HorizontalBarChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "horizontal_bar_chart_example.pdf"));
        }
        Console.WriteLine("Generating Horizontal Lollipop Chart...");
        using (var doc = HorizontalLollipopChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "horizontal_lollipop_chart_example.pdf"));
        }
        Console.WriteLine("Generating Horizontal Grouped Bar Chart...");
        using (var doc = HorizontalGroupedBarChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "horizontal_grouped_bar_chart_example.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
