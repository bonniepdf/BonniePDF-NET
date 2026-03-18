using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Charting Showcase...");
                    BonniePDF.Examples.Examples._05_Charting.ChartingExample.Generate(Path.Combine(outputDir, "charting_example.pdf"));
        Console.WriteLine("Generating Area Chart...");
                    BonniePDF.Examples.Examples._05_Charting.Components.AreaChartExample.Generate(Path.Combine(outputDir, "area_chart_example.pdf"));
        Console.WriteLine("Generating Bar Chart...");
                    BonniePDF.Examples.Examples._05_Charting.Components.BarChartExample.Generate(Path.Combine(outputDir, "bar_chart_example.pdf"));
        Console.WriteLine("Generating Dot Plot Chart...");
                    BonniePDF.Examples.Examples._05_Charting.Components.DotPlotChartExample.Generate(Path.Combine(outputDir, "dot_plot_chart_example.pdf"));
        Console.WriteLine("Generating Grouped Bar Chart...");
                    BonniePDF.Examples.Examples._05_Charting.Components.GroupedBarChartExample.Generate(Path.Combine(outputDir, "grouped_bar_chart_example.pdf"));
        Console.WriteLine("Generating Line Chart...");
                    BonniePDF.Examples.Examples._05_Charting.Components.LineChartExample.Generate(Path.Combine(outputDir, "line_chart_example.pdf"));
        Console.WriteLine("Generating Lollipop Chart...");
                    BonniePDF.Examples.Examples._05_Charting.Components.LollipopChartExample.Generate(Path.Combine(outputDir, "lollipop_chart_example.pdf"));
        Console.WriteLine("Generating Pie Chart...");
                    BonniePDF.Examples.Examples._05_Charting.Components.PieChartExample.Generate(Path.Combine(outputDir, "pie_chart_example.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
