using BonniePDF.Examples.Charting.ChartShowcase.Components;

namespace BonniePDF.Examples.Charting.ChartShowcase;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Charting Showcase...");
        ChartingExample.Generate(Path.Combine(outputDir, "charting_example.pdf"));
        Console.WriteLine("Generating Area Chart...");
        AreaChartExample.Generate(Path.Combine(outputDir, "area_chart_example.pdf"));
        Console.WriteLine("Generating Bar Chart...");
        BarChartExample.Generate(Path.Combine(outputDir, "bar_chart_example.pdf"));
        Console.WriteLine("Generating Dot Plot Chart...");
        DotPlotChartExample.Generate(Path.Combine(outputDir, "dot_plot_chart_example.pdf"));
        Console.WriteLine("Generating Grouped Bar Chart...");
        GroupedBarChartExample.Generate(Path.Combine(outputDir, "grouped_bar_chart_example.pdf"));
        Console.WriteLine("Generating Line Chart...");
        LineChartExample.Generate(Path.Combine(outputDir, "line_chart_example.pdf"));
        Console.WriteLine("Generating Lollipop Chart...");
        LollipopChartExample.Generate(Path.Combine(outputDir, "lollipop_chart_example.pdf"));
        Console.WriteLine("Generating Pie Chart...");
        PieChartExample.Generate(Path.Combine(outputDir, "pie_chart_example.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}